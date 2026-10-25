using Bruinell.Core.Testing;
using Bruinell.Html.Bunit;
using Bruinell.Html.Context;
using Bruinell.Html.Playwright;
using Bruinell.Html.Testing;
using Bruinell.Samples.Todo.Core.Services;
using Bruinell.Samples.Todo.Infrastructure;
using Bruinell.Samples.Todo.Infrastructure.Data;
using Bruinell.Samples.Todo.Web.Services;
using Bruinell.Samples.Todo.Web.UITests.Spike;
using Microsoft.Extensions.DependencyInjection;

namespace Bruinell.Samples.Todo.Web.UITests;

/// <summary>
/// Where one device's app runs: its own host process behind a browser, or its composition rendered by bUnit
/// (<see cref="WebHeads"/>, <c>BRUINELL_HEAD=bunit</c>). Started again on a restart with the device's settings.
/// </summary>
public interface ITodoWebHead : IAsyncDisposable
{
    /// <summary>The context page objects are built on.</summary>
    IHtmlTestContext Context { get; }

    /// <summary>The app's address.</summary>
    string BaseUrl { get; }

    /// <summary>The time the app spent in its repositories, on a head that measures it; else null.</summary>
    DatabaseClock? Clock { get; }

    /// <summary>Starts the app with <paramref name="settings"/>, stopping it first if it runs.</summary>
    void Start(LaunchSettings settings);
}

/// <summary>The device's head for this run.</summary>
public static class TodoWebHeads
{
    public static async Task<ITodoWebHead> CreateAsync(string name)
        => WebHeads.IsBunit ? new BunitTodoWebHead(name) : await BrowserTodoWebHead.CreateAsync(name);
}

/// <summary>The app as its own process (<see cref="WebAppHost"/>), one browser context that outlives restarts.</summary>
public sealed class BrowserTodoWebHead : ITodoWebHead
{
    private readonly string _name;
    private readonly PlaywrightTestContext _context;
    private WebAppHost? _host;

    private BrowserTodoWebHead(string name, PlaywrightTestContext context)
    {
        _name = name;
        _context = context;
    }

    public static async Task<BrowserTodoWebHead> CreateAsync(string name)
        => new(name, await PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions
        {
            Headless = !string.Equals(Environment.GetEnvironmentVariable("HEADLESS"), "false", StringComparison.OrdinalIgnoreCase),
            Name = name,
        }));

    public IHtmlTestContext Context => _context;

    public string BaseUrl => _host?.BaseUrl ?? throw new InvalidOperationException($"{_name} is not running.");

    // The app runs in its own process: its repositories are not reachable from here.
    public DatabaseClock? Clock => null;

    public void Start(LaunchSettings settings)
    {
        _host?.Dispose();
        _host = WebAppHost.Start(new WebAppHostOptions
        {
            AppPath = WebApp.Assembly(),
            Environment = settings.ToVariables(),
            ReadyPath = "/health",
            Name = $"Todo web ({_name})",
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        _host?.Dispose();
    }
}

/// <summary>
/// The app's composition (<c>Program.cs</c>'s registrations) as the fallback of a bUnit context over its
/// <c>Routes</c>: the singletons outlive page loads as in the host, and a restart builds them again. A restart
/// makes a new context, so page objects made before it are made again from <see cref="Context"/>.
/// </summary>
public sealed class BunitTodoWebHead(string name) : ITodoWebHead
{
    private const string Address = "http://localhost";
    private ServiceProvider? _services;
    private BunitTestContext? _context;

    public IHtmlTestContext Context => _context ?? throw new InvalidOperationException($"{name} is not running.");

    public string BaseUrl => Address;

    public DatabaseClock? Clock { get; private set; }

    public void Start(LaunchSettings settings)
    {
        Stop();
        Clock = new DatabaseClock();
        _services = Compose(settings, Clock);
        _context = new BunitTestContext(new BunitTestContextOptions
        {
            RootComponent = typeof(Web.Components.Routes),
            StartBlank = true,
            FallbackServices = _services,
            BusyScript = "bruinellTodo.setBusy",
            Name = name,
        });
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        return ValueTask.CompletedTask;
    }

    // Program.cs's registrations, with the repositories timed for the spike's measurement (.my/sql-replay/plan.md, 0.4).
    private static ServiceProvider Compose(LaunchSettings settings, DatabaseClock clock)
    {
        var services = new ServiceCollection();
        services.AddTodoServices(settings);
        services.AddSingleton<WebNavigator>();
        services.AddSingleton<INavigator>(provider => provider.GetRequiredService<WebNavigator>());
        services.AddSingleton<WebDialogs>();
        services.AddSingleton<IDialogs>(provider => provider.GetRequiredService<WebDialogs>());

        // The SQL log (BRUINELL_SQL_LOG): every command the repositories send, through Bruinell's ADO.NET wrapper.
        if (Bruinell.Mocking.Data.SqlLog.FromEnvironment() is { } log)
        {
            services.AddSingleton(new TodoDatabase(settings.DatabasePath) { Wrap = connection => new Bruinell.Mocking.Data.LoggingDbConnection(connection, log) });
        }

        services.AddSingleton(clock);
        services.AddSingleton<SqliteTodoRepository>();
        services.AddSingleton<ITodoRepository>(provider => new TimedTodoRepository(provider.GetRequiredService<SqliteTodoRepository>(), clock));
        services.AddSingleton<SqliteUserRepository>();
        services.AddSingleton<IUserRepository>(provider => new TimedUserRepository(provider.GetRequiredService<SqliteUserRepository>(), clock));
        return services.BuildServiceProvider();
    }

    private void Stop()
    {
        _context?.Dispose();
        _context = null;
        _services?.Dispose();
        _services = null;
    }
}
