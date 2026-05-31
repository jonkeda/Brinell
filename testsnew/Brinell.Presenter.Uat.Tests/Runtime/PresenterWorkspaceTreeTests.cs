using Brinell.Presenter.Uat.Tests.PageObjects;
using Brinell.Presenter.ViewModels;

namespace Brinell.Presenter.Uat.Tests.Runtime;

[Collection(PresenterUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "Presenter")]
public sealed class PresenterWorkspaceTreeTests
{
    private const string GreetingScenarioName = "Greeting appears when a name is entered";
    private readonly PresenterPage _page;

    public PresenterWorkspaceTreeTests(PresenterFixture fixture)
    {
        _page = fixture.PresenterPage;
    }

    [Fact]
    public void WorkspaceTree_ShowsMarkdownOnly()
    {
        ReloadWorkspace();

        var allTree = _page.AllWorkspaceTree.GetText(timeoutMs: 10000) ?? string.Empty;
        Assert.Contains("uat.config.md", allTree, StringComparison.Ordinal);
        Assert.Contains("main-page-greeting.uat.md", allTree, StringComparison.Ordinal);
        Assert.Contains("main-page-validation.uat.md", allTree, StringComparison.Ordinal);
        Assert.DoesNotContain(".csproj", allTree, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".dll", allTree, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bin", allTree, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("obj", allTree, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkspaceTree_CollapsesAndExpandsVisibleRows()
    {
        ReloadWorkspace();
        _page.ExpandTreeNode(UatWorkspaceNodeKind.Folder, "Scenarios", "main-page-greeting.uat.md", timeoutMs: 10000);

        _page.TreeToggle(UatWorkspaceNodeKind.Folder, "Scenarios").Click();
        Assert.True(
            WaitUntil(() => !VisibleTreeContains("main-page-greeting.uat.md")),
            "Expected Scenarios collapse to hide its file rows.");

        _page.TreeToggle(UatWorkspaceNodeKind.Folder, "Scenarios").Click();
        Assert.True(
            WaitUntil(() => VisibleTreeContains("main-page-greeting.uat.md")),
            "Expected Scenarios expansion to show its file rows again.");

        _page.ExpandTreeNode(UatWorkspaceNodeKind.MarkdownFile, "main-page-greeting.uat.md", "MAUI Main Page Greeting", timeoutMs: 10000);
        _page.ExpandTreeNode(UatWorkspaceNodeKind.Suite, "MAUI Main Page Greeting", GreetingScenarioName, timeoutMs: 10000);
        Assert.True(
            _page.WorkspaceRows.TrySelectByText(GreetingScenarioName, timeoutMs: 10000),
            $"Could not select '{GreetingScenarioName}' after expanding the tree.");
    }

    [Fact]
    public void CollapsedFolderSelection_RunsDescendantScenariosAndReportsAutPlacement()
    {
        ReloadWorkspace();
        _page.ExpandTreeNode(UatWorkspaceNodeKind.Folder, "Scenarios", "main-page-greeting.uat.md", timeoutMs: 10000);
        _page.TreeToggle(UatWorkspaceNodeKind.Folder, "Scenarios").Click();
        Assert.True(
            WaitUntil(() => !VisibleTreeContains("main-page-greeting.uat.md")),
            "Expected Scenarios to be collapsed before selecting it.");
        Assert.True(
            _page.WorkspaceRows.TrySelectByText("Scenarios", timeoutMs: 10000),
            "Could not select the collapsed Scenarios folder.");

        _page.DelayMillisecondsInput.SetText("0");
        _page.RunButton.Click();
        _page.StatusSummary.AssertTextContains("Passed", timeoutMs: 60000);

        var scope = _page.RunScope.GetText(timeoutMs: 5000) ?? string.Empty;
        Assert.Contains("Selected node kind: Folder", scope, StringComparison.Ordinal);
        Assert.Contains("Selected node name: Scenarios", scope, StringComparison.Ordinal);
        Assert.Contains("Scenario count: 3", scope, StringComparison.Ordinal);

        var placement = _page.AutPlacement.GetText(timeoutMs: 5000) ?? string.Empty;
        Assert.Contains("AUT placement:", placement, StringComparison.Ordinal);
        Assert.Contains("Result:", placement, StringComparison.Ordinal);
    }

    private void ReloadWorkspace()
    {
        Assert.True(_page.IsLoaded(timeoutMs: 30000), "Presenter page was not loaded.");
        _page.ReloadButton.Click();
        _page.StatusSummary.AssertTextContains("Ready", timeoutMs: 30000);
    }

    private bool VisibleTreeContains(string text)
    {
        return (_page.WorkspaceTree.GetText(timeoutMs: 1000) ?? string.Empty).Contains(text, StringComparison.Ordinal);
    }

    private static bool WaitUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        var startedAt = DateTimeOffset.UtcNow;
        while (DateTimeOffset.UtcNow - startedAt < TimeSpan.FromMilliseconds(timeoutMs))
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return condition();
    }
}
