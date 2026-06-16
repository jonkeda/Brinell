using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

/// <summary>
/// Layout handler that returns AutomationLayoutPanel instead of the
/// standard LayoutPanel. This ensures every layout control (Grid,
/// StackLayout, etc.) with an AutomationId exposes it in the Windows UIA tree.
/// </summary>
public class AutomationLayoutHandler : LayoutHandler
{
    protected override LayoutPanel CreatePlatformView()
    {
        return new AutomationLayoutPanel();
    }
}
