using System.Diagnostics;
using Brinell.Presenter.ViewModels;
using Brinell.Presenter.Uat.Tests.PageObjects;

namespace Brinell.Presenter.Uat.Tests.Runtime;

[Collection(PresenterUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "Presenter")]
public sealed class PresenterRunDelayTests
{
    private const string GreetingScenarioName = "Greeting appears when a name is entered";
    private readonly PresenterPage _page;

    public PresenterRunDelayTests(PresenterFixture fixture)
    {
        _page = fixture.PresenterPage;
    }

    [Fact]
    public void AutoRun_HonorsConfiguredDelayAndRecordsScope()
    {
        PrepareGreetingScenario("1000");

        var stopwatch = Stopwatch.StartNew();
        _page.RunButton.Click();
        _page.StatusSummary.AssertTextContains("Passed: 1/1 scenarios", timeoutMs: 30000);
        stopwatch.Stop();

        Assert.True(
            stopwatch.ElapsedMilliseconds >= 5750,
            $"Expected six 1000 ms inter-step waits for the 7-step greeting scenario. Actual elapsed: {stopwatch.ElapsedMilliseconds} ms.");

        var scope = _page.RunScope.GetText(timeoutMs: 5000) ?? string.Empty;
        Assert.Contains("Selected node kind: Scenario", scope, StringComparison.Ordinal);
        Assert.Contains($"Selected node name: {GreetingScenarioName}", scope, StringComparison.Ordinal);
        Assert.Contains("Scenario count: 1", scope, StringComparison.Ordinal);
        Assert.Contains("Step count: 7", scope, StringComparison.Ordinal);
        Assert.Contains("Effective delay: 1000 ms", scope, StringComparison.Ordinal);

        var timing = _page.ExecutionTiming.GetText(timeoutMs: 5000) ?? string.Empty;
        Assert.Contains("Effective delay: 1000 ms", timing, StringComparison.Ordinal);
        Assert.Equal(7, CountOccurrences(timing, "- Step "));
        Assert.True(
            CountOccurrences(timing, "wait ") >= 6,
            "Expected at least six recorded inter-step waits in the execution timing trace.");
    }

    [Fact]
    public void AutoRun_ShowsStepNameDuringInterStepDelay()
    {
        PrepareGreetingScenario("1000");

        _page.RunButton.Click();
        _page.StatusSummary.AssertTextContains("I clear Name", timeoutMs: 12000);
        Assert.DoesNotContain(
            "Waiting 1000 ms",
            _page.StatusSummary.GetText(timeoutMs: 5000) ?? string.Empty,
            StringComparison.Ordinal);

        var timingWhileWaiting = _page.ExecutionTiming.GetText(timeoutMs: 5000) ?? string.Empty;
        Assert.Contains("Effective delay: 1000 ms", timingWhileWaiting, StringComparison.Ordinal);
        Assert.InRange(CountOccurrences(timingWhileWaiting, "- Step "), 1, 6);

        _page.StatusSummary.AssertTextContains("Passed: 1/1 scenarios", timeoutMs: 30000);
    }

    [Fact]
    public void Next_RunsOneStepAndDoesNotUseAutoDelay()
    {
        PrepareGreetingScenario("1000");

        _page.NextButton.Click();
        _page.StatusSummary.AssertTextContains("Ready", timeoutMs: 15000);

        var steps = _page.StepList.GetText(timeoutMs: 5000) ?? string.Empty;
        Assert.Equal(1, CountOccurrences(steps, "✓"));
        Assert.DoesNotContain("Waiting 1000 ms before next step", _page.StatusSummary.GetText() ?? string.Empty, StringComparison.Ordinal);
    }

    private void PrepareGreetingScenario(string delayMilliseconds)
    {
        Assert.True(_page.IsLoaded(timeoutMs: 30000), "Presenter page was not loaded.");
        _page.ReloadButton.Click();
        _page.StatusSummary.AssertTextContains("Ready", timeoutMs: 30000);
        _page.ExpandTreeNode(UatWorkspaceNodeKind.Folder, "Scenarios", "main-page-greeting.uat.md", timeoutMs: 10000);
        _page.ExpandTreeNode(UatWorkspaceNodeKind.MarkdownFile, "main-page-greeting.uat.md", "MAUI Main Page Greeting", timeoutMs: 10000);
        _page.ExpandTreeNode(UatWorkspaceNodeKind.Suite, "MAUI Main Page Greeting", GreetingScenarioName, timeoutMs: 10000);
        Assert.True(
            _page.WorkspaceRows.TrySelectByText(GreetingScenarioName, timeoutMs: 10000),
            $"Could not select '{GreetingScenarioName}' in the Presenter workspace tree.");
        _page.DelayMillisecondsInput.SetText(delayMilliseconds);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
