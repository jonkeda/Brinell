using Microsoft.UI.Xaml.Automation.Peers;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

/// <summary>
/// AutomationPeer for layout containers (Grid, StackLayout, etc.) that exposes
/// the control to UI Automation. Standard MAUI layouts use LayoutPanel which
/// inherits from Panel — Panel has no AutomationPeer, so AutomationId is
/// invisible to UIA tools (FlaUI, WinAppDriver, Appium).
/// This peer surfaces the AutomationId as a Group control type.
/// </summary>
public class LayoutAutomationPeer : FrameworkElementAutomationPeer
{
    public LayoutAutomationPeer(Microsoft.UI.Xaml.FrameworkElement owner)
        : base(owner)
    {
    }

    protected override string GetClassNameCore() => "Layout";

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Group;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;
}
