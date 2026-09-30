using System.ComponentModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class ScrapingTabViewModel : ViewModelBase, IDisposable
{
    private readonly CorpusService _corpusService;
    private readonly IMessageDialogService _dialogs;
    private readonly DomCaptureService _domCapture;
    private readonly ElementHighlightService _highlight;
    private readonly PageTransitionDetector _pageTransition;
    private readonly ControlGroupDetector _controlGroupDetector;
    private readonly ILogger<ScrapingTabViewModel> _logger;
    private bool _isSessionPanelVisible = true;
    private bool _disposed;

    public ScrapingTabViewModel(
        BrowserViewModel browser,
        InspectorViewModel inspector,
        RecordingViewModel recording,
        SessionPanelViewModel session,
        CorpusService corpusService,
        IMessageDialogService dialogs,
        DomCaptureService domCapture,
        ElementHighlightService highlight,
        PageTransitionDetector pageTransition,
        ControlGroupDetector controlGroupDetector,
        ILogger<ScrapingTabViewModel> logger)
    {
        Browser = browser;
        Inspector = inspector;
        Recording = recording;
        Session = session;
        _corpusService = corpusService;
        _dialogs = dialogs;
        _domCapture = domCapture;
        _highlight = highlight;
        _pageTransition = pageTransition;
        _controlGroupDetector = controlGroupDetector;
        _logger = logger;

        Inspector.PropertyChanged += OnInspectorPropertyChanged;
        Inspector.CaptureSnapshotRequested += OnManualCaptureSnapshotAsync;
        Inspector.DomTree.ElementHovered += OnTreeElementHovered;
        Inspector.DomTree.ElementUnhovered += OnTreeElementUnhovered;
        Inspector.DomTree.ElementClicked += OnTreeElementClicked;
        Inspector.ElementSelectionChanged += OnElementSelectionChanged;
        Inspector.SelectionCleared += OnSelectionCleared;
        Browser.ElementSelected += OnBrowserElementSelected;
        Recording.SessionSnapshots.CollectionChanged += OnSessionSnapshotsChanged;
        Recording.RecordingStarted += OnRecordingStarted;
        Recording.RecordingStopped += OnRecordingStopped;
        Recording.AnalyzePromptRequested += OnAnalyzePromptRequested;
        Browser.NavigationSucceeded += OnNavigationSucceeded;
        Browser.IFrameNavigationSucceededWithUrl += OnIFrameNavigationSucceeded;

        Session.SetNavigateCallback(url =>
        {
            NavigateToSessionUrl(url);
        });
        Session.SetRemoveCallbacks(RemoveCorpusPage, RemoveRecordedPage, ClearRecordedPages);
        Session.SetTransferSessionToCorpusCallback(TransferSessionToCorpusFromButton);

        Session.IsRecording = Recording.IsRecording;
        Session.SyncRecordedPages(Recording.SessionSnapshots);

        ToggleSessionPanelCommand = new RelayCommand(
            () => IsSessionPanelVisible = !IsSessionPanelVisible);
    }

    public BrowserViewModel Browser { get; }
    public InspectorViewModel Inspector { get; }
    public RecordingViewModel Recording { get; }
    public SessionPanelViewModel Session { get; }

    public bool IsSessionPanelVisible
    {
        get => _isSessionPanelVisible;
        set => SetProperty(ref _isSessionPanelVisible, value);
    }

    public bool IsInspectorVisible => Inspector.IsInspecting;

    public ICommand ToggleSessionPanelCommand { get; }

    private async void OnInspectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(InspectorViewModel.IsInspecting))
            return;

        OnPropertyChanged(nameof(IsInspectorVisible));

        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null) return;

        if (Inspector.IsInspecting)
        {
            var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
            snapshot.SiteName = Session.SiteHeader;
            snapshot.PageName = snapshot.PageTitle;
            Inspector.LoadSnapshot(snapshot);
            RunAutoGroupDetection(snapshot);
            await _highlight.EnableAsync(webView);
            _logger.LogInformation("Inspect mode enabled — {ElementCount} elements", Inspector.TotalElementCount);
        }
        else
        {
            await _highlight.DisableAsync(webView);
        }
    }

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

    private async Task OnManualCaptureSnapshotAsync()
    {
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null)
        {
            _logger.LogWarning("Capture DOM Snapshot: no WebView available");
            Browser.StatusText = "Capture failed — browser not ready";
            return;
        }

        // Capture only the top-level page DOM. Iframes are recorded separately.
        var snapshot = await _domCapture.CaptureAsync(webView, null);
        snapshot.SiteName = Session.SiteHeader;
        snapshot.PageName = snapshot.PageTitle;

        // Manual capture uses the same path as automatic recording for consistency.
        if (Recording.OnPageTransition(snapshot.PageUrl, snapshot, "top-level"))
        {
            Browser.StatusText = "Snapshot added to This Session";
            _logger.LogInformation("Manual capture → session: {PageName} ({Url})", snapshot.PageName, snapshot.PageUrl);
        }

        // Emit iframe session entries for any tracked frames.
        await CaptureTrackedFramesForSessionAsync();

        Inspector.LoadSnapshot(snapshot);
        RunAutoGroupDetection(snapshot);
        Inspector.IsInspecting = true;
    }

    private async Task CaptureTrackedFramesForSessionAsync()
    {
        if (_highlight.TrackedFrames.Count == 0)
            return;

        foreach (var frame in _highlight.TrackedFrames.ToArray())
        {
            try
            {
                // Extract frame URL via script execution.
                var frameUrlJson = await frame.ExecuteScriptAsync("window.location.href");
                if (frameUrlJson is null || frameUrlJson == "null")
                    continue;

                // Result comes back as a JSON string, so unwrap it.
                var frameUrl = frameUrlJson.Trim('"');
                if (string.IsNullOrWhiteSpace(frameUrl) || frameUrl == "about:blank")
                    continue;

                // Create a minimal iframe session entry.
                var iframeSnapshot = new DomSnapshot
                {
                    SiteName = Session.SiteHeader,
                    PageName = $"[iframe] {frameUrl}",
                    PageUrl = frameUrl,
                    PageTitle = frameUrl,
                    CapturedAt = DateTimeOffset.UtcNow,
                    RootElement = new DomElement { Tag = "html" }
                };

                // Add iframe to session using iframe source type for proper dedupe isolation.
                if (Recording.OnPageTransition(frameUrl, iframeSnapshot, "iframe"))
                {
                    _logger.LogInformation("Manual iframe capture: {PageName} ({Url})", iframeSnapshot.PageName, frameUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to capture tracked frame URL");
            }
        }
    }

    private void RunAutoGroupDetection(DomSnapshot snapshot)
    {
        var groups = _controlGroupDetector.Detect(snapshot.RootElement);
        Inspector.LoadControlGroups(groups);
        _logger.LogInformation("Auto-detected {Count} control groups", groups.Count);
    }

    private void NavigateToSessionUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!CanNavigateToTargetUrl(url))
            return;

        Browser.AddressUrl = url;
        if (Browser.NavigateCommand.CanExecute(null))
            Browser.NavigateCommand.Execute(null);
    }

    private bool CanNavigateToTargetUrl(string targetUrl)
    {
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
            return false;

        if (!Uri.TryCreate(Browser.AddressUrl, UriKind.Absolute, out var currentUri))
            return true;

        if (string.Equals(currentUri.Host, targetUri.Host, StringComparison.OrdinalIgnoreCase))
            return true;

        return _dialogs.ShowYesNo(
            $"Selected page belongs to another site ({targetUri.Host}). Navigate anyway?",
            "Navigate to Selected Page");
    }

    private void RemoveCorpusPage(SidebarPageItem item)
    {
        if (Session.SiteId <= 0 || string.IsNullOrWhiteSpace(item.Name))
            return;

        var confirmed = _dialogs.ShowYesNo(
            $"Delete corpus page '{item.Name}' and all snapshots?",
            "Remove Corpus Page");
        if (!confirmed)
            return;

        _corpusService.DeletePageByName(Session.SiteId, item.Name);
        Session.Load(Session.SiteId, Session.SiteHeader);
    }

    private void RemoveRecordedPage(SidebarPageItem item)
    {
        var snapshot = Recording.SessionSnapshots.FirstOrDefault(s =>
            string.Equals(s.PageUrl, item.Url, StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.PageName, item.Name, StringComparison.Ordinal));

        if (snapshot is null)
            return;

        Recording.SessionSnapshots.Remove(snapshot);
    }

    private void ClearRecordedPages()
    {
        if (Recording.SessionSnapshots.Count == 0)
            return;

        var confirmed = _dialogs.ShowYesNo(
            "Clear all recorded pages from this session?",
            "Clear Recordings");
        if (!confirmed)
            return;

        Recording.ClearSnapshots();
    }

    private void OnSessionSnapshotsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Session.SyncRecordedPages(Recording.SessionSnapshots);
    }

    private async void OnRecordingStarted()
    {
        Session.IsRecording = true;

        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null)
            return;

        _pageTransition.PageTransitionDetected += OnSpaTransitionDetected;
        await _pageTransition.StartAsync(webView);
        _logger.LogInformation("SPA transition detector started");
    }

    private async void OnRecordingStopped()
    {
        Session.IsRecording = false;

        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null || !_pageTransition.IsActive)
            return;

        _pageTransition.PageTransitionDetected -= OnSpaTransitionDetected;
        await _pageTransition.StopAsync(webView);
        _logger.LogInformation("SPA transition detector stopped");
    }

    private void OnAnalyzePromptRequested()
    {
        var capturedCount = Recording.SessionSnapshots.Count;
        if (capturedCount == 0)
            return;

        var confirmed = _dialogs.ShowYesNo(
            $"{capturedCount} pages captured. Transfer to corpus and analyze now?",
            "Recording Complete");

        if (confirmed)
            TransferSessionToCorpus();
    }

    private void TransferSessionToCorpusFromButton()
    {
        TransferSessionToCorpus();
    }

    private bool TransferSessionToCorpus()
    {
        if (Session.SiteId <= 0 || Recording.SessionSnapshots.Count == 0)
            return false;

        var snapshots = Recording.SessionSnapshots.ToList();
        foreach (var snapshot in snapshots)
        {
            _corpusService.StoreSnapshot(Session.SiteId, snapshot);
        }

        Session.Load(Session.SiteId, Session.SiteHeader);
        Recording.ClearSnapshots();
        Browser.StatusText = "Session transferred to corpus";

        _logger.LogInformation("Session transferred — {PageCount} pages moved to corpus", snapshots.Count);
        return true;
    }

    private void OnBrowserElementSelected(WebViewMessage msg)
    {
        if (Inspector.Snapshot is null) return;
        var element = FindElement(Inspector.Snapshot.RootElement, msg);
        if (element is not null)
            Inspector.ToggleElement(element);
    }

    private async void OnElementSelectionChanged(DomElement element, bool selected)
    {
        if (element.BoundingBox is null) return;
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null) return;
        await _highlight.SetElementSelectionHighlightAsync(webView, element.BoundingBox, selected);
    }

    private async void OnSelectionCleared()
    {
        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null) return;
        await _highlight.ClearAllSelectionHighlightsAsync(webView);
    }

    private static DomElement? FindElement(DomElement root, WebViewMessage msg)
    {
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

    private async void OnNavigationSucceeded()
    {
        await RefreshInspectAfterNavigationAsync(isIFrameNavigation: false);
        await CaptureTransitionAsync(isIFrameNavigation: false, urlOverride: null);
    }

    private async void OnIFrameNavigationSucceeded(string? frameUrl)
    {
        await RefreshInspectAfterNavigationAsync(isIFrameNavigation: true);
        await CaptureTransitionAsync(isIFrameNavigation: true, urlOverride: frameUrl);
    }

    private async void OnSpaTransitionDetected(string url)
    {
        if (!Recording.IsRecording)
            return;

        // Allow dynamic content to settle after SPA route changes.
        await Task.Delay(600);
        await CaptureTransitionAsync(isIFrameNavigation: false, urlOverride: url);
    }

    private async Task CaptureTransitionAsync(bool isIFrameNavigation, string? urlOverride)
    {
        if (!Recording.IsRecording)
            return;

        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null)
            return;

        var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
        snapshot.SiteName = Session.SiteHeader;
        var hasFrameUrl = isIFrameNavigation && !string.IsNullOrWhiteSpace(urlOverride);
        snapshot.PageName = hasFrameUrl ? $"[iframe] {snapshot.PageTitle}" : snapshot.PageTitle;

        var transitionUrl = string.IsNullOrWhiteSpace(urlOverride) ? snapshot.PageUrl : urlOverride;
        var sourceType = hasFrameUrl ? "iframe" : "top-level";
        if (Recording.OnPageTransition(transitionUrl, snapshot, sourceType))
        {
            _logger.LogInformation("Page captured: {PageName} ({Url})", snapshot.PageName, transitionUrl);
        }
    }

    private async Task RefreshInspectAfterNavigationAsync(bool isIFrameNavigation)
    {
        if (!Inspector.IsInspecting)
            return;

        var webView = Browser.GetCoreWebView2?.Invoke();
        if (webView is null)
            return;

        var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
        snapshot.SiteName = Session.SiteHeader;
        snapshot.PageName = isIFrameNavigation ? $"[iframe] {snapshot.PageTitle}" : snapshot.PageTitle;

        Inspector.LoadSnapshot(snapshot);
        RunAutoGroupDetection(snapshot);

        // Re-inject overlay listeners into the new document context after navigation.
        await _highlight.EnableAsync(webView, force: true);

        _logger.LogInformation(
            "Inspect mode refreshed after {NavigationKind} navigation — {ElementCount} elements",
            isIFrameNavigation ? "iframe" : "page",
            Inspector.TotalElementCount);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Inspector.PropertyChanged -= OnInspectorPropertyChanged;
        Inspector.CaptureSnapshotRequested -= OnManualCaptureSnapshotAsync;
        Inspector.DomTree.ElementHovered -= OnTreeElementHovered;
        Inspector.DomTree.ElementUnhovered -= OnTreeElementUnhovered;
        Inspector.DomTree.ElementClicked -= OnTreeElementClicked;
        Inspector.ElementSelectionChanged -= OnElementSelectionChanged;
        Inspector.SelectionCleared -= OnSelectionCleared;
        Browser.ElementSelected -= OnBrowserElementSelected;
        Recording.SessionSnapshots.CollectionChanged -= OnSessionSnapshotsChanged;
        Recording.RecordingStarted -= OnRecordingStarted;
        Recording.RecordingStopped -= OnRecordingStopped;
        Recording.AnalyzePromptRequested -= OnAnalyzePromptRequested;
        Browser.NavigationSucceeded -= OnNavigationSucceeded;
        Browser.IFrameNavigationSucceededWithUrl -= OnIFrameNavigationSucceeded;

        if (_pageTransition.IsActive)
            _pageTransition.PageTransitionDetected -= OnSpaTransitionDetected;
    }
}
