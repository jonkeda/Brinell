using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using StreamJsonRpc;

namespace Brinell.Uat.VisualStudio;

/// <summary>
/// The language server, started as <c>dotnet Server\Brinell.Uat.LanguageServer.dll</c> from the
/// extension's folder, and the messages the extension sends it. The extension holds no engine
/// types: everything here is JSON (P2). A server that exits is started again on the next call.
/// </summary>
public sealed class UatServerClient : IDisposable
{
    private readonly string _serverPath;
    private readonly Action<string> _log;
    private readonly SemaphoreSlim _starting = new(1, 1);
    private Process? _process;
    private JsonRpc? _rpc;
    private string? _solutionDir;

    /// <summary>Creates the client; the server starts on the first call.</summary>
    /// <param name="serverPath">The server's dll.</param>
    /// <param name="log">Where the server's stderr and the client's notes go (the Output pane).</param>
    public UatServerClient(string serverPath, Action<string> log)
    {
        _serverPath = serverPath;
        _log = log;
    }

    /// <summary>The server says a run wrote new results for an open file (its path).</summary>
    public event EventHandler<string>? ResultsChanged;

    /// <summary>The server's path beside the extension's assembly.</summary>
    public static string DefaultServerPath =>
        Path.Combine(Path.GetDirectoryName(typeof(UatServerClient).Assembly.Location)!, "Server", "Brinell.Uat.LanguageServer.dll");

    /// <summary>Starts the server for a solution folder, or restarts it for another one.</summary>
    /// <param name="solutionDir">The folder whose suites the server serves.</param>
    /// <param name="cancellationToken">Cancels the start.</param>
    public async Task InitializeAsync(string solutionDir, CancellationToken cancellationToken)
    {
        if (_rpc is not null && string.Equals(_solutionDir, solutionDir, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _solutionDir = solutionDir;
        Stop();
        await RpcAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>LSP <c>textDocument/didOpen</c>.</summary>
    public async Task DidOpenAsync(string path, int version, string text, CancellationToken cancellationToken)
        => await (await RpcAsync(cancellationToken).ConfigureAwait(false)).NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new { textDocument = new { uri = Uri(path), languageId = "markdown", version, text } }).ConfigureAwait(false);

    /// <summary>LSP <c>textDocument/didChange</c>, the whole text.</summary>
    public async Task DidChangeAsync(string path, int version, string text, CancellationToken cancellationToken)
        => await (await RpcAsync(cancellationToken).ConfigureAwait(false)).NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new { textDocument = new { uri = Uri(path), version }, contentChanges = new[] { new { text } } }).ConfigureAwait(false);

    /// <summary>LSP <c>textDocument/didClose</c>.</summary>
    public async Task DidCloseAsync(string path, CancellationToken cancellationToken)
        => await (await RpcAsync(cancellationToken).ConfigureAwait(false)).NotifyWithParameterObjectAsync(
            "textDocument/didClose",
            new { textDocument = new { uri = Uri(path) } }).ConfigureAwait(false);

    /// <summary><c>brinell/buildDone</c>: the suites load their new build output on next use.</summary>
    public async Task BuildDoneAsync(CancellationToken cancellationToken)
        => await (await RpcAsync(cancellationToken).ConfigureAwait(false)).NotifyWithParameterObjectAsync("brinell/buildDone", new { }).ConfigureAwait(false);

    /// <summary><c>brinell/previewHost</c>: the page the webview loads once.</summary>
    public async Task<string> PreviewHostAsync(CancellationToken cancellationToken)
    {
        var result = await (await RpcAsync(cancellationToken).ConfigureAwait(false))
            .InvokeWithParameterObjectAsync<JObject>("brinell/previewHost", new { }, cancellationToken).ConfigureAwait(false);
        return (string?)result["html"] ?? string.Empty;
    }

    /// <summary><c>brinell/preview</c>: the file rendered, with its suites and bind state, as the page's <c>update</c> message takes it.</summary>
    public async Task<JObject> PreviewAsync(string path, string? suite, CancellationToken cancellationToken)
        => await (await RpcAsync(cancellationToken).ConfigureAwait(false))
            .InvokeWithParameterObjectAsync<JObject>("brinell/preview", new { uri = Uri(path), suite }, cancellationToken).ConfigureAwait(false);

    /// <summary><c>brinell/runTarget</c>: the project and filter for the scenario at a heading's line, or the whole file.</summary>
    public async Task<JObject> RunTargetAsync(string path, int? line, string? suite, CancellationToken cancellationToken)
        => await (await RpcAsync(cancellationToken).ConfigureAwait(false))
            .InvokeWithParameterObjectAsync<JObject>("brinell/runTarget", new { uri = Uri(path), line, suite }, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _starting.Dispose();
    }

    private async Task<JsonRpc> RpcAsync(CancellationToken cancellationToken)
    {
        if (_rpc is { IsDisposed: false } running)
        {
            return running;
        }

        await _starting.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_rpc is { IsDisposed: false } started)
            {
                return started;
            }

            if (!File.Exists(_serverPath))
            {
                throw new FileNotFoundException($"The Brinell language server is not at {_serverPath}. Reinstall the extension.", _serverPath);
            }

            var process = Process.Start(new ProcessStartInfo("dotnet", $"\"{_serverPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _solutionDir ?? Path.GetDirectoryName(_serverPath)!,
            }) ?? throw new InvalidOperationException("dotnet did not start. The Brinell language server needs the .NET 10 runtime.");
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is { } line)
                {
                    _log(line);
                }
            };
            process.BeginErrorReadLine();

            var formatter = new JsonMessageFormatter();
            formatter.JsonSerializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(process.StandardInput.BaseStream, process.StandardOutput.BaseStream, formatter));
            rpc.Disconnected += (_, e) => _log($"The language server stopped: {e.Description}");
            rpc.AddLocalRpcMethod("brinell/resultsChanged", new Action<string>(uri => ResultsChanged?.Invoke(this, new Uri(uri).LocalPath)));
            rpc.StartListening();
            await rpc.InvokeWithParameterObjectAsync<JObject>(
                "initialize",
                new { processId = Process.GetCurrentProcess().Id, initializationOptions = new { solutionDir = _solutionDir } },
                cancellationToken).ConfigureAwait(false);
            await rpc.NotifyWithParameterObjectAsync("initialized", new { }).ConfigureAwait(false);
            _log($"Started the language server for {_solutionDir}.");

            _process = process;
            _rpc = rpc;
            return rpc;
        }
        finally
        {
            _starting.Release();
        }
    }

    private void Stop()
    {
        var rpc = _rpc;
        var process = _process;
        _rpc = null;
        _process = null;
        rpc?.Dispose();
        if (process is { HasExited: false })
        {
            process.Kill();
        }

        process?.Dispose();
    }

    private static string Uri(string path) => new Uri(path).AbsoluteUri;
}
