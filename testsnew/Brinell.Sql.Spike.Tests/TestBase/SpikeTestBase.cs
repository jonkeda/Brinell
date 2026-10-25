using System.Diagnostics;
using Brinell.Core.Testing;
using Brinell.Html.Bunit;
using Brinell.Html.Context;
using Brinell.Html.Testing;
using Brinell.Samples.Sql.App.Components;
using Brinell.Samples.Sql.App.Data;
using Brinell.Sql.Spike.Tests.PageObjects;
using Brinell.Sql.Spike.Tests.Spike;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Sql.Spike.Tests.TestBase;

/// <summary>
/// The SQL Server sample with its five places written afresh before each test, on this run's head: one host for the
/// run behind a browser per test, or its <c>Routes</c> rendered by bUnit with its data composition, the
/// <see cref="CommandLog"/> and, with <c>BRINELL_SQL_REPLAY</c>, the <see cref="ReplayInterceptor"/>
/// (<c>BRINELL_HEAD=bunit</c>). In replay nothing is reset: the database may not exist.
/// </summary>
/// <remarks>
/// The context is made when the test's name is known (<see cref="Begin"/>, from <see cref="MeasureDatabaseShareAttribute"/>),
/// since the recording is the test's. One test at a time: the suite is one class, so one collection.
/// </remarks>
public abstract class SpikeTestBase : IAsyncLifetime
{
    public const string ReplayVariable = "BRINELL_SQL_REPLAY";
    private const string BunitAddress = "http://localhost";
    private IHtmlTestContext? _context;
    private SqlSampleHost? _host;
    private TimeSpan _reset;

    /// <summary>The test that is running, for the attribute that brackets it.</summary>
    internal static SpikeTestBase? Current { get; private set; }

    public static ReplayMode Mode => Enum.TryParse<ReplayMode>(Environment.GetEnvironmentVariable(ReplayVariable), ignoreCase: true, out var mode) ? mode : ReplayMode.Off;

    protected IHtmlTestContext Context => _context ?? throw new InvalidOperationException("Context has not been initialized.");

    protected string BaseUrl => WebHeads.IsBunit ? BunitAddress : _host?.BaseUrl ?? SqlSample.BaseUrl;

    /// <summary>The app's commands; null in the browser when off, where the app is another process.</summary>
    protected CommandLog? Log { get; private set; }

    /// <summary>The test's recording or replay; null when off.</summary>
    internal ReplaySession? Session { get; private set; }

    /// <summary>The five places as written for this test, with their ids.</summary>
    protected IReadOnlyList<Place> Seeded { get; private set; } = PlaceSeed.Places();

    public virtual async Task InitializeAsync()
    {
        Current = this;
        if (Mode == ReplayMode.Replay)
        {
            return;
        }

        var start = Stopwatch.GetTimestamp();
        await using (var db = new SpikeDbContext(SpikeData.Options(SpikeData.Connection)))
        {
            Seeded = await PlaceSeed.ResetAsync(db);
        }

        _reset = Stopwatch.GetElapsedTime(start);
    }

