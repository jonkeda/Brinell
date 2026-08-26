using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Brinell.Maui.AppSupport.Handlers;

/// <summary>
/// AutomationPeer for content containers (ContentView, Border) that exposes the
/// control to UI Automation.
/// </summary>
/// <remarks>
/// Standard MAUI ContentView and Border use <c>ContentPanel</c>, which has no
/// AutomationPeer, so their AutomationId is never visible to UIA tools. This peer
/// surfaces it as a Group.
/// </remarks>
public class ContentViewAutomationPeer : FrameworkElementAutomationPeer
{
    public ContentViewAutomationPeer(FrameworkElement owner)
        : base(owner)
    {
    }

    protected override string GetClassNameCore() => "ContentView";

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Group;

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;
}
