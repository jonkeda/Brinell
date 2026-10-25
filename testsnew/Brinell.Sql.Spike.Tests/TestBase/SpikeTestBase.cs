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
    private TimeSpan _reset;

    /// <summary>The test that is running, for the attribute that brackets it.</summary>
    internal static SpikeTestBase? Current { get; private set; }

    public static ReplayMode Mode => Enum.TryParse<ReplayMode>(Environment.GetEnvironmentVariable(ReplayVariable), ignoreCase: true, out var mode) ? mode : ReplayMode.Off;

    protected IHtmlTestContext Context => _context ?? throw new InvalidOperationException("Context has not been initialized.");

    protected string BaseUrl => WebHeads.IsBunit ? BunitAddress : SqlSample.BaseUrl;

    /// <summary>The app's commands on the bUnit head; null in the browser, where the app is another process.</summary>
    protected CommandLog? Log { get; private set; }

    /// <summary>The test's recording or replay on the bUnit head; null when off or in the browser.</summary>
    internal ReplaySession? Session { get; private set; }

    /// <summary>The five places as written for this test, with their ids.</summary>
    protected IReadOnlyList<Place> Seeded { get; private set; } = PlaceSeed.Places();

    public virtual async Task InitializeAsync()
    {
        Current = this;
        if (Mode == ReplayMode.Replay && WebHeads.IsBunit)
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
        if (WebHeads.IsBunit)
        {
            var log = new CommandLog();
            Log = log;
            var session = Mode == ReplayMode.Off ? null : new ReplaySession(Mode, Path.Combine(Recordings, "bunit", name + ".json"), CurrentStamp());
            Session = session;
            _context = new BunitTestContext(new BunitTestContextOptions
            {
                RootComponent = typeof(Routes),
                StartBlank = true,
                ConfigureServices = services => services.AddSpikeData(SpikeData.Connection, options =>
                {
                    options.AddInterceptors(log, log.Connections);
                    if (session is not null)
                    {
                        var replay = new ReplayInterceptor(session);
                        options.AddInterceptors(replay, replay.Connections);
                        options.UseSpatialReplay();
                    }
                }),
            });
        }
        else
        {
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

    // The migrations in the app's assembly (no connection needed) and the versions that shape the SQL.
    private static Stamp CurrentStamp()
    {
        using var db = new SpikeDbContext(SpikeData.Options(SpikeData.Connection));
        var migrations = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join(";", db.Database.GetMigrations()))))[..12];
        return new Stamp(migrations, Version(typeof(DbContext)), Version(typeof(Microsoft.Data.SqlClient.SqlConnection)));
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
