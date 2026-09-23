using System.IO;
using Brinell.Scraper.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.Data;

public class CorpusDatabaseTests : IDisposable
{
    private string? _dbPath;

    private CorpusDatabase CreateDb()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"scraper-test-{Guid.NewGuid()}.db");
        return new CorpusDatabase(_dbPath, NullLogger<CorpusDatabase>.Instance);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (_dbPath is not null && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void EnsureCreated_CreatesSitesTable()
    {
        var db = CreateDb();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Sites'";
        var result = cmd.ExecuteScalar();

        Assert.NotNull(result);
    }

    [Fact]
    public void CreateSite_InsertsAndReturnsId()
    {
        var db = CreateDb();

        var site = db.CreateSite("TestSite", "https://example.com", "Test.Ns", "/out", []);

        Assert.True(site.Id > 0);
        Assert.Equal("TestSite", site.Name);
        Assert.Equal("https://example.com", site.StartUrl);
    }

    [Fact]
    public void GetAllSites_ReturnsAllSites()
    {
        var db = CreateDb();
        db.CreateSite("Site1", "https://one.com", "Ns1", "/out1", []);
        db.CreateSite("Site2", "https://two.com", "Ns2", "/out2", []);

        var sites = db.GetAllSites();

        Assert.Equal(2, sites.Count);
    }

    [Fact]
    public void GetAllSites_ReturnsEmpty_WhenNoSites()
    {
        var db = CreateDb();

        var sites = db.GetAllSites();

        Assert.Empty(sites);
    }

    [Fact]
    public void TouchSite_UpdatesLastOpenedAt()
    {
        var db = CreateDb();
        var site = db.CreateSite("TouchMe", "https://touch.com", "Ns", "/out", []);
        var before = db.GetAllSites().First(s => s.Id == site.Id).LastOpenedAt;

        Thread.Sleep(1100);
        db.TouchSite(site.Id);

        var after = db.GetAllSites().First(s => s.Id == site.Id).LastOpenedAt;
        Assert.True(after > before);
    }

    [Fact]
    public void CreateSite_WithAliases_StoresCorrectly()
    {
        var db = CreateDb();
        var aliases = new List<string> { "https://a.com", "https://b.com" };

        db.CreateSite("AliasSite", "https://alias.com", "Ns", "/out", aliases);
        var sites = db.GetAllSites();

        var site = Assert.Single(sites);
        Assert.Equal(2, site.UrlAliases.Count);
        Assert.Contains("https://a.com", site.UrlAliases);
        Assert.Contains("https://b.com", site.UrlAliases);
    }
}
