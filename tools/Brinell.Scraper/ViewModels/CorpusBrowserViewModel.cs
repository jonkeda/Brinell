using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class CorpusBrowserViewModel : ViewModelBase
{
    private readonly ILogger<CorpusBrowserViewModel> _logger;
    private CorpusService? _corpusService;
    private long _siteId;
    private string _filterText = string.Empty;
    private SnapshotSummary? _selectedSnapshot;

    public CorpusBrowserViewModel(ILogger<CorpusBrowserViewModel> logger)
    {
        _logger = logger;
        Snapshots = [];

        ViewSnapshotCommand = new RelayCommand(OnViewSnapshot, () => SelectedSnapshot is not null);
        ViewDiffCommand = new RelayCommand(OnViewDiff, () => SelectedSnapshot is not null);
        ReRecordCommand = new RelayCommand(OnReRecord, () => SelectedSnapshot is not null);
        DeletePageCommand = new RelayCommand(OnDeletePage, () => SelectedSnapshot is not null);
    }

    public ObservableCollection<SnapshotSummary> Snapshots { get; }

    public ICollectionView? FilteredSnapshots { get; private set; }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                FilteredSnapshots?.Refresh();
        }
    }

    public SnapshotSummary? SelectedSnapshot
    {
        get => _selectedSnapshot;
        set
        {
            if (SetProperty(ref _selectedSnapshot, value))
                RaiseCommandStates();
        }
    }

    public ICommand ViewSnapshotCommand { get; }
    public ICommand ViewDiffCommand { get; }
    public ICommand ReRecordCommand { get; }
    public ICommand DeletePageCommand { get; }

    public event Action<SnapshotSummary>? ViewSnapshotRequested;
    public event Action<SnapshotSummary>? ViewDiffRequested;
    public event Action<SnapshotSummary>? ReRecordRequested;

    public void Load(CorpusService corpusService, long siteId)
    {
        _corpusService = corpusService;
        _siteId = siteId;
        Refresh();

        FilteredSnapshots = CollectionViewSource.GetDefaultView(Snapshots);
        FilteredSnapshots.Filter = FilterByText;
        OnPropertyChanged(nameof(FilteredSnapshots));
    }

    public void Refresh()
    {
        if (_corpusService is null) return;

        Snapshots.Clear();
        var summaries = _corpusService.ListSnapshots(_siteId);
        foreach (var s in summaries)
            Snapshots.Add(s);

        _logger.LogDebug("Corpus browser loaded {Count} snapshots for site {SiteId}", summaries.Count, _siteId);
    }

    private bool FilterByText(object obj)
    {
        if (string.IsNullOrWhiteSpace(_filterText)) return true;
        return obj is SnapshotSummary s &&
            (s.PageName.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
             s.PageUrl.Contains(_filterText, StringComparison.OrdinalIgnoreCase));
    }

    private void OnViewSnapshot() => ViewSnapshotRequested?.Invoke(SelectedSnapshot!);
    private void OnViewDiff() => ViewDiffRequested?.Invoke(SelectedSnapshot!);
    private void OnReRecord() => ReRecordRequested?.Invoke(SelectedSnapshot!);
    private void OnDeletePage() { /* placeholder */ }

    private void RaiseCommandStates()
    {
        ((RelayCommand)ViewSnapshotCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ViewDiffCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ReRecordCommand).RaiseCanExecuteChanged();
        ((RelayCommand)DeletePageCommand).RaiseCanExecuteChanged();
    }
}
