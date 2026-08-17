using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using WinUIAutomation = Microsoft.UI.Xaml.Automation;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

/// <summary>
/// Configures TabbedPage handler to properly map AutomationId to NavigationViewItem tab elements.
/// 
/// This fixes GitHub issue dotnet/maui#3996 where TabbedPage tabs don't expose AutomationId
/// on Windows. The MAUI handler creates NavigationViewItem elements for tabs but doesn't
/// set AutomationProperties.AutomationId on them.
/// </summary>
/// <remarks>
/// Usage in MauiProgram.cs:
/// <code>
/// #if WINDOWS
///     TabbedPageAutomationMapper.Configure();
/// #endif
/// </code>
/// 
/// Then in XAML, set AutomationId on child ContentPages:
/// <code>
/// &lt;TabbedPage&gt;
///     &lt;ContentPage Title="Tab 1" AutomationId="Tab1Page"&gt;...&lt;/ContentPage&gt;
///     &lt;ContentPage Title="Tab 2" AutomationId="Tab2Page"&gt;...&lt;/ContentPage&gt;
/// &lt;/TabbedPage&gt;
/// </code>
/// 
/// Appium can then find tabs:
/// <code>
/// var tab1 = driver.FindElement(MobileBy.AccessibilityId("Tab1Page"));
/// tab1.Click();
/// </code>
/// </remarks>
public static class TabbedPageAutomationMapper
{
    private static bool _configured = false;

    /// <summary>
    /// Registers the TabbedPage handler customization.
    /// Call this once during app startup in MauiProgram.cs.
    /// </summary>
    public static void Configure()
    {
        if (_configured)
            return;

        _configured = true;

        // Use AppendToMapping to add our customization after the default mappings
        // TabbedPage uses TabbedViewHandler in MAUI
        TabbedViewHandler.Mapper.AppendToMapping("AutomationIdFix", MapAutomationIds);
    }

    /// <summary>
    /// Maps AutomationId from child ContentPages to their NavigationViewItem tabs.
    /// </summary>
    private static void MapAutomationIds(ITabbedViewHandler handler, ITabbedView tabbedView)
    {
        try
        {
            // The platform view is a NavigationView on Windows
            if (handler.PlatformView is not NavigationView navigationView)
            {
                System.Diagnostics.Debug.WriteLine("[TabbedPageAutomationMapper] PlatformView is not NavigationView");
                return;
            }

            // Get the TabbedPage from the virtual view
            if (tabbedView is not TabbedPage tabbedPage)
            {
                System.Diagnostics.Debug.WriteLine("[TabbedPageAutomationMapper] VirtualView is not TabbedPage");
                return;
            }

            // The NavigationView's MenuItems contain the tab items
            var menuItems = navigationView.MenuItems;
            var children = tabbedPage.Children;

            // Map each child's AutomationId to the corresponding NavigationViewItem
            for (int i = 0; i < children.Count && i < menuItems.Count; i++)
            {
                var child = children[i];
                var menuItem = menuItems[i];

                if (menuItem is NavigationViewItem navItem && !string.IsNullOrEmpty(child.AutomationId))
                {
                    WinUIAutomation.AutomationProperties.SetAutomationId(navItem, child.AutomationId);
                    System.Diagnostics.Debug.WriteLine($"[TabbedPageAutomationMapper] Set AutomationId '{child.AutomationId}' on tab {i}");
                }
            }
        }
        catch (Exception ex)
        {
            // Don't throw - gracefully degrade if structure is unexpected
            System.Diagnostics.Debug.WriteLine($"[TabbedPageAutomationMapper] Error mapping automation IDs: {ex.Message}");
        }
    }
}
