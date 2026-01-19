using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.CommunityToolkit.Controls;
using Brinell.Maui.Pages;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for MAUI TabView navigation (migrated from Shell TabBar).
/// Uses TabViewControl for CommunityToolkit TabView with AutomationId locators.
/// </summary>
public class AppShellPage : MauiPageObjectBase<AppShellPage>
{
    public AppShellPage(IMauiTestContext context)
        : base(context)
    {
        // TabViewControl uses AutomationId - fast and reliable
        MainTab = new TabViewControl<AppShellPage>(this, "MainTab");
        DashboardTab = new TabViewControl<AppShellPage>(this, "DashboardTab");
        FormsTab = new TabViewControl<AppShellPage>(this, "FormsTab");
        DataTab = new TabViewControl<AppShellPage>(this, "DataTab");
        MediaTab = new TabViewControl<AppShellPage>(this, "MediaTab");
        NavigationTab = new TabViewControl<AppShellPage>(this, "NavigationTab");
        ValidationTab = new TabViewControl<AppShellPage>(this, "ValidationTab");
        AdvancedTab = new TabViewControl<AppShellPage>(this, "AdvancedTab");
        ContainersTab = new TabViewControl<AppShellPage>(this, "ContainersTab");
    }

    /// <inheritdoc />
    public override string Name => "AppShell";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return MainTab.IsExists();
    }

    #region Tab Controls

    /// <summary>Main page tab.</summary>
    public ITabControlObject<AppShellPage> MainTab { get; }

    /// <summary>Dashboard page tab.</summary>
    public ITabControlObject<AppShellPage> DashboardTab { get; }

    /// <summary>User Form page tab.</summary>
    public ITabControlObject<AppShellPage> FormsTab { get; }

    /// <summary>Data Grid page tab.</summary>
    public ITabControlObject<AppShellPage> DataTab { get; }

    /// <summary>Media Gallery page tab.</summary>
    public ITabControlObject<AppShellPage> MediaTab { get; }

    /// <summary>Navigation Demo page tab.</summary>
    public ITabControlObject<AppShellPage> NavigationTab { get; }

    /// <summary>Validation page tab.</summary>
    public ITabControlObject<AppShellPage> ValidationTab { get; }

    /// <summary>Advanced page tab.</summary>
    public ITabControlObject<AppShellPage> AdvancedTab { get; }

    /// <summary>Container Demo page tab.</summary>
    public ITabControlObject<AppShellPage> ContainersTab { get; }

    #endregion
}
