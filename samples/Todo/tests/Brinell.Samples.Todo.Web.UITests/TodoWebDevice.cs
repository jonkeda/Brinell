using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Services;
using Brinell.Core.Testing;
using Brinell.Html.Context;
using Brinell.Html.Playwright;
using Brinell.Html.Testing;
using Brinell.Mocking;
using Brinell.Samples.Todo.Infrastructure;
using Brinell.Samples.Todo.Web.UITests.Pages;

namespace Brinell.Samples.Todo.Web.UITests;

/// <summary>
/// One device running the Todo web head: its own seeded SQLite file, network-state file, host
/// process and browser context. Devices share a backend; the fixture that owns the backend owns
/// the devices (<see cref="TodoWebFixture"/>).
/// </summary>
/// <remarks>
/// A restart stops the host and starts it again, on a new port, with the same database; the
/// browser context stays, so page objects built on <see cref="Context"/> keep working.
/// </remarks>
public sealed class TodoWebDevice : ITestApp
{
    private readonly List<TempDatabase> _retired = [];
    private readonly PlaywrightTestContext _context;
    private TempDatabase _database;
    private readonly NetworkStateFile _network;
    private WebAppHost? _host;
    private LaunchSettings _settings;
    private LaunchSettings? _unchangedSettings;
    private IScreenshotService? _screenshots;

    private TodoWebDevice(TestAppDefinition definition, string name, TempDatabase database, NetworkStateFile network, LaunchSettings settings, PlaywrightTestContext context)
    {
        Definition = definition;
        Name = name;
        _database = database;
        _network = network;
        _settings = settings;
        _context = context;
    }

    /// <summary>The device's name: its browser context's, for the call log.</summary>
    public string Name { get; }

    /// <inheritdoc />
    public TestAppDefinition Definition { get; }

    /// <inheritdoc />
    /// <remarks>Into a folder named after the app, beside the run's other screenshots.</remarks>
    public IScreenshotService Screenshots => _screenshots ??= new ScreenshotService(
        Context,
        Context.Logger,
        new ScreenshotSettings { OutputDirectory = Path.Combine(new ScreenshotSettings().OutputDirectory, FolderName(Definition.Name)) });

    /// <summary>The name the app sends in <c>X-Todo-Client</c>.</summary>
    public string? ClientId => _settings.ClientId;

    public IHtmlTestContext Context => _context;

    public string BaseUrl => _host?.BaseUrl ?? throw new InvalidOperationException($"{Name} is not running.");

    /// <summary>The device's network: the app reads it from a file.</summary>
    public NetworkStateFile Network => _network;

    /// <summary>The app's SQLite file.</summary>
    public string DatabasePath => _database.Path;

    public TodoListPage List => new(Context);

    /// <summary>
    /// Seeds a database from the local half of <paramref name="testSet"/>, starts the app against
    /// <paramref name="apiBaseUrl"/>, and opens a browser context for it.
    /// </summary>
    /// <param name="name">The device's name.</param>
    /// <param name="testSet">The test set to seed from.</param>
    /// <param name="apiBaseUrl">The backend.</param>
    /// <param name="clientId">What the app sends in <c>X-Todo-Client</c>.</param>
    /// <param name="adjust">Changes the launch settings before the app starts.</param>
    /// <param name="definition">The app, as a row of an <c>Apps</c> table; one named <paramref name="name"/> when null.</param>
    public static async Task<TodoWebDevice> StartAsync(
        string name,
        TodoTestSet testSet,
        string apiBaseUrl,
        string clientId,
        Func<LaunchSettings, LaunchSettings>? adjust = null,
        TestAppDefinition? definition = null)
    {
        var database = new TempDatabase($"web-{name}-{testSet.Name}");
        await testSet.SeedDatabaseAsync(database.Path);
        var network = NetworkStateFile.Create(Path.Combine(database.Folder, "network-state.txt"));

        var settings = new LaunchSettings
        {
            DatabasePath = database.Path,
            ApiBaseUrl = apiBaseUrl,
            FixedNow = testSet.Now,
            NetworkStateFile = network.Path,
            ApiTimeout = TimeSpan.FromSeconds(5),
            ClientId = clientId,
        };

        var context = await PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions
        {
            Headless = !string.Equals(Environment.GetEnvironmentVariable("HEADLESS"), "false", StringComparison.OrdinalIgnoreCase),
            Name = name,
        });

        definition ??= new TestAppDefinition(name, name, "web", new Dictionary<string, string>());
        var device = new TodoWebDevice(definition, name, database, network, adjust?.Invoke(settings) ?? settings, context);
        device.StartHost();
        return device;
    }

    /// <inheritdoc />
    /// <remarks>Online, and the list open in a fresh page load, showing every todo.</remarks>
    public ValueTask ResetAsync(CancellationToken cancellationToken = default)
    {
        _network.SetOnline();

        // The list's view model outlives a page load, and with it the filter a test chose.
        OpenList().Filter.SelectByText("All");
        return ValueTask.CompletedTask;
    }

    /// <summary>Opens the list in a fresh page load and waits until it is interactive.</summary>
    public TodoListPage OpenList()
    {
        Context.NavigateTo(BaseUrl + "/");
        var list = List;
        list.AssertIdle();
        return list;
    }

    /// <summary>Stops the app and starts it again on the same database and backend, then opens the list.</summary>
    public TodoListPage Restart()
    {
        _host?.Dispose();
        StartHost();
        return OpenList();
    }

    /// <summary>Restarts with changed launch settings; <see cref="RestoreLaunchSettings"/> puts them back.</summary>
    public TodoListPage RestartWith(Func<LaunchSettings, LaunchSettings> change)
    {
        _unchangedSettings ??= _settings;
        _settings = change(_settings);
        return Restart();
    }

    /// <summary>Undoes <see cref="RestartWith"/>, restarting only when something was changed.</summary>
    public void RestoreLaunchSettings()
    {
        if (_unchangedSettings is not { } unchanged)
        {
            return;
        }

        _unchangedSettings = null;
        _settings = unchanged;
        Restart();
    }

    /// <summary>Moves the device to another test set: a new seeded database, then a restart.</summary>
    public TodoListPage UseTestSet(TodoTestSet testSet)
    {
        var database = new TempDatabase($"web-{Name}-{testSet.Name}");
        testSet.SeedDatabaseAsync(database.Path).GetAwaiter().GetResult();

        // The first database's folder holds the network-state file, so earlier ones stay until Dispose.
        _retired.Add(_database);
        _database = database;
        _settings = (_unchangedSettings ?? _settings) with { DatabasePath = database.Path, FixedNow = testSet.Now };
        _unchangedSettings = null;
        return Restart();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        _host?.Dispose();
        _network.Dispose();
        _database.Dispose();
        _retired.ForEach(database => database.Dispose());
    }

    private static string FolderName(string app)
        => string.Concat(app.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private void StartHost()
        => _host = WebAppHost.Start(new WebAppHostOptions
        {
            AppPath = WebApp.Assembly(),
            Environment = _settings.ToVariables(),
            ReadyPath = "/health",
            Name = $"Todo web ({Name})",
        });
}
