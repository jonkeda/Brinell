using System.IO;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.ViewModels;
using Brinell.Scraper.ViewModels.Tabs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels.Tabs;

public sealed class ScrapingTabViewModelTests : IDisposable
{
    private readonly List<string> _dbPaths = [];

    [Fact]
    public void Constructor_SyncsSessionFromExistingRecordingSnapshots()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        recording.StartRecording();
        recording.OnPageTransition("https://example.com/start", CreateSnapshot("Start", "https://example.com/start"));

        var (_, _, vm, _) = CreateViewModel(recording);

        Assert.Single(vm.Session.RecordedPages);
        Assert.Equal("Start", vm.Session.RecordedPages[0].Name);
    }

    [Fact]
    public void RecordingStartAndStop_TogglesSessionRecordingState()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var (_, _, vm, _) = CreateViewModel(recording);

        recording.StartRecording();
        Assert.True(vm.Session.IsRecording);

        recording.StopRecording();
        Assert.False(vm.Session.IsRecording);
    }

    [Fact]
    public void SessionNavigateSelectedRecordingCommand_UsesBrowserNavigateCommand()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var (browser, _, vm, _) = CreateViewModel(recording);

        vm.Session.SyncRecordedPages([
            CreateSnapshot("Reports", "https://example.com/reports")
        ]);
        vm.Session.SelectedRecordingPage = vm.Session.RecordedPages[0];

        vm.Session.NavigateSelectedRecordingCommand.Execute(null);

        Assert.Equal("https://example.com/reports", browser.AddressUrl);
        Assert.Equal("https://example.com/reports", browser.PendingNavigateUrl);
    }

    [Fact]
    public void SessionNavigateSelectedCorpusCommand_CrossSiteRejected_DoesNotNavigate()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var dialogs = Substitute.For<IMessageDialogService>();
        dialogs.ShowYesNo(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        var (browser, _, vm, _) = CreateViewModel(recording, dialogs);

        browser.AddressUrl = "https://one.example.com/home";
        vm.Session.SelectedCorpusPage = new SidebarPageItem
        {
            Name = "Other",
            Url = "https://two.example.com/page"
        };

        vm.Session.NavigateSelectedCorpusCommand.Execute(null);

        Assert.Equal("https://one.example.com/home", browser.AddressUrl);
        dialogs.Received(1).ShowYesNo(Arg.Any<string>(), "Navigate to Selected Page");
    }

    [Fact]
    public void StopRecording_WithNoSnapshots_DoesNotPromptOrThrow()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var dialogs = Substitute.For<IMessageDialogService>();
        var (_, _, vm, _) = CreateViewModel(recording, dialogs);

        recording.StartRecording();
        var ex = Record.Exception(recording.StopRecording);

        Assert.Null(ex);
        Assert.Empty(vm.Session.RecordedPages);
        dialogs.DidNotReceive().ShowYesNo(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void StopRecording_WithSnapshotsAndPromptNo_KeepsSessionSnapshots()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var dialogs = Substitute.For<IMessageDialogService>();
        dialogs.ShowYesNo(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var (_, _, vm, _) = CreateViewModel(recording, dialogs);
        vm.Session.Load(10, "Example Site");

        recording.StartRecording();
        recording.OnPageTransition("https://example.com/a", CreateSnapshot("A", "https://example.com/a"));
        recording.OnPageTransition("https://example.com/b", CreateSnapshot("B", "https://example.com/b"));

        recording.StopRecording();

        Assert.Equal(2, recording.SessionSnapshots.Count);
        Assert.Equal(2, vm.Session.RecordedPages.Count);
    }

    [Fact]
    public void StopRecording_WithSnapshotsAndPromptYes_TransfersAndClearsSession()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var dialogs = Substitute.For<IMessageDialogService>();
        dialogs.ShowYesNo(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var (_, _, vm, _) = CreateViewModel(recording, dialogs);
        vm.Session.Load(11, "Example Site");

        recording.StartRecording();
        recording.OnPageTransition("https://example.com/profile", CreateSnapshot("Profile", "https://example.com/profile"));
        recording.OnPageTransition("https://example.com/reports", CreateSnapshot("Reports", "https://example.com/reports"));

        recording.StopRecording();

        Assert.Empty(recording.SessionSnapshots);
        Assert.Empty(vm.Session.RecordedPages);
        Assert.Equal(2, vm.Session.CorpusPages.Count);
        Assert.Contains(vm.Session.CorpusPages, p => p.Name == "Profile");
        Assert.Contains(vm.Session.CorpusPages, p => p.Name == "Reports");
    }

    [Fact]
    public void Dispose_UnsubscribesFromRecordingEvents()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var (_, _, vm, _) = CreateViewModel(recording);

        vm.Dispose();

        recording.StartRecording();
        Assert.False(vm.Session.IsRecording);

        recording.OnPageTransition("https://example.com/after-dispose", CreateSnapshot("AfterDispose", "https://example.com/after-dispose"));
        Assert.Empty(vm.Session.RecordedPages);
    }

    [Fact]
    public void RemoveSelectedRecordingCommand_RemovesSelectedSnapshot()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var (_, _, vm, _) = CreateViewModel(recording);

        recording.StartRecording();
        recording.OnPageTransition("https://example.com/a", CreateSnapshot("A", "https://example.com/a"));
        recording.OnPageTransition("https://example.com/b", CreateSnapshot("B", "https://example.com/b"));

        Assert.Equal(2, recording.SessionSnapshots.Count);
        vm.Session.SelectedRecordingPage = vm.Session.RecordedPages[0];

        vm.Session.RemoveSelectedRecordingCommand.Execute(null);

        Assert.Single(recording.SessionSnapshots);
        Assert.Single(vm.Session.RecordedPages);
    }

    [Fact]
    public void ClearRecordingsCommand_WhenConfirmed_ClearsAllSnapshots()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var dialogs = Substitute.For<IMessageDialogService>();
        dialogs.ShowYesNo("Clear all recorded pages from this session?", "Clear Recordings").Returns(true);
        var (_, _, vm, _) = CreateViewModel(recording, dialogs);

        recording.StartRecording();
        recording.OnPageTransition("https://example.com/a", CreateSnapshot("A", "https://example.com/a"));
        recording.OnPageTransition("https://example.com/b", CreateSnapshot("B", "https://example.com/b"));

        vm.Session.ClearRecordingsCommand.Execute(null);

        Assert.Empty(recording.SessionSnapshots);
        Assert.Empty(vm.Session.RecordedPages);
    }

    [Fact]
    public void RemoveSelectedCorpusCommand_WhenCanceled_DoesNotDeleteCorpusPage()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var dialogs = Substitute.For<IMessageDialogService>();
        dialogs.ShowYesNo(Arg.Any<string>(), "Remove Corpus Page").Returns(false);
        var (_, _, vm, corpusService) = CreateViewModel(recording, dialogs);

        corpusService.StoreSnapshot(44, CreateSnapshot("Login", "https://example.com/login"));
        vm.Session.Load(44, "Example Site");
        Assert.Single(vm.Session.CorpusPages);

        vm.Session.SelectedCorpusPage = vm.Session.CorpusPages[0];
        vm.Session.RemoveSelectedCorpusCommand.Execute(null);

        Assert.Single(vm.Session.CorpusPages);
    }

    [Fact]
    public void TransferSessionToCorpusCommand_TransfersAndClearsSnapshots()
    {
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var (_, _, vm, _) = CreateViewModel(recording);
        vm.Session.Load(31, "Example Site");

        recording.StartRecording();
        recording.OnPageTransition("https://example.com/a", CreateSnapshot("A", "https://example.com/a"));
        recording.OnPageTransition("https://example.com/b", CreateSnapshot("B", "https://example.com/b"));
        recording.StopRecording();

        vm.Session.TransferSessionToCorpusCommand.Execute(null);

        Assert.Empty(recording.SessionSnapshots);
        Assert.Empty(vm.Session.RecordedPages);
        Assert.Equal(2, vm.Session.CorpusPages.Count);
    }

    private (BrowserViewModel browser, SessionPanelViewModel session, ScrapingTabViewModel vm, CorpusService corpusService) CreateViewModel(
        RecordingViewModel? recordingOverride = null,
        IMessageDialogService? dialogOverride = null)
    {
        var browser = new BrowserViewModel(NullLogger<BrowserViewModel>.Instance);
        var inspector = new InspectorViewModel();
        var recording = recordingOverride ?? new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var dialogs = dialogOverride ?? Substitute.For<IMessageDialogService>();

        var dbPath = Path.Combine(Path.GetTempPath(), $"scraping-tab-test-{Guid.NewGuid()}.db");
        _dbPaths.Add(dbPath);

        var corpusService = new CorpusService($"Data Source={dbPath}", NullLogger<CorpusService>.Instance);
        var registry = Substitute.For<IControlRegistry>();
        registry.GetAllControls().Returns(Array.Empty<GeneratedControl>());
        var session = new SessionPanelViewModel(corpusService, registry, NullLogger<SessionPanelViewModel>.Instance);

        var vm = new ScrapingTabViewModel(
            browser,
            inspector,
            recording,
            session,
            corpusService,
            dialogs,
            new DomCaptureService(NullLogger<DomCaptureService>.Instance),
            new ElementHighlightService(NullLogger<ElementHighlightService>.Instance),
            new PageTransitionDetector(NullLogger<PageTransitionDetector>.Instance),
            new ControlGroupDetector(),
            NullLogger<ScrapingTabViewModel>.Instance);

        return (browser, session, vm, corpusService);
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

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var path in _dbPaths)
        {
            try { File.Delete(path); } catch { }
        }
    }
}
