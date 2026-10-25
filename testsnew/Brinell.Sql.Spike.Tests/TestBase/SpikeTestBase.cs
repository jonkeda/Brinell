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
/// run behind a browser per test, or its <c>Routes</c> rendered by bUnit with its data composition and the
/// <see cref="CommandLog"/> as interceptors (<c>BRINELL_HEAD=bunit</c>).
/// </summary>
public abstract class SpikeTestBase : IAsyncLifetime
{
    private const string BunitAddress = "http://localhost";
    private IHtmlTestContext? _context;

    protected IHtmlTestContext Context => _context ?? throw new InvalidOperationException("Context has not been initialized.");

    protected string BaseUrl => WebHeads.IsBunit ? BunitAddress : SqlSample.BaseUrl;

    /// <summary>The app's commands on the bUnit head; null in the browser, where the app is another process.</summary>
    protected CommandLog? Log { get; private set; }

    /// <summary>The five places as written for this test, with their ids.</summary>
    protected IReadOnlyList<Place> Seeded { get; private set; } = [];

    public virtual async Task InitializeAsync()
    {
        var start = Stopwatch.GetTimestamp();
        await using (var db = new SpikeDbContext(SpikeData.Options(SpikeData.Connection)))
        {
            Seeded = await PlaceSeed.ResetAsync(db);
        }

        var reset = Stopwatch.GetElapsedTime(start);

        if (WebHeads.IsBunit)
        {
            var log = new CommandLog();
            Log = log;
            _context = new BunitTestContext(new BunitTestContextOptions
            {
                RootComponent = typeof(Routes),
                ConfigureServices = services => services.AddSpikeData(SpikeData.Connection, options => options.AddInterceptors(log, log.Connections)),
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

        DatabaseShare.Prepared(Log, reset);
    }

    public virtual async Task DisposeAsync()
    {
        if (_context is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
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
