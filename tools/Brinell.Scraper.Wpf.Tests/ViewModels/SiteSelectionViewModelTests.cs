using System.IO;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Brinell.Scraper.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public sealed class SiteSelectionViewModelTests : IDisposable
{
    private readonly string _dbPath;
    private readonly CorpusDatabase _db;

    public SiteSelectionViewModelTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.db");
        _db = new CorpusDatabase(_dbPath, NullLogger<CorpusDatabase>.Instance);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void LoadSites_PopulatesSitesCollection()
    {
        _db.CreateSite("Test Site", "https://example.com", "", "", []);

        var vm = new SiteSelectionViewModel(_db, NullLogger<SiteSelectionViewModel>.Instance);

        Assert.True(vm.Sites.Count > 0);
    }

    [Fact]
    public void SelectSiteCommand_FiresSiteSelected()
    {
        _db.CreateSite("Test Site", "https://example.com", "", "", []);
        var vm = new SiteSelectionViewModel(_db, NullLogger<SiteSelectionViewModel>.Instance);
        SiteInfo? selectedSite = null;
        vm.SiteSelected += site => selectedSite = site;

        var site = vm.Sites[0];
        vm.SelectSiteCommand.Execute(site);

        Assert.NotNull(selectedSite);
    }

    [Fact]
    public void NewSiteCommand_FiresNewSiteRequested()
    {
        var vm = new SiteSelectionViewModel(_db, NullLogger<SiteSelectionViewModel>.Instance);
        var fired = false;
        vm.NewSiteRequested = () => fired = true;

        vm.NewSiteCommand.Execute(null);

        Assert.True(fired);
    }

    [Fact]
    public void Sites_IsEmpty_WhenNoSitesInDb()
    {
        var vm = new SiteSelectionViewModel(_db, NullLogger<SiteSelectionViewModel>.Instance);

        Assert.Empty(vm.Sites);
    }

    [Fact]
    public void SelectedSite_InitiallyNull()
    {
        SiteInfo? selectedSite = null;
        var vm = new SiteSelectionViewModel(_db, NullLogger<SiteSelectionViewModel>.Instance);
        vm.SiteSelected += site => selectedSite = site;

        Assert.Null(selectedSite);
    }

    [Fact]
    public void SiteSelected_IncludesCorrectSiteInfo()
    {
        var vm = new SiteSelectionViewModel(_db, NullLogger<SiteSelectionViewModel>.Instance);
        SiteInfo? receivedSite = null;
        vm.SiteSelected += site => receivedSite = site;

        var siteInfo = new SiteInfo { Name = "My Site", StartUrl = "https://mysite.com" };
        vm.AddSite(siteInfo);

        Assert.NotNull(receivedSite);
        Assert.Equal("My Site", receivedSite.Name);
        Assert.Equal("https://mysite.com", receivedSite.StartUrl);
    }
}
