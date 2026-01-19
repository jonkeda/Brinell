using Microsoft.UI.Xaml.Automation.Peers;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Controls;

/// <summary>
/// AutomationPeer for AutomationContainer that exposes the control to UI Automation.
/// This enables Appium/WinAppDriver to find the container by AutomationId.
/// </summary>
/// <remarks>
/// Standard MAUI layout controls (Grid, StackLayout, Border, ContentView) don't have
/// AutomationPeers on Windows because "a Panel has no peer because it is providing 
/// a layout behavior that is visual only" (Microsoft documentation).
/// 
/// This peer provides:
/// - AutomationId exposure (inherited from FrameworkElementAutomationPeer)
/// - Proper control type (Group)
/// - Class name for debugging
/// </remarks>
public class AutomationContainerPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Creates a new AutomationContainerPeer for the specified element.
    /// </summary>
    /// <param name="owner">The framework element this peer represents.</param>
    public AutomationContainerPeer(Microsoft.UI.Xaml.FrameworkElement owner) 
        : base(owner)
    {
    }

    /// <summary>
    /// Returns the class name for this automation element.
    /// </summary>
    protected override string GetClassNameCore() => "AutomationContainer";

    /// <summary>
    /// Returns the control type for this automation element.
    /// Group is appropriate for container controls that logically group other elements.
    /// </summary>
    protected override AutomationControlType GetAutomationControlTypeCore() 
        => AutomationControlType.Group;

    /// <summary>
    /// Returns whether this element is a content element (yes, it contains content).
    /// </summary>
    protected override bool IsContentElementCore() => true;

    /// <summary>
    /// Returns whether this element is a control element (yes, it's a control).
    /// </summary>
    protected override bool IsControlElementCore() => true;
}
