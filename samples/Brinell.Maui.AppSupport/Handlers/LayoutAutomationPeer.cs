using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Brinell.Maui.AppSupport.Handlers;

/// <summary>
/// AutomationPeer for layout containers (Grid, StackLayout, FlexLayout, …) that
/// exposes the control to UI Automation.
/// </summary>
/// <remarks>
/// Standard MAUI layouts use <c>LayoutPanel</c>, which derives from WinUI
/// <c>Panel</c>. Panel has no AutomationPeer, so a layout's AutomationId is invisible
/// to UIA tools (FlaUI, WinAppDriver, Appium). This peer surfaces it as a Group.
/// </remarks>
public class LayoutAutomationPeer : FrameworkElementAutomationPeer
{
    public LayoutAutomationPeer(FrameworkElement owner)
        : base(owner)
    {
    }

    protected override string GetClassNameCore() => "Layout";

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Group;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;
}
