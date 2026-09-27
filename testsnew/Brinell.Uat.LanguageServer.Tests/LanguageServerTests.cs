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
    private readonly TaskCompletionSource<string> _resultsChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly PublishedDiagnostics _published = new();

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
        _client.AddLocalRpcMethod("brinell/resultsChanged", new Action<string>(uri => _resultsChanged.TrySetResult(uri)));
        _client.AddLocalRpcTarget(_published, new JsonRpcTargetOptions { UseSingleObjectParameterDeserialization = true });
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

        Assert.Equal(1, result.Capabilities.TextDocumentSync.Change);
        Assert.True(result.Capabilities.TextDocumentSync.OpenClose);
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

    [Fact]
    public async Task Preview_ShowsTheNewestRun_FoundThroughTheManifests()
    {
        Build();
        File.WriteAllText(Path.Combine(_solution, "Shelf.sln"), string.Empty);
        WriteRun("20260101-000000-older1", "failed", "Failed");
        WriteRun("20260102-000000-newer2", "passed", "Passed");
        await InitializeAsync(_client);

        var preview = await PreviewAsync();

        Assert.Equal(Path.Combine(_solution, "TestResults"), preview.ResultsRoot);
        Assert.Contains("<span class=\"uat-result\" data-status=\"passed\"", preview.Html);
        Assert.Contains("<span class=\"uat-status\" data-status=\"passed\"", preview.Html);
    }

    [Fact]
    public async Task ANewRun_TellsTheClientTheResultsChanged()
    {
        Build();
        File.WriteAllText(Path.Combine(_solution, "Shelf.sln"), string.Empty);
        Directory.CreateDirectory(Path.Combine(_solution, "TestResults"));
        await InitializeAsync(_client);
        await _client.NotifyWithParameterObjectAsync("textDocument/didOpen", new { textDocument = new { uri = new Uri(Feature).AbsoluteUri, version = 1, text = File.ReadAllText(Feature) } });

        WriteRun("20260103-000000-latest", "passed", "Passed");

        Assert.Equal(new Uri(Feature).AbsoluteUri, await _resultsChanged.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    // A run as UatScenarioTestBase writes it: the report, and its manifest record naming suite and file.
    private void WriteRun(string runId, string status, string stepStatus)
    {
        var run = Path.Combine(_solution, "TestResults", runId);
        var uat = Directory.CreateDirectory(Path.Combine(run, "suites", "testhost", "uat")).FullName;
        File.WriteAllText(Path.Combine(uat, "shelf__One__result.json"), $$"""
            { "scenario": "One", "file": "Features/shelf.feature.md", "line": 3, "status": "{{status}}", "durationMs": 12,
              "steps": [ { "line": 6, "file": "Features/shelf.feature.md", "callLine": null, "status": "{{stepStatus}}", "message": null } ] }
            """);
        File.WriteAllText(Path.Combine(run, "manifest.json"), $$"""
            { "RunId": "{{runId}}", "Suites": [ { "Name": "testhost", "Artifacts": [ {
                "Kind": "uat-scenario", "Name": "One", "Path": "suites\\testhost\\uat\\shelf__One__result.json", "Reason": "{{status}}", "Status": "{{status}}",
                "CreatedAtUtc": "2026-01-01T00:00:00Z", "Metadata": { "suite": "Shelf.Steps", "file": "Features/shelf.feature.md", "line": "3" } } ] } ] }
            """);
        File.SetLastWriteTimeUtc(Path.Combine(run, "manifest.json"), DateTime.UtcNow.AddMinutes(runId.EndsWith("older1", StringComparison.Ordinal) ? -10 : 0));
    }

    [Fact]
    public async Task RunTarget_ForAScenarioHeading_IsTheSuitesProject_AndItsAddress()
    {
        Build();
        File.WriteAllText(Path.Combine(Shelf, "Shelf.Steps.csproj"), "<Project />");
        await InitializeAsync(_client);

        var target = await _client.InvokeWithParameterObjectAsync<RunTargetResult>("brinell/runTarget", new { uri = new Uri(Feature).AbsoluteUri, line = 3 });

        Assert.True(target.CanRun, target.Reason);
        Assert.Equal(Path.Combine(Shelf, "Shelf.Steps.csproj"), target.Project);
        Assert.Equal("UatScenario=Features/shelf.feature.md#One", target.Filter);
        Assert.Equal(["One"], target.Scenarios);
    }

    [Fact]
    public async Task RunTarget_ForTheFile_MatchesEachOfItsScenarios()
    {
        Build();
        File.WriteAllText(Path.Combine(Shelf, "Shelf.Steps.csproj"), "<Project />");
        await InitializeAsync(_client);

        var target = await _client.InvokeWithParameterObjectAsync<RunTargetResult>("brinell/runTarget", new { uri = new Uri(Feature).AbsoluteUri });

        Assert.Equal("UatScenario~Features/shelf.feature.md#", target.Filter);
    }

    [Fact]
    public async Task RunTarget_BeforeABuild_SaysWhy()
    {
        await InitializeAsync(_client);

        var target = await _client.InvokeWithParameterObjectAsync<RunTargetResult>("brinell/runTarget", new { uri = new Uri(Feature).AbsoluteUri, line = 3 });

        Assert.False(target.CanRun);
        Assert.Contains("Build the UAT project first", target.Reason);
    }

    [Fact]
    public async Task AnOpenFile_GetsTheGatesDiagnostics_AtTheStepsLine()
    {
        Build();
        await InitializeAsync(_client, editor: true);
        var uri = new Uri(Feature).AbsoluteUri;

        await _client.NotifyWithParameterObjectAsync("textDocument/didOpen", new
        {
            textDocument = new { uri, version = 1, text = "# Feature: Shelf\n\n## Scenario: One\n\n```gherkin\n  When I dust the shelf\n```\n" },
        });
        var published = await _published.WaitAsync(uri, diagnostics => diagnostics.Count > 0);

        var diagnostic = Assert.Single(published.Diagnostics, d => d.Code == "UATB001");
        Assert.Equal(1, diagnostic.Severity);
        Assert.Equal("Brinell UAT", diagnostic.Source);
        Assert.Equal(new Range(new Position(5, 2), new Position(5, 23)), diagnostic.Range);
        Assert.Equal(1, published.Version);
    }

    [Fact]
    public async Task FixingTheStep_ClearsItsDiagnostic()
    {
        Build();
        await InitializeAsync(_client, editor: true);
        var uri = new Uri(Feature).AbsoluteUri;
        await _client.NotifyWithParameterObjectAsync("textDocument/didOpen", new { textDocument = new { uri, version = 1, text = "# Feature: Shelf\n\n## Scenario: One\n\n```gherkin\nWhen I dust the shelf\n```\n" } });
        await _published.WaitAsync(uri, diagnostics => diagnostics.Count > 0);

        await _client.NotifyWithParameterObjectAsync("textDocument/didChange", new
        {
            textDocument = new { uri, version = 2 },
            contentChanges = new[] { new { text = File.ReadAllText(Feature) } },
        });

        Assert.Empty((await _published.WaitAsync(uri, diagnostics => diagnostics.Count == 0)).Diagnostics);
    }

    [Fact]
    public async Task Save_ChecksTheWholeSuite_ClosedFilesToo()
    {
        Build();
        var other = Path.Combine(Shelf, "Features", "other.feature.md");
        File.WriteAllText(other, "# Feature: Other\n\n## Scenario: Two\n\n```gherkin\nWhen I lose the shelf\n```\n");
        await InitializeAsync(_client, editor: true);

        await _client.NotifyWithParameterObjectAsync("textDocument/didSave", new { textDocument = new { uri = new Uri(Feature).AbsoluteUri } });

        var published = await _published.WaitAsync(new Uri(other).AbsoluteUri, diagnostics => diagnostics.Count > 0);
        Assert.Contains(published.Diagnostics, d => d.Code == "UATB001" && d.Range.Start.Line == 5);
        Assert.Null(published.Version);
    }

    [Fact]
    public async Task AStepThatFailedInTheLastRun_IsAnErrorAtItsLine()
    {
        Build();
        File.WriteAllText(Path.Combine(_solution, "Shelf.sln"), string.Empty);
        WriteRun("20260104-000000-failed", "failed", "Failed");
        await InitializeAsync(_client, editor: true);
        var uri = new Uri(Feature).AbsoluteUri;

        await _client.NotifyWithParameterObjectAsync("textDocument/didOpen", new { textDocument = new { uri, version = 1, text = File.ReadAllText(Feature) } });

        var published = await _published.WaitAsync(uri, diagnostics => diagnostics.Count > 0);
        var failed = Assert.Single(published.Diagnostics);
        Assert.Equal("UATRUN", failed.Code);
        Assert.Equal(5, failed.Range.Start.Line);
        Assert.StartsWith("Failed in the last run of 'One'", failed.Message);
    }

    private Task<InitializeResult> InitializeAsync(JsonRpc client, bool editor = false)
        => client.InvokeWithParameterObjectAsync<InitializeResult>("initialize", new
        {
            processId = (int?)null,
            initializationOptions = new { solutionDir = _solution },
            capabilities = editor ? new { textDocument = new { publishDiagnostics = new { } } } : null,
        });

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

// The suite's test class, as a suite that runs by address has it.
public sealed class ShelfScenarioTests
{
    [Brinell.Uat.UatScenarioTheory]
    [InlineData("x.feature.md", "x")]
    public void Scenario_Passes(string filePath, string scenarioName)
    {
        Assert.NotNull(filePath);
        Assert.NotNull(scenarioName);
    }
}

[BddStepDefinitions]
public sealed class ShelfSteps : BddStepDefinitionsBase
{
    [BddWhen("I shelve {title}")]
    public void Shelve(string title)
    {
    }
}

/// <summary>The diagnostics the server publishes, per file, as an editor's LSP client takes them.</summary>
public sealed class PublishedDiagnostics
{
    private readonly object _gate = new();
    private readonly Dictionary<string, PublishDiagnosticsParams> _latest = [];
    private event Action? Published;

    [JsonRpcMethod("textDocument/publishDiagnostics")]
    public void Publish(PublishDiagnosticsParams published)
    {
        lock (_gate)
        {
            _latest[published.Uri] = published;
        }

        Published?.Invoke();
    }

    /// <summary>The file's latest diagnostics once they meet the condition.</summary>
    public async Task<PublishDiagnosticsParams> WaitAsync(string uri, Func<IReadOnlyList<Diagnostic>, bool> condition)
    {
        var met = new TaskCompletionSource<PublishDiagnosticsParams>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Check()
        {
            lock (_gate)
            {
                if (_latest.TryGetValue(uri, out var latest) && condition(latest.Diagnostics))
                {
                    met.TrySetResult(latest);
                }
            }
        }

        Published += Check;
        try
        {
            Check();
            return await met.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
        finally
        {
            Published -= Check;
        }
    }
}