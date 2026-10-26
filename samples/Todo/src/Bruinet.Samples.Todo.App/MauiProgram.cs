using Bruinet.Maui.AppSupport;
using Bruinet.Samples.Todo.App.Services;
using Bruinet.Samples.Todo.Core.Services;
using Bruinet.Samples.Todo.Infrastructure;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace Bruinet.Samples.Todo.App;

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
                // Makes layouts, ContentView and Border expose their AutomationId, so Bruinet
                // containers (the list rows, SectionCard, the status control) resolve on Windows.
                handlers.AddBruinetAutomationHandlers();
#endif
            });

        // Off unless this build has the bridge and the test run asks for it. Unconditional, as in
        // the sample app: the gates decide, not the call site.
        builder.UseBruinetGestureBridge();

        // Every setting a test may change, read once, on first use: on Android the launch intent
        // that carries them does not exist yet while the container is built (see LaunchValues).
        // A normal launch sets none and gets the defaults: the app's own database file and the
        // development API.
        builder.Services.AddTodoApp(
            _ => LaunchSettings.From(LaunchValues.Read, Path.Combine(FileSystem.AppDataDirectory, "todo.db")),
            new TodoPlatform(
                DeviceConnectivity: new DeviceConnectivity(),
                Dispatch: MainThread.BeginInvokeOnMainThread));

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
