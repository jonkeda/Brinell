using Brinell.Samples.Maui.App.Controls;
using Microsoft.Maui.Handlers;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Controls;

/// <summary>
/// Windows handler for AutomationContainer that uses AutomationContentPanel
/// to provide proper UI Automation support.
/// </summary>
/// <remarks>
/// This handler extends ContentViewHandler but overrides CreatePlatformView
/// to return our custom AutomationContentPanel instead of the standard ContentPanel.
/// The AutomationContentPanel provides an AutomationPeer that exposes the container
/// to UI Automation tools like Appium/WinAppDriver.
/// </remarks>
public class AutomationContainerHandler : ContentViewHandler
{
    /// <summary>
    /// Creates the platform-specific view (AutomationContentPanel instead of ContentPanel).
    /// </summary>
    /// <returns>An AutomationContentPanel that supports UI Automation.</returns>
    protected override Microsoft.Maui.Platform.ContentPanel CreatePlatformView()
    {
        return new AutomationContentPanel();
    }
}
