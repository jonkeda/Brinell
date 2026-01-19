namespace Brinell.Samples.Maui.App.Controls;

/// <summary>
/// A container control that exposes AutomationId to UI Automation on Windows.
/// 
/// Use this control instead of Grid, Border, Frame, or ContentView when you need 
/// the container to be discoverable by automation tools like Appium/WinAppDriver.
/// 
/// On Windows, standard MAUI layout controls (Grid, StackLayout, Border, ContentView, Frame)
/// do not expose AutomationId to UI Automation because they lack AutomationPeers.
/// This control provides a custom AutomationPeer that properly exposes the AutomationId.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// &lt;controls:AutomationContainer AutomationId="MyContainer"&gt;
///     &lt;VerticalStackLayout&gt;
///         &lt;Label AutomationId="ChildLabel" Text="Hello" /&gt;
///         &lt;Button AutomationId="ChildButton" Text="Click" /&gt;
///     &lt;/VerticalStackLayout&gt;
/// &lt;/controls:AutomationContainer&gt;
/// </code>
/// 
/// The container can then be found by automation tools:
/// <code>
/// var container = driver.FindElement(MobileBy.AccessibilityId("MyContainer"));
/// var button = container.FindElement(MobileBy.AccessibilityId("ChildButton"));
/// </code>
/// </remarks>
public class AutomationContainer : ContentView
{
    /// <summary>
    /// Creates a new AutomationContainer.
    /// </summary>
    public AutomationContainer()
    {
        // No special initialization needed.
        // The magic happens in the platform-specific handler which provides
        // a custom AutomationPeer on Windows.
    }
}
