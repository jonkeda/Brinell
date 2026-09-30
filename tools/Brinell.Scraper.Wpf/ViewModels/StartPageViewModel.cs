using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class StartPageViewModel : ViewModelBase
{
    private readonly CorpusDatabase _db;
    private readonly CorpusService _corpusService;
    private readonly ILogger<StartPageViewModel> _logger;

    private string _searchText = string.Empty;

    public StartPageViewModel(
        CorpusDatabase db,
        CorpusService corpusService,
        ILogger<StartPageViewModel> logger)
    {
        _db = db;
        _corpusService = corpusService;
        _logger = logger;

        FilteredSites = CollectionViewSource.GetDefaultView(Sites);
        FilteredSites.Filter = FilterSite;

        OpenSiteCommand = new RelayCommand<SiteCardItem>(OnOpenSite);
        EditSiteCommand = new RelayCommand<SiteCardItem>(OnEditSite);
        DeleteSiteCommand = new RelayCommand<SiteCardItem>(OnDeleteSite);
        NewSiteCommand = new RelayCommand(OnNewSite);
        OpenSettingsCommand = new RelayCommand(() => SettingsRequested?.Invoke());
    }

    public ObservableCollection<SiteCardItem> Sites { get; } = [];

    public ICollectionView FilteredSites { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilteredSites.Refresh();
        }
    }

    public ICommand OpenSiteCommand { get; }
    public ICommand EditSiteCommand { get; }
    public ICommand DeleteSiteCommand { get; }
    public ICommand NewSiteCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public event Action<SiteCardItem>? SiteSelected;
    public event Action<long, string>? SiteOpenWithUrlRequested;
    public event Action? SettingsRequested;

    public event Action<SiteCardItem>? EditSiteRequested;
    public event Action? NewSiteRequested;
    public event Func<SiteCardItem, bool>? DeleteSiteConfirmRequested;

    public async Task LoadAsync()
    {
        var sites = await Task.Run(() => _db.GetAllSites());

        Sites.Clear();
        foreach (var s in sites)
        {
            var pageCount = _corpusService.GetDistinctPageCount(s.Id);
            Sites.Add(ToCardItem(s, pageCount));
        }

        _logger.LogInformation("Start page loaded — Sites: {Count}", Sites.Count);
    }

    public void AddOrUpdateSite(SiteInfo site)
    {
        var existing = Sites.FirstOrDefault(c => c.Id == site.Id);
        var card = ToCardItem(site, _corpusService.GetDistinctPageCount(site.Id));
        if (existing is null)
        {
            Sites.Insert(0, card);
        }
        else
        {
            var index = Sites.IndexOf(existing);
            Sites[index] = card;
        }
    }

    public void RaiseSiteSelected(SiteCardItem card) => SiteSelected?.Invoke(card);

    public void RaiseSiteOpenWithUrl(long siteId, string pageUrl)
    {
        if (siteId > 0 && !string.IsNullOrWhiteSpace(pageUrl))
            SiteOpenWithUrlRequested?.Invoke(siteId, pageUrl);
    }

    private static SiteCardItem ToCardItem(SiteInfo site, int pageCount) => new()
    {
        Id = site.Id,
        Name = site.Name,
        StartUrl = site.StartUrl,
        DomainShort = ExtractHost(site.StartUrl),
        PageCount = pageCount,
        ControlCount = site.ControlCount,
        LastOpenedAt = site.LastOpenedAt == default ? null : site.LastOpenedAt,
        LastOpenedRelative = FormatRelative(site.LastOpenedAt)
    };

    private static string ExtractHost(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
    }

    private static string FormatRelative(DateTime when)
    {
        if (when == default) return "never";

        var delta = DateTime.UtcNow - when.ToUniversalTime();
        if (delta.TotalSeconds < 60) return "just now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays}d ago";
        if (delta.TotalDays < 30) return $"{(int)(delta.TotalDays / 7)}w ago";
        if (delta.TotalDays < 365) return $"{(int)(delta.TotalDays / 30)}mo ago";
        return $"{(int)(delta.TotalDays / 365)}y ago";
    }

    private bool FilterSite(object obj)
    {
        if (obj is not SiteCardItem item) return false;
        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        var q = _searchText.Trim();
        return item.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
            || item.StartUrl.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private void OnOpenSite(SiteCardItem? card)
    {
        if (card is null) return;
        SiteSelected?.Invoke(card);
    }

    private void OnEditSite(SiteCardItem? card)
    {
        if (card is null) return;
        EditSiteRequested?.Invoke(card);
    }

    private void OnDeleteSite(SiteCardItem? card)
    {
        if (card is null) return;

        var confirmed = DeleteSiteConfirmRequested?.Invoke(card) ?? false;
        if (!confirmed) return;

        try
        {
            _db.DeleteSite(card.Id);
            Sites.Remove(card);
            _logger.LogInformation("Site deleted — Id: {SiteId}, Name: {Name}", card.Id, card.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete site — Id: {SiteId}", card.Id);
        }
    }

    private void OnNewSite() => NewSiteRequested?.Invoke();
}
