using Brinell.Maui.Controls.Navigation;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the navigation controls demo page.
/// </summary>
/// <remarks>
/// The two toolbars carry the <b>same</b> items. That is deliberate: a toolbar finds its
/// items within its own element, so a scoped click must reach the right bar. Distinct items
/// would make those tests pass without testing anything.
/// </remarks>
public class NavigationDemoPage : PageObjectBase<NavigationDemoPage>
{
    public NavigationDemoPage(IMauiTestContext context)
        : base(context)
    {
        PrimaryToolbar = new Toolbar<NavigationDemoPage>(this, "PrimaryToolbar");
        SecondaryToolbar = new Toolbar<NavigationDemoPage>(this, "SecondaryToolbar");
        ActionsMenu = new Menu<NavigationDemoPage>(this, "ActionsMenu",
            triggerLocator: Locator.ByAutomationId("ActionsMenuTrigger"),
            itemsHostLocator: Locator.ByAutomationId("ActionsMenuItems"));
        Tabs = new TabMenu<NavigationDemoPage>(this);
    }

    /// <inheritdoc />
    public override string Name => "NavigationDemoPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null) => PageTitle.IsExists();

    #region Observed state

    /// <summary>The page title label.</summary>
    public Label<NavigationDemoPage> PageTitle => new(this, "PageTitle");

    /// <summary>The most recent action recorded by the view model.</summary>
    public Label<NavigationDemoPage> LastAction => new(this, "LastActionLabel");

    /// <summary>How many actions have fired since the last reset.</summary>
    public Label<NavigationDemoPage> ActionCount => new(this, "ActionCountLabel");

    /// <summary>The most recently selected tab.</summary>
    public Label<NavigationDemoPage> SelectedTab => new(this, "SelectedTabLabel");

    /// <summary>Restores the initial state.</summary>
    public Button<NavigationDemoPage> ResetButton => new(this, "NavigationResetButton");

    #endregion

    #region Navigation controls

    /// <summary>The primary toolbar.</summary>
    public Toolbar<NavigationDemoPage> PrimaryToolbar { get; }

    /// <summary>A second toolbar holding the same items as the primary.</summary>
    public Toolbar<NavigationDemoPage> SecondaryToolbar { get; }

    /// <summary>
    /// The actions menu: the whole menu, with its trigger and its item host inside it.
    /// </summary>
    public Menu<NavigationDemoPage> ActionsMenu { get; }

    /// <summary>The bottom tab menu.</summary>
    public TabMenu<NavigationDemoPage> Tabs { get; }

    #endregion

    #region Raw lookups (probe)

    /// <summary>
    /// Resolves an element by automation id from page scope, or null.
    /// </summary>
    /// <remarks>
    /// Used by the probe to measure addressability of page-level chrome
    /// (<c>ToolbarItem</c>, <c>MenuBarItem</c>) without presupposing it works.
    /// </remarks>
    public IMauiElement? TryFindByAutomationId(string automationId)
    {
        try
        {
            return Context.TryFindElement(Locator.ByAutomationId(automationId));
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves an element by its visible name, or null.
    /// </summary>
    /// <remarks>
    /// MAUI chrome frequently exposes a Name but not an AutomationId — Shell's tabs and
    /// flyout items are found that way. Separating the two lets the probe distinguish
    /// "absent" from "present but not addressable by id".
    /// </remarks>
    public IMauiElement? TryFindByName(string name)
    {
        try
        {
            return Context.TryFindElement(new Locator(LocatorStrategy.XPath, $"//*[@Name='{name}']"));
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }

    #endregion
}
