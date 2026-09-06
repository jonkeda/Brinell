using Brinell.Maui.Controls.Navigation;

namespace Brinell.Maui.UITests.Pages.Shell;

/// <summary>
/// Page object for the Shell sample app.
/// </summary>
/// <remarks>
/// One page object rather than one per tab: the Shell chrome is on screen throughout, and
/// each page announces itself with a layout carrying its own AutomationId, so "which page am
/// I on" is a plain lookup rather than a separate object per page.
/// </remarks>
public class ShellSamplePage : PageObjectBase<ShellSamplePage>
{
    public ShellSamplePage(IMauiTestContext context)
        : base(context)
    {
        Shell = new Shell<ShellSamplePage>(this);
    }

    /// <inheritdoc />
    public override string Name => "ShellSamplePage";

    /// <summary>
    /// The shell is up once its tab strip is there.
    /// </summary>
    /// <remarks>
    /// A plain lookup, never a scrolling one: a readiness check that scrolls turns "not ready
    /// yet" into a sweep of the whole page. The rule from the Android performance work.
    /// </remarks>
    public override bool IsLoaded(int? timeoutMs = null) => Shell.Tabs.IsExists();

    /// <summary>The shell's tabs and flyout.</summary>
    public Shell<ShellSamplePage> Shell { get; }

    #region Page markers

    /// <summary>The Home tab's page.</summary>
    public Label<ShellSamplePage> HomePage => new(this, "ShellHomePage");

    /// <summary>The Controls tab's page.</summary>
    public Label<ShellSamplePage> ControlsPage => new(this, "ShellControlsPage");

    /// <summary>The Detail tab's page.</summary>
    public Label<ShellSamplePage> DetailPage => new(this, "ShellDetailPage");

    /// <summary>The page the Detail tab pushes onto its stack.</summary>
    public Label<ShellSamplePage> DetailSubPage => new(this, "ShellDetailSubPage");

    /// <summary>The Status tab's page.</summary>
    public Label<ShellSamplePage> StatusPage => new(this, "ShellStatusPage");

    /// <summary>Any flyout item's page: one label naming the item that led here.</summary>
    public Label<ShellSamplePage> FlyoutPageTitle => new(this, "ShellFlyoutPageTitle");

    /// <summary>
    /// Whether the Detail tab currently has its sub-page pushed.
    /// </summary>
    /// <remarks>
    /// A plain lookup rather than <c>DetailSubPage.IsExists()</c>, which scrolls to be sure of
    /// an absence. The fixture asks this before every test, and the usual answer is "no", so
    /// the scrolling form would spend a sweep per test confirming nothing is there.
    /// </remarks>
    public bool IsSubPagePushed()
        => Context.TryFindElement(Locator.ByAutomationId("ShellDetailSubPage")) != null;

    #endregion

    #region Controls

    /// <summary>Pushes the Detail tab's sub-page.</summary>
    public Button<ShellSamplePage> PushSubPageButton => new(this, "ShellDetailPushButton");

    /// <summary>The sub-page's own way back, which pops the stack.</summary>
    public Button<ShellSamplePage> SubPageBackButton => new(this, "ShellDetailSubBackButton");

    /// <summary>A button on the Controls tab, so a tab test can prove the page is live.</summary>
    public Button<ShellSamplePage> ControlsButton => new(this, "ShellControlsButton");

    /// <summary>What <see cref="ControlsButton"/> writes.</summary>
    public Label<ShellSamplePage> ControlsResult => new(this, "ShellControlsResult");

    #endregion
}
