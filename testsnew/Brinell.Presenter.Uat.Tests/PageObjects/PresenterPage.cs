using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Controls.Selection;
using Brinell.Maui.Controls.Text;
using Brinell.Maui.Extensions.Controls.Selection;
using Brinell.Maui.Interfaces;
using Brinell.Maui.Pages;
using Brinell.Presenter.ViewModels;
using Brinell.Uat;

namespace Brinell.Presenter.Uat.Tests.PageObjects;

[TestPage("Presenter")]
public sealed class PresenterPage : PageObjectBase<PresenterPage>
{
    public PresenterPage(IMauiTestContext context)
        : base(context)
    {
    }

    public override string Name => "Presenter";

    [UatName("Status Summary")]
    public PresenterStatusLabel<PresenterPage> StatusSummary => new(this, "StatusSummaryLabel");

    [UatName("Workspace Summary")]
    public Label<PresenterPage> WorkspaceSummary => new(this,"WorkspaceSummaryLabel");

    [UatName("Workspace Tree")]
    public Label<PresenterPage> WorkspaceTree => new(this,"WorkspaceTreeText");

    [UatName("All Workspace Tree")]
    public Label<PresenterPage> AllWorkspaceTree => new(this,"AllWorkspaceTreeText");

    public SelectionList<PresenterPage> WorkspaceRows => new(this);

    [UatName("Step List")]
    public Label<PresenterPage> StepList => new(this,"StepListText");

    [UatName("Workspace Config")]
    public Label<PresenterPage> WorkspaceConfig => new(this,"WorkspaceConfigText");

    [UatName("Execution Timing")]
    public Label<PresenterPage> ExecutionTiming => new(this,"ExecutionTimingText");

    [UatName("Run Scope")]
    public Label<PresenterPage> RunScope => new(this,"RunScopeText");

    [UatName("Open Recent")]
    public Button<PresenterPage> OpenRecentButton => new(this,"OpenRecentButton");

    [UatName("Recent Folders")]
    public Label<PresenterPage> RecentFolders => new(this,"RecentFoldersText");

    [UatName("AUT Placement")]
    public Label<PresenterPage> AutPlacement => new(this,"AutPlacementText");

    [UatName("Run")]
    public Button<PresenterPage> RunButton => new(this,"RunButton");

    public Button<PresenterPage> StopButton => new(this,"StopButton");

    public Button<PresenterPage> ReloadButton => new(this,"ReloadButton");

    public Button<PresenterPage> ValidateButton => new(this,"ValidateButton");

    [UatName("Next")]
    public Button<PresenterPage> NextButton => new(this,"NextButton");

    [UatName("Delay")]
    public Entry<PresenterPage> DelayMillisecondsInput => new(this, "DelayMillisecondsInput");

    public Button<PresenterPage> TreeToggle(UatWorkspaceNodeKind kind, string name)
    {
        return new(this, $"WorkspaceNodeToggle_{SanitizeAutomationId($"{kind}_{name}")}");
    }

    public void ExpandTreeNode(UatWorkspaceNodeKind kind, string name, string expectedVisibleText, int timeoutMs = 5000)
    {
        var treeText = WorkspaceTree.GetText(timeoutMs: timeoutMs) ?? string.Empty;
        if (treeText.Contains(expectedVisibleText, StringComparison.Ordinal))
        {
            return;
        }

        TreeToggle(kind, name).Click();
        WorkspaceTree.AssertTextContains(expectedVisibleText, timeoutMs: timeoutMs);
    }

    public override bool IsLoaded(int? timeoutMs = null)
    {
        return StatusSummary.IsExists();
    }

    private static string SanitizeAutomationId(string value)
    {
        return string.Concat(value.Select(c => char.IsLetterOrDigit(c) ? c : '_')).Trim('_');
    }
}

public sealed class PresenterStatusLabel<TScope> : Label<TScope>
    where TScope : IMauiScope<TScope>
{
    public PresenterStatusLabel(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    public new TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        return base.AssertTextContains(expected, message, timeoutMs ?? 90000);
    }
}