    /// <summary>The test <paramref name="name"/> starts: its context, and its recording or replay.</summary>
    internal async Task Begin(string name)
    {
        if (Mode == ReplayMode.Snapshot)
        {
            CurrentSchema();
        }

        var head = WebHeads.IsBunit ? "bunit" : "browser";
        var log = Mode == ReplayMode.Off && !WebHeads.IsBunit ? null : new CommandLog();
        var session = Mode is ReplayMode.Off or ReplayMode.Snapshot ? null : new ReplaySession(Mode, Path.Combine(Recordings, head, name + ".json"), CurrentSchema());
        Log = log;
        Session = session;

        // The test's interceptors in the app's own composition: the log, and the recording or replay.
        void Intercept(DbContextOptionsBuilder options)
        {
            if (log is not null)
            {
                options.AddInterceptors(log, log.Connections);
            }

            if (session is not null)
            {
                var replay = new ReplayInterceptor(session);
                options.AddInterceptors(replay, replay.Connections);
                options.UseSpatialReplay();
            }
        }

        if (WebHeads.IsBunit)
        {
            _context = new BunitTestContext(new BunitTestContextOptions
            {
                RootComponent = typeof(Routes),
                StartBlank = true,
                ConfigureServices = services => services.AddSpikeData(SpikeData.Connection, Intercept),
            });
        }
        else
        {
            // Off, the sample runs as its own process for the run. Otherwise a host per test in this process, so every
            // command it sends is this test's: what the browser does reaches the session through the app's composition.
            if (Mode != ReplayMode.Off)
            {
                _host = new SqlSampleHost(Intercept);
            }

            _context = await BlazorTestContext.CreateAsync(new HtmlTestContextOptions
            {
                BaseUrl = BaseUrl,
                Headless = !string.Equals(Environment.GetEnvironmentVariable("HEADLESS"), "false", StringComparison.OrdinalIgnoreCase),
            });
        }

        DatabaseShare.Prepared(Log, _reset);
    }

    public virtual async Task DisposeAsync()
    {
        Current = null;
        if (_context is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }

        if (_host is not null)
        {
            await _host.DisposeAsync();
        }

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

    private static readonly Lazy<Schema> Snapshot = new(ReadSchema, LazyThreadSafetyMode.ExecutionAndPublication);

    // The schema from the checked-in snapshot (Recordings/schema.json), with the versions that shape the SQL. Recording
    // writes it from the database first, once a run, after the first test's reset has migrated it; replay only reads it.
    private static Schema CurrentSchema() => Snapshot.Value;

    private static Schema ReadSchema()
    {
        var path = Path.Combine(Recordings, "schema.json");
        SchemaSnapshot snapshot;
        if (Mode is ReplayMode.Record or ReplayMode.Snapshot)
        {
            snapshot = SchemaSnapshot.ReadAsync(SpikeData.Connection).GetAwaiter().GetResult();
            snapshot.Save(path);
        }
        else
        {
            snapshot = SchemaSnapshot.Load(path) ?? throw new ReplayMissException($"No schema snapshot at {path}: record first (BRINELL_SQL_REPLAY=Record).");
        }

        return snapshot.ToSchema(Version(typeof(DbContext)), Version(typeof(Microsoft.Data.SqlClient.SqlConnection)));
    }

    private static string Version(Type type)
        => (type.Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion ?? "?").Split('+')[0];

    // Beside the project, so recordings are kept in git: Recordings/<head>/<test>.json.
    private static string Recordings
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
}

/// <summary>
/// The sample hosted in the test's process on Kestrel (a free port), one per test, with the test's interceptors added to
/// its data composition; it doesn't migrate, since the fixture does and replay has no database.
/// </summary>
internal sealed class SqlSampleHost : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>
{
    private readonly Action<DbContextOptionsBuilder> _intercept;

    public SqlSampleHost(Action<DbContextOptionsBuilder> intercept)
    {
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
        builder.ConfigureServices(services => services.ConfigureDbContext<SpikeDbContext>(_intercept));
    }
}

/// <summary>The sample started once for the run on a free port, on the same database the tests seed.</summary>
internal static class SqlSample
{
    private static readonly Lazy<string> Url = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string BaseUrl => Url.Value;

    private static string Start()
    {
        var host = WebAppHost.Start(new WebAppHostOptions
        {
            AppPath = WebAppHost.FindBuiltApp("samples/Brinell.Samples.Sql.App"),
            Environment = new Dictionary<string, string> { [SpikeData.ConnectionVariable] = SpikeData.Connection },
            ReadyPath = "/health",
            Name = "SQL sample",
        });
        AppDomain.CurrentDomain.ProcessExit += (_, _) => host.Dispose();
        return host.BaseUrl;
    }
}
