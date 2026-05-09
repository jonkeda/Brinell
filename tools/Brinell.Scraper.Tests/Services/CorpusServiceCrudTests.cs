using System.IO;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.Services;

public sealed class CorpusServiceCrudTests : IDisposable
{
    private readonly string _dbPath;
    private readonly CorpusService _sut;

    public CorpusServiceCrudTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"scraper-corpus-service-{Guid.NewGuid()}.db");
        _sut = new CorpusService($"Data Source={_dbPath}", NullLogger<CorpusService>.Instance);
        EnsureSitesTable();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void ListPagesBySiteId_ReturnsDistinctPages_ForRequestedSiteOnly()
    {
        var siteA = 101L;
        var siteB = 202L;

        EnsureSiteExists(siteA);
        EnsureSiteExists(siteB);

        StoreSnapshot(siteA, "Home", "https://a.local/home", DateTimeOffset.UtcNow.AddMinutes(-10));
        StoreSnapshot(siteA, "Home", "https://a.local/home", DateTimeOffset.UtcNow.AddMinutes(-5));
        StoreSnapshot(siteA, "Products", "https://a.local/products", DateTimeOffset.UtcNow.AddMinutes(-2));
        StoreSnapshot(siteB, "Home", "https://b.local/home", DateTimeOffset.UtcNow.AddMinutes(-1));

        var pages = _sut.ListPagesBySiteId(siteA);

        Assert.Equal(2, pages.Count);
        Assert.Contains(pages, p => p.PageName == "Home");
        Assert.Contains(pages, p => p.PageName == "Products");
        Assert.DoesNotContain(pages, p => p.PageUrl.Contains("b.local", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetSnapshotsByPageName_ReturnsOrderedSnapshots_AndIsSiteScoped()
    {
        var siteA = 101L;
        var siteB = 202L;
        var older = DateTimeOffset.UtcNow.AddDays(-2);
        var newer = DateTimeOffset.UtcNow.AddDays(-1);

        EnsureSiteExists(siteA);
        EnsureSiteExists(siteB);

        StoreSnapshot(siteA, "Home", "https://a.local/home", older);
        StoreSnapshot(siteA, "Home", "https://a.local/home", newer);
        StoreSnapshot(siteA, "Products", "https://a.local/products", DateTimeOffset.UtcNow);
        StoreSnapshot(siteB, "Home", "https://b.local/home", DateTimeOffset.UtcNow);

        var snapshots = _sut.GetSnapshotsByPageName(siteA, "Home");

        Assert.Equal(2, snapshots.Count);
        Assert.All(snapshots, s =>
        {
            Assert.Equal(siteA, s.SiteId);
            Assert.Equal("Home", s.PageName);
        });
        Assert.True(snapshots[0].CapturedAt >= snapshots[1].CapturedAt);
    }

    [Fact]
    public void DeletePageByName_DeletesPageSnapshotsAndElements_WithoutAffectingOtherPages()
    {
        var siteId = 303L;

        EnsureSiteExists(siteId);

        StoreSnapshot(siteId, "Home", "https://a.local/home", DateTimeOffset.UtcNow.AddMinutes(-2));
        StoreSnapshot(siteId, "Products", "https://a.local/products", DateTimeOffset.UtcNow.AddMinutes(-1));

        var beforeSnapshotCount = CountTableRows("Snapshots");
        var beforeElementCount = CountTableRows("Elements");
        Assert.True(beforeSnapshotCount >= 2);
        Assert.True(beforeElementCount >= 2);

        _sut.DeletePageByName(siteId, "Home");

        var remainingHome = _sut.GetSnapshotsByPageName(siteId, "Home");
        var remainingProducts = _sut.GetSnapshotsByPageName(siteId, "Products");

        Assert.Empty(remainingHome);
        Assert.Single(remainingProducts);
        Assert.True(CountTableRows("Snapshots") < beforeSnapshotCount);
        Assert.True(CountTableRows("Elements") < beforeElementCount);
    }

    [Fact]
    public void DeleteStaleSnapshots_DeletesOnlyOlderThanCutoff_AndReturnsDeletedCount()
    {
        var siteId = 404L;

        EnsureSiteExists(siteId);

        StoreSnapshot(siteId, "OldPage", "https://a.local/old", DateTimeOffset.UtcNow.AddDays(-90));
        StoreSnapshot(siteId, "RecentPage", "https://a.local/recent", DateTimeOffset.UtcNow.AddDays(-1));

        var deleted = _sut.DeleteStaleSnapshots(siteId, olderThanDays: 30);

        Assert.Equal(1, deleted);
        Assert.Empty(_sut.GetSnapshotsByPageName(siteId, "OldPage"));
        Assert.Single(_sut.GetSnapshotsByPageName(siteId, "RecentPage"));
    }

    private void StoreSnapshot(long siteId, string pageName, string pageUrl, DateTimeOffset capturedAt)
    {
        _sut.StoreSnapshot(siteId, new DomSnapshot
        {
            SiteName = "Test",
            PageName = pageName,
            PageUrl = pageUrl,
            PageTitle = pageName,
            CapturedAt = capturedAt,
            RootElement = new DomElement
            {
                Tag = "html",
                Children =
                {
                    new DomElement { Tag = "body" }
                }
            }
        });
    }

    private int CountTableRows(string table)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private void EnsureSitesTable()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Sites (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                StartUrl TEXT NOT NULL,
                Namespace TEXT,
                OutputPath TEXT,
                CreatedAt TEXT,
                LastOpenedAt TEXT,
                UrlAliasesJson TEXT
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private void EnsureSiteExists(long siteId)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO Sites (Id, Name, StartUrl, Namespace, OutputPath, CreatedAt, LastOpenedAt, UrlAliasesJson)
            VALUES (@id, @name, @url, @ns, @out, @created, @opened, @aliases);
            """;
        cmd.Parameters.AddWithValue("@id", siteId);
        cmd.Parameters.AddWithValue("@name", $"Site-{siteId}");
        cmd.Parameters.AddWithValue("@url", $"https://site-{siteId}.local");
        cmd.Parameters.AddWithValue("@ns", "Test.Namespace");
        cmd.Parameters.AddWithValue("@out", "out");
        cmd.Parameters.AddWithValue("@created", DateTimeOffset.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@opened", DateTimeOffset.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@aliases", "[]");
        cmd.ExecuteNonQuery();
    }
}
