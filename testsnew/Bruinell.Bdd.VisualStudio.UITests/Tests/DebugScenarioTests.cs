using Bruinell.Bdd.VisualStudio.UITests.TestBase;
using Bruinell.Bdd.Workspace.UITests.PageObjects;

namespace Bruinell.Bdd.VisualStudio.UITests.Tests;

/// <summary>
/// The Scenario Preview and a Debug run of Todo's TOD.01.1 in a real Visual Studio: what the four
/// fixes of 2026-10-03/04 were about (attach once, the feature file in front, F10 as the BDD step).
/// </summary>
public sealed class DebugScenarioTests(VisualStudioIde ide) : IClassFixture<VisualStudioIde>
{
    private const int HeadingLine = 15;
    private const string Scenario = "TOD.01.1 Opening the app shows the list; a loading state shows first, then the content";

    // Named waits: a Debug run builds, starts the test host and the app before its first stop.
    private static readonly TimeSpan FirstStop = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NextStop = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan FileInFront = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan RunEnd = TimeSpan.FromMinutes(3);
    private const int BindTimeoutMs = 120_000;

    private static readonly string Feature = Path.Combine(VisualStudioIde.Root, "samples", "Todo", "tests", "Bruinell.Samples.Todo.Uat", "Features", "TOD.01-see-my-todos.feature.md");

    [IdeFact]
    public async Task ThePreview_OpensBound_ForAFeatureFile()
    {
        var preview = await OpenPreviewAsync();

        preview.BindState.AssertTextStartsWith("Bound in", timeoutMs: BindTimeoutMs);
    }

    [IdeFact]
    public async Task Debug_StopsAtTheBreakpoint_F10GoesToTheNextStep_AndContinueEndsTheRun()
    {
        await ide.StartAsync();
        ide.UseBreakpoints(Feature, 24);
        var preview = await OpenPreviewAsync();
        preview.BindState.AssertTextStartsWith("Bound in", timeoutMs: BindTimeoutMs);
        var started = DateTime.Now;

        VisualStudioIde.Log("click Debug");
        preview.Debug(HeadingLine).Click();

        await VisualStudioIde.WaitAsync(() => VisualStudioIde.Paused() is { Line: 24 } && ide.InBreakMode, FirstStop, "the stop before line 24");
        await VisualStudioIde.WaitAsync(() => IsFeatureAt(24), FileInFront, () => $"the feature file in front at line 24; in front: {InFront()}");
        preview.PausedAt.AssertTextContains("(line 24)");
        VisualStudioIde.Log("stopped at line 24; F10");

        ide.ExecuteCommand("Debug.StepOver");

        await VisualStudioIde.WaitAsync(() => VisualStudioIde.Paused() is { Line: 25 } && ide.InBreakMode, NextStop, "F10 to stop before line 25");
        await VisualStudioIde.WaitAsync(() => IsFeatureAt(25), FileInFront, () => $"the feature file in front at line 25; in front: {InFront()}");

        VisualStudioIde.Log("stopped at line 25; Continue");
        preview.Continue.Click();

        await VisualStudioIde.WaitAsync(() => ide.InDesignMode, RunEnd, "the run to end");
        Assert.NotNull(VisualStudioIde.ResultSince(Scenario, started));
    }

