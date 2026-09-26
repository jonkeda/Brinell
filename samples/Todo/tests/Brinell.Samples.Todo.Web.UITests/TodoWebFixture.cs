using Brinell.Html.Context;
using Brinell.Html.Playwright;
using Brinell.Html.Testing;
using Brinell.Mocking;
using Brinell.Samples.Todo.Infrastructure;
using Brinell.Samples.Todo.Web.UITests.Pages;

namespace Brinell.Samples.Todo.Web.UITests;

/// <summary>
/// The Todo web head, hermetic: a seeded SQLite file, the WireMock fake of <c>Todo.Api</c>, a pinned
/// clock, and one browser. The same arrangement as the MAUI fixtures (<c>TodoAppFixture</c>), on
/// the web head's own host (<see cref="WebAppHost"/>).
/// </summary>
/// <remarks>
/// One host per fixture: the host process is one device, so tests in a collection share its
/// database, as the MAUI tests share their app. Each test starts from the list, online, with the
/// backend reset (<see cref="StartTest"/>).
/// </remarks>
public abstract class TodoWebFixture : IAsyncLifetime
{
    /// <summary>The name this app sends in <c>X-Todo-Client</c>.</summary>
    public const string ClientId = "web-hermetic";

    private TempDatabase? _database;
    private NetworkStateFile? _network;
    private MockApiServer? _backend;
    private WebAppHost? _host;
    private PlaywrightTestContext? _context;

    /// <summary>The test set the fixture seeds from.</summary>
    protected abstract string TestDataName { get; }

    /// <summary>Changes the launch settings before the app starts.</summary>
    protected virtual LaunchSettings AdjustSettings(LaunchSettings settings) => settings;

    public TodoTestSet TestSet { get; private set; } = null!;

    public MockApiServer Backend => _backend ?? throw new InvalidOperationException("The fixture is not started.");

    public TodoServerStub Server { get; private set; } = null!;

    public IHtmlTestContext Context => _context ?? throw new InvalidOperationException("The fixture is not started.");

    public string BaseUrl => _host?.BaseUrl ?? throw new InvalidOperationException("The fixture is not started.");

    public TodoListPage List => new(Context);

    public async Task InitializeAsync()
    {
        TestSet = TodoTestSet.Load(TestDataName);
        _database = new TempDatabase("web-" + TestSet.Name);
        await TestSet.SeedDatabaseAsync(_database.Path);
        _network = NetworkStateFile.Create(Path.Combine(_database.Folder, "network-state.txt"));
        _backend = MockApiServer.Start();
        Server = TestSet.StubServer(_backend);

        var settings = AdjustSettings(new LaunchSettings
        {
            DatabasePath = _database.Path,
            ApiBaseUrl = _backend.BaseUrl,
            FixedNow = TestSet.Now,
            NetworkStateFile = _network.Path,
            ApiTimeout = TimeSpan.FromSeconds(5),
            ClientId = ClientId,
        });

        _host = WebAppHost.Start(new WebAppHostOptions
        {
            AppPath = WebApp.Assembly(),
            Environment = settings.ToVariables(),
            ReadyPath = "/health",
            Name = "Todo web",
        });
        _context = await PlaywrightTestContext.CreateAsync(new HtmlTestContextOptions
        {
            Headless = !string.Equals(Environment.GetEnvironmentVariable("HEADLESS"), "false", StringComparison.OrdinalIgnoreCase),
            Name = "todo-web",
        });
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        _host?.Dispose();
        _backend?.Dispose();
        _network?.Dispose();
        _database?.Dispose();
    }

    /// <summary>Online, the backend back to the test set, and the list open.</summary>
    public TodoListPage StartTest()
    {
        _network!.SetOnline();
        Backend.Reset();
        Server = TestSet.StubServer(Backend);
        return OpenList();
    }

    /// <summary>Opens the list in a fresh page load and waits until it is interactive.</summary>
    public TodoListPage OpenList()
    {
        Context.NavigateTo(BaseUrl + "/");
        var list = List;
        list.AssertIdle();
        return list;
    }

    /// <summary>A todo on the server only, with a unique title; the app has it after a sync.</summary>
    public string ServerHasTodo(string title, TodoStatusDto status = TodoStatusDto.Open, DateOnly? dueDate = null)
    {
        var unique = $"{title} {Guid.NewGuid().ToString("N")[..6]}";
        var yesterday = TestSet.Now.AddDays(-1);
        Server.Store(new TodoDto(Guid.NewGuid(), unique, null, dueDate, status, yesterday, yesterday));
        return unique;
    }
}

/// <summary>Where the built web head is.</summary>
internal static class WebApp
{
    public static string Assembly()
    {
        const string project = "Brinell.Samples.Todo.Web";
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", project, "bin", configuration, "net10.0", project + ".dll");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"{project}.dll was not found above {AppContext.BaseDirectory}. Build it first.");
    }
}

public sealed class ThreeTodosWebFixture : TodoWebFixture
{
    protected override string TestDataName => "three-todos";
}

public sealed class EmptyWebFixture : TodoWebFixture
{
    protected override string TestDataName => "empty";
}

public sealed class PendingChangesWebFixture : TodoWebFixture
{
    protected override string TestDataName => "pending-local-change";

    protected override LaunchSettings AdjustSettings(LaunchSettings settings) => settings with { SyncOnStart = false };
}

[CollectionDefinition(Name)]
public sealed class ThreeTodosWebCollection : ICollectionFixture<ThreeTodosWebFixture>
{
    public const string Name = "Todo web: three todos";
}

[CollectionDefinition(Name)]
public sealed class EmptyWebCollection : ICollectionFixture<EmptyWebFixture>
{
    public const string Name = "Todo web: empty";
}

[CollectionDefinition(Name)]
public sealed class PendingChangesWebCollection : ICollectionFixture<PendingChangesWebFixture>
{
    public const string Name = "Todo web: pending changes";
}
