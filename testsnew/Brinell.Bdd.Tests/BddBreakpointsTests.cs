using Brinell.Core.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Brinell.Bdd.Tests;

/// <summary>Step breakpoints (R2): where a session stops, without a debugger attached.</summary>
public sealed class BddBreakpointsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Brinell", "BddBreakpointsTests", Guid.NewGuid().ToString("N"));

    public BddBreakpointsTests()
    {
        Directory.CreateDirectory(Features);
        File.WriteAllText(Path.Combine(Features, "Shelf.steps.md"), """
            # Steps: Shelf

            ## Step: the shelf holds {title}

            ```gherkin
            When I shelve <title>
            And I dust the shelf
            ```
            """);
        File.WriteAllText(Feature, """
            # Feature: Shelf

            ## Scenario: Shelve

            ```gherkin
            When I shelve "Dune"
            And the shelf holds "Emma"
            And the shelf holds "Hamlet"
            And I dust the shelf
            ```
            """);
    }

    private string Features => Path.Combine(_root, "Suite", "Features");

    private string Feature => Path.Combine(Features, "shelf.feature.md");

    private string Steps => Path.Combine(Features, "Shelf.steps.md");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public async Task AListedLine_Breaks_AndAnUnlistedOneDoesNot()
    {
        var breaks = await RunAsync($$"""{ "files": { {{Json(Feature)}}: [6, 9] } }""");

        Assert.Equal(["I shelve \"Dune\"", "I dust the shelf"], breaks.Select(b => b.Text));
        Assert.Equal("Dune", breaks[0].Arguments["title"]);
        Assert.Empty(breaks[0].Calls);
    }

    [Fact]
    public async Task ALineInAStepFile_BreaksAtEachCall()
    {
        var breaks = await RunAsync($$"""{ "files": { {{Json(Steps)}}: [6] } }""");

        Assert.Equal(["I shelve \"Emma\"", "I shelve \"Hamlet\""], breaks.Select(b => b.Text));
        Assert.Equal(Steps, breaks[0].Source.FilePath);
        Assert.Equal("Step: the shelf holds {title} (Shelf.steps.md:3) at shelf.feature.md:7", Assert.Single(breaks[0].Calls));
    }

    [Fact]
    public async Task ACallsLine_BreaksOnce_BeforeItsFirstStep()
    {
        var breaks = await RunAsync($$"""{ "files": { {{Json(Feature)}}: [7] } }""");

        Assert.Equal(["I shelve \"Emma\""], breaks.Select(b => b.Text));
    }

    [Fact]
    public async Task TheSourceFile_MatchesTheBuildOutputsCopy()
    {
        var source = Path.Combine(_root, "Source", "Features", "shelf.feature.md");

        var breaks = await RunAsync($$"""{ "files": { {{Json(source)}}: [9] } }""");

        Assert.Equal(["I dust the shelf"], breaks.Select(b => b.Text));
    }

    [Fact]
    public async Task BreakOnFailure_StopsAfterAFailedStep_WithItsResult()
    {
        ShelfSteps.FailDusting = true;
        try
        {
            var breaks = await RunAsync("""{ "files": {}, "breakOnFailure": true }""");

            var failure = Assert.Single(breaks);
            Assert.Equal("I dust the shelf", failure.Text);
            Assert.Equal(BddStepResultStatus.Failed, failure.Result?.Status);
        }
        finally
        {
            ShelfSteps.FailDusting = false;
        }
    }

    [Fact]
    public void WithoutADebugger_NothingIsRead()
    {
        Assert.Null(BddBreakpoints.ForDebugger(attached: () => false, path: Path.Combine(_root, "never-read.json")));
        Assert.NotNull(BddBreakpoints.ForDebugger(attached: () => true, path: Path.Combine(_root, "absent.json")));
    }

    [Fact]
    public async Task NoFile_NoBreaks()
    {
        var breaks = await RunAsync(json: null);

        Assert.Empty(breaks);
    }

    private async Task<List<BddStepBreak>> RunAsync(string? json)
    {
        var file = BddScenarioFiles.Load(Feature);
        var catalog = BddStepDefinitionRuntime.FromTypes(new object(), typeof(ShelfSteps)).CreateCommandCatalog();
        var bind = file.Bind(new BddCatalogSet(catalog));
        Assert.True(bind.Success, string.Join(Environment.NewLine, bind.Diagnostics.Select(d => d.Message)));

        var hook = new RecordingHook(new BddBreakpoints(Path.Combine(_root, "uat-breakpoints.json"), _ => json));
        var runner = new BddScenarioRunner(new BddExecutionContext { Breaks = hook });
        using var services = new ServiceCollection().AddTransient<ShelfSteps>().BuildServiceProvider();
        BddStepDefinitionRuntime.SetServiceProvider(runner.Context, services);
        await runner.RunAsync(Assert.Single(bind.Document!.Scenarios));
        return hook.Breaks;
    }

    private static string Json(string path) => System.Text.Json.JsonSerializer.Serialize(path);

    // The running hook's decisions, with the stop recorded instead of Debugger.Break.
    private sealed class RecordingHook(BddBreakpoints inner) : IBddBreakHook
    {
        public List<BddStepBreak> Breaks { get; } = [];

        public BddStepBreak? Before(BddStepInvocation invocation, BddExecutionContext context) => inner.Before(invocation, context);

        public bool AfterFailure(BddStepResult result) => inner.AfterFailure(result);

        public void Break(BddStepBreak stepBreak) => Breaks.Add(stepBreak);
    }

    [BddStepDefinitions]
    public sealed class ShelfSteps
    {
        public static bool FailDusting { get; set; }

        [BddWhen("I shelve {title}")]
        public void Shelve(string title)
        {
        }

        [BddWhen("I dust the shelf")]
        public void Dust()
        {
            if (FailDusting)
            {
                throw new InvalidOperationException("dusty");
            }
        }
    }
}
