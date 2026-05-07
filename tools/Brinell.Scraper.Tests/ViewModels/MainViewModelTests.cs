using System.IO;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels;

public class MainViewModelTests : IDisposable
{
    private readonly List<string> _dbPaths = [];

    private (MainViewModel vm, CorpusDatabase db, SiteSelectionViewModel siteSelection) CreateViewModel()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.db");
        _dbPaths.Add(dbPath);
        var db = new CorpusDatabase(dbPath, NullLogger<CorpusDatabase>.Instance);
        var browser = new BrowserViewModel(NullLogger<BrowserViewModel>.Instance);
        var sidebar = new SidebarViewModel();
        var siteSelection = new SiteSelectionViewModel(db, NullLogger<SiteSelectionViewModel>.Instance);
        var inspector = new InspectorViewModel();
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var domCapture = new DomCaptureService(NullLogger<DomCaptureService>.Instance);
        var highlight = new ElementHighlightService(NullLogger<ElementHighlightService>.Instance);
        var pageTransition = new PageTransitionDetector(NullLogger<PageTransitionDetector>.Instance);
        var exportService = new SnapshotExportService();
        var controlGroupDetector = new ControlGroupDetector();
        var logger = NullLogger<MainViewModel>.Instance;
        var vm = new MainViewModel(db, browser, sidebar, siteSelection, inspector, recording, domCapture, highlight, pageTransition, exportService, controlGroupDetector, logger);
        return (vm, db, siteSelection);
    }

    private static SiteInfo CreateTestSite() => new()
    {
        Id = 1, Name = "TestSite", StartUrl = "https://test.com",
        PageCount = 5, ControlCount = 3
    };

    [Fact]
    public void Constructor_SetsDefaultWindowTitle()
    {
        var (vm, _, _) = CreateViewModel();
        Assert.Equal("Brinell Scraper", vm.WindowTitle);
    }

    [Fact]
    public void Constructor_HasActiveSite_IsFalse()
    {
        var (vm, _, _) = CreateViewModel();
        Assert.False(vm.HasActiveSite);
    }

    [Fact]
    public void OnSiteSelected_SetsActiveSite()
    {
        var (vm, _, siteSelection) = CreateViewModel();
        siteSelection.AddSite(CreateTestSite());
        Assert.NotNull(vm.ActiveSite);
    }

    [Fact]
    public void OnSiteSelected_UpdatesWindowTitle()
    {
        var (vm, _, siteSelection) = CreateViewModel();
        var site = CreateTestSite();
        siteSelection.AddSite(site);
        Assert.Contains(site.Name, vm.WindowTitle);
    }

    [Fact]
    public void OnSiteSelected_UpdatesSiteName()
    {
        var (vm, _, siteSelection) = CreateViewModel();
        var site = CreateTestSite();
        siteSelection.AddSite(site);
        Assert.Equal(site.Name, vm.SiteName);
    }

    [Fact]
    public void OnSiteSelected_UpdatesBrowserAddress()
    {
        var (vm, _, siteSelection) = CreateViewModel();
        var site = CreateTestSite();
        siteSelection.AddSite(site);
        Assert.Equal(site.StartUrl, vm.Browser.AddressUrl);
    }

    [Fact]
    public void OnSiteSelected_UpdatesCorpusStats()
    {
        var (vm, _, siteSelection) = CreateViewModel();
        var site = CreateTestSite();
        siteSelection.AddSite(site);
        Assert.Contains("5 pages", vm.Sidebar.CorpusStats);
        Assert.Contains("3 controls", vm.Sidebar.CorpusStats);
    }

    [Fact]
    public void OnSiteSelected_FiresBrowserViewRequested()
    {
        var (vm, _, siteSelection) = CreateViewModel();
        var fired = false;
        vm.BrowserViewRequested += () => fired = true;
        siteSelection.AddSite(CreateTestSite());
        Assert.True(fired);
    }

    [Fact]
    public void HasActiveSite_TrueAfterSiteSelected()
    {
        var (vm, _, siteSelection) = CreateViewModel();
        siteSelection.AddSite(CreateTestSite());
        Assert.True(vm.HasActiveSite);
    }

    [Fact]
    public void SwitchSiteCommand_FiresSiteSelectorRequested()
    {
        var (vm, _, _) = CreateViewModel();
        var fired = false;
        vm.SiteSelectorRequested += () => fired = true;
        vm.SwitchSiteCommand.Execute(null);
        Assert.True(fired);
    }

    [Fact]
    public void ManageControlsCommand_DisabledWithoutSite()
    {
        var (vm, _, _) = CreateViewModel();
        Assert.False(vm.ManageControlsCommand.CanExecute(null));
    }

    [Fact]
    public void ManageControlsCommand_EnabledWithSite()
    {
        var (vm, _, siteSelection) = CreateViewModel();
        siteSelection.AddSite(CreateTestSite());
        Assert.True(vm.ManageControlsCommand.CanExecute(null));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var path in _dbPaths)
        {
            try { File.Delete(path); } catch { }
        }
    }
}
