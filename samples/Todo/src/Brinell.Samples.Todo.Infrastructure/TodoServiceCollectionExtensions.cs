using Brinell.Samples.Todo.Core.Services;
using Brinell.Samples.Todo.Core.Sync;
using Brinell.Samples.Todo.Core.ViewModels;
using Brinell.Samples.Todo.Infrastructure.Api;
using Brinell.Samples.Todo.Infrastructure.Connectivity;
using Brinell.Samples.Todo.Infrastructure.Data;
using Brinell.Samples.Todo.Infrastructure.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Samples.Todo.Infrastructure;

/// <summary>
/// What the platform head adds to the composition.
/// </summary>
/// <param name="DeviceConnectivity">The device's own connectivity; <c>null</c> for always online.</param>
/// <param name="Dispatch">Runs a connectivity notification on the UI thread; <c>null</c> to raise it where it happens.</param>
/// <param name="HttpHandler">The HTTP handler to use; <c>null</c> for the default.</param>
public sealed record TodoPlatform(
    IConnectivityState? DeviceConnectivity = null,
    Action<Action>? Dispatch = null,
    HttpMessageHandler? HttpHandler = null);

/// <summary>
/// The app's one composition root.
/// </summary>
/// <remarks>
/// <c>MauiProgram</c> calls this, and so do the integration tests, which is what makes those tests
/// a test of the app's real wiring rather than of a copy. Only <see cref="INavigator"/> and
/// <see cref="IDialogs"/> are left to the caller: they are Shell and <c>DisplayAlert</c> in the app.
/// </remarks>
public static class TodoServiceCollectionExtensions
{
    /// <summary>Registers storage, the API client, sync, connectivity, the clock and the view models.</summary>
    public static IServiceCollection AddTodoServices(
        this IServiceCollection services,
        LaunchSettings settings,
        TodoPlatform? platform = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        platform ??= new TodoPlatform();

        services.AddSingleton(settings);
        services.AddSingleton(new TodoAppOptions(settings.SyncOnStart));
        services.AddSingleton<TimeProvider>(settings.FixedNow is { } now ? new FixedTimeProvider(now) : TimeProvider.System);

        services.AddSingleton(new TodoDatabase(settings.DatabasePath));
        services.AddSingleton<ITodoRepository, SqliteTodoRepository>();

        services.AddSingleton(_ => TodoApiClient.CreateHttpClient(
            settings.ApiBaseUrl, settings.ApiKey, settings.ApiTimeout, platform.HttpHandler));
        services.AddSingleton<ITodoApi>(provider => new TodoApiClient(provider.GetRequiredService<HttpClient>()));

        services.AddSingleton<IConnectivityState>(_ => CreateConnectivity(settings, platform));
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<TodoService>();

        services.AddSingleton<TodoListViewModel>();
        services.AddTransient<TodoDetailViewModel>();
        services.AddTransient<TodoEditViewModel>();

        return services;
    }

    private static IConnectivityState CreateConnectivity(LaunchSettings settings, TodoPlatform platform)
    {
        var sources = new List<IConnectivityState>();

        if (platform.DeviceConnectivity is { } device)
        {
            sources.Add(device);
        }

        if (settings.NetworkStateFile is { } file)
        {
            sources.Add(new NetworkStateFileConnectivity(file));
        }

        return sources.Count == 0 ? new AlwaysOnline() : new CompositeConnectivity(sources, platform.Dispatch);
    }
}
