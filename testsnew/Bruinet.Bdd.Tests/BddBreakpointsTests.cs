using Bruinet.Core.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bruinet.Bdd.Tests;

/// <summary>Step breakpoints (R2): where a session stops, without a debugger attached.</summary>
public sealed class BddBreakpointsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Bruinet", "BddBreakpointsTests", Guid.NewGuid().ToString("N"));

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

        // It stops at the call itself, before its first step; Step into goes inside.
        var call = Assert.Single(breaks);
        Assert.Equal("the shelf holds \"Emma\"", call.Text);
        Assert.True(call.IsCall);
        Assert.Equal(7, call.Source.LineNumber);
    }

    [Fact]
    public async Task TheSourceFile_MatchesTheBuildOutputsCopy()
    {
        var source = Path.Combine(_root, "Source", "Features", "shelf.feature.md");

        var breaks = await RunAsync($$"""{ "files": { {{Json(source)}}: [9] } }""");

        Assert.Equal(["I dust the shelf"], breaks.Select(b => b.Text));
    }

    [Fact]
    public void AHost_ReadsTheListedLines_AsADebuggedRunMatchesThem()
    {
        var source = Path.Combine(_root, "Source", "Features", "shelf.feature.md");
        File.WriteAllText(BreakpointsFile, BreakAt(source, 9));

        Assert.True(BddBreakpoints.Lists(Feature, 9, BreakpointsFile));
        Assert.False(BddBreakpoints.Lists(Feature, 8, BreakpointsFile));
        Assert.False(BddBreakpoints.Lists(Steps, 9, BreakpointsFile));
        Assert.False(BddBreakpoints.Lists(Feature, 9, Path.Combine(_root, "none.json")));
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

    // Line 7 and 8 call "the shelf holds {title}"; a call is a position of its own ("7 call").
    [Fact]
    public async Task StepOver_StopsAtEachScenarioStep_RunningACallWhole()
    {
        var stops = await StepAsync(BreakAt(Feature, 6), _ => "over");

        Assert.Equal(["shelf.feature.md:6", "shelf.feature.md:7 call", "shelf.feature.md:8 call", "shelf.feature.md:9"], stops);
    }

    [Fact]
    public async Task StepInto_GoesIntoACall_WithoutRunningIt_AndThroughItsSteps()
    {
        var stops = await StepAsync(BreakAt(Feature, 6), _ => "into");

        Assert.Equal(
            ["shelf.feature.md:6", "shelf.feature.md:7 call", "Shelf.steps.md:6", "Shelf.steps.md:7", "shelf.feature.md:8 call", "Shelf.steps.md:6", "Shelf.steps.md:7", "shelf.feature.md:9"],
            stops);
    }

    [Fact]
    public async Task StepOut_StopsAtTheFirstPositionAfterTheCall()
    {
        var stops = await StepAsync(BreakAt(Steps, 6), stop => stop == 0 ? "out" : null);

        Assert.Equal(["Shelf.steps.md:6", "shelf.feature.md:8 call"], stops);
    }

    [Fact]
    public async Task AMoveForAnotherProcess_IsIgnored_AndLeftInTheFile()
    {
        var stops = await StepAsync(BreakAt(Feature, 6), _ => "over", process: Environment.ProcessId + 1);

        Assert.Equal(["shelf.feature.md:6"], stops);
        Assert.Contains("\"next\"", File.ReadAllText(BreakpointsFile));
    }

    [Fact]
    public async Task WhilePaused_ThePausedFileSaysWhere_AndIsGoneAfter()
    {
        System.Text.Json.JsonElement? seen = null;
        var breakpoints = new BddBreakpoints(BreakpointsFile, stop: () => seen = System.Text.Json.JsonDocument.Parse(File.ReadAllText(PausedFile)).RootElement.Clone());
        File.WriteAllText(BreakpointsFile, BreakAt(Feature, 7));

        await RunWithAsync(breakpoints);

        Assert.NotNull(seen);
        Assert.Equal(Environment.ProcessId, seen.Value.GetProperty("process").GetInt32());
        Assert.Equal(7, seen.Value.GetProperty("line").GetInt32());
        Assert.True(seen.Value.GetProperty("isCall").GetBoolean());
        Assert.Equal(0, seen.Value.GetProperty("depth").GetInt32());
        Assert.False(File.Exists(PausedFile));
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

        var hook = new RecordingHook(new BddBreakpoints(Path.Combine(_root, "bdd-breakpoints.json"), _ => json));
        var runner = new BddScenarioRunner(new BddExecutionContext { Breaks = hook });
        using var services = new ServiceCollection().AddTransient<ShelfSteps>().BuildServiceProvider();
        BddStepDefinitionRuntime.SetServiceProvider(runner.Context, services);
        await runner.RunAsync(Assert.Single(bind.Document!.Scenarios));
        return hook.Breaks;
    }

    private static string Json(string path) => System.Text.Json.JsonSerializer.Serialize(path);

    private string BreakpointsFile => Path.Combine(_root, "bdd-breakpoints.json");

    private string PausedFile => Path.Combine(_root, "bdd-paused.json");

    private static string BreakAt(string file, int line) => $$"""{ "files": { {{Json(file)}}: [{{line}}] } }""";

    // Runs with the real files, as the IDE does it: at each stop, reads where the run is paused, and
    // writes the move the script gives for that stop (null: none, the run goes on).
    private async Task<List<string>> StepAsync(string breakpoints, Func<int, string?> move, int? process = null)
    {
        File.WriteAllText(BreakpointsFile, breakpoints);
        List<string> stops = [];
        var hook = new BddBreakpoints(BreakpointsFile, stop: () =>
        {
            var paused = System.Text.Json.JsonDocument.Parse(File.ReadAllText(PausedFile)).RootElement;
            stops.Add($"{Path.GetFileName(paused.GetProperty("file").GetString())}:{paused.GetProperty("line").GetInt32()}{(paused.GetProperty("isCall").GetBoolean() ? " call" : string.Empty)}");
            if (move(stops.Count - 1) is { } mode)
            {
                var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(BreakpointsFile))!.AsObject();
                root["next"] = new System.Text.Json.Nodes.JsonObject
                {
                    ["mode"] = mode,
                    ["depth"] = paused.GetProperty("depth").GetInt32(),
                    ["process"] = process ?? paused.GetProperty("process").GetInt32(),
                };
                File.WriteAllText(BreakpointsFile, root.ToJsonString());
            }
        });

        await RunWithAsync(hook);
        return stops;
    }

    private async Task RunWithAsync(IBddBreakHook hook)
    {
        var file = BddScenarioFiles.Load(Feature);
        var catalog = BddStepDefinitionRuntime.FromTypes(new object(), typeof(ShelfSteps)).CreateCommandCatalog();
        var bind = file.Bind(new BddCatalogSet(catalog));
        Assert.True(bind.Success, string.Join(Environment.NewLine, bind.Diagnostics.Select(d => d.Message)));

        var runner = new BddScenarioRunner(new BddExecutionContext { Breaks = hook });
        using var services = new ServiceCollection().AddTransient<ShelfSteps>().BuildServiceProvider();
        BddStepDefinitionRuntime.SetServiceProvider(runner.Context, services);
        var result = await runner.RunAsync(Assert.Single(bind.Document!.Scenarios));

        // Stopping never fails a step: a failed step here is the hook's doing.
        Assert.All(result.Steps, step => Assert.NotEqual(BddStepResultStatus.Failed, step.Status));
    }

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
