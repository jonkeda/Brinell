using System.IO;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.ViewModels;
using Brinell.Scraper.ViewModels.Tabs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Brinell.Scraper.Tests.ViewModels.Tabs;

public sealed class ScrapingTabViewModelInspectTests : IDisposable
{
    private readonly List<string> _dbPaths = [];

    [Fact]
    public void EnablingInspect_WithNoWebView_DoesNotThrow_AndUpdatesVisibility()
    {
        var (_, _, vm, _) = CreateViewModel();

        var ex = Record.Exception(() => vm.Inspector.IsInspecting = true);

        Assert.Null(ex);
        Assert.True(vm.IsInspectorVisible);
    }

    [Fact]
    public void BrowserElementSelected_WithMatchingBoundingBox_TogglesInspectorSelection()
    {
        var (browser, _, vm, _) = CreateViewModel();
        var target = new DomElement
        {
            Tag = "input",
            Id = "email",
            BoundingBox = new BoundingBox(10, 20, 100, 30)
        };
        vm.Inspector.LoadSnapshot(CreateSnapshotWithElements(target));

        browser.OnElementSelected(new WebViewMessage
        {
            Type = "elementSelected",
            Tag = "input",
            Id = "email",
            BoundingBox = new WebViewBoundingBox { X = 10, Y = 20, Width = 100, Height = 30 }
        });

        Assert.Single(vm.Inspector.SelectedElements);
        Assert.Same(target, vm.Inspector.SelectedElements[0]);
    }

    [Fact]
    public void BrowserElementSelected_WithNoMatchingElement_DoesNotChangeSelection()
    {
        var (browser, _, vm, _) = CreateViewModel();
        vm.Inspector.LoadSnapshot(CreateSnapshotWithElements(new DomElement
        {
            Tag = "button",
            Id = "save",
            BoundingBox = new BoundingBox(1, 1, 20, 20)
        }));

        browser.OnElementSelected(new WebViewMessage
        {
            Type = "elementSelected",
            Tag = "input",
            Id = "email",
            BoundingBox = new WebViewBoundingBox { X = 10, Y = 20, Width = 100, Height = 30 }
        });

        Assert.Empty(vm.Inspector.SelectedElements);
    }

    [Fact]
    public void Dispose_UnsubscribesBrowserSelectionEvents()
    {
        var (browser, _, vm, _) = CreateViewModel();
        var target = new DomElement
        {
            Tag = "input",
            Id = "email",
            BoundingBox = new BoundingBox(10, 20, 100, 30)
        };
        vm.Inspector.LoadSnapshot(CreateSnapshotWithElements(target));

        vm.Dispose();

        browser.OnElementSelected(new WebViewMessage
        {
            Type = "elementSelected",
            Tag = "input",
            Id = "email",
            BoundingBox = new WebViewBoundingBox { X = 10, Y = 20, Width = 100, Height = 30 }
        });

        Assert.Empty(vm.Inspector.SelectedElements);
    }

    private (BrowserViewModel browser, SessionPanelViewModel session, ScrapingTabViewModel vm, CorpusService corpusService) CreateViewModel()
    {
        var browser = new BrowserViewModel(NullLogger<BrowserViewModel>.Instance);
        var inspector = new InspectorViewModel();
        var recording = new RecordingViewModel(NullLogger<RecordingViewModel>.Instance);
        var dialogs = Substitute.For<IMessageDialogService>();

        var dbPath = Path.Combine(Path.GetTempPath(), $"scraping-tab-inspect-test-{Guid.NewGuid()}.db");
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

    private static DomSnapshot CreateSnapshotWithElements(params DomElement[] elements) => new()
    {
        SiteName = "Example",
        PageName = "Inspect",
        PageUrl = "https://example.com/inspect",
        PageTitle = "Inspect",
        CapturedAt = DateTimeOffset.UtcNow,
        RootElement = new DomElement
        {
            Tag = "html",
            BoundingBox = new BoundingBox(0, 0, 500, 500),
            Children = [.. elements]
        }
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
