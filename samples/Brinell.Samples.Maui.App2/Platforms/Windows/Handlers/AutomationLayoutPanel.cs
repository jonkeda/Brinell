namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;



/// <summary>
/// LayoutPanel subclass that provides a custom AutomationPeer.
/// Replaces the standard LayoutPanel (which has no peer) so that
/// layout AutomationId values are visible in the UIA tree on Windows.
/// Only exposes a peer when AutomationId is set to avoid UIA tree bloat.
/// </summary>
public class AutomationLayoutPanel : LayoutPanel
{
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new LayoutAutomationPeer(this);
    }
}
