using Brinell.Core.Composition;
using Brinell.Core.Testing;
using Nerdbank.Streams;
using StreamJsonRpc;
using Xunit;

namespace Brinell.Uat.LanguageServer.Tests;

/// <summary>The server over JSON-RPC with LSP framing, on an in-memory stream, as a client talks to it.</summary>
public sealed class LanguageServerTests : IAsyncLifetime
{
    private readonly string _solution = Path.Combine(Path.GetTempPath(), "Brinell", "LanguageServerTests", Guid.NewGuid().ToString("N"));
    private readonly UatLanguageServer _server = new();
    private JsonRpc _client = null!;
    private JsonRpc _serverRpc = null!;

    private string Shelf => Path.Combine(_solution, "Shelf.Steps");

    private string Linked => Path.Combine(_solution, "Shelf.Web");

    private string Feature => Path.Combine(Shelf, "Features", "shelf.feature.md");

    private string Built => Path.Combine(Shelf, "bin", "Debug", "net10.0", "Shelf.Steps.dll");

    public Task InitializeAsync()
    {
        // Two suites: one holds the feature file, the other links it.
        Directory.CreateDirectory(Path.Combine(Shelf, "Features"));
        Directory.CreateDirectory(Linked);
        File.WriteAllText(Path.Combine(Shelf, "bdd.config.md"), Config("bin/Debug/net10.0/Shelf.Steps.dll"));
        File.WriteAllText(Path.Combine(Linked, "uat.config.md"), Config("bin/Debug/net10.0/Shelf.Web.dll"));
        File.WriteAllText(Path.Combine(Linked, "Shelf.Web.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <None Include="..\Shelf.Steps\Features\**\*.feature.md" Link="Features\%(RecursiveDir)%(Filename)%(Extension)" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Feature, """
            # Feature: Shelf

            ## Scenario: One

            ```gherkin
            When I shelve "Dune"
            ```
            """);

        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        _serverRpc = _server.Attach(serverStream, serverStream);
        var formatter = new SystemTextJsonFormatter { JsonSerializerOptions = { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase } };
        _client = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, formatter));
        _client.StartListening();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _serverRpc.Dispose();
        _server.Dispose();
        try
        {
            Directory.Delete(_solution, recursive: true);
        }
        catch (IOException)
        {
            // A load context not collected yet; the temp folder is cleaned by the OS.
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Initialize_ListsTheSuites()
    {
        var result = await InitializeAsync(_client);

        Assert.Equal(1, result.Capabilities.TextDocumentSync);
        Assert.Equal(["Shelf.Steps", "Shelf.Web"], result.Suites.Select(suite => suite.Name).Order());
    }

    [Fact]
    public async Task Preview_BeforeABuild_ParsesOnly_AndSaysWhy()
    {
        await InitializeAsync(_client);

        var preview = await PreviewAsync();

        Assert.Equal("parse-only", preview.BindState);
        Assert.Contains("Build the UAT project first", preview.Reason);
        Assert.Contains("Parsed only, not bound", preview.Html);
    }

    [Fact]
    public async Task BuildDone_ReloadsTheCatalog_AndThePreviewBinds()
    {
        await InitializeAsync(_client);
        Assert.Equal("parse-only", (await PreviewAsync()).BindState);

        Build();
        await _client.NotifyWithParameterObjectAsync("brinell/buildDone", new { });
        var preview = await PreviewAsync();

        Assert.Equal("bound", preview.BindState);
        Assert.Equal("Shelf.Steps", preview.Suite);
        Assert.Contains("uat-bound", preview.Html);
    }

    [Fact]
    public async Task OpenAndChange_ThePreviewShowsTheEditorsText()
    {
        Build();
        await InitializeAsync(_client);
        var uri = new Uri(Feature).AbsoluteUri;

        await _client.NotifyWithParameterObjectAsync("textDocument/didOpen", new { textDocument = new { uri, languageId = "markdown", version = 1, text = File.ReadAllText(Feature) } });
        await _client.NotifyWithParameterObjectAsync("textDocument/didChange", new
        {
            textDocument = new { uri, version = 2 },
            contentChanges = new[] { new { text = "# Feature: Shelf\n\n## Scenario: One\n\n```gherkin\nWhen I dust the shelf\n```\n" } },
        });
        var preview = await PreviewAsync();

        Assert.Equal(2, preview.Version);
        Assert.Contains("I dust the shelf", preview.Html);
        Assert.Contains("data-code=\"UATB001\"", preview.Html);
    }

    [Fact]
    public async Task ALinkedFile_OffersEachSuite_AndBindsInThePickedOne()
    {
        Build();
        await InitializeAsync(_client);

        var first = await PreviewAsync();
        var picked = await PreviewAsync("Shelf.Web");

        Assert.Equal(["Shelf.Steps", "Shelf.Web"], first.Suites);
        Assert.Equal("Shelf.Steps", first.Suite);
        Assert.Equal("Shelf.Web", picked.Suite);
        Assert.Equal("parse-only", picked.BindState);
    }

    [Fact]
    public async Task AFileNoSuiteHolds_ParsesOnly_SayingSo()
    {
        await InitializeAsync(_client);
        var loose = Path.Combine(_solution, "loose.feature.md");
        File.WriteAllText(loose, "# Feature: Loose\n");

        var preview = await _client.InvokeWithParameterObjectAsync<PreviewResult>("brinell/preview", new { uri = new Uri(loose).AbsoluteUri });

        Assert.Empty(preview.Suites);
        Assert.Contains("No suite holds or links this file", preview.Reason);
    }

    [Fact]
    public async Task PreviewHost_IsThePageTheWebviewLoads()
    {
        await InitializeAsync(_client);

        var host = await _client.InvokeWithParameterObjectAsync<PreviewHostResult>("brinell/previewHost", new { });

        Assert.Contains("<main id=\"uat-root\"></main>", host.Html);
    }

    private Task<InitializeResult> InitializeAsync(JsonRpc client)
        => client.InvokeWithParameterObjectAsync<InitializeResult>("initialize", new { processId = (int?)null, initializationOptions = new { solutionDir = _solution } });

    private Task<PreviewResult> PreviewAsync(string? suite = null)
        => _client.InvokeWithParameterObjectAsync<PreviewResult>("brinell/preview", new { uri = new Uri(Feature).AbsoluteUri, suite });

    // What a build does: the suite's assembly (this test assembly, holding its fixture and steps) appears.
    private void Build()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Built)!);
        File.Copy(typeof(LanguageServerTests).Assembly.Location, Built, overwrite: true);
    }

    private static string Config(string assembly) => $"""
        # BDD Config

        ## Runtime

        | Field | Value |
        | --- | --- |
        | Fixture | {nameof(ShelfFixture)} |

        ## Assemblies

        | Kind | Assembly |
        | --- | --- |
        | Steps | {assembly} |
        """;
}

public sealed class ShelfFixture
{
    public TestComposition Composition => throw new NotSupportedException("The server reads the fixture's type only.");
}

[BddStepDefinitions]
public sealed class ShelfSteps : BddStepDefinitionsBase
{
    [BddWhen("I shelve {title}")]
    public void Shelve(string title)
    {
    }
}
