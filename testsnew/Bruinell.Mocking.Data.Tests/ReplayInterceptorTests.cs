using Bruinell.Mocking.Data.EntityFrameworkCore;
using Bruinell.Mocking.Data.Replay;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bruinell.Mocking.Data.Tests;

/// <summary>
/// The EF interceptor over SQLite (R3 and R4 of .my/sql-replay/plan.md): a context recorded against a database file and
/// replayed with the file gone; and one set of options serving sessions in parallel through <see cref="ReplayScope"/>.
/// </summary>
public sealed class ReplayInterceptorTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bruinell", "ReplayTests", Guid.NewGuid().ToString("N"));

    public ReplayInterceptorTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task Replay_AnswersTheContext_WithTheDatabaseGone()
    {
        var database = Path.Combine(_folder, "places.db");
        var recording = Path.Combine(_folder, "places.json");
        var recorded = await Run(new ReplaySession(ReplayMode.Record, recording), database);
        SqliteConnection.ClearAllPools();
        File.Delete(database);

        var session = new ReplaySession(ReplayMode.Replay, recording);
        var replayed = await Run(session, database);

        Assert.Equal(recorded, replayed);
        Assert.Equal("Enkhuizen,Hoorn|2|19", replayed);
        Assert.Empty(session.Misses);
        Assert.False(File.Exists(database));
    }

    [Fact]
    public async Task Scope_SessionsInParallel_ThroughOneSetOfOptions()
    {
        var workers = Enumerable.Range(1, 8).ToList();
        var options = new DbContextOptionsBuilder<PlacesContext>().UseSqlite($"Data Source={Path.Combine(_folder, "unused.db")}").UseSqlReplay().Options;

        await Task.WhenAll(workers.Select(worker => Task.Run(() => Worker(options, ReplayMode.Record, worker))));
        SqliteConnection.ClearAllPools();
        foreach (var worker in workers)
        {
            File.Delete(Database(worker));
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 5).SelectMany(_ => workers).Select(worker => Task.Run(() => Worker(options, ReplayMode.Replay, worker))));

        Assert.Equal(Enumerable.Range(0, 5).SelectMany(_ => workers).Select(worker => $"Town {worker}"), results);
    }

    private string Database(int worker) => Path.Combine(_folder, $"worker{worker}.db");

    // One set of options for every worker: the connection string set per context, the session from the flow's scope.
    private async Task<string> Worker(DbContextOptions<PlacesContext> options, ReplayMode mode, int worker)
    {
        var session = new ReplaySession(mode, Path.Combine(_folder, $"worker{worker}.json"));
        using (ReplayScope.Enter(session))
        {
            using var context = new PlacesContext(options);
            context.Database.SetConnectionString($"Data Source={Database(worker)}");
            await CreateTables(context);
            context.Places.Add(new Place { Id = worker, Name = $"Town {worker}" });
            await context.SaveChangesAsync();
            var name = (await context.Places.AsNoTracking().SingleAsync()).Name;
            session.Finish();
            Assert.Empty(session.Misses);
            return name;
        }
    }

    private static async Task<string> Run(ReplaySession session, string database)
    {
        var options = new DbContextOptionsBuilder<PlacesContext>().UseSqlite($"Data Source={database}").UseSqlReplay(session).Options;
        using (var context = new PlacesContext(options))
        {
            await CreateTables(context);
            context.Places.AddRange(new Place { Id = 1, Name = "Hoorn" }, new Place { Id = 2, Name = "Enkhuizen" });
            await context.SaveChangesAsync();
        }

        using var reading = new PlacesContext(options);
        var names = await reading.Places.OrderBy(place => place.Name).Select(place => place.Name).ToListAsync();
        var count = await reading.Places.CountAsync();
        reading.Places.Add(new Place { Id = 3, Name = "Hoorn" });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => reading.SaveChangesAsync());
        session.Finish();
        return $"{string.Join(",", names)}|{count}|{Assert.IsType<SqliteException>(error.InnerException).SqliteErrorCode}";
    }

    // Not EnsureCreated: SQLite's probes for the file by opening it, which replay doesn't, so it would take another path.
    private static Task<int> CreateTables(DbContext context) => context.Database.ExecuteSqlRawAsync(context.Database.GenerateCreateScript());

    public sealed class Place
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class PlacesContext(DbContextOptions<PlacesContext> options) : DbContext(options)
    {
        public DbSet<Place> Places => Set<Place>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Place>().HasIndex(place => place.Name).IsUnique();
    }
}
