using Brinell.Maui.CommunityToolkit.Controls;
using Brinell.Maui.Controls.Navigation;

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
        ButtonsTab = new ShellContent<AppShellPage>(this, "ButtonsTab", "Buttons");
        DateTimeTab = new ShellContent<AppShellPage>(this, "DateTimeTab", "DateTime");
        DisplayTab = new ShellContent<AppShellPage>(this, "DisplayTab", "Display");
        RangeTab = new ShellContent<AppShellPage>(this, "RangeTab", "Range");
        SelectionTab = new ShellContent<AppShellPage>(this, "SelectionTab", "Selection");
        TextTab = new ShellContent<AppShellPage>(this, "TextTab", "Text");
        ToggleTab = new ShellContent<AppShellPage>(this, "ToggleTab", "Toggle");
        GridCollectionTab = new ShellContent<AppShellPage>(this, "GridCollectionTab", "Containers");
        AutomationProbeTab = new ShellContent<AppShellPage>(this, "AutomationProbeTab", "Probe");
    }

    /// <inheritdoc />
    public override string Name => "AppShell";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return ButtonsTab.IsExists();
    }

    #region Tab Controls

    /// <summary>Buttons page tab (default/first tab).</summary>
    public ShellContent<AppShellPage> ButtonsTab { get; }

    /// <summary>DateTime page tab.</summary>
    public ShellContent<AppShellPage> DateTimeTab { get; }

    /// <summary>Display page tab.</summary>
    public ShellContent<AppShellPage> DisplayTab { get; }

    /// <summary>Range page tab.</summary>
    public ShellContent<AppShellPage> RangeTab { get; }

    /// <summary>Selection page tab.</summary>
    public ShellContent<AppShellPage> SelectionTab { get; }

    /// <summary>Text page tab.</summary>
    public ShellContent<AppShellPage> TextTab { get; }

    /// <summary>Toggle page tab.</summary>
    public ShellContent<AppShellPage> ToggleTab { get; }

    /// <summary>Grid + CollectionView container demo tab.</summary>
    public ShellContent<AppShellPage> GridCollectionTab { get; }

    /// <summary>Phase 0 automation probe tab.</summary>
    public ShellContent<AppShellPage> AutomationProbeTab { get; }

    #endregion
}
