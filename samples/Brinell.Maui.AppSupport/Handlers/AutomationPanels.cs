using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Brinell.Maui.AppSupport.Handlers;

/// <summary>
/// <c>LayoutPanel</c> subclass that supplies an AutomationPeer, so layout
/// AutomationId values appear in the Windows UIA tree.
/// </summary>
public class AutomationLayoutPanel : LayoutPanel
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new LayoutAutomationPeer(this);
}

/// <summary>
/// <c>ContentPanel</c> subclass that supplies an AutomationPeer, so ContentView and
/// Border AutomationId values appear in the Windows UIA tree.
/// </summary>
public class AutomationContentPanel : ContentPanel
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new ContentViewAutomationPeer(this);
}
