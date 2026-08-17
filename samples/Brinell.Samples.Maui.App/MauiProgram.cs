using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Hosting;

#if WINDOWS
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
                // Register custom handler for AutomationContainer
                // This enables container controls to expose AutomationId to UI Automation
                handlers.AddHandler<ContentView, AutomationContentViewHandler>();
                handlers.AddHandler<Layout, AutomationLayoutHandler>();
#endif
            });

#if WINDOWS
        // Configure TabbedPage to properly map AutomationId to tab elements
        // This fixes dotnet/maui#3996 where tabs don't expose AutomationId
        TabbedPageAutomationMapper.Configure();

        // Same fix, for Shell/TabBar tabs
        ShellAutomationMapper.Configure();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
