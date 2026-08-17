using System.Linq;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUIAutomation = Microsoft.UI.Xaml.Automation;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

/// <summary>
/// Configures the Shell handler to map ShellContent.AutomationId onto the
/// generated NavigationViewItem tab elements on Windows, mirroring the fix
/// applied for TabbedPage in <see cref="TabbedPageAutomationMapper"/>.
///
/// Unlike TabbedViewHandler, Shell's Windows renderer (ShellItemHandler) binds
/// NavigationView.MenuItems to internal NavigationViewItemViewModel instances via
/// a MenuItemTemplate ("TabBarNavigationViewMenuItem"), not directly to
/// NavigationViewItem. The generated NavigationViewItem containers only exist once
/// the NavigationView has realized its item containers, so this mapper must run
/// after layout (Loaded), not immediately when items change.
/// </summary>
/// <remarks>
/// Usage in MauiProgram.cs:
/// <code>
/// #if WINDOWS
///     ShellAutomationMapper.Configure();
/// #endif
/// </code>
///
/// Then in XAML, set AutomationId on ShellContent:
/// <code>
/// &lt;ShellContent Title="Buttons" AutomationId="ButtonsTab" ... /&gt;
/// </code>
/// </remarks>
public static class ShellAutomationMapper
{
    private static bool _configured;

    public static void Configure()
    {
        if (_configured)
            return;

        _configured = true;

        ShellHandler.Mapper.AppendToMapping("AutomationIdFix", MapAutomationIds);
    }

    private static void MapAutomationIds(ShellHandler handler, Shell shell)
    {
        if (handler.PlatformView is not FrameworkElement shellView)
            return;

        // NavigationView items are realized asynchronously; wait until the
        // ShellView (and its internal NavigationView) has finished loading
        // before walking MenuItems/ContainerFromItem.
        shellView.Loaded += (_, _) => TryMapAutomationIds(shellView, shell);
        if (shellView.IsLoaded)
            TryMapAutomationIds(shellView, shell);
    }

    private static void TryMapAutomationIds(FrameworkElement shellView, Shell shell)
    {
        try
        {
            var navigationView = FindDescendant<NavigationView>(shellView);
            if (navigationView is null)
            {
                System.Diagnostics.Debug.WriteLine("[ShellAutomationMapper] NavigationView not found");
                return;
            }

            foreach (var shellItem in shell.Items)
            {
                foreach (var shellSection in shellItem.Items)
                {
                    // Single-ShellContent sections are what TabBar renders as a tab;
                    // the section itself carries the visible title/icon in that case.
                    var automationId = shellSection.CurrentItem?.AutomationId;
                    if (string.IsNullOrEmpty(automationId))
                        continue;

                    var container = navigationView.MenuItems
                        .OfType<NavigationViewItem>()
                        .FirstOrDefault(item => ReferenceEquals(item.DataContext, shellSection)
                            || string.Equals(item.Content?.ToString(), shellSection.Title, StringComparison.Ordinal));

                    if (container is not null)
                    {
                        WinUIAutomation.AutomationProperties.SetAutomationId(container, automationId);
                        System.Diagnostics.Debug.WriteLine($"[ShellAutomationMapper] Set AutomationId '{automationId}' on tab '{shellSection.Title}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAutomationMapper] Error mapping automation IDs: {ex.Message}");
        }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                return match;

            var descendant = FindDescendant<T>(child);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }
}
