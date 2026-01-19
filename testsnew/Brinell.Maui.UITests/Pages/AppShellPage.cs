using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.CommunityToolkit.Controls;
using Brinell.Maui.Pages;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for MAUI TabbedPage navigation.
/// Uses TabViewControl for tab items with AutomationId locators.
/// Matches the 8-tab structure in MainPage.xaml TabbedPage.
/// </summary>
public class AppShellPage : MauiPageObjectBase<AppShellPage>
{
    public AppShellPage(IMauiTestContext context)
        : base(context)
    {
        // TabViewControl uses AutomationId - fast and reliable
        // Tab AutomationIds match MainPage.xaml TabbedPage children
        BasicsTab = new TabViewControl<AppShellPage>(this, "BasicsTab");
        ContainersTab = new TabViewControl<AppShellPage>(this, "ContainersTab");
        FormsTab = new TabViewControl<AppShellPage>(this, "FormsTab");
        ListsTab = new TabViewControl<AppShellPage>(this, "ListsTab");
        GesturesTab = new TabViewControl<AppShellPage>(this, "GesturesTab");
        NavigationTab = new TabViewControl<AppShellPage>(this, "NavigationTab");
        ToolkitTab = new TabViewControl<AppShellPage>(this, "ToolkitTab");
        MediaTab = new TabViewControl<AppShellPage>(this, "MediaTab");
    }

    /// <inheritdoc />
    public override string Name => "AppShell";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return BasicsTab.IsExists();
    }

    #region Tab Controls

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

    #endregion
}
