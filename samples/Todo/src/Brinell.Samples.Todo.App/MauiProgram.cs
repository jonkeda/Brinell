using Brinell.Maui.AppSupport;
using Brinell.Samples.Todo.App.Pages;
using Brinell.Samples.Todo.App.Services;
using Brinell.Samples.Todo.Core.Services;
using Brinell.Samples.Todo.Infrastructure;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace Brinell.Samples.Todo.App;

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
                // Makes layouts, ContentView and Border expose their AutomationId, so Brinell
                // containers (the list rows, SectionCard, the status control) resolve on Windows.
                handlers.AddBrinellAutomationHandlers();
#endif
            });

        // Off unless this build has the bridge and the test run asks for it. Unconditional, as in
        // the sample app: the gates decide, not the call site.
        builder.UseBrinellGestureBridge();

        // Every setting a test may change, read once. A normal launch sets none and gets the
        // defaults: the app's own database file and the development API.
        var settings = LaunchSettings.From(
            Environment.GetEnvironmentVariable,
            Path.Combine(FileSystem.AppDataDirectory, "todo.db"));

        builder.Services.AddTodoServices(settings, new TodoPlatform(
            DeviceConnectivity: new DeviceConnectivity(),
            Dispatch: MainThread.BeginInvokeOnMainThread));

        builder.Services.AddSingleton<INavigator, ShellNavigator>();
        builder.Services.AddSingleton<IDialogs, ShellDialogs>();

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<TodoListPage>();
        builder.Services.AddTransient<TodoDetailPage>();
        builder.Services.AddTransient<TodoEditPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
