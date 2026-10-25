using System.Diagnostics;
using System.Reflection;
using Brinell.Core.Testing;
using Brinell.Html.Bunit;
using Brinell.Html.Context;
using Brinell.Samples.Sql.App.Components;
using Brinell.Samples.Sql.App.Data;
using Brinell.Sql.Spike.Tests.PageObjects;
using Brinell.Mocking.Data;
using Brinell.Mocking.Data.EntityFrameworkCore;
using Brinell.Sql.Spike.Tests.Spike;
using SpikeSchemaSnapshot = Brinell.Sql.Spike.Tests.Spike.SchemaSnapshot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Brinell.Sql.Spike.Tests.TestBase;

/// <summary>
/// The SQL Server sample with its five places written afresh before each test, on this run's head: its <c>Routes</c>
/// rendered by bUnit (<c>BRINELL_HEAD=bunit</c>), or a host of its own in this process behind a browser. Either way the
/// app's data composition gets this test's SQL log (<see cref="SqlLog"/>, through <c>UseSqlLog</c>) and, with <c>BRINELL_SQL_REPLAY</c>, its
/// <see cref="ReplayInterceptor"/>.
/// </summary>
/// <remarks>
/// Tests run in parallel (each class is its own collection): nothing of a test is static. Its session lives in its own
/// container or host; its database is a worker's, leased for the test (<see cref="WorkerDatabases"/>); in replay
/// there is none.
/// </remarks>
public abstract class SpikeTestBase(ITestOutputHelper output) : IAsyncLifetime
{
    public const string ReplayVariable = "BRINELL_SQL_REPLAY";
    private const string BunitAddress = "http://localhost";
    private readonly string _name = TestName(output);
    private IHtmlTestContext? _context;
    private SqlSampleHost? _host;
    private string? _database;
    private TimeSpan _reset;
    private long _start;

    public static ReplayMode Mode => Enum.TryParse<ReplayMode>(Environment.GetEnvironmentVariable(ReplayVariable), ignoreCase: true, out var mode) ? mode : ReplayMode.Off;

    protected IHtmlTestContext Context => _context ?? throw new InvalidOperationException("Context has not been initialized.");

    protected string BaseUrl => WebHeads.IsBunit ? BunitAddress : _host?.BaseUrl ?? throw new InvalidOperationException("No host.");

    /// <summary>The app's commands.</summary>
    protected SqlLog Log { get; } = new(Environment.GetEnvironmentVariable(SqlLog.Variable), keep: true);

    /// <summary>The test's recording or replay; null when off.</summary>
    internal ReplaySession? Session { get; private set; }

    /// <summary>The five places as written for this test, with their ids.</summary>
    protected IReadOnlyList<Place> Seeded { get; private set; } = PlaceSeed.Places();

    // The worker's database, or the configured one in replay, which needn't exist.
    private string Connection => _database is null ? SpikeData.Connection : WorkerDatabases.Connection(_database);

    public virtual async Task InitializeAsync()
    {
        if (Mode != ReplayMode.Replay)
        {
            _database = WorkerDatabases.Lease();
            var started = Stopwatch.GetTimestamp();
            await using (var db = new SpikeDbContext(SpikeData.Options(Connection)))
            {
                Seeded = await PlaceSeed.ResetAsync(db);
            }

            _reset = Stopwatch.GetElapsedTime(started);
        }

        var schema = Mode == ReplayMode.Off ? null : Schemas.Current(Connection);
        var head = WebHeads.IsBunit ? "bunit" : "browser";
        var session = Mode is ReplayMode.Record or ReplayMode.Replay
            ? new ReplaySession(Mode, Path.Combine(Schemas.Recordings, head, _name + ".json"), schema!)
            : null;
        Session = session;

        // The test's interceptors in the app's own composition. EF must not build an internal service provider per test.
        void Intercept(DbContextOptionsBuilder options)
        {
            options.ConfigureWarnings(warnings => warnings.Throw(CoreEventId.ManyServiceProvidersCreatedWarning));
            options.UseSqlLog(Log);
            if (session is not null)
            {
                var replay = new ReplayInterceptor(session);
                options.AddInterceptors(replay, replay.Connections);
                options.UseSpatialReplay();
            }
        }

        if (WebHeads.IsBunit)
        {
            var connection = Connection;
            _context = new BunitTestContext(new BunitTestContextOptions
            {
                RootComponent = typeof(Routes),
                StartBlank = true,
                ConfigureServices = services => services.AddSpikeData(connection, Intercept),
            });
        }
        else
        {
            // A host per test in this process: every command it sends is this test's, whatever the browser does.
            _host = new SqlSampleHost(Connection, Intercept);
            _context = await BlazorTestContext.CreateAsync(new HtmlTestContextOptions
            {
                BaseUrl = BaseUrl,
                Headless = !string.Equals(Environment.GetEnvironmentVariable("HEADLESS"), "false", StringComparison.OrdinalIgnoreCase),
            });
        }

        Log.Clear();
        _start = Stopwatch.GetTimestamp();
    }

