using Nerdbank.Streams;
using StreamJsonRpc;

namespace Bruinet.Bdd.LanguageServer.Tests;

/// <summary>A server and an editor's client on an in-memory stream, initialized for a solution folder.</summary>
public sealed class ServerHarness : IAsyncDisposable
{
    private readonly BddLanguageServer _server = new();
    private readonly JsonRpc _serverRpc;

    private ServerHarness()
    {
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        _serverRpc = _server.Attach(serverStream, serverStream);
        var formatter = new SystemTextJsonFormatter { JsonSerializerOptions = { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase } };
        Client = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, formatter));
        Client.AddLocalRpcTarget(Published, new JsonRpcTargetOptions { UseSingleObjectParameterDeserialization = true });
        Client.StartListening();
    }

    public JsonRpc Client { get; }

    public PublishedDiagnostics Published { get; } = new();

    public InitializeResult Initialized { get; private set; } = null!;

    /// <summary>A server for <paramref name="solutionDir"/>, initialized by an editor's client.</summary>
    public static async Task<ServerHarness> StartAsync(string solutionDir)
    {
        var harness = new ServerHarness();
        harness.Initialized = await harness.Client.InvokeWithParameterObjectAsync<InitializeResult>("initialize", new
        {
            initializationOptions = new { solutionDir },
            capabilities = new { textDocument = new { publishDiagnostics = new { } } },
        });
        return harness;
    }

    /// <summary>Opens a file in the editor with <paramref name="text"/>, or its text on disk.</summary>
    public async Task<string> OpenAsync(string path, string? text = null)
    {
        var uri = new Uri(path).AbsoluteUri;
        await Client.NotifyWithParameterObjectAsync("textDocument/didOpen", new { textDocument = new { uri, version = 1, text = text ?? File.ReadAllText(path) } });
        return uri;
    }

    /// <summary>A request about a position: 0-based line and character.</summary>
    public Task<T> AtAsync<T>(string method, string uri, int line, int character, object? extra = null)
        => Client.InvokeWithParameterObjectAsync<T>(method, extra is null
            ? new { textDocument = new { uri }, position = new { line, character } }
            : new { textDocument = new { uri }, position = new { line, character }, context = extra });

    /// <summary>A request about a whole file.</summary>
    public Task<T> OfAsync<T>(string method, string uri)
        => Client.InvokeWithParameterObjectAsync<T>(method, new { textDocument = new { uri } });

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        _serverRpc.Dispose();
        _server.Dispose();
        return ValueTask.CompletedTask;
    }
}
