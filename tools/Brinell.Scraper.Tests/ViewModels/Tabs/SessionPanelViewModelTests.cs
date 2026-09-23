using System.IO;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.ViewModels.Tabs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels.Tabs;

public sealed class SessionPanelViewModelTests : IDisposable
{
    private readonly List<string> _dbPaths = [];

    [Fact]
    public void Load_SetsSiteContextAndPopulatesCorpusAndControls()
    {
        var corpusService = CreateCorpusService();
        SeedSnapshot(corpusService, 42, "Login", "https://example.com/login");
        SeedSnapshot(corpusService, 42, "Dashboard", "https://example.com/dashboard");

        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns([
            new GeneratedControl { Name = "NavMenu" },
            new GeneratedControl { Name = "UserHeader" }
        ]);

        var vm = new SessionPanelViewModel(corpusService, registry, NullLogger<SessionPanelViewModel>.Instance);

        vm.Load(42, "Example Site");

        Assert.Equal(42, vm.SiteId);
        Assert.Equal("Example Site", vm.SiteHeader);
        Assert.Equal(2, vm.CorpusPages.Count);
        Assert.Equal(2, vm.Controls.Count);
        Assert.Equal("2 pages · 2 controls", vm.CorpusStats);
    }

    [Fact]
    public void SyncRecordedPages_MapsSnapshotsWithNewIcon()
    {
        var vm = CreateViewModel();

        vm.SyncRecordedPages([
            CreateSnapshot("Settings", "https://example.com/settings"),
            CreateSnapshot("Reports", "https://example.com/reports")
        ]);

        Assert.Equal(2, vm.RecordedPages.Count);
        Assert.All(vm.RecordedPages, p => Assert.Equal("🆕", p.StatusIcon));
    }

    [Fact]
    public void SessionSummary_WhenEmpty_ShowsDefaultMessage()
    {
        var vm = CreateViewModel();

        Assert.Equal("No pages captured yet", vm.SessionSummary);
    }

    [Fact]
    public void SessionSummary_WhenRecording_ShowsNewAndTotal()
    {
        var corpusService = CreateCorpusService();
        SeedSnapshot(corpusService, 7, "Login", "https://example.com/login");

        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns(Array.Empty<GeneratedControl>());

        var vm = new SessionPanelViewModel(corpusService, registry, NullLogger<SessionPanelViewModel>.Instance);
        vm.Load(7, "Example Site");
        vm.SyncRecordedPages([
            CreateSnapshot("Profile", "https://example.com/profile"),
            CreateSnapshot("Reports", "https://example.com/reports")
        ]);

        vm.IsRecording = true;

        Assert.Equal("+2 new · 3 total", vm.SessionSummary);
    }

    [Fact]
    public void SessionSummary_WhenStopped_ShowsCapturedCount()
    {
        var vm = CreateViewModel();
        vm.SyncRecordedPages([
            CreateSnapshot("Page A", "https://example.com/a"),
            CreateSnapshot("Page B", "https://example.com/b")
        ]);

        vm.IsRecording = false;

        Assert.Equal("2 captured this session", vm.SessionSummary);
    }

    [Fact]
    public void NavigateSelectedCorpusCommand_InvokesCallbackWithUrl()
    {
        var vm = CreateViewModel();
        var navigatedUrl = string.Empty;
        vm.SetNavigateCallback(url => navigatedUrl = url);
        vm.SelectedCorpusPage = new SidebarPageItem
        {
            Name = "Target",
            Url = "https://example.com/target"
        };

        vm.NavigateSelectedCorpusCommand.Execute(null);

        Assert.Equal("https://example.com/target", navigatedUrl);
    }

    [Fact]
    public void RemoveSelectedRecordingCommand_InvokesCallbackWithSelection()
    {
        var vm = CreateViewModel();
        SidebarPageItem? removed = null;
        vm.SetRemoveCallbacks(_ => { }, item => removed = item, () => { });
        vm.SyncRecordedPages([
            CreateSnapshot("Settings", "https://example.com/settings")
        ]);
        vm.SelectedRecordingPage = vm.RecordedPages[0];

        vm.RemoveSelectedRecordingCommand.Execute(null);

        Assert.NotNull(removed);
        Assert.Equal("Settings", removed!.Name);
    }

    [Fact]
    public void ClearRecordingsCommand_TracksCanExecuteAndInvokesCallback()
    {
        var vm = CreateViewModel();
        var clearCount = 0;
        vm.SetRemoveCallbacks(_ => { }, _ => { }, () => clearCount++);

        Assert.False(vm.ClearRecordingsCommand.CanExecute(null));

        vm.SyncRecordedPages([
            CreateSnapshot("A", "https://example.com/a")
        ]);
        Assert.True(vm.ClearRecordingsCommand.CanExecute(null));

        vm.ClearRecordingsCommand.Execute(null);

        Assert.Equal(1, clearCount);
    }

    [Fact]
    public void TransferSessionToCorpusCommand_TracksCanExecuteAndInvokesCallback()
    {
        var vm = CreateViewModel();
        var transferCount = 0;
        vm.SetTransferSessionToCorpusCallback(() => transferCount++);

        Assert.False(vm.TransferSessionToCorpusCommand.CanExecute(null));

        vm.Load(9, "Example Site");
        vm.SyncRecordedPages([
            CreateSnapshot("A", "https://example.com/a")
        ]);

        Assert.True(vm.TransferSessionToCorpusCommand.CanExecute(null));

        vm.TransferSessionToCorpusCommand.Execute(null);

        Assert.Equal(1, transferCount);
    }

    private SessionPanelViewModel CreateViewModel()
    {
        var corpusService = CreateCorpusService();
        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns(Array.Empty<GeneratedControl>());
        return new SessionPanelViewModel(corpusService, registry, NullLogger<SessionPanelViewModel>.Instance);
    }

    private CorpusService CreateCorpusService()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"session-panel-test-{Guid.NewGuid()}.db");
        _dbPaths.Add(dbPath);
        return new CorpusService($"Data Source={dbPath}", NullLogger<CorpusService>.Instance);
    }

    private static DomSnapshot CreateSnapshot(string pageName, string pageUrl) => new()
    {
        SiteName = "Example",
        PageName = pageName,
        PageUrl = pageUrl,
        PageTitle = pageName,
        CapturedAt = DateTimeOffset.UtcNow,
        RootElement = new DomElement { Tag = "html" }
    };

    private static void SeedSnapshot(CorpusService corpusService, long siteId, string pageName, string pageUrl)
    {
        corpusService.StoreSnapshot(siteId, CreateSnapshot(pageName, pageUrl));
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
