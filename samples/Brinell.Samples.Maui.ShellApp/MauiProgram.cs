using Microsoft.Extensions.Logging;

using Brinell.Maui.AppSupport;

namespace Brinell.Samples.Maui.ShellApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureMauiHandlers(handlers =>
            {
#if WINDOWS
                // Layouts expose their AutomationId to UI Automation. Every page below marks
                // itself with a layout carrying an id, so this is what makes the pages
                // addressable on Windows at all.
                handlers.AddBrinellAutomationHandlers();
#endif
            });

        // The gesture bridge's run-time switch (step 27). Without this line the bridge compiles in
        // and publishes nothing - which is how this app's flyout verbs went silent: the gate was
        // added to the hub app and not here, and nothing noticed until this app was rebuilt.
        builder.UseBrinellGestureBridge();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
