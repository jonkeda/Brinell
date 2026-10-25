using Bruinell.Mocking.Data.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bruinell.Mocking.Data.Tests;

/// <summary>The EF interceptor over SQLite: queries and saves logged, what EF reads unchanged.</summary>
public sealed class SqlLogInterceptorTests : IDisposable
{
    private readonly SqliteConnection _database = new("Data Source=:memory:");
    private readonly SqlLog _log = new(null, keep: true);

    public SqlLogInterceptorTests()
    {
        _database.Open();
        using var context = Context();
        context.Database.EnsureCreated();
        _log.Clear();
    }

    [Fact]
    public async Task SaveAndQuery_AreLogged_WithRowsAndObjects()
    {
        await using (var context = Context())
        {
            context.Towns.AddRange(new Town { Name = "Hoorn" }, new Town { Name = "Enkhuizen" });
            await context.SaveChangesAsync();
        }

        await using (var context = Context())
        {
            var names = await context.Towns.OrderBy(town => town.Name).Select(town => town.Name).ToListAsync();
            Assert.Equal(["Enkhuizen", "Hoorn"], names);
        }

        var query = _log.Entries.Last(entry => entry.Kind == "reader");
        Assert.Equal(2, query.Rows);
        Assert.Equal(["dbo.towns"], query.Objects!);
        Assert.Contains(_log.Entries, entry => entry.Text?.StartsWith("INSERT INTO", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(_log.Entries, entry => entry.Text?.Contains("Hoorn", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Failure_IsLoggedAsAnError()
    {
        await using var context = Context();

        await Assert.ThrowsAnyAsync<Exception>(() => context.Database.ExecuteSqlRawAsync("SELECT * FROM NoSuchTable"));

        Assert.Equal("error", _log.Entries.Last().Kind);
        Assert.Equal(1, _log.Entries.Last().Error);
    }

    public void Dispose() => _database.Dispose();

    private TownContext Context() => new(new DbContextOptionsBuilder<TownContext>().UseSqlite(_database).UseSqlLog(_log).Options);

    public sealed class Town
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class TownContext(DbContextOptions<TownContext> options) : DbContext(options)
    {
        public DbSet<Town> Towns => Set<Town>();
    }
}
