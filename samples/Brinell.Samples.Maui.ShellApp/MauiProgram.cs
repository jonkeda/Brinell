using Microsoft.Extensions.Logging;

#if WINDOWS
using Brinell.Maui.AppSupport;
#endif

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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
