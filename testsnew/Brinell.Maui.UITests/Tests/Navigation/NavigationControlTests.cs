using Brinell.Maui.Controls.Navigation;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Navigation;

/// <summary>
/// UI tests for the navigation control objects: Toolbar, Menu, and TabMenu.
/// </summary>
/// <remarks>
/// <para>
/// Every assertion is on view-model state observed through a label — never on a fixed
/// delay. A click either changed <c>LastActionLabel</c> or it did not.
/// </para>
/// <para>
/// <c>FlyoutItem</c> is not covered here; see <see cref="NavigationProbeTests"/>.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Navigation")]
public class NavigationControlTests
{
    private readonly MauiFixture _fixture;

    public NavigationControlTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToNavigationDemo();
    }

    private NavigationDemoPage Page => _fixture.NavigationDemoPage;

    #region Toolbar

    /// <summary>1. The toolbar resolves, and both toolbars are distinct elements.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Control", "Toolbar")]
    public Task Toolbar_Exists()
    {
        Page.PrimaryToolbar.AssertExists();
        Page.SecondaryToolbar.AssertExists();

        return Task.CompletedTask;
    }

    /// <summary>
    /// 1b. GetTitle returns without throwing on a container that carries no caption.
    /// </summary>
    /// <remarks>
    /// <c>GetTitleCore</c> falls back through the Title attribute, the text attribute,
    /// then <c>Text</c>. A layout container has none of those, so the meaningful contract
    /// is that the call completes rather than that it yields a particular value — asserting
    /// a value here would only pin whichever fallback the platform happens to answer with.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetTitle")]
    public Task Toolbar_GetTitle_DoesNotThrowWithoutACaption()
    {
        var exception = Record.Exception(() => Page.PrimaryToolbar.GetTitle());

        Assert.Null(exception);

        return Task.CompletedTask;
    }

    /// <summary>2. ClickToolbarItem activates an item inside the toolbar.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "ClickToolbarItem")]
    public Task Toolbar_ClickItem_FiresTheItem()
    {
        Page.PrimaryToolbar.ClickToolbarItem(Locator.ByAutomationId("ToolbarSaveButton"));

        Page.LastAction.AssertText("Primary/Save");
        Page.ActionCount.AssertText("1");

        return Task.CompletedTask;
    }

    /// <summary>
    /// 3. The item search is scoped to its own toolbar.
    /// </summary>
    /// <remarks>
    /// Both toolbars declare <c>ToolbarSaveButton</c>. If <c>ClickToolbarItem</c> resolved
    /// page-wide, both calls would hit whichever came first in the tree and the two
    /// recorded actions would be identical. This is the test that makes the shared ids in
    /// the demo markup worth having.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Scoping")]
    public Task Toolbar_ItemSearch_IsScopedToItsOwnToolbar()
    {
        var save = Locator.ByAutomationId("ToolbarSaveButton");

        Page.PrimaryToolbar.ClickToolbarItem(save);
        Page.LastAction.AssertText("Primary/Save");

        Page.SecondaryToolbar.ClickToolbarItem(save);
        Page.LastAction.AssertText("Secondary/Save");

        return Task.CompletedTask;
    }

    /// <summary>4. An item present in one toolbar but absent from the other is not found.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "NoParentFallback")]
    public Task Toolbar_DoesNotReachItemsOutsideItself()
    {
        // Only the primary toolbar has a Back button.
        Page.PrimaryToolbar.ClickToolbarItem(Locator.ByAutomationId("ToolbarBackButton"));
        Page.LastAction.AssertText("Primary/Back");

        Assert.Throws<ElementNotFoundException>(() =>
            Page.SecondaryToolbar.ClickToolbarItem(
                Locator.ByAutomationId("ToolbarBackButton"), TestConstants.ShortTestTimeoutMs));

        return Task.CompletedTask;
    }

    /// <summary>5. GoBack clicks the supplied back-button locator.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GoBack")]
    public Task Toolbar_GoBack_ClicksTheBackItem()
    {
        Page.PrimaryToolbar.GoBack(Locator.ByAutomationId("ToolbarBackButton"));

        Page.LastAction.AssertText("Primary/Back");

        return Task.CompletedTask;
    }

    /// <summary>6. Toolbar actions return the containing scope.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "FluentReturn")]
    public Task Toolbar_Actions_ReturnTheContainingScope()
    {
        var page = Page;

        NavigationDemoPage afterClick =
            page.PrimaryToolbar.ClickToolbarItem(Locator.ByAutomationId("ToolbarSaveButton"));

        Assert.Same(page, afterClick);

        return Task.CompletedTask;
    }

    #endregion

    #region Menu

    /// <summary>7. The menu resolves and starts closed.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Control", "Menu")]
    public Task Menu_Exists_AndStartsClosed()
    {
        Page.ActionsMenu.AssertExists();

        // IsVisible drives presence in the Windows tree, so a closed menu's item host is
        // absent rather than merely hidden. AssertExists(false) expresses that directly.
        Page.MenuItemsHost.AssertExists(false);

        return Task.CompletedTask;
    }

    /// <summary>8. Open expands the menu; its items become reachable.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Open")]
    public Task Menu_Open_RevealsItems()
    {
        Page.ActionsMenu.Open();

        Assert.True(Page.MenuItemsHost.WaitExists(true, TestConstants.DefaultTestTimeoutMs));
        Assert.True(Page.ActionsMenu.IsOpen());

        return Task.CompletedTask;
    }

    /// <summary>9. ClickMenuItem activates an item and dismisses the menu.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "ClickMenuItem")]
    public Task Menu_ClickItem_FiresItAndCloses()
    {
        Page.ActionsMenu.Open();
        Assert.True(Page.MenuItemsHost.WaitExists(true, TestConstants.DefaultTestTimeoutMs));

        // The menu control is bound to its trigger, so items are addressed from the page
        // scope that contains them.
        var menuScope = new Menu<NavigationDemoPage>(Page, "ActionsMenuItems");
        menuScope.ClickMenuItem(Locator.ByAutomationId("MenuItemOpen"));

        Page.LastAction.AssertText("Menu/Open");

        // Selecting an item dismisses the menu, as a real menu would.
        Assert.True(Page.MenuItemsHost.WaitExists(false, TestConstants.DefaultTestTimeoutMs),
            "The menu did not close after an item was selected.");

        return Task.CompletedTask;
    }

    /// <summary>10. Opening twice toggles closed again.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Control", "Menu")]
    public Task Menu_OpenTwice_Toggles()
    {
        Page.ActionsMenu.Open();
        Assert.True(Page.MenuItemsHost.WaitExists(true, TestConstants.DefaultTestTimeoutMs));

        Page.ActionsMenu.Open();
        Assert.True(Page.MenuItemsHost.WaitExists(false, TestConstants.DefaultTestTimeoutMs),
            "The menu did not close when its trigger was clicked again.");

        return Task.CompletedTask;
    }

    #endregion

    #region TabMenu

    /// <summary>11. The tab menu resolves.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Control", "TabMenu")]
    public Task TabMenu_Exists()
    {
        Page.Tabs.AssertExists();
        return Task.CompletedTask;
    }

    /// <summary>12. Select activates a tab by its caption.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Select")]
    public Task TabMenu_Select_ActivatesTheNamedTab()
    {
        Page.Tabs.Select("Search");

        Page.SelectedTab.AssertText("Search");
        Page.LastAction.AssertText("Tab/Search");

        return Task.CompletedTask;
    }

    /// <summary>13. Selecting a different tab moves the selection.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Select")]
    public Task TabMenu_SelectAnother_MovesSelection()
    {
        Page.Tabs.Select("Home");
        Page.SelectedTab.AssertText("Home");

        Page.Tabs.Select("Profile");
        Page.SelectedTab.AssertText("Profile");

        return Task.CompletedTask;
    }

    /// <summary>14. TrySelect reports false for an unknown caption rather than throwing.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "TrySelect")]
    public Task TabMenu_TrySelect_UnknownCaption_ReturnsFalse()
    {
        Assert.False(Page.Tabs.TrySelect("Nonexistent", TestConstants.ShortTestTimeoutMs));

        // Nothing was selected, so the recorded state is untouched.
        Page.SelectedTab.AssertText("none");

        return Task.CompletedTask;
    }

    /// <summary>15. Select throws for an unknown caption.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Select")]
    public Task TabMenu_Select_UnknownCaption_Throws()
    {
        Assert.Throws<ElementNotFoundException>(
            () => Page.Tabs.Select("Nonexistent", TestConstants.ShortTestTimeoutMs));

        return Task.CompletedTask;
    }

    #endregion

    #region Fixture contract

    /// <summary>16. Reset restores the initial state, so tests are order-independent.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "Reset")]
    public Task Reset_RestoresInitialState()
    {
        Page.PrimaryToolbar.ClickToolbarItem(Locator.ByAutomationId("ToolbarSaveButton"));
        Page.Tabs.Select("Home");

        Page.ResetButton.Click();

        Page.LastAction.AssertText("none");
        Page.ActionCount.AssertText("0");
        Page.SelectedTab.AssertText("none");

        return Task.CompletedTask;
    }

    #endregion
}
