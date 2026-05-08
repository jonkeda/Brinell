using System.Collections.ObjectModel;
using System.Windows.Input;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace Brinell.Scraper.ViewModels;

public sealed class BrowserViewModel : ViewModelBase
{
    private string _addressUrl = string.Empty;
    private bool _isLoading;
    private string _statusText = "Ready";
    private bool _canGoBack;
    private bool _canGoForward;
    private readonly ILogger<BrowserViewModel> _logger;

    public BrowserViewModel(ILogger<BrowserViewModel> logger)
    {
        _logger = logger;
        GoBackCommand = new RelayCommand(OnGoBack, () => CanGoBack);
        GoForwardCommand = new RelayCommand(OnGoForward, () => CanGoForward);
        RefreshCommand = new RelayCommand(OnRefresh);
        NavigateCommand = new RelayCommand(OnNavigate, () => !string.IsNullOrWhiteSpace(AddressUrl));
        OpenDevToolsCommand = new RelayCommand(OnOpenDevTools);
    }

    public string AddressUrl
    {
        get => _addressUrl;
        set
        {
            if (SetProperty(ref _addressUrl, value))
                ((RelayCommand)NavigateCommand).RaiseCanExecuteChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool CanGoBack
    {
        get => _canGoBack;
        set
        {
            if (SetProperty(ref _canGoBack, value))
                ((RelayCommand)GoBackCommand).RaiseCanExecuteChanged();
        }
    }

    public bool CanGoForward
    {
        get => _canGoForward;
        set
        {
            if (SetProperty(ref _canGoForward, value))
                ((RelayCommand)GoForwardCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand GoBackCommand { get; }
    public ICommand GoForwardCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand OpenDevToolsCommand { get; }

    /// <summary>
    /// Set by BrowserView to provide access to the underlying CoreWebView2 instance.
    /// </summary>
    public Func<CoreWebView2?>? GetCoreWebView2 { get; set; }

    // Events that the BrowserView subscribes to for WebView2 operations
    public event Action<string>? NavigateRequested;
    public event Action? GoBackRequested;
    public event Action? GoForwardRequested;
    public event Action? RefreshRequested;
    public event Action? OpenDevToolsRequested;
    public event Action<WebViewMessage>? ElementSelected;

    // Called by BrowserView when WebView2 events fire
    public void OnNavigationStarting(string url)
    {
        _logger.LogInformation("Navigating to {Url}", url);
        IsLoading = true;
        StatusText = $"Navigating to {url}...";
    }

    public event Action? NavigationSucceeded;

    /// <summary>Fired when a child iframe completes a navigation.</summary>
    public event Action? IFrameNavigationSucceeded;

    public void OnNavigationCompleted(bool isSuccess, string? errorStatus)
    {
        if (isSuccess)
        {
            _logger.LogInformation("Navigation completed: {Url}", AddressUrl);
            NavigationSucceeded?.Invoke();
        }
        else
        {
            _logger.LogWarning("Navigation failed: {Url}, Error: {ErrorStatus}", AddressUrl, errorStatus);
        }
        IsLoading = false;
        StatusText = isSuccess ? AddressUrl : $"Navigation failed: {errorStatus}";
    }

    public void OnSourceChanged(string url)
    {
        _addressUrl = url; // bypass command update since this is a sync from browser
        OnPropertyChanged(nameof(AddressUrl));
    }

    public void OnHistoryChanged(bool canGoBack, bool canGoForward)
    {
        CanGoBack = canGoBack;
        CanGoForward = canGoForward;
    }

    /// <summary>
    /// URL queued before the WebView2 was ready. Consumed by BrowserView after initialization.
    /// </summary>
    public string? PendingNavigateUrl { get; private set; }

    public void ConsumePendingNavigation()
    {
        if (PendingNavigateUrl is null) return;
        var url = PendingNavigateUrl;
        PendingNavigateUrl = null;
        NavigateRequested?.Invoke(url);
    }

    private void OnGoBack() => GoBackRequested?.Invoke();
    private void OnGoForward() => GoForwardRequested?.Invoke();
    private void OnRefresh() => RefreshRequested?.Invoke();

    private void OnNavigate()
    {
        if (NavigateRequested is not null)
            NavigateRequested.Invoke(AddressUrl);
        else
            PendingNavigateUrl = AddressUrl;
    }

    private void OnOpenDevTools() => OpenDevToolsRequested?.Invoke();

    public void OnElementSelected(WebViewMessage msg) => ElementSelected?.Invoke(msg);

    public void OnIFrameNavigationCompleted() => IFrameNavigationSucceeded?.Invoke();
}

public sealed class SiteSelectionViewModel : ViewModelBase
{
    private readonly CorpusDatabase _db;
    private readonly ILogger<SiteSelectionViewModel> _logger;

    public SiteSelectionViewModel(CorpusDatabase db, ILogger<SiteSelectionViewModel> logger)
    {
        _db = db;
        _logger = logger;
        Sites = new ObservableCollection<SiteInfo>(_db.GetAllSites());
        NewSiteCommand = new RelayCommand(() => NewSiteRequested?.Invoke());
        EditSiteCommand = new RelayCommand<SiteInfo>(site =>
        {
            if (site is not null)
                EditSiteRequested?.Invoke(site);
        });
        SelectSiteCommand = new RelayCommand<SiteInfo>(site =>
        {
            if (site is not null)
            {
                _db.TouchSite(site.Id);
                SiteSelected?.Invoke(site);
            }
        });
    }

    public ObservableCollection<SiteInfo> Sites { get; }

    public ICommand NewSiteCommand { get; }
    public ICommand EditSiteCommand { get; }
    public ICommand SelectSiteCommand { get; }

    public Action? NewSiteRequested { get; set; }
    public Action<SiteInfo>? EditSiteRequested { get; set; }
    public event Action<SiteInfo>? SiteSelected;

    public void AddSite(SiteInfo site)
    {
        Sites.Insert(0, site);
        SiteSelected?.Invoke(site);
    }

    public void RefreshSite(SiteInfo updated)
    {
        var index = -1;
        for (var i = 0; i < Sites.Count; i++)
        {
            if (Sites[i].Id == updated.Id)
            {
                index = i;
                break;
            }
        }
        if (index >= 0)
            Sites[index] = updated;
    }
}
