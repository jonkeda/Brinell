using Brinell.Core.Locators;
using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Controls.Selection;
using Brinell.Maui.Controls.Text;
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

    /// <summary>
    /// The page's root element. The default locator is the page <see cref="Name"/>, which is
    /// "Presenter" because that is what scenarios say; the root grid is <c>PresenterRoot</c>.
    /// </summary>
    protected override Locator Locator => new(LocatorStrategy.AutomationId, "PresenterRoot");

    [UatName("Status Summary")]
    public PresenterStatusLabel<PresenterPage> StatusSummary => new(this, "StatusSummaryLabel");

    [UatName("Workspace Summary")]
    public Label<PresenterPage> WorkspaceSummary => new(this,"WorkspaceSummaryLabel");

    [UatName("Workspace Tree")]
    public Label<PresenterPage> WorkspaceTree => new(this,"WorkspaceTreeText");

    [UatName("All Workspace Tree")]
    public Label<PresenterPage> AllWorkspaceTree => new(this,"AllWorkspaceTreeText");

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

    /// <summary>The Config tab's state header: clean, unsaved, or problems.</summary>
    [UatName("Config State")]
    public Label<PresenterPage> ConfigState => new(this, "ConfigStateLabel");

    /// <summary>The one editable path: the UAT project.</summary>
    [UatName("Config Project")]
    public Entry<PresenterPage> ConfigProjectInput => new(this, "ConfigProjectInput");

    /// <summary>The fixture name.</summary>
    [UatName("Config Fixture")]
    public Entry<PresenterPage> ConfigFixtureInput => new(this, "ConfigFixtureInput");

    /// <summary>The problem attached to the fixture row, empty when there is none.</summary>
    [UatName("Config Fixture Problem")]
    public Label<PresenterPage> ConfigFixtureProblem => new(this, "ConfigDiagnosticRuntimeFixture");

    /// <summary>The target, derived from the fixture's base type.</summary>
    [UatName("Config Derived Target")]
    public Label<PresenterPage> ConfigDerivedTarget => new(this, "ConfigDerivedTarget");

    /// <summary>Where the Pages assembly resolved, and how.</summary>
    [UatName("Config Derived Pages")]
    public Label<PresenterPage> ConfigDerivedPages => new(this, "ConfigDerivedPages");

    /// <summary>What owns the app path.</summary>
    [UatName("Config Derived App")]
    public Label<PresenterPage> ConfigDerivedApp => new(this, "ConfigDerivedApp");

    /// <summary>What the last save did.</summary>
    [UatName("Config Save Message")]
    public Label<PresenterPage> ConfigSaveMessage => new(this, "ConfigSaveMessage");

    [UatName("Config Save")]
    public Button<PresenterPage> ConfigSaveButton => new(this, "ConfigSaveButton");

    [UatName("Config Revert")]
    public Button<PresenterPage> ConfigRevertButton => new(this, "ConfigRevertButton");

    [UatName("Config Rescan Fixtures")]
    public Button<PresenterPage> ConfigFixtureRescanButton => new(this, "ConfigFixtureRescanButton");

    /// <summary>The Diagnostics tab's text, which carries a failed run's exception.</summary>
    [UatName("Diagnostics Text")]
    public Label<PresenterPage> DiagnosticsText => new(this, "DiagnosticsText");

    [UatName("Config")]
    public Button<PresenterPage> ConfigTabButton => new(this, "ConfigTabButton");

    [UatName("Tree")]
    public Button<PresenterPage> TreeTabButton => new(this, "TreeTabButton");

    [UatName("Diagnostics")]
    public Button<PresenterPage> DiagnosticsTabButton => new(this, "DiagnosticsTabButton");

    /// <summary>The workspace tree control; <see cref="WorkspaceTree"/> is its hidden text mirror.</summary>
    public PresenterWorkspaceTree WorkspaceTreeView => new(this);

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

        WorkspaceTreeView.Node(kind, name).Expand(timeoutMs);
        WorkspaceTree.AssertTextContains(expectedVisibleText, timeoutMs: timeoutMs);
    }

    // No IsLoaded override: the base checks the page root, which every tab shares.
    // Probing a tab-specific element instead made the page look unloaded the moment
    // anything but the tree tab was selected.

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