    [IdeFact]
    public async Task Step_PausesBeforeTheFirstStep_F5GoesToTheBreakpoint_F10ToTheEnd_AndItPasses()
    {
        await ide.StartAsync();
        ide.UseBreakpoints(Feature, 24);
        var preview = await OpenPreviewAsync();
        preview.BindState.AssertTextStartsWith("Bound in", timeoutMs: BindTimeoutMs);
        var started = DateTime.Now;

        VisualStudioIde.Log("click Step");
        preview.StepThrough(HeadingLine).Click();

        await WaitPausedAsync(preview, 18, FirstStop);
        VisualStudioIde.Log("paused at line 18; F5");
        ide.ExecuteCommand("Debug.Start");
        await WaitPausedAsync(preview, 24, NextStop);
        Assert.False(ide.InBreakMode);

        foreach (var line in new[] { 25, 26, 27 })
        {
            VisualStudioIde.Log($"F10 to line {line}");
            ide.ExecuteCommand("Debug.StepOver");
            await WaitPausedAsync(preview, line, NextStop);
        }

        VisualStudioIde.Log("F10 past the last step");
        ide.ExecuteCommand("Debug.StepOver");
        await VisualStudioIde.WaitAsync(() => VisualStudioIde.ResultSince(Scenario, started) is not null, RunEnd, "the run's result");
        Assert.Equal("passed", VisualStudioIde.ResultSince(Scenario, started));
        preview.PausedBar.AssertVisible(false);
        Assert.True(ide.InDesignMode);
    }

    [IdeFact]
    public async Task DebugFromHere_AtAStepThroughStop_BreaksBeforeTheStep_AndF11LandsInItsStepDefinition()
    {
        await ide.StartAsync();
        ide.UseBreakpoints(Feature, 24);
        var preview = await OpenPreviewAsync();
        preview.BindState.AssertTextStartsWith("Bound in", timeoutMs: BindTimeoutMs);

        VisualStudioIde.Log("click Step");
        preview.StepThrough(HeadingLine).Click();
        await WaitPausedAsync(preview, 18, FirstStop);
        ide.ExecuteCommand("Debug.Start");
        await WaitPausedAsync(preview, 24, NextStop);

        VisualStudioIde.Log("paused at line 24 (When I tap Sync); Debug from here");
        preview.DebugHere.Click();
        await VisualStudioIde.WaitAsync(() => ide.InBreakMode, NextStop, "the debugger to break in the runner");
        VisualStudioIde.Log($"break mode in {ide.CurrentFunction}");

        // F11 as a developer would, until the step definition: the control's Click (I tap {control}).
        for (var step = 0; step < StepIntoLimit && ide.CurrentFunction?.EndsWith(".Click", StringComparison.Ordinal) != true; step++)
        {
            ide.ExecuteCommand("Debug.StepInto");
            await VisualStudioIde.WaitAsync(() => ide.InBreakMode, NextStop, "F11 to stop again");
            VisualStudioIde.Log($"F11 {step + 1}: {ide.CurrentFunction}");
        }

        Assert.EndsWith(".Click", ide.CurrentFunction ?? "(none)", StringComparison.Ordinal);
        VisualStudioIde.Log("in the step definition; Continue, then Stop");
        ide.ExecuteCommand("Debug.Start");
        await WaitPausedAsync(preview, 25, NextStop);
        preview.Stop.Click();

        // The debugged runner ends with its run, and the debugging session with it.
        await VisualStudioIde.WaitAsync(() => ide.InDesignMode, RunEnd, "the debugging session to end");
    }

    // How many F11 presses the step definition may take from the break: the call is the next statement.
    private const int StepIntoLimit = 3;

    // A Step-through stop: the bar says the line, and the feature file is in front at it.
    private async Task WaitPausedAsync(ScenarioPreviewPage preview, int line, TimeSpan within)
    {
        preview.PausedAt.AssertTextContains($"(line {line})", timeoutMs: (int)within.TotalMilliseconds);
        await VisualStudioIde.WaitAsync(() => IsFeatureAt(line), FileInFront, () => $"the feature file in front at line {line}; in front: {InFront()}");
    }

    private async Task<ScenarioPreviewPage> OpenPreviewAsync()
    {
        await ide.StartAsync();
        ide.OpenFile(Feature);
        ide.ExecuteCommand("View.ScenarioPreview");
        return new ScenarioPreviewPage(await ide.PreviewAsync());
    }

    private string InFront() => $"{ide.ActiveDocument}:{ide.CaretLine}";

    private bool IsFeatureAt(int line)
        => string.Equals(ide.ActiveDocument, Feature, StringComparison.OrdinalIgnoreCase) && ide.CaretLine == line;
}
