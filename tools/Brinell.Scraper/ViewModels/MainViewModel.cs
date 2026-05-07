using System.IO;
using System.Text.Json;
using System.Windows.Input;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly CorpusDatabase _db;
    private readonly DomCaptureService _domCapture;
    private readonly ElementHighlightService _highlight;
    private readonly PageTransitionDetector _pageTransition;
    private readonly SnapshotExportService _exportService;
    private readonly ControlGroupDetector _controlGroupDetector;
    private readonly ILogger<MainViewModel> _logger;
    private bool _copilotInitialized;
    private object? _activeView;
    private SiteInfo? _activeSite;
    private string _siteName = string.Empty;
    private string _windowTitle = "Brinell Scraper";
    private bool _isLogViewerVisible;

    public MainViewModel(CorpusDatabase db, BrowserViewModel browser, SidebarViewModel sidebar, SiteSelectionViewModel siteSelection, InspectorViewModel inspector, RecordingViewModel recording, DomCaptureService domCapture, ElementHighlightService highlight, PageTransitionDetector pageTransition, SnapshotExportService exportService, ControlGroupDetector controlGroupDetector, ILogger<MainViewModel> logger)
    {
        _db = db;
        _domCapture = domCapture;
        _highlight = highlight;
        _pageTransition = pageTransition;
        _exportService = exportService;
        _controlGroupDetector = controlGroupDetector;
        _logger = logger;
        _logger.LogInformation("MainViewModel initialized");
        Browser = browser;
        Sidebar = sidebar;
        SiteSelection = siteSelection;
        Inspector = inspector;
        Recording = recording;

        SwitchSiteCommand = new RelayCommand(ShowSiteSelector);
        ManageControlsCommand = new RelayCommand(ShowControlsManager, () => HasActiveSite);
        BrowseCorpusCommand = new RelayCommand(ShowCorpusBrowser, () => HasActiveSite);
        InspectCommand = new AsyncRelayCommand(ToggleInspectAsync, () => HasActiveSite);
        RecordCommand = new RelayCommand(ToggleRecording, () => HasActiveSite);
        RecordPageCommand = new AsyncRelayCommand(RecordPageAsync, () => HasActiveSite);
        AnalyzeSessionCommand = new RelayCommand(AnalyzeSession);
        AnalyzeCommand = new AsyncRelayCommand(RunAnalysisAsync, () => HasActiveSite);
        ExportCorpusCommand = new RelayCommand(ExportCorpus, () => HasActiveSite);
        ImportSnapshotCommand = new RelayCommand(ImportSnapshot, () => HasActiveSite);

        SiteSelection.SiteSelected += OnSiteSelected;
        Browser.ElementSelected += OnElementSelected;
        Browser.NavigationSucceeded += OnNavigationSucceeded;
        Browser.IFrameNavigationSucceeded += OnIFrameNavigationSucceeded;
        Recording.AnalyzePromptRequested += OnAnalyzePromptRequested;
        Recording.RecordingStarted += OnRecordingStarted;
        Recording.RecordingStopped += OnRecordingStopped;

        // Step 7: DOM tree ↔ browser sync
        Inspector.DomTree.ElementHovered += OnTreeElementHovered;
        Inspector.DomTree.ElementUnhovered += OnTreeElementUnhovered;
        Inspector.DomTree.ElementClicked += OnTreeElementClicked;

        Sidebar.SetNavigateCallback(url =>
        {
            Browser.AddressUrl = url;
            Browser.NavigateCommand.Execute(null);
        });
    }

    public BrowserViewModel Browser { get; }
    public SidebarViewModel Sidebar { get; }
    public SiteSelectionViewModel SiteSelection { get; }
    public InspectorViewModel Inspector { get; }
    public RecordingViewModel Recording { get; }

    public object? ActiveView
    {
        get => _activeView;
        set => SetProperty(ref _activeView, value);
    }

    public SiteInfo? ActiveSite
    {
        get => _activeSite;
        private set
        {
            if (SetProperty(ref _activeSite, value))
            {
                OnPropertyChanged(nameof(HasActiveSite));
                RaiseAllCommandStates();
            }
        }
    }

    public bool HasActiveSite => _activeSite is not null;

    public string SiteName
    {
        get => _siteName;
        private set => SetProperty(ref _siteName, value);
    }

    public string WindowTitle
    {
        get => _windowTitle;
        private set => SetProperty(ref _windowTitle, value);
    }

    public bool IsLogViewerVisible
    {
        get => _isLogViewerVisible;
        set => SetProperty(ref _isLogViewerVisible, value);
    }

    public ICommand SwitchSiteCommand { get; }
    public ICommand ManageControlsCommand { get; }
    public ICommand BrowseCorpusCommand { get; }
    public ICommand InspectCommand { get; }
    public ICommand RecordCommand { get; }
    public ICommand RecordPageCommand { get; }
    public ICommand AnalyzeSessionCommand { get; }
    public ICommand AnalyzeCommand { get; }
    public ICommand ExportCorpusCommand { get; }
    public ICommand ImportSnapshotCommand { get; }

    // Called by MainWindow after views are created
    public event Action? SiteSelectorRequested;
    public event Action? BrowserViewRequested;
    public event Action<CorpusBrowserViewModel>? CorpusBrowserRequested;
    public event Action<ControlsManagerViewModel>? ControlsManagerRequested;
    public event Action<AnalysisViewModel>? AnalysisViewRequested;

    public void ShowSiteSelector()
    {
        SiteSelectorRequested?.Invoke();
    }

    private void OnSiteSelected(SiteInfo site)
    {
        ActiveSite = site;
        SiteName = site.Name;
        WindowTitle = $"Brinell Scraper \u2014 {site.Name}";
        Browser.AddressUrl = site.StartUrl;

        Sidebar.SiteHeader = site.Name;
        Sidebar.CorpusStats = $"{site.PageCount} pages \u00b7 {site.ControlCount} controls";
        Sidebar.ClearSession();

        // RCA-022: Load persisted pages from DB
        var pages = _db.GetPages(site.Id);
        Sidebar.LoadCorpusPages(pages.Select(p => new SidebarPageItem
        {
            PageId = p.Id,
            Name = p.Name,
            Url = p.Url,
            StatusIcon = "\ud83d\udcc4"
        }));

        BrowserViewRequested?.Invoke();
    }

    private void RaiseAllCommandStates()
    {
        ((RelayCommand)ManageControlsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)BrowseCorpusCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)InspectCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RecordCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)RecordPageCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)AnalyzeCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ExportCorpusCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ImportSnapshotCommand).RaiseCanExecuteChanged();
    }

    // Step 3: Recording lifecycle with SPA transition detector
    private async void OnRecordingStarted()
    {
        Sidebar.IsRecording = true;
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is not null)
        {
            _pageTransition.PageTransitionDetected += OnSpaTransitionDetected;
            await _pageTransition.StartAsync(webView);
            _logger.LogInformation("SPA transition detector started");
        }
    }

    private async void OnRecordingStopped()
    {
        Sidebar.IsRecording = false;
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is not null && _pageTransition.IsActive)
        {
            _pageTransition.PageTransitionDetected -= OnSpaTransitionDetected;
            await _pageTransition.StopAsync(webView);
            _logger.LogInformation("SPA transition detector stopped");
        }
    }

    private async void OnSpaTransitionDetected(string url)
    {
        if (!Recording.IsRecording) return;
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null) return;

        // Small delay for DOM to settle after SPA navigation
        await Task.Delay(600);

        var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
        snapshot.SiteName = ActiveSite?.Name ?? "";
        snapshot.PageName = snapshot.PageTitle;

        if (Recording.OnPageTransition(url, snapshot))
        {
            Sidebar.AddSessionPage(snapshot);
            _logger.LogInformation("SPA transition captured: {PageName} ({Url})", snapshot.PageName, url);
        }
    }

    // Step 4: Command handlers
    private void ShowCorpusBrowser()
    {
        var vm = App.Services.GetRequiredService<CorpusBrowserViewModel>();
        var corpusService = App.Services.GetRequiredService<CorpusService>();
        if (ActiveSite is not null)
            vm.Load(corpusService, ActiveSite.Id);
        CorpusBrowserRequested?.Invoke(vm);
    }

    private void ShowControlsManager()
    {
        var vm = App.Services.GetRequiredService<ControlsManagerViewModel>();
        vm.LoadControls();
        ControlsManagerRequested?.Invoke(vm);
    }

    private async Task RunAnalysisAsync(CancellationToken ct)
    {
        if (ActiveSite is null) return;

        await EnsureCopilotInitializedAsync();

        var vm = App.Services.GetRequiredService<AnalysisViewModel>();
        AnalysisViewRequested?.Invoke(vm);

        await vm.RunAnalysisAsync(ActiveSite.Id, ct);
    }

    // Step 2: Lazy CopilotService init
    private async Task EnsureCopilotInitializedAsync()
    {
        if (_copilotInitialized) return;
        var copilot = App.Services.GetRequiredService<ICopilotService>();
        await copilot.InitializeAsync();
        _copilotInitialized = true;
        _logger.LogInformation("CopilotService initialized (lazy)");
    }

    // Step 5: Export / Import
    private void ExportCorpus()
    {
        if (ActiveSite is null) return;

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Corpus Pages",
            Filter = "JSON files (*.json)|*.json",
            FileName = $"{ActiveSite.Name}-corpus.json"
        };

        if (dlg.ShowDialog() != true) return;

        var pages = _db.GetPages(ActiveSite.Id);
        var snapshots = new List<DomSnapshot>();
        foreach (var page in pages)
        {
            var json = _db.GetPageSnapshot(page.Id);
            if (json is not null)
            {
                var snapshot = JsonSerializer.Deserialize<DomSnapshot>(json, JsonOptions);
                if (snapshot is not null)
                    snapshots.Add(snapshot);
            }
        }

        var exportJson = JsonSerializer.Serialize(snapshots, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        File.WriteAllText(dlg.FileName, exportJson);

        Browser.StatusText = $"Exported {snapshots.Count} pages to {Path.GetFileName(dlg.FileName)}";
        _logger.LogInformation("Corpus exported — {PageCount} pages to {Path}", snapshots.Count, dlg.FileName);
    }

    private void ImportSnapshot()
    {
        if (ActiveSite is null) return;

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Snapshot(s)",
            Filter = "JSON files (*.json)|*.json",
            Multiselect = false
        };

        if (dlg.ShowDialog() != true) return;

        var fileJson = File.ReadAllText(dlg.FileName);
        var importedCount = 0;

        // Try array first, then single object
        List<DomSnapshot>? snapshots = null;
        try
        {
            snapshots = JsonSerializer.Deserialize<List<DomSnapshot>>(fileJson, JsonOptions);
        }
        catch
        {
            var single = JsonSerializer.Deserialize<DomSnapshot>(fileJson, JsonOptions);
            if (single is not null)
                snapshots = [single];
        }

        if (snapshots is null || snapshots.Count == 0)
        {
            System.Windows.MessageBox.Show("No valid snapshots found in the file.", "Import Failed",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        foreach (var snapshot in snapshots)
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            var elementCount = DomCaptureService.CountElements(snapshot.RootElement);
            var pageId = _db.SavePage(ActiveSite.Id, snapshot.PageName, snapshot.PageUrl,
                snapshot.PageTitle, elementCount, json);

            Sidebar.CorpusPages.Add(new SidebarPageItem
            {
                PageId = pageId,
                Name = snapshot.PageName,
                Url = snapshot.PageUrl,
                StatusIcon = "\ud83d\udcc4"
            });
            importedCount++;
        }

        _db.UpdateSitePageCount(ActiveSite.Id);
        ActiveSite.PageCount = Sidebar.CorpusPages.Count;
        Sidebar.CorpusStats = $"{ActiveSite.PageCount} pages \u00b7 {ActiveSite.ControlCount} controls";

        Browser.StatusText = $"Imported {importedCount} pages from {Path.GetFileName(dlg.FileName)}";
        _logger.LogInformation("Corpus imported — {PageCount} pages from {Path}", importedCount, dlg.FileName);
    }

    private async Task ToggleInspectAsync(CancellationToken ct)
    {
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null)
        {
            _logger.LogWarning("Cannot inspect: WebView2 is not initialized");
            return;
        }

        if (Inspector.IsInspecting)
        {
            await _highlight.DisableAsync(webView);
            Inspector.IsInspecting = false;
            _logger.LogInformation("Inspect mode disabled");
        }
        else
        {
            var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
            Inspector.LoadSnapshot(snapshot);
            await _highlight.EnableAsync(webView);
            Inspector.IsInspecting = true;

            // Step 8: Auto-detect control groups
            var groups = _controlGroupDetector.Detect(snapshot.RootElement);
            Inspector.LoadControlGroups(groups);

            _logger.LogInformation("Inspect mode enabled — {ElementCount} elements captured, {GroupCount} control groups detected",
                Inspector.TotalElementCount, groups.Count);
        }
    }

    private async void OnNavigationSucceeded()
    {
        if (Inspector.IsInspecting)
        {
            var webView = Browser.GetCoreWebView2?.Invoke();
            if (webView is not null)
            {
                var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
                Inspector.LoadSnapshot(snapshot);
                await _highlight.EnableAsync(webView, force: true);
                _logger.LogInformation("Inspect mode refreshed after navigation \u2014 {ElementCount} elements",
                    Inspector.TotalElementCount);
            }
        }

        if (Recording.IsRecording)
        {
            var webView = Browser.GetCoreWebView2?.Invoke();
            if (webView is not null)
            {
                var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
                snapshot.SiteName = ActiveSite?.Name ?? "";
                snapshot.PageName = snapshot.PageTitle;

                if (Recording.OnPageTransition(snapshot.PageUrl, snapshot))
                {
                    Sidebar.AddSessionPage(snapshot);
                }
            }
        }
    }

    private async void OnIFrameNavigationSucceeded()
    {
        if (!Recording.IsRecording) return;

        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null) return;

        var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
        snapshot.SiteName = ActiveSite?.Name ?? "";
        snapshot.PageName = $"[iframe] {snapshot.PageTitle}";

        if (Recording.OnPageTransition(snapshot.PageUrl, snapshot))
        {
            Sidebar.AddSessionPage(snapshot);
            _logger.LogInformation("IFrame page captured: {PageName} ({Url})", snapshot.PageName, snapshot.PageUrl);
        }
    }

    private async Task RecordPageAsync(CancellationToken ct)
    {
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null)
        {
            _logger.LogWarning("Cannot record page: WebView2 is not initialized");
            return;
        }

        var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
        snapshot.SiteName = ActiveSite?.Name ?? "";
        snapshot.PageName = snapshot.PageTitle;

        if (Recording.IsRecording)
        {
            // During recording: add to session (dedup applies)
            if (Recording.OnPageTransition(snapshot.PageUrl, snapshot))
                Sidebar.AddSessionPage(snapshot);
        }
        else if (ActiveSite is not null)
        {
            // Outside recording: add directly to corpus with DB persistence
            var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);

            // Check for duplicate URL in corpus
            var existing = Sidebar.CorpusPages.FirstOrDefault(p =>
                string.Equals(p.Url, snapshot.PageUrl, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                // RCA-019: Check if iframe content differs before prompting
                var newIframeSources = CollectIFrameSources(snapshot.RootElement);
                var iframeContentDiffers = false;

                if (newIframeSources.Count > 0 && existing.PageId > 0)
                {
                    var existingJson = _db.GetPageSnapshot(existing.PageId);
                    if (existingJson is not null)
                    {
                        var existingSnapshot = JsonSerializer.Deserialize<DomSnapshot>(existingJson, JsonOptions);
                        if (existingSnapshot is not null)
                        {
                            var existingIframeSources = CollectIFrameSources(existingSnapshot.RootElement);
                            if (!newIframeSources.SequenceEqual(existingIframeSources))
                            {
                                iframeContentDiffers = true;
                                var iframeContext = newIframeSources[0];
                                if (Uri.TryCreate(iframeContext, UriKind.Absolute, out var iframeUri))
                                {
                                    var lastSegment = iframeUri.Segments.LastOrDefault()?.TrimEnd('/');
                                    if (!string.IsNullOrEmpty(lastSegment))
                                        iframeContext = Uri.UnescapeDataString(lastSegment);
                                }
                                snapshot.PageName = $"{snapshot.PageTitle} \u2014 [iframe: {iframeContext}]";
                                snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
                            }
                        }
                    }
                }

                if (!iframeContentDiffers)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"This page is already recorded:\n{snapshot.PageUrl}\n\nOverwrite?",
                        "Duplicate Page",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question);

                    if (result != System.Windows.MessageBoxResult.Yes)
                        return;

                    if (existing.PageId > 0)
                        _db.DeletePage(existing.PageId);
                    Sidebar.CorpusPages.Remove(existing);
                }
            }

            // RCA-022: Persist to SQLite
            var elementCount = DomCaptureService.CountElements(snapshot.RootElement);
            var pageId = _db.SavePage(ActiveSite.Id, snapshot.PageName, snapshot.PageUrl,
                snapshot.PageTitle, elementCount, snapshotJson);

            Sidebar.CorpusPages.Add(new SidebarPageItem
            {
                PageId = pageId,
                Name = snapshot.PageName,
                Url = snapshot.PageUrl,
                StatusIcon = "\ud83d\udcc4"
            });

            // RCA-020: Update corpus page count and stats
            _db.UpdateSitePageCount(ActiveSite.Id);
            ActiveSite.PageCount = Sidebar.CorpusPages.Count;
            Sidebar.CorpusStats = $"{ActiveSite.PageCount} pages \u00b7 {ActiveSite.ControlCount} controls";
        }

        Browser.StatusText = $"Page captured: {snapshot.PageName}";
        _logger.LogInformation("Page manually captured: {PageName} ({Url})", snapshot.PageName, snapshot.PageUrl);
    }

    private void ToggleRecording()
    {
        if (Recording.IsRecording)
            Recording.StopRecording();
        else
            Recording.StartRecording();
    }

    private void OnAnalyzePromptRequested()
    {
        var capturedCount = Recording.SessionSnapshots.Count;
        if (capturedCount == 0)
            return;

        var result = System.Windows.MessageBox.Show(
            $"{capturedCount} pages captured. Transfer to corpus and analyze now?",
            "Recording Complete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            AnalyzeSession();
        }
    }

    private void AnalyzeSession()
    {
        if (ActiveSite is null || Recording.SessionSnapshots.Count == 0)
            return;

        var count = Recording.SessionSnapshots.Count;
        foreach (var snapshot in Recording.SessionSnapshots)
        {
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            var elementCount = DomCaptureService.CountElements(snapshot.RootElement);
            var pageId = _db.SavePage(ActiveSite.Id, snapshot.PageName, snapshot.PageUrl,
                snapshot.PageTitle, elementCount, json);

            Sidebar.CorpusPages.Add(new SidebarPageItem
            {
                PageId = pageId,
                Name = snapshot.PageName,
                Url = snapshot.PageUrl,
                StatusIcon = "\ud83d\udcc4"
            });
        }

        _db.UpdateSitePageCount(ActiveSite.Id);
        ActiveSite.PageCount = Sidebar.CorpusPages.Count;
        Sidebar.CorpusStats = $"{ActiveSite.PageCount} pages \u00b7 {ActiveSite.ControlCount} controls";

        Recording.ClearSnapshots();
        Sidebar.ClearSession();

        _logger.LogInformation("Session analyzed \u2014 {PageCount} pages transferred to corpus", count);
    }

    private void OnElementSelected(WebViewMessage msg)
    {
        if (Inspector.Snapshot is null) return;

        var element = FindElement(Inspector.Snapshot.RootElement, msg);
        if (element is not null)
        {
            Inspector.ToggleElement(element);
            _logger.LogDebug("Element {Action}: <{Tag}> id={Id}", 
                Inspector.SelectedElements.Contains(element) ? "selected" : "deselected",
                msg.Tag, msg.Id);
        }
    }

    private static DomElement? FindElement(DomElement root, WebViewMessage msg)
    {
        // Match by bounding box first (most precise), then fall back to id + tag
        if (msg.BoundingBox is { } box && root.BoundingBox is { } rb
            && Math.Abs(rb.X - box.X) < 1 && Math.Abs(rb.Y - box.Y) < 1
            && Math.Abs(rb.Width - box.Width) < 1 && Math.Abs(rb.Height - box.Height) < 1)
        {
            return root;
        }

        if (root.BoundingBox is null
            && string.Equals(root.Tag, msg.Tag, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(msg.Id) && msg.Id == root.Id)
        {
            return root;
        }

        foreach (var child in root.Children)
        {
            var found = FindElement(child, msg);
            if (found is not null) return found;
        }

        return null;
    }

    // Step 7: DOM tree ↔ browser sync handlers
    private async void OnTreeElementHovered(DomElement element)
    {
        if (element.BoundingBox is null) return;
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null) return;
        await _highlight.HighlightElementByBoundsAsync(webView, element.BoundingBox);
    }

    private async void OnTreeElementUnhovered()
    {
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null) return;
        await _highlight.ClearTreeHighlightAsync(webView);
    }

    private async void OnTreeElementClicked(DomElement element)
    {
        if (element.BoundingBox is null) return;
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null) return;
        await _highlight.ScrollToElementAsync(webView, element.BoundingBox);
    }

    private static List<string> CollectIFrameSources(DomElement element)
    {
        var sources = new List<string>();
        if (element.FrameSource is { Length: > 0 } src)
            sources.Add(src);
        foreach (var child in element.Children)
            sources.AddRange(CollectIFrameSources(child));
        return sources;
    }
}
