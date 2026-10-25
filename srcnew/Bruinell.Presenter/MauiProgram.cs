using Bruinell.Maui.AppSupport;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace Bruinell.Presenter;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            // Layouts (the page's root grid) get an AutomationId on Windows: MAUI gives them no peer.
            .ConfigureMauiHandlers(handlers => handlers.AddBruinellAutomationHandlers());

        // Off unless the build has BRUINELL_UIA_BRIDGE and the launcher sets BRUINELL_UIA_BRIDGE=1.
        // Before the first page loads: elements publish themselves on Loaded.
        builder.UseBruinellGestureBridge();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
