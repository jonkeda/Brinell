using Bruinell.Maui.AppSupport;
using Bruinell.Samples.Todo.App.Pages;
using Bruinell.Samples.Todo.App.Services;
using Bruinell.Samples.Todo.Core.Services;
using Bruinell.Samples.Todo.Infrastructure;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace Bruinell.Samples.Todo.App;

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
                // Makes layouts, ContentView and Border expose their AutomationId, so Bruinell
                // containers (the list rows, SectionCard, the status control) resolve on Windows.
                handlers.AddBruinellAutomationHandlers();
#endif
            });

        // Off unless this build has the bridge and the test run asks for it. Unconditional, as in
        // the sample app: the gates decide, not the call site.
        builder.UseBruinellGestureBridge();

        // Every setting a test may change, read once, on first use: on Android the launch intent
        // that carries them does not exist yet while the container is built (see LaunchValues).
        // A normal launch sets none and gets the defaults: the app's own database file and the
        // development API.
        builder.Services.AddTodoServices(
            _ => LaunchSettings.From(LaunchValues.Read, Path.Combine(FileSystem.AppDataDirectory, "todo.db")),
            new TodoPlatform(
                DeviceConnectivity: new DeviceConnectivity(),
                Dispatch: MainThread.BeginInvokeOnMainThread));

        builder.Services.AddSingleton<INavigator, ShellNavigator>();
        builder.Services.AddSingleton<IDialogs, ShellDialogs>();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<TodoListPage>();
        builder.Services.AddTransient<TodoDetailPage>();
        builder.Services.AddTransient<TodoEditPage>();
        builder.Services.AddTransient<UserListPage>();
        builder.Services.AddTransient<UserEditPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
