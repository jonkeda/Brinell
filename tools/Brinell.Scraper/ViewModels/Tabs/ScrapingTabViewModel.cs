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
        _logger = logger;

        Inspector.PropertyChanged += OnInspectorPropertyChanged;
        Inspector.CaptureSnapshotRequested += OnManualCaptureSnapshotAsync;
        Recording.SessionSnapshots.CollectionChanged += OnSessionSnapshotsChanged;
        Recording.RecordingStarted += OnRecordingStarted;
        Recording.RecordingStopped += OnRecordingStopped;
        Recording.AnalyzePromptRequested += OnAnalyzePromptRequested;
        Browser.NavigationSucceeded += OnNavigationSucceeded;
        Browser.IFrameNavigationSucceeded += OnIFrameNavigationSucceeded;

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

    private void OnInspectorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InspectorViewModel.IsInspecting))
            OnPropertyChanged(nameof(IsInspectorVisible));
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

        var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
        snapshot.SiteName = Session.SiteHeader;
        snapshot.PageName = snapshot.PageTitle;

        if (Recording.IsRecording)
        {
            if (!Recording.OnPageTransition(snapshot.PageUrl, snapshot))
                Recording.SessionSnapshots.Add(snapshot);

            Browser.StatusText = "Snapshot added to This Session";
            _logger.LogInformation("Manual capture → session: {PageName} ({Url})", snapshot.PageName, snapshot.PageUrl);
        }
        else if (Session.SiteId > 0)
        {
            _corpusService.StoreSnapshot(Session.SiteId, snapshot);
            Session.Load(Session.SiteId, Session.SiteHeader);
            Browser.StatusText = "Snapshot saved to corpus";
            _logger.LogInformation("Manual capture → corpus: {PageName} ({Url})", snapshot.PageName, snapshot.PageUrl);
        }
        else
        {
            Recording.SessionSnapshots.Add(snapshot);
            Browser.StatusText = "Snapshot added to This Session";
            _logger.LogWarning("Manual capture: no active site — fallback to session snapshot");
        }

        Inspector.LoadSnapshot(snapshot);
        Inspector.IsInspecting = true;
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

    private async void OnNavigationSucceeded()
    {
        await CaptureTransitionAsync(isIFrameNavigation: false, urlOverride: null);
    }

    private async void OnIFrameNavigationSucceeded()
    {
        await CaptureTransitionAsync(isIFrameNavigation: true, urlOverride: null);
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
        snapshot.PageName = isIFrameNavigation ? $"[iframe] {snapshot.PageTitle}" : snapshot.PageTitle;

        var transitionUrl = string.IsNullOrWhiteSpace(urlOverride) ? snapshot.PageUrl : urlOverride;
        if (Recording.OnPageTransition(transitionUrl, snapshot))
        {
            _logger.LogInformation("Page captured: {PageName} ({Url})", snapshot.PageName, transitionUrl);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Inspector.PropertyChanged -= OnInspectorPropertyChanged;
        Inspector.CaptureSnapshotRequested -= OnManualCaptureSnapshotAsync;
        Recording.SessionSnapshots.CollectionChanged -= OnSessionSnapshotsChanged;
        Recording.RecordingStarted -= OnRecordingStarted;
        Recording.RecordingStopped -= OnRecordingStopped;
        Recording.AnalyzePromptRequested -= OnAnalyzePromptRequested;
        Browser.NavigationSucceeded -= OnNavigationSucceeded;
        Browser.IFrameNavigationSucceeded -= OnIFrameNavigationSucceeded;

        if (_pageTransition.IsActive)
            _pageTransition.PageTransitionDetected -= OnSpaTransitionDetected;
    }
}
