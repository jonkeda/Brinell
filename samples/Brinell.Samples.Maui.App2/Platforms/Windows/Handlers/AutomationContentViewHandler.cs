using Microsoft.Maui.Handlers;

namespace Brinell.Samples.Maui.App.Platforms.Windows.Handlers;

/// <summary>
/// ContentView handler that returns AutomationContentPanel instead of
/// the standard ContentPanel. This ensures every ContentView exposes
/// its AutomationId in the Windows UIA tree.
/// </summary>
public class AutomationContentViewHandler : ContentViewHandler
{
    protected override ContentPanel CreatePlatformView()
    {
        return new AutomationContentPanel();
    }
}
