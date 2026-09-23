global using Brinell.Mocking;
global using Brinell.Samples.Todo.Contracts;
global using Brinell.Samples.Todo.Core.Models;
global using Brinell.Samples.Todo.Core.Services;
global using Brinell.Samples.Todo.Core.Sync;
global using Brinell.Samples.Todo.Infrastructure;
global using Brinell.Samples.Todo.TestSupport;
global using Xunit;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Brinell.Samples.Todo.IntegrationTests;

/// <summary>
/// The app without its UI: the real composition, a database file of its own and a WireMock
/// backend of its own, per test.
/// </summary>
/// <remarks>
/// <para>
/// The container is built with <see cref="TodoServiceCollectionExtensions.AddTodoServices"/>,
/// the same call <c>MauiProgram</c> makes, so these tests check the app's wiring, not a copy of
/// it. Only navigation and dialogs are substitutes: they are Shell and DisplayAlert in the app.
/// </para>
/// <para>
/// <see cref="Restart"/> throws the container away and builds a new one on the same file, which is
/// what an app restart does to everything below the UI (TOD.09.3).
/// </para>
/// </remarks>
internal sealed class TodoHost : IDisposable
{
    private ServiceProvider _services;

    private TodoHost(TodoScenario scenario, LaunchSettings settings, MockApiServer mock, TempDatabase database, TodoServerStub server)
    {
        Scenario = scenario;
        Settings = settings;
        Mock = mock;
        Database = database;
        Server = server;
        _services = Build(settings);
    }

    public TodoScenario Scenario { get; }

    public LaunchSettings Settings { get; }

    /// <summary>The WireMock server, for request assertions and per-test overrides.</summary>
    public MockApiServer Mock { get; }

    /// <summary>The fake backend's state.</summary>
    public TodoServerStub Server { get; }

    public TempDatabase Database { get; }

    public ISyncService Sync => Get<ISyncService>();

    public TodoService Todos => Get<TodoService>();

    public ITodoRepository Repository => Get<ITodoRepository>();

    /// <summary>Starts a host from a scenario: local half in the database, server half in WireMock.</summary>
    public static async Task<TodoHost> StartAsync(
        string scenarioName = "empty",
        Func<LaunchSettings, LaunchSettings>? adjust = null)
    {
        var scenario = TodoScenario.Load(scenarioName);
        var database = new TempDatabase("integration");
        var mock = MockApiServer.Start();

        await scenario.SeedDatabaseAsync(database.Path);
        var server = scenario.StubServer(mock);

        var settings = new LaunchSettings
        {
            DatabasePath = database.Path,
            ApiBaseUrl = mock.BaseUrl,
            FixedNow = scenario.Now,
            ApiTimeout = TimeSpan.FromSeconds(5),
        };

        return new TodoHost(scenario, adjust?.Invoke(settings) ?? settings, mock, database, server);
    }

    public T Get<T>()
        where T : notnull
        => _services.GetRequiredService<T>();

    /// <summary>A new container on the same database file: an app restart, below the UI.</summary>
    public void Restart()
    {
        _services.Dispose();
        _services = Build(Settings);
    }

    /// <summary>Every local todo, tombstones included.</summary>
    public Task<IReadOnlyList<TodoItem>> LocalAsync() => Repository.GetAllAsync();

    public void Dispose()
    {
        _services.Dispose();
        Mock.Dispose();
        Database.Dispose();
    }

    private static ServiceProvider Build(LaunchSettings settings)
    {
        var services = new ServiceCollection();
        services.AddTodoServices(settings);
        services.AddSingleton(Substitute.For<INavigator>());
        services.AddSingleton(Substitute.For<IDialogs>());

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
