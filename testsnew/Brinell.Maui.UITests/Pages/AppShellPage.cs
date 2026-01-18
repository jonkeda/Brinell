using Brinell.Maui.Controls;
using Brinell.Maui.Pages;
using OpenQA.Selenium;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for MAUI Shell (AppShell) providing flyout navigation.
/// FlyoutItems are located by Title using XPath @Name strategy.
/// </summary>
public class AppShellPage : MauiPageObjectBase<AppShellPage>
{
    // Locators for shell elements
    private static readonly Locator FlyoutTitleLocator = new(LocatorStrategy.AccessibilityId, "FlyoutTitle");
    private static readonly Locator MenuScrollViewerLocator = new(LocatorStrategy.AccessibilityId, "MenuItemsScrollViewer");
    
    // Scroll viewer control for flyout menu
    private readonly MauiControlBase<AppShellPage> _menuScrollViewer;

    public AppShellPage(IMauiTestContext context)
        : base(context)
    {
        // Initialize scroll viewer control
        _menuScrollViewer = new MauiControlBase<AppShellPage>(this, MenuScrollViewerLocator);
        
        // Initialize flyout items - use Title property (becomes @Name in UI tree)
        MainFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "Main");
        DashboardFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "Dashboard");
        UserFormFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "User Form");
        DataGridFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "Data Grid");
        MediaGalleryFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "Media Gallery");
        NavigationFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "Navigation");
        ValidationFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "Validation");
        AdvancedFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "Advanced");
        ContainerDemoFlyout = new MauiFlyoutItemControl<AppShellPage>(this, "Container Demo");
    }

    /// <inheritdoc />
    public override string Name => "AppShell";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Shell is loaded if we can find the flyout title - uses framework's TryFindElement
        return TryFindElement(FlyoutTitleLocator) != null;
    }

    #region Flyout Items

    /// <summary>Main page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> MainFlyout { get; }

    /// <summary>Dashboard page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> DashboardFlyout { get; }

    /// <summary>User Form page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> UserFormFlyout { get; }

    /// <summary>Data Grid page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> DataGridFlyout { get; }

    /// <summary>Media Gallery page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> MediaGalleryFlyout { get; }

    /// <summary>Navigation Demo page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> NavigationFlyout { get; }

    /// <summary>Validation page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> ValidationFlyout { get; }

    /// <summary>Advanced page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> AdvancedFlyout { get; }

    /// <summary>Container Demo page flyout item.</summary>
    public MauiFlyoutItemControl<AppShellPage> ContainerDemoFlyout { get; }

    #endregion

    #region Scroll Helpers

    /// <summary>
    /// Scrolls the flyout menu to the bottom to reveal items like Container Demo.
    /// Uses framework control for element access.
    /// </summary>
    public AppShellPage ScrollFlyoutToBottom()
    {
        if (_menuScrollViewer.IsExists())
        {
            _menuScrollViewer.SendKeys(Keys.End);
        }
        
        return this;
    }

    /// <summary>
    /// Scrolls the flyout menu to the top.
    /// Uses framework control for element access.
    /// </summary>
    public AppShellPage ScrollFlyoutToTop()
    {
        if (_menuScrollViewer.IsExists())
        {
            _menuScrollViewer.SendKeys(Keys.Home);
        }
        
        return this;
    }

    #endregion
}