    public virtual async Task DisposeAsync()
    {
        var elapsed = Stopwatch.GetElapsedTime(_start);
        if (_context is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }

        if (_host is not null)
        {
            await _host.DisposeAsync();
        }

        if (_database is not null)
        {
            WorkerDatabases.Return(_database);
        }

        DatabaseShare.Write(_name, elapsed, Log, _reset);
        if (Session is { } session)
        {
            session.Finish();
            if (session.Misses.Count > 0)
            {
                throw new ReplayMissException(string.Join("\n\n", session.Misses));
            }
        }
    }

    protected PlacesPage OpenPlaces()
    {
        Context.NavigateTo(BaseUrl + "/");
        var page = new PlacesPage(Context);
        page.AssertLoaded(true);
        return page;
    }

    protected PlacePage OpenPlace(string name)
    {
        Context.NavigateTo($"{BaseUrl}/places/{Seeded.Single(place => place.Name == name).Id}");
        var page = new PlacePage(Context);
        page.AssertLoaded(true);
        return page;
    }

    // "Class.Method", from xUnit 2's output helper, which keeps its test in a private field.
    private static string TestName(ITestOutputHelper output)
    {
        var test = (ITest)output.GetType().GetField("test", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(output)!;
        var method = test.TestCase.TestMethod;
        return $"{method.TestClass.Class.Name.Split('.')[^1]}.{method.Method.Name}";
    }
}

/// <summary>
/// The databases tests record and run on, one per worker: a test leases one that no other running test holds, and
/// returns it when done. A new one is made from the migrations on its first reset. Several tests in parallel never
/// share rows; the next test on a returned database resets it as before.
/// </summary>
internal static class WorkerDatabases
{
    private static readonly System.Collections.Concurrent.ConcurrentBag<string> Free = [];
    private static int _made;

    public static string Lease() => Free.TryTake(out var name) ? name : $"BrinellSqlSpike_w{Interlocked.Increment(ref _made)}";

    public static void Return(string name) => Free.Add(name);

    public static string Connection(string name)
        => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(SpikeData.Connection) { InitialCatalog = name }.ConnectionString;

    /// <summary>How many worker databases this run has made.</summary>
    public static int Made => Volatile.Read(ref _made);
}

/// <summary>
/// The schema snapshot (<c>Recordings/schema.json</c>), shared and read-only: recording writes it once a run from the
/// first worker database that asks, after its reset migrated it; replay only reads the file.
/// </summary>
internal static class Schemas
{
    private static readonly Lock Gate = new();
    private static Schema? _schema;

    public static Schema Current(string connection)
    {
        lock (Gate)
        {
            return _schema ??= Read(connection);
        }
    }

    // Beside the project, so recordings are kept in git: Recordings/<head>/<test>.json.
    public static string Recordings
    {
        get
        {
            var folder = new DirectoryInfo(AppContext.BaseDirectory);
            while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "Brinell.Sql.Spike.Tests.csproj")))
            {
                folder = folder.Parent;
            }

            return Path.Combine(folder?.FullName ?? throw new DirectoryNotFoundException("The spike's project folder"), "Recordings");
        }
    }

    private static Schema Read(string connection)
    {
        var path = Path.Combine(Recordings, "schema.json");
        SpikeSchemaSnapshot snapshot;
        if (SpikeTestBase.Mode is ReplayMode.Record or ReplayMode.Snapshot)
        {
            snapshot = SpikeSchemaSnapshot.ReadAsync(connection).GetAwaiter().GetResult();
            snapshot.Save(path);
        }
        else
        {
            snapshot = SpikeSchemaSnapshot.Load(path) ?? throw new ReplayMissException($"No schema snapshot at {path}: record first (BRINELL_SQL_REPLAY=Record).");
        }

        return snapshot.ToSchema(Version(typeof(DbContext)), Version(typeof(Microsoft.Data.SqlClient.SqlConnection)));
    }

    private static string Version(Type type)
        => (type.Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .OfType<AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion ?? "?").Split('+')[0];
}

/// <summary>
/// The sample hosted in the test's process on Kestrel (a free port), one per test, on the test's database and with the
/// test's interceptors added to its data composition; it doesn't migrate, since the fixture does and replay has none.
/// </summary>
internal sealed class SqlSampleHost : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
{
    private readonly string _connection;
    private readonly Action<DbContextOptionsBuilder> _intercept;

    public SqlSampleHost(string connection, Action<DbContextOptionsBuilder> intercept)
    {
        _connection = connection;
        _intercept = intercept;
        UseKestrel(0);
        StartServer();
        var addresses = Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>().Features
            .Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!.Addresses;
        BaseUrl = addresses.First().Replace("[::]", "localhost", StringComparison.Ordinal).Replace("0.0.0.0", "localhost", StringComparison.Ordinal).TrimEnd('/');
    }

    public string BaseUrl { get; }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Spike:Migrate", "false");
        builder.UseSetting("Spike:Connection", _connection);
        builder.ConfigureServices(services => services.ConfigureDbContext<SpikeDbContext>(_intercept));
    }
}
