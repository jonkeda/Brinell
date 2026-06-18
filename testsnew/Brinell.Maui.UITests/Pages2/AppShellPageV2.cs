using Brinell.Maui.Extensions.Controls.Navigation;

namespace Brinell.Maui.UITests.Pages2;

/// <summary>
/// Page object for MAUI Shell-based navigation using TabBar.
/// Uses Shell control object and individual ShellContent controls to manage tab navigation and state.
/// </summary>
public class AppShellPageV2 : PageObjectBase<AppShellPageV2>
{
    private readonly Shell<AppShellPageV2> _shell;

    public AppShellPageV2(IMauiTestContext context)
        : base(context)
    {
        _shell = new Shell<AppShellPageV2>(this, "AppShell");

        // Initialize ShellContent controls by route
        ButtonsContent = new ShellContent<AppShellPageV2>(this, "ButtonsShell", "Buttons");
        DateTimeContent = new ShellContent<AppShellPageV2>(this, "DateTimePage", "DateTime");
        TextContent = new ShellContent<AppShellPageV2>(this, "TextPage", "Text");
        RangeContent = new ShellContent<AppShellPageV2>(this, "RangePage", "Range");
        BasicsContent = new ShellContent<AppShellPageV2>(this, "BasicsPage", "Basics");
        ContainersContent = new ShellContent<AppShellPageV2>(this, "ContainersPage", "Containers");
        FormsContent = new ShellContent<AppShellPageV2>(this, "FormsPage", "Forms");
        ListsContent = new ShellContent<AppShellPageV2>(this, "ListsPage", "Lists");
        GesturesContent = new ShellContent<AppShellPageV2>(this, "GesturesPage", "Gestures");
        NavigationContent = new ShellContent<AppShellPageV2>(this, "NavigationPage", "Navigation");
        ToolkitContent = new ShellContent<AppShellPageV2>(this, "ToolkitPage", "Toolkit");
        MediaContent = new ShellContent<AppShellPageV2>(this, "MediaPage", "Media");
        DataGridContent = new ShellContent<AppShellPageV2>(this, "DataGridPage", "DataGrid");
    }

    /// <inheritdoc />
    public override string Name => "AppShell";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return _shell.IsLoaded();
    }

    #region Shell Properties

    /// <summary>
    /// Gets the Shell control object for direct shell interaction.
    /// </summary>
    public Shell<AppShellPageV2> ShellControl => _shell;

    #endregion

    #region ShellContent Controls

    /// <summary>
    /// Buttons ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> ButtonsContent { get; }

    /// <summary>
    /// DateTime ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> DateTimeContent { get; }

    /// <summary>
    /// Text ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> TextContent { get; }

    /// <summary>
    /// Range ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> RangeContent { get; }

    /// <summary>
    /// Basics ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> BasicsContent { get; }

    /// <summary>
    /// Containers ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> ContainersContent { get; }

    /// <summary>
    /// Forms ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> FormsContent { get; }

    /// <summary>
    /// Lists ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> ListsContent { get; }

    /// <summary>
    /// Gestures ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> GesturesContent { get; }

    /// <summary>
    /// Navigation ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> NavigationContent { get; }

    /// <summary>
    /// Toolkit ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> ToolkitContent { get; }

    /// <summary>
    /// Media ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> MediaContent { get; }

    /// <summary>
    /// DataGrid ShellContent control.
    /// </summary>
    public ITabControlObject<AppShellPageV2> DataGridContent { get; }

    #endregion

    #region Tab Navigation

    /// <summary>
    /// Navigates to the Buttons tab.
    /// </summary>
    public AppShellPageV2 GoToButtonsTab()
    {
        return _shell.NavigateTo("Buttons");
    }

    /// <summary>
    /// Navigates to the DateTime tab.
    /// </summary>
    public AppShellPageV2 GoToDateTimeTab()
    {
        return _shell.NavigateTo("DateTime");
    }

    /// <summary>
    /// Navigates to the Text tab.
    /// </summary>
    public AppShellPageV2 GoToTextTab()
    {
        return _shell.NavigateTo("Text");
    }

    /// <summary>
    /// Navigates to the Range tab.
    /// </summary>
    public AppShellPageV2 GoToRangeTab()
    {
        return _shell.NavigateTo("Range");
    }

    /// <summary>
    /// Navigates to the Basics tab.
    /// </summary>
    public AppShellPageV2 GoToBasicsTab()
    {
        return _shell.NavigateTo("Basics");
    }

    /// <summary>
    /// Navigates to the Containers tab.
    /// </summary>
    public AppShellPageV2 GoToContainersTab()
    {
        return _shell.NavigateTo("Containers");
    }

    /// <summary>
    /// Navigates to the Forms tab.
    /// </summary>
    public AppShellPageV2 GoToFormsTab()
    {
        return _shell.NavigateTo("Forms");
    }

    /// <summary>
    /// Navigates to the Lists tab.
    /// </summary>
    public AppShellPageV2 GoToListsTab()
    {
        return _shell.NavigateTo("Lists");
    }

    /// <summary>
    /// Navigates to the Gestures tab.
    /// </summary>
    public AppShellPageV2 GoToGesturesTab()
    {
        return _shell.NavigateTo("Gestures");
    }

    /// <summary>
    /// Navigates to the Navigation tab.
    /// </summary>
    public AppShellPageV2 GoToNavigationTab()
    {
        return _shell.NavigateTo("Navigation");
    }

    /// <summary>
    /// Navigates to the Toolkit tab.
    /// </summary>
    public AppShellPageV2 GoToToolkitTab()
    {
        return _shell.NavigateTo("Toolkit");
    }

    /// <summary>
    /// Navigates to the Media tab.
    /// </summary>
    public AppShellPageV2 GoToMediaTab()
    {
        return _shell.NavigateTo("Media");
    }

    /// <summary>
    /// Navigates to the DataGrid tab.
    /// </summary>
    public AppShellPageV2 GoToDataGridTab()
    {
        return _shell.NavigateTo("DataGrid");
    }

    #endregion

    #region Tab Assertions

    /// <summary>
    /// Asserts that a specific tab is selected.
    /// </summary>
    public AppShellPageV2 AssertTabIsSelected(string tabTitle)
    {
        return _shell.AssertTabSelected(tabTitle, true);
    }

    /// <summary>
    /// Asserts that a specific tab is not selected.
    /// </summary>
    public AppShellPageV2 AssertTabIsNotSelected(string tabTitle)
    {
        return _shell.AssertTabSelected(tabTitle, false);
    }

    /// <summary>
    /// Asserts that the Shell is loaded.
    /// </summary>
    public AppShellPageV2 AssertShellLoaded()
    {
        return _shell.AssertLoaded();
    }

    #endregion
}
