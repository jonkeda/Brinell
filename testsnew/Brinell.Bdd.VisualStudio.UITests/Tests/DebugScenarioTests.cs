using Brinell.Bdd.VisualStudio.UITests.TestBase;
using Brinell.Bdd.Workspace.UITests.PageObjects;

namespace Brinell.Bdd.VisualStudio.UITests.Tests;

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

    private static readonly string Feature = Path.Combine(VisualStudioIde.Root, "samples", "Todo", "tests", "Brinell.Samples.Todo.Uat", "Features", "TOD.01-see-my-todos.feature.md");

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
