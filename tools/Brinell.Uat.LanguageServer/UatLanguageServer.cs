using System.Reflection;
using Brinell.Uat.Preview;
using Brinell.Uat.Workspace;
using StreamJsonRpc;

namespace Brinell.Uat.LanguageServer;

/// <summary>
/// The server's methods (P2): LSP's lifecycle and document sync, and Brinell's own requests. A file
/// belongs to the suites whose project holds or links it (AD-014); the client picks one when there
/// are several. Until a suite is built, its files are parsed only, and the preview says why.
/// </summary>
public sealed class UatLanguageServer : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (int Version, string Text)> _documents = new(StringComparer.OrdinalIgnoreCase);
    private UatWorkspace? _workspace;
    private UatRunResults? _results;
    private FileSystemWatcher? _resultsWatcher;
    private Timer? _resultsChanged;
    private JsonRpc? _rpc;

    /// <summary>The virtual host a webview maps to the results folder, so it can show screenshots.</summary>
    public const string ResultsHost = "brinell-results.local";

    /// <summary>Screenshots as <c>file:</c> URLs, for a page opened on its own (<c>--preview</c>); the IDE maps <see cref="ResultsHost"/>.</summary>
    public bool FileImageUrls { get; init; }

    /// <summary>Raised when the client asks the server to exit.</summary>
    public event EventHandler? ExitRequested;

    /// <summary>Starts serving on a stream pair with LSP's header framing, until the connection closes.</summary>
    /// <param name="sending">Where the server writes: the client reads it.</param>
    /// <param name="receiving">What the server reads: the client writes it.</param>
    public JsonRpc Attach(Stream sending, Stream receiving)
    {
        var formatter = new SystemTextJsonFormatter { JsonSerializerOptions = Json.Options };
        var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(sending, receiving, formatter));
        rpc.AddLocalRpcTarget(this, new JsonRpcTargetOptions { UseSingleObjectParameterDeserialization = true });
        rpc.StartListening();
        _rpc = rpc;
        return rpc;
    }

    /// <summary>LSP <c>initialize</c>: opens the suites under the solution folder.</summary>
    [JsonRpcMethod("initialize")]
    public InitializeResult Initialize(InitializeParams request)
    {
        var root = request.InitializationOptions?.SolutionDir
            ?? (request.RootUri is { } uri ? new Uri(uri).LocalPath : null)
            ?? Environment.CurrentDirectory;
        lock (_gate)
        {
            _workspace?.Dispose();
            _workspace = UatWorkspace.Open(root);
            _results = new UatRunResults(UatRunResults.RootFor(_workspace.Root));
            WatchResults(_results.Root);
            return new InitializeResult(
                new ServerCapabilities(TextDocumentSync: 1),
                new ServerInfo("Brinell.Uat.LanguageServer", typeof(UatLanguageServer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0"),
                _workspace.Suites.Select(Info).ToList());
        }
    }

    /// <summary>LSP <c>initialized</c>: nothing to do.</summary>
    [JsonRpcMethod("initialized")]
    public void Initialized()
    {
    }

    /// <summary>LSP <c>shutdown</c>: releases the suites.</summary>
    [JsonRpcMethod("shutdown")]
    public object? Shutdown()
    {
        Dispose();
        return null;
    }

    /// <summary>LSP <c>exit</c>.</summary>
    [JsonRpcMethod("exit")]
    public void Exit() => ExitRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>LSP <c>textDocument/didOpen</c>.</summary>
    [JsonRpcMethod("textDocument/didOpen")]
    public void DidOpen(DidOpenTextDocumentParams request)
    {
        lock (_gate)
        {
            _documents[Key(request.TextDocument.Uri)] = (request.TextDocument.Version, request.TextDocument.Text);
        }
    }

    /// <summary>LSP <c>textDocument/didChange</c>, full sync.</summary>
    [JsonRpcMethod("textDocument/didChange")]
    public void DidChange(DidChangeTextDocumentParams request)
    {
        if (request.ContentChanges.Count == 0)
        {
            return;
        }

        lock (_gate)
        {
            _documents[Key(request.TextDocument.Uri)] = (request.TextDocument.Version, request.ContentChanges[^1].Text);
        }
    }

    /// <summary>LSP <c>textDocument/didClose</c>.</summary>
    [JsonRpcMethod("textDocument/didClose")]
    public void DidClose(DidCloseTextDocumentParams request)
    {
        lock (_gate)
        {
            _documents.Remove(Key(request.TextDocument.Uri));
        }
    }

    /// <summary><c>brinell/buildDone</c>: the solution was built, so every suite loads its new output on next use.</summary>
    [JsonRpcMethod("brinell/buildDone")]
    public void BuildDone()
    {
        lock (_gate)
        {
            _workspace?.Reload();
        }
    }

    /// <summary><c>brinell/previewHost</c>: the page the client's webview loads once; <c>brinell/preview</c> fills it.</summary>
    [JsonRpcMethod("brinell/previewHost")]
    public PreviewHostResult PreviewHost() => new(UatPreview.HostPage);

    /// <summary><c>brinell/preview</c>: the file rendered, bound in its suite when it is built.</summary>
    [JsonRpcMethod("brinell/preview")]
    public PreviewResult Preview(PreviewParams request)
    {
        var path = Key(request.Uri);
        (int Version, string Text) document;
        UatSuite? suite;
        IReadOnlyList<UatSuite> suites;
        lock (_gate)
        {
            document = _documents.TryGetValue(path, out var open) ? open : (0, File.ReadAllText(path));
            suites = Order(path, _workspace?.SuitesFor(path) ?? []);
            suite = suites.FirstOrDefault(candidate => string.Equals(candidate.Name, request.Suite, StringComparison.OrdinalIgnoreCase))
                ?? suites.FirstOrDefault();
        }

        string? reason = null;
        var catalogs = suite is null
            ? null
            : suite.TryGetCatalogs(out reason);
        reason = catalogs is not null
            ? null
            : suite is null
                ? $"No suite holds or links this file: no project with a {UatFileFormat.UiConfigFileName} or {UatFileFormat.ServiceConfigFileName} under {_workspace?.Root ?? "the solution"} has it."
                : reason;
        var results = _results;
        IReadOnlyDictionary<string, Brinell.Uat.Preview.UatScenarioRun> runs = new Dictionary<string, Brinell.Uat.Preview.UatScenarioRun>();
        if (suite is not null && results is not null && path.EndsWith(UatFileFormat.FeatureExtension, StringComparison.OrdinalIgnoreCase))
        {
            lock (results)
            {
                runs = results.For(suite.Name, path);
            }
        }

        var result = UatPreview.Render(document.Text, path, new UatPreviewOptions
        {
            Catalogs = catalogs,
            ParseOnlyReason = reason,
            Runs = runs,
            ImageUrl = image => ImageUrl(image, results?.Root),
        });
        return new PreviewResult(
            result.Html,
            document.Version,
            suites.Select(candidate => candidate.Name).ToList(),
            suite?.Name,
            catalogs is null ? "parse-only" : "bound",
            reason)
        {
            ResultsRoot = results?.Root,
            ResultsHost = ResultsHost,
        };
    }

    // A screenshot under the results folder, through the virtual host the client maps to it.
    private string? ImageUrl(string path, string? root)
    {
        if (FileImageUrls)
        {
            return new Uri(path).AbsoluteUri;
        }

        if (root is null || !Path.GetFullPath(path).StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relative = Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Select(Uri.EscapeDataString);
        return $"https://{ResultsHost}/{string.Join('/', relative)}";
    }

    // A run writes its manifest after each scenario: the open files' previews are told to refresh,
    // once the writes pause.
    private void WatchResults(string root)
    {
        _resultsWatcher?.Dispose();
        _resultsWatcher = null;
        if (!Directory.Exists(root))
        {
            return;
        }

        _resultsChanged ??= new Timer(_ => NotifyResultsChanged());
        _resultsWatcher = new FileSystemWatcher(root, "manifest.json") { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName };
        _resultsWatcher.Changed += (_, _) => _resultsChanged.Change(TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
        _resultsWatcher.Created += (_, _) => _resultsChanged.Change(TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
        _resultsWatcher.EnableRaisingEvents = true;
    }

    private void NotifyResultsChanged()
    {
        List<string> open;
        lock (_gate)
        {
            open = _documents.Keys.Where(path => path.EndsWith(UatFileFormat.FeatureExtension, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        foreach (var path in open)
        {
            _ = _rpc?.NotifyWithParameterObjectAsync("brinell/resultsChanged", new { uri = new Uri(path).AbsoluteUri });
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            _resultsWatcher?.Dispose();
            _resultsWatcher = null;
            _resultsChanged?.Dispose();
            _resultsChanged = null;
            _workspace?.Dispose();
            _workspace = null;
        }
    }

    // The suite whose own project holds the file first, then the ones that link it.
    private static IReadOnlyList<UatSuite> Order(string path, IReadOnlyList<UatSuite> suites)
        => suites
            .OrderBy(suite => path.StartsWith(suite.ProjectDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(suite => suite.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static SuiteInfo Info(UatSuite suite) => new(suite.Name, suite.ConfigPath, TryHead(suite));

    private static string? TryHead(UatSuite suite)
    {
        try
        {
            return suite.Head;
        }
        catch (UatWorkspaceException)
        {
            return null;
        }
    }

    private static string Key(string uri) => Path.GetFullPath(Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsFile ? parsed.LocalPath : uri);
}
