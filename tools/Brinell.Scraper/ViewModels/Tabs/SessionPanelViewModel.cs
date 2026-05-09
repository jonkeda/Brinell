using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class SessionPanelViewModel : ViewModelBase
{
    private readonly CorpusService _corpusService;
    private readonly IControlRegistry _controlRegistry;
    private readonly ILogger<SessionPanelViewModel> _logger;
    private string _siteHeader = string.Empty;
    private string _corpusStats = "0 pages · 0 controls";
    private Action<string>? _navigateCallback;

    public SessionPanelViewModel(
        CorpusService corpusService,
        IControlRegistry controlRegistry,
        ILogger<SessionPanelViewModel> logger)
    {
        _corpusService = corpusService;
        _controlRegistry = controlRegistry;
        _logger = logger;

        NavigateToPageCommand = new RelayCommand<SidebarPageItem>(NavigateToPage);

        CorpusPages.CollectionChanged += OnCollectionChanged;
        Controls.CollectionChanged += OnCollectionChanged;
    }

    public ObservableCollection<SidebarPageItem> RecordedPages { get; } = [];
    public ObservableCollection<SidebarPageItem> CorpusPages { get; } = [];
    public ObservableCollection<string> Controls { get; } = [];

    public ICommand NavigateToPageCommand { get; }

    public string SiteHeader
    {
        get => _siteHeader;
        private set => SetProperty(ref _siteHeader, value);
    }

    public string CorpusStats
    {
        get => _corpusStats;
        private set => SetProperty(ref _corpusStats, value);
    }

    public void Load(long siteId, string siteName)
    {
        CorpusPages.Clear();
        Controls.Clear();

        SiteHeader = siteName;

        var pages = _corpusService.ListPagesBySiteId(siteId);
        foreach (var p in pages)
        {
            CorpusPages.Add(new SidebarPageItem
            {
                PageId = p.LatestSnapshotId,
                Name = p.PageName,
                Url = p.PageUrl,
                StatusIcon = "\ud83d\udcc4",
            });
        }

        foreach (var control in _controlRegistry.GetAllControls())
            Controls.Add(control.Name);

        UpdateCorpusStats();

        _logger.LogInformation("Session panel loaded — Site: {SiteId}, CorpusPages: {Count}", siteId, CorpusPages.Count);
    }

    public void SyncRecordedPages(IEnumerable<DomSnapshot> snapshots)
    {
        RecordedPages.Clear();
        foreach (var snapshot in snapshots)
        {
            RecordedPages.Add(new SidebarPageItem
            {
                Name = snapshot.PageName,
                Url = snapshot.PageUrl,
                StatusIcon = "🆕"
            });
        }
    }

    public void SetNavigateCallback(Action<string> callback)
    {
        _navigateCallback = callback;
    }

    private void NavigateToPage(SidebarPageItem? item)
    {
        if (item?.Url is { Length: > 0 } url)
            _navigateCallback?.Invoke(url);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCorpusStats();
    }

    private void UpdateCorpusStats()
    {
        CorpusStats = $"{CorpusPages.Count} pages · {Controls.Count} controls";
    }
}
