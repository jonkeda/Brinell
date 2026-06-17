namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

/// <summary>
/// ContentPanel subclass that provides a custom AutomationPeer.
/// Replaces the standard ContentPanel (which has no peer) so that
/// ContentView AutomationId is visible in the UIA tree on Windows.
/// </summary>
public class AutomationContentPanel : ContentPanel
{
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new ContentViewAutomationPeer(this);
    }
}
