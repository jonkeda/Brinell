using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Controls;

/// <summary>
/// A ContentPanel that provides a custom AutomationPeer for UI Automation discovery.
/// This panel is used by AutomationContainerHandler to enable automation ID exposure.
/// </summary>
/// <remarks>
/// The standard ContentPanel does not override OnCreateAutomationPeer, which means
/// it uses the default Panel behavior (no peer). By overriding OnCreateAutomationPeer,
/// we ensure the panel is visible to UI Automation tools.
/// </remarks>
public class AutomationContentPanel : ContentPanel
{
    /// <summary>
    /// Creates a new AutomationContentPanel.
    /// </summary>
    public AutomationContentPanel()
    {
    }

    /// <summary>
    /// Creates the automation peer for this panel.
    /// This is the key override that enables UI Automation discovery.
    /// </summary>
    /// <returns>An AutomationContainerPeer that exposes this panel to automation.</returns>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new AutomationContainerPeer(this);
    }
}
