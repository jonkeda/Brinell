using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Hosting;

#if WINDOWS
using Brinell.Maui.AppSupport;
using Brinell.Samples.Maui.App.Platforms.Windows.Handlers;
#endif

namespace Brinell.Samples.Maui.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureMauiHandlers(handlers =>
            {
#if WINDOWS
                // Makes Grid, the stack layouts, FlexLayout, AbsoluteLayout,
                // ContentView, and Border expose their AutomationId to UI Automation.
                // Without this, Brinell container objects cannot resolve on Windows.
                //
                // Referenced here as a project; copying AppSupport's sources into the
                // app is the equally supported alternative.
                handlers.AddBrinellAutomationHandlers();
#endif
            });

#if WINDOWS
        // Configure TabbedPage to properly map AutomationId to tab elements
        // This fixes dotnet/maui#3996 where tabs don't expose AutomationId
        TabbedPageAutomationMapper.Configure();

#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
