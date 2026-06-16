using Brinell.Maui.CommunityToolkit.Controls;

namespace Brinell.Maui.UITests.Pages2;

/// <summary>
/// Page object for MAUI TabbedPage navigation.
/// Uses TabViewControl for tab items with AutomationId locators.
/// Matches the 8-tab structure in MainPage.xaml TabbedPage.
/// </summary>
/// <remarks>
/// Uses fallback locators (tab Title) for Windows TabbedPage where AutomationId
/// may not propagate to NavigationViewItem elements (see dotnet/maui#3996).
/// </remarks>
public class AppShellPage : PageObjectBase<AppShellPage>
{
    public AppShellPage(IMauiTestContext context)
        : base(context)
    {
        ButtonsTab = new TabViewControl<AppShellPage>(this, "ButtonsTab", "Buttons");
        DateTimeTab = new TabViewControl<AppShellPage>(this, "DateTimeTab", "DateTime");

        // TabViewControl uses AutomationId as primary, Title as fallback for Windows
        BasicsTab = new TabViewControl<AppShellPage>(this, "BasicsTab", "Basics");
        ContainersTab = new TabViewControl<AppShellPage>(this, "ContainersTab", "Containers");
        FormsTab = new TabViewControl<AppShellPage>(this, "FormsTab", "Forms");
        ListsTab = new TabViewControl<AppShellPage>(this, "ListsTab", "Lists");
        GesturesTab = new TabViewControl<AppShellPage>(this, "GesturesTab", "Gestures");
        NavigationTab = new TabViewControl<AppShellPage>(this, "NavigationTab", "Navigation");
        ToolkitTab = new TabViewControl<AppShellPage>(this, "ToolkitTab", "Toolkit");
        MediaTab = new TabViewControl<AppShellPage>(this, "MediaTab", "Media");
        DataGridTab = new TabViewControl<AppShellPage>(this, "DataGridTab", "DataGrid");
    }

    /// <inheritdoc />
    public override string Name => "AppShell";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return BasicsTab.IsExists();
    }

    #region Tab Controls

    /// <summary>Buttons page tab (default/first tab).</summary>
    public ITabControlObject<AppShellPage> ButtonsTab { get; }

    /// <summary>DateTime page tab.</summary>
    public ITabControlObject<AppShellPage> DateTimeTab { get; }

    /// <summary>Basics page tab (default/first tab).</summary>
    public ITabControlObject<AppShellPage> BasicsTab { get; }

    /// <summary>Container Demo page tab.</summary>
    public ITabControlObject<AppShellPage> ContainersTab { get; }

    /// <summary>Forms page tab.</summary>
    public ITabControlObject<AppShellPage> FormsTab { get; }

    /// <summary>Lists page tab.</summary>
    public ITabControlObject<AppShellPage> ListsTab { get; }

    /// <summary>Gestures page tab.</summary>
    public ITabControlObject<AppShellPage> GesturesTab { get; }

    /// <summary>Navigation Demo page tab.</summary>
    public ITabControlObject<AppShellPage> NavigationTab { get; }

    /// <summary>Toolkit page tab.</summary>
    public ITabControlObject<AppShellPage> ToolkitTab { get; }

    /// <summary>Media Gallery page tab.</summary>
    public ITabControlObject<AppShellPage> MediaTab { get; }

    /// <summary>DataGrid page tab.</summary>
    public ITabControlObject<AppShellPage> DataGridTab { get; }

    #endregion
}
