using Bruinet.Samples.Todo.App.Pages;
using Bruinet.Samples.Todo.Core.Services;
using Bruinet.Samples.Todo.Infrastructure;

namespace Bruinet.Samples.Todo.App.Services;

/// <summary>
/// The app's services, pages and Shell: what <see cref="MauiProgram"/> registers on a device, and
/// what a test registers when it renders the app in memory (Bruinet's MauiUnit head). Kept apart
/// from the app builder because that one needs a platform.
/// </summary>
public static class TodoAppServices
{
    /// <summary>Registers the Todo services, the Shell's navigator and dialogs, the Shell and its pages.</summary>
    /// <param name="services">Where to register them.</param>
    /// <param name="settings">The launch settings: read from the launch on a device, given by a test in memory.</param>
    /// <param name="platform">The device's connectivity and main thread, or a test's.</param>
    public static IServiceCollection AddTodoApp(
        this IServiceCollection services,
        Func<IServiceProvider, LaunchSettings> settings,
        TodoPlatform platform)
    {
        services.AddTodoServices(settings, platform);

        services.AddSingleton<INavigator, ShellNavigator>();
        services.AddSingleton<IDialogs, ShellDialogs>();

        services.AddSingleton<AppShell>();
        services.AddSingleton<TodoListPage>();
        services.AddTransient<TodoDetailPage>();
        services.AddTransient<TodoEditPage>();
        services.AddTransient<UserListPage>();
        services.AddTransient<UserEditPage>();
        return services;
    }
}
