using System.Windows.Input;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly CorpusDatabase _db;
    private readonly DomCaptureService _domCapture;
    private readonly ElementHighlightService _highlight;
    private readonly ILogger<MainViewModel> _logger;
    private object? _activeView;
    private SiteInfo? _activeSite;
    private string _siteName = string.Empty;
    private string _windowTitle = "Brinell Scraper";
    private bool _isLogViewerVisible;

    public MainViewModel(CorpusDatabase db, BrowserViewModel browser, SidebarViewModel sidebar, SiteSelectionViewModel siteSelection, InspectorViewModel inspector, RecordingViewModel recording, DomCaptureService domCapture, ElementHighlightService highlight, ILogger<MainViewModel> logger)
    {
        _db = db;
        _domCapture = domCapture;
        _highlight = highlight;
        _logger = logger;
        _logger.LogInformation("MainViewModel initialized");
        Browser = browser;
        Sidebar = sidebar;
        SiteSelection = siteSelection;
        Inspector = inspector;
        Recording = recording;

        SwitchSiteCommand = new RelayCommand(ShowSiteSelector);
        ManageControlsCommand = new RelayCommand(() => { }, () => HasActiveSite);
        BrowseCorpusCommand = new RelayCommand(() => { }, () => HasActiveSite);
        InspectCommand = new AsyncRelayCommand(ToggleInspectAsync, () => HasActiveSite);
        RecordCommand = new RelayCommand(ToggleRecording, () => HasActiveSite);
        AnalyzeCommand = new RelayCommand(() => { }, () => HasActiveSite);

        SiteSelection.SiteSelected += OnSiteSelected;
        Browser.ElementSelected += OnElementSelected;
        Browser.NavigationSucceeded += OnNavigationSucceeded;
        Recording.AnalyzePromptRequested += OnAnalyzePromptRequested;
        Recording.RecordingStarted += () => Sidebar.IsRecording = true;
        Recording.RecordingStopped += () =>
        {
            Sidebar.ClearSession();
            if (ActiveSite is not null)
                Sidebar.CorpusStats = $"{ActiveSite.PageCount} pages \u00b7 {ActiveSite.ControlCount} controls";
        };

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
    public ICommand AnalyzeCommand { get; }

    // Called by MainWindow after views are created
    public event Action? SiteSelectorRequested;
    public event Action? BrowserViewRequested;

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

        BrowserViewRequested?.Invoke();
    }

    private void RaiseAllCommandStates()
    {
        ((RelayCommand)ManageControlsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)BrowseCorpusCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)InspectCommand).RaiseCanExecuteChanged();
        ((RelayCommand)RecordCommand).RaiseCanExecuteChanged();
        ((RelayCommand)AnalyzeCommand).RaiseCanExecuteChanged();
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
            var snapshot = await _domCapture.CaptureAsync(webView);
            Inspector.LoadSnapshot(snapshot);
            await _highlight.EnableAsync(webView);
            Inspector.IsInspecting = true;
            _logger.LogInformation("Inspect mode enabled — {ElementCount} elements captured", Inspector.TotalElementCount);
        }
    }

    private async void OnNavigationSucceeded()
    {
        if (Inspector.IsInspecting)
        {
            var webView = Browser.GetCoreWebView2?.Invoke();
            if (webView is not null)
            {
                var snapshot = await _domCapture.CaptureAsync(webView);
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
                var snapshot = await _domCapture.CaptureAsync(webView);
                snapshot.SiteName = ActiveSite?.Name ?? "";
                snapshot.PageName = snapshot.PageTitle;

                if (Recording.OnPageTransition(snapshot.PageUrl, snapshot))
                {
                    Sidebar.AddSessionPage(snapshot);
                }
            }
        }
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
        if (Recording.SessionSnapshots.Count == 0)
            return;

        var result = System.Windows.MessageBox.Show(
            $"{Recording.SessionSnapshots.Count} pages captured. Analyze corpus now?",
            "Recording Complete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
            _logger.LogInformation("User chose to analyze corpus");
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
}
