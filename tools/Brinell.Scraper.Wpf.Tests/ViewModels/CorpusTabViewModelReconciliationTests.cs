using System.IO;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.Tests.TestHelpers;
using Brinell.Scraper.ViewModels.Tabs;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public sealed class CorpusTabViewModelReconciliationTests : IClassFixture<StaThreadFixture>, IDisposable
{
    private readonly StaThreadFixture _sta;
    private readonly List<string> _dbPaths = [];

    public CorpusTabViewModelReconciliationTests(StaThreadFixture sta)
    {
        _sta = sta;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in _dbPaths)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best effort cleanup only.
            }
        }
    }

    [Fact]
    public void LoadPagesWithReconciliationAsync_AddsPagesThatExistInDbButNotInUi()
    {
        _sta.Run(() =>
        {
            var (vm, service) = CreateSut();
            var siteId = 501L;

            EnsureSiteExists(siteId);

            StoreSnapshot(service, siteId, "Home", "https://a.local/home", DateTimeOffset.UtcNow.AddMinutes(-5), 2);
            vm.Load(siteId);
            Assert.Single(vm.Pages);

            StoreSnapshot(service, siteId, "Products", "https://a.local/products", DateTimeOffset.UtcNow.AddMinutes(-1), 3);

            vm.LoadPagesWithReconciliationAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(2, vm.Pages.Count);
            Assert.Contains(vm.Pages, p => p.PageName == "Products");
        });
    }

    [Fact]
    public void LoadPagesWithReconciliationAsync_RemovesOrphanedUiPages()
    {
        _sta.Run(() =>
        {
            var (vm, service) = CreateSut();
            var siteId = 502L;

            EnsureSiteExists(siteId);

            StoreSnapshot(service, siteId, "Home", "https://a.local/home", DateTimeOffset.UtcNow.AddMinutes(-5), 2);
            vm.Load(siteId);
            Assert.Single(vm.Pages);

            service.DeletePageByName(siteId, "Home");

            vm.LoadPagesWithReconciliationAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.Empty(vm.Pages);
            Assert.Equal(0, vm.TotalPages);
            Assert.Equal(0, vm.TotalSnapshots);
        });
    }

    [Fact]
    public void RefreshPageAsync_UpdatesSnapshotCollection_FromDatabaseState()
    {
        _sta.Run(() =>
        {
            var (vm, service) = CreateSut();
            var siteId = 503L;

            EnsureSiteExists(siteId);

            StoreSnapshot(service, siteId, "Home", "https://a.local/home", DateTimeOffset.UtcNow.AddMinutes(-10), 1);
            vm.Load(siteId);

            var page = Assert.Single(vm.Pages);
            Assert.Single(page.Versions);

            StoreSnapshot(service, siteId, "Home", "https://a.local/home", DateTimeOffset.UtcNow.AddMinutes(-1), 4);

            vm.RefreshPageAsync(page, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Equal(2, page.Versions.Count);
            Assert.Contains(page.Versions, v => v.IsLatest);
            Assert.Equal(2, vm.TotalSnapshots);
        });
    }

    private (CorpusTabViewModel vm, CorpusService service) CreateSut()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"scraper-corpus-vm-{Guid.NewGuid()}.db");
        _dbPaths.Add(dbPath);

        var service = new CorpusService($"Data Source={dbPath}", NullLogger<CorpusService>.Instance);
        EnsureSitesTable(dbPath);
        var vm = new CorpusTabViewModel(
            service,
            new DomDiffService(),
            NullLogger<CorpusTabViewModel>.Instance);

        return (vm, service);
    }

    private static void StoreSnapshot(
        CorpusService service,
        long siteId,
        string pageName,
        string pageUrl,
        DateTimeOffset capturedAt,
        int bodyChildCount)
    {
        var root = new DomElement { Tag = "html" };
        var body = new DomElement { Tag = "body" };
        for (var i = 0; i < bodyChildCount; i++)
            body.Children.Add(new DomElement { Tag = "div", Id = $"n{i}" });

        root.Children.Add(body);

        service.StoreSnapshot(siteId, new DomSnapshot
        {
            SiteName = "Test",
            PageName = pageName,
            PageUrl = pageUrl,
            PageTitle = pageName,
            CapturedAt = capturedAt,
            RootElement = root
        });
    }

    private static void EnsureSitesTable(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
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
        using var conn = new SqliteConnection($"Data Source={_dbPaths[^1]}");
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
