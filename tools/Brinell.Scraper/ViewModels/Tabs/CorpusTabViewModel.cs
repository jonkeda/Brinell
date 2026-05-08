using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.Views;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class CorpusTabViewModel : ViewModelBase
{
    private readonly CorpusService _corpusService;
    private readonly DomDiffService _diffService;
    private readonly ILogger<CorpusTabViewModel> _logger;

    private long _siteId;
    private string _filterText = "";
    private CorpusPageGroup? _selectedPage;
    private SnapshotVersionRow? _selectedVersion;
    private string _elementStatsText = "Select a version to see element statistics.";

    public CorpusTabViewModel(
        CorpusService corpusService,
        DomDiffService diffService,
        ILogger<CorpusTabViewModel> logger)
    {
        _corpusService = corpusService;
        _diffService = diffService;
        _logger = logger;

        FilteredPages = CollectionViewSource.GetDefaultView(Pages);
        FilteredPages.Filter = MatchesFilter;

        DomTree = new DomTreeViewModel();

        ReRecordAllCommand = new AsyncRelayCommand(ReRecordAllAsync, () => Pages.Count > 0);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ExportCommand = new RelayCommand(Export, () => Pages.Count > 0);
        ImportCommand = new RelayCommand(Import);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedPage is not null);

        ViewVersionCommand = new RelayCommand<SnapshotVersionRow>(ViewVersion, v => v is not null);
        CompareCommand = new RelayCommand<SnapshotVersionRow>(CompareWithLatest, CanCompareWithLatest);
        GeneratePageObjectCommand = new AsyncRelayCommand<SnapshotVersionRow>(
            GeneratePageObjectAsync,
            v => v is not null);

        ReRecordPageCommand = new AsyncRelayCommand(ReRecordPageAsync, () => SelectedPage is not null);
        ExportPageCommand = new RelayCommand(ExportPage, () => SelectedPage is not null);
        DeleteAllVersionsCommand = new RelayCommand(DeleteAllVersions, () => SelectedPage is not null);
        OpenInBrowserCommand = new RelayCommand<CorpusPageGroup>(
            OpenInBrowser,
            p => p is not null && !string.IsNullOrEmpty(p.PageUrl));
    }

    public ObservableCollection<CorpusPageGroup> Pages { get; } = [];

    public ICollectionView FilteredPages { get; }

    public DomTreeViewModel DomTree { get; }

    public CorpusPageGroup? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (SetProperty(ref _selectedPage, value))
            {
                SelectedVersion = value?.LatestSnapshot;
                RaisePageCommandStates();
            }
        }
    }

    public SnapshotVersionRow? SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            if (SetProperty(ref _selectedVersion, value))
                LoadVersionDetails(value);
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                FilteredPages.Refresh();
        }
    }

    public string ElementStatsText
    {
        get => _elementStatsText;
        private set => SetProperty(ref _elementStatsText, value);
    }

    public int TotalPages => Pages.Count;
    public int TotalSnapshots => Pages.Sum(p => p.Versions.Count);
    public int TotalElements => Pages.Sum(p => p.TotalElements);
    public long TotalSizeBytes => Pages.Sum(p => p.Versions.Sum(v => v.SnapshotSizeBytes));

    public string TotalSizeLabel => TotalSizeBytes switch
    {
        < 1024 => $"{TotalSizeBytes} B",
        < 1024 * 1024 => $"{TotalSizeBytes / 1024.0:F1} KB",
        _ => $"{TotalSizeBytes / (1024.0 * 1024.0):F2} MB",
    };

    public ICommand ReRecordAllCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand DeleteSelectedCommand { get; }

    public ICommand ViewVersionCommand { get; }
    public ICommand CompareCommand { get; }
    public ICommand GeneratePageObjectCommand { get; }

    public ICommand ReRecordPageCommand { get; }
    public ICommand ExportPageCommand { get; }
    public ICommand DeleteAllVersionsCommand { get; }
    public ICommand OpenInBrowserCommand { get; }

    public event Action<string>? OpenInBrowserRequested;
    public event Action<CorpusPageGroup>? ReRecordRequested;

    public void Load(long siteId)
    {
        _siteId = siteId;
        Pages.Clear();

        IReadOnlyList<SnapshotSummary> snapshots;
        try
        {
            snapshots = _corpusService.ListSnapshots(siteId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to list snapshots — SiteId: {SiteId}", siteId);
            snapshots = Array.Empty<SnapshotSummary>();
        }

        var groups = snapshots
            .GroupBy(s => s.PageName)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var g in groups)
        {
            var ordered = g.OrderByDescending(s => s.CapturedAt).ToList();
            var page = new CorpusPageGroup
            {
                PageName = g.Key,
                PageUrl = ordered[0].PageUrl,
                // Phase 13.5/13.6 will join with Controls / PageObjects tables.
                HasControlObjects = false,
                ControlObjectsPending = false,
                PageObjectStatus = PageObjectStatus.NotGenerated,
            };

            // Newest gets the highest version number; oldest gets v1.
            var total = ordered.Count;
            for (var i = 0; i < total; i++)
            {
                var s = ordered[i];
                page.Versions.Add(new SnapshotVersionRow
                {
                    SnapshotId = s.Id,
                    VersionNumber = total - i,
                    IsLatest = s.IsLatest,
                    CapturedAt = s.CapturedAt.LocalDateTime,
                    ElementCount = s.ElementCount,
                    SnapshotSizeBytes = s.SnapshotSizeBytes,
                    HasPageObject = false,
                    PageObjectStatus = PageObjectStatus.NotGenerated,
                });
            }

            Pages.Add(page);
        }

        _logger.LogInformation(
            "Corpus loaded — SiteId: {SiteId}, Pages: {Pages}, Snapshots: {Snapshots}",
            siteId, Pages.Count, snapshots.Count);

        RaiseTotalsChanged();
        RaisePageCommandStates();
        ((AsyncRelayCommand)ReRecordAllCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
    }

    private bool MatchesFilter(object obj)
    {
        if (obj is not CorpusPageGroup page) return false;
        if (string.IsNullOrWhiteSpace(_filterText)) return true;

        return page.PageName.Contains(_filterText, StringComparison.OrdinalIgnoreCase)
            || page.PageUrl.Contains(_filterText, StringComparison.OrdinalIgnoreCase);
    }

    private void RaiseTotalsChanged()
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(TotalSnapshots));
        OnPropertyChanged(nameof(TotalElements));
        OnPropertyChanged(nameof(TotalSizeBytes));
        OnPropertyChanged(nameof(TotalSizeLabel));
    }

    private void RaisePageCommandStates()
    {
        ((RelayCommand)DeleteSelectedCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)ReRecordPageCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ExportPageCommand).RaiseCanExecuteChanged();
        ((RelayCommand)DeleteAllVersionsCommand).RaiseCanExecuteChanged();
    }

    private void LoadVersionDetails(SnapshotVersionRow? version)
    {
        if (version is null)
        {
            DomTree.RootElements.Clear();
            ElementStatsText = "Select a version to see element statistics.";
            return;
        }

        DomSnapshot? snapshot;
        try
        {
            snapshot = _corpusService.GetSnapshotById(version.SnapshotId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load snapshot — Id: {Id}", version.SnapshotId);
            snapshot = null;
        }

        if (snapshot is null)
        {
            DomTree.RootElements.Clear();
            ElementStatsText = "Snapshot DOM is not available.";
            return;
        }

        DomTree.LoadSnapshot(snapshot);
        ElementStatsText = ComputeElementStats(snapshot.RootElement);
    }

    private static string ComputeElementStats(DomElement root)
    {
        var byTag = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var withId = 0;
        var withClass = 0;
        var withTestId = 0;
        var withAria = 0;
        var total = 0;

        Walk(root);

        var topTags = byTag
            .OrderByDescending(kv => kv.Value)
            .Take(8)
            .Select(kv => $"{kv.Key} × {kv.Value}");

        var stable = withId + withTestId + withAria;
        var stablePct = total == 0 ? 0 : 100.0 * stable / total;

        return string.Join(Environment.NewLine, new[]
        {
            $"Total elements: {total}",
            $"With id:          {withId}",
            $"With class:       {withClass}",
            $"With data-testid: {withTestId}",
            $"With aria-label:  {withAria}",
            $"Stable locators:  {stable} ({stablePct:F0}%)",
            "",
            "Top tags: " + string.Join(", ", topTags),
        });

        void Walk(DomElement el)
        {
            total++;
            byTag[el.Tag] = byTag.TryGetValue(el.Tag, out var c) ? c + 1 : 1;
            if (!string.IsNullOrEmpty(el.Id)) withId++;
            if (!string.IsNullOrEmpty(el.ClassName)) withClass++;
            if (!string.IsNullOrEmpty(el.DataTestId)) withTestId++;
            if (!string.IsNullOrEmpty(el.AriaLabel)) withAria++;
            foreach (var child in el.Children)
                Walk(child);
        }
    }

    private Task ReRecordAllAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Re-Record All requested — SiteId: {SiteId} (cross-tab orchestration not yet wired)", _siteId);
        return Task.CompletedTask;
    }

    private Task RefreshAsync(CancellationToken ct)
    {
        if (_siteId > 0)
            Load(_siteId);
        return Task.CompletedTask;
    }

    private void Export()
        => _logger.LogInformation(
            "Corpus export requested (not yet implemented) — SiteId: {SiteId}", _siteId);

    private void Import()
        => _logger.LogInformation(
            "Corpus import requested (not yet implemented) — SiteId: {SiteId}", _siteId);

    private void DeleteSelected()
    {
        if (SelectedPage is null) return;

        var result = MessageBox.Show(
            $"Delete all {SelectedPage.Versions.Count} version(s) of \"{SelectedPage.PageName}\"?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        DeletePageGroup(SelectedPage);
    }

    private void DeleteAllVersions() => DeleteSelected();

    private void DeletePageGroup(CorpusPageGroup page)
    {
        foreach (var v in page.Versions.ToList())
        {
            try
            {
                _corpusService.DeleteSnapshot(v.SnapshotId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Delete snapshot failed — Id: {Id}", v.SnapshotId);
            }
        }
        Pages.Remove(page);
        SelectedPage = null;
        SelectedVersion = null;
        RaiseTotalsChanged();
    }

    private void ViewVersion(SnapshotVersionRow? version)
    {
        if (version is null) return;
        SelectedVersion = version;
    }

    private bool CanCompareWithLatest(SnapshotVersionRow? version)
        => version is not null
            && SelectedPage is not null
            && SelectedPage.LatestSnapshot is not null
            && SelectedPage.LatestSnapshot.SnapshotId != version.SnapshotId;

    private void CompareWithLatest(SnapshotVersionRow? version)
    {
        if (!CanCompareWithLatest(version)) return;
        var latest = SelectedPage!.LatestSnapshot!;
        Compare(latest.SnapshotId, version!.SnapshotId, SelectedPage.PageName);
    }

    private void Compare(long latestId, long olderId, string pageName)
    {
        try
        {
            var after = _corpusService.GetSnapshotById(latestId);
            var before = _corpusService.GetSnapshotById(olderId);
            if (after is null || before is null)
            {
                _logger.LogWarning(
                    "Compare aborted — snapshot not found. Latest: {Latest}, Older: {Older}",
                    latestId, olderId);
                return;
            }

            var diff = _diffService.Compare(before, after);
            var vm = DiffViewModel.FromResult(diff, pageName);
            var window = new DiffWindow
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow,
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Compare failed — Latest: {Latest}, Older: {Older}", latestId, olderId);
        }
    }

    private Task GeneratePageObjectAsync(SnapshotVersionRow? version, CancellationToken ct)
    {
        if (version is null) return Task.CompletedTask;
        _logger.LogInformation(
            "Generate page object requested — SnapshotId: {Id} (Phase 13 not yet wired)", version.SnapshotId);
        return Task.CompletedTask;
    }

    private Task ReRecordPageAsync(CancellationToken ct)
    {
        if (SelectedPage is null) return Task.CompletedTask;
        _logger.LogInformation("Re-record page requested — Page: {Page}", SelectedPage.PageName);
        ReRecordRequested?.Invoke(SelectedPage);
        return Task.CompletedTask;
    }

    private void ExportPage()
        => _logger.LogInformation(
            "Export page requested (not yet implemented) — Page: {Page}", SelectedPage?.PageName);

    private void OpenInBrowser(CorpusPageGroup? page)
    {
        var url = page?.PageUrl;
        if (string.IsNullOrWhiteSpace(url)) return;
        _logger.LogInformation("Open in browser requested — Url: {Url}", url);
        OpenInBrowserRequested?.Invoke(url);
    }
}
