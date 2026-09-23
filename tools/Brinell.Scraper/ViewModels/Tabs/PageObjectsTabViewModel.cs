using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class PageObjectsTabViewModel : ViewModelBase
{
    private readonly CorpusService _corpusService;
    private readonly ILogger<PageObjectsTabViewModel> _logger;
    private readonly PipelineOrchestrator? _pipelineOrchestrator;

    private long _siteId;
    private string _filterText = "";
    private PageObjectListItem? _selectedPageObject;

    public PageObjectsTabViewModel(
        CorpusService corpusService,
        ILogger<PageObjectsTabViewModel> logger,
        PipelineOrchestrator? pipelineOrchestrator = null)
    {
        _corpusService = corpusService;
        _logger = logger;
        _pipelineOrchestrator = pipelineOrchestrator;

        if (_pipelineOrchestrator is null)
        {
            _logger.LogInformation(
                "PipelineOrchestrator not injected — Generate/Regenerate commands disabled.");
        }

        FilteredPageObjects = CollectionViewSource.GetDefaultView(PageObjects);
        FilteredPageObjects.Filter = MatchesFilter;

        GenerateAllCommand = new AsyncRelayCommand(
            GenerateAllAsync,
            () => _pipelineOrchestrator is not null && PendingCount > 0);

        RegenerateSelectedCommand = new AsyncRelayCommand(
            RegenerateSelectedAsync,
            () => _pipelineOrchestrator is not null && SelectedPageObject is not null);

        ExportCommand = new RelayCommand(Export, () => GeneratedCount > 0);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder);

        CopyCodeCommand = new RelayCommand<PageObjectListItem>(
            CopyCode, p => p is not null && !string.IsNullOrEmpty(p.MainCode));

        OpenSourcePageCommand = new RelayCommand<PageObjectListItem>(
            OpenSourcePage, p => p is not null && !string.IsNullOrEmpty(p.PageUrl));

        NavigateToControlObjectCommand = new RelayCommand<ControlObjectReference>(
            NavigateToControlObject, c => c is not null && !string.IsNullOrEmpty(c.Name));

        DeleteCommand = new RelayCommand<PageObjectListItem>(
            Delete, p => p is not null);
    }

    public ObservableCollection<PageObjectListItem> PageObjects { get; } = [];

    public ICollectionView FilteredPageObjects { get; }

    public PageObjectListItem? SelectedPageObject
    {
        get => _selectedPageObject;
        set
        {
            if (SetProperty(ref _selectedPageObject, value))
                ((AsyncRelayCommand)RegenerateSelectedCommand).RaiseCanExecuteChanged();
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                FilteredPageObjects.Refresh();
        }
    }

    public int TotalCount => PageObjects.Count;
    public int GeneratedCount => PageObjects.Count(p => p.Status == PageObjectStatus.Generated);
    public int PendingCount => PageObjects.Count(p => p.Status == PageObjectStatus.NotGenerated);
    public int ErrorCount => PageObjects.Count(p => p.Status == PageObjectStatus.Error);

    public ICommand GenerateAllCommand { get; }
    public ICommand RegenerateSelectedCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }

    public ICommand CopyCodeCommand { get; }
    public ICommand OpenSourcePageCommand { get; }
    public ICommand NavigateToControlObjectCommand { get; }
    public ICommand DeleteCommand { get; }

    public event Action<string>? OpenSourcePageRequested;
    public event Action<string>? NavigateToControlObjectRequested;

    public void LoadPageObjects(long siteId)
    {
        _siteId = siteId;
        PageObjects.Clear();

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

        // Latest per page only.
        var latest = snapshots
            .Where(s => s.IsLatest)
            .GroupBy(s => s.PageName)
            .Select(g => g.OrderByDescending(s => s.CapturedAt).First());

        // Phase 13.6/13.7: join with PageObjects table when it exists.
        // For now mark all rows NotGenerated.
        foreach (var snap in latest)
        {
            PageObjects.Add(new PageObjectListItem
            {
                SnapshotId = snap.Id,
                PageName = snap.PageName,
                PageUrl = snap.PageUrl,
                ElementCount = snap.ElementCount,
                Status = PageObjectStatus.NotGenerated,
            });
        }

        _logger.LogInformation(
            "Page Objects loaded — SiteId: {SiteId}, Count: {Count}, PipelineAvailable: {PipelineAvailable}",
            siteId, PageObjects.Count, _pipelineOrchestrator is not null);

        RaiseSummaryChanged();
    }

    private bool MatchesFilter(object obj)
    {
        if (obj is not PageObjectListItem item) return false;
        if (string.IsNullOrWhiteSpace(_filterText)) return true;

        return (item.PageName?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.PageUrl?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void RaiseSummaryChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(GeneratedCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(ErrorCount));
        ((AsyncRelayCommand)GenerateAllCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
    }

    private Task GenerateAllAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "GenerateAll page objects requested — SiteId: {SiteId} (Phase 13.4 PipelineOrchestrator not yet wired)",
            _siteId);
        return Task.CompletedTask;
    }

    private Task RegenerateSelectedAsync(CancellationToken ct)
    {
        if (SelectedPageObject is null) return Task.CompletedTask;
        _logger.LogInformation(
            "Regenerate page object requested — Page: {Page} (Phase 13.4 not yet wired)",
            SelectedPageObject.PageName);
        return Task.CompletedTask;
    }

    private void Export()
        => _logger.LogInformation("Export page objects requested (not yet implemented)");

    private void OpenOutputFolder()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Brinell.Scraper", "output");
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Open output folder failed");
        }
    }

    private void CopyCode(PageObjectListItem? item)
    {
        if (item is null || string.IsNullOrEmpty(item.MainCode)) return;
        try
        {
            Clipboard.SetText(item.MainCode);
            _logger.LogInformation("Copied page object code — Page: {Page}", item.PageName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Clipboard copy failed — Page: {Page}", item.PageName);
        }
    }

    private void OpenSourcePage(PageObjectListItem? item)
    {
        if (item is null || string.IsNullOrEmpty(item.PageUrl)) return;
        _logger.LogInformation("Open source page requested — Url: {Url}", item.PageUrl);
        OpenSourcePageRequested?.Invoke(item.PageUrl);
    }

    private void NavigateToControlObject(ControlObjectReference? reference)
    {
        if (reference is null || string.IsNullOrEmpty(reference.Name)) return;
        _logger.LogInformation(
            "Navigate to control object requested — Name: {Name}", reference.Name);
        NavigateToControlObjectRequested?.Invoke(reference.Name);
    }

    private void Delete(PageObjectListItem? item)
    {
        if (item is null) return;
        PageObjects.Remove(item);
        _logger.LogInformation("Page object removed — Page: {Page}", item.PageName);
        RaiseSummaryChanged();
    }
}
