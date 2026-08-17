namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

/// <summary>
/// AutomationPeer for ContentView that exposes the control to UI Automation.
/// Standard MAUI ContentView uses ContentPanel which lacks an AutomationPeer,
/// so AutomationId is never visible to UIA tools (FlaUI, WinAppDriver, Appium).
/// This peer surfaces the AutomationId as a Group control type.
/// </summary>
public class ContentViewAutomationPeer : FrameworkElementAutomationPeer
{
    public ContentViewAutomationPeer(Microsoft.UI.Xaml.FrameworkElement owner)
        : base(owner)
    {
    }

    protected override string GetClassNameCore() => "ContentView";

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Group;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;
}
