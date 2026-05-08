using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class ControlObjectsTabViewModel : ViewModelBase
{
    private readonly IControlRegistry _controlRegistry;
    private readonly ILogger<ControlObjectsTabViewModel> _logger;
    private readonly PipelineOrchestrator? _pipelineOrchestrator;

    private long _siteId;
    private string _filterText = "";
    private ControlObjectListItem? _selectedControlObject;

    public ControlObjectsTabViewModel(
        IControlRegistry controlRegistry,
        ILogger<ControlObjectsTabViewModel> logger,
        PipelineOrchestrator? pipelineOrchestrator = null)
    {
        _controlRegistry = controlRegistry;
        _logger = logger;
        _pipelineOrchestrator = pipelineOrchestrator;

        if (_pipelineOrchestrator is null)
        {
            _logger.LogInformation(
                "PipelineOrchestrator not injected — Analyze/Generate/Regenerate commands disabled.");
        }

        FilteredControlObjects = CollectionViewSource.GetDefaultView(ControlObjects);
        FilteredControlObjects.Filter = MatchesFilter;

        AnalyzeCorpusCommand = new AsyncRelayCommand(
            AnalyzeCorpusAsync,
            () => _pipelineOrchestrator is not null);

        GenerateAllPendingCommand = new AsyncRelayCommand(
            GenerateAllPendingAsync,
            () => _pipelineOrchestrator is not null && PendingCount > 0);

        ImportCommand = new RelayCommand(Import);
        ExportCommand = new RelayCommand(Export);

        ApproveCommand = new RelayCommand<ControlObjectListItem>(
            Approve, c => c is not null && c.Status != ControlObjectStatus.Approved);
        RejectCommand = new RelayCommand<ControlObjectListItem>(
            Reject, c => c is not null && c.Status != ControlObjectStatus.Rejected);
        RegenerateCommand = new AsyncRelayCommand<ControlObjectListItem>(
            RegenerateAsync, c => c is not null && _pipelineOrchestrator is not null);
        DeleteCommand = new RelayCommand<ControlObjectListItem>(
            Delete, c => c is not null);
        CopyCodeCommand = new RelayCommand<ControlObjectListItem>(
            CopyCode, c => c is not null && !string.IsNullOrEmpty(c.Code));

        AddPropertyCommand = new RelayCommand(
            AddProperty, () => SelectedControlObject is not null);
        RemovePropertyCommand = new RelayCommand<ControlPropertyItem>(
            RemoveProperty, p => p is not null && SelectedControlObject is not null);
    }

    public ObservableCollection<ControlObjectListItem> ControlObjects { get; } = [];

    public ICollectionView FilteredControlObjects { get; }

    public ControlObjectListItem? SelectedControlObject
    {
        get => _selectedControlObject;
        set
        {
            if (SetProperty(ref _selectedControlObject, value))
                ((RelayCommand)AddPropertyCommand).RaiseCanExecuteChanged();
        }
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                FilteredControlObjects.Refresh();
        }
    }

    public int TotalCount => ControlObjects.Count;
    public int ApprovedCount => ControlObjects.Count(c => c.Status == ControlObjectStatus.Approved);
    public int PendingCount => ControlObjects.Count(c => c.Status == ControlObjectStatus.Pending);
    public int RejectedCount => ControlObjects.Count(c => c.Status == ControlObjectStatus.Rejected);

    public ICommand AnalyzeCorpusCommand { get; }
    public ICommand GenerateAllPendingCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }

    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }
    public ICommand RegenerateCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CopyCodeCommand { get; }

    public ICommand AddPropertyCommand { get; }
    public ICommand RemovePropertyCommand { get; }

    public void LoadControlObjects(long siteId)
    {
        _siteId = siteId;
        ControlObjects.Clear();

        foreach (var ctrl in _controlRegistry.GetAllControls())
            ControlObjects.Add(MapFromGenerated(ctrl));

        // Phase 13.1 will populate pending proposals from the analysis-result store; no store yet.
        _logger.LogInformation(
            "Control Objects loaded — SiteId: {SiteId}, Count: {Count}, PipelineAvailable: {PipelineAvailable}",
            siteId, ControlObjects.Count, _pipelineOrchestrator is not null);

        RaiseSummaryChanged();
    }

    private static ControlObjectListItem MapFromGenerated(GeneratedControl c) => new()
    {
        Name = c.Name,
        Namespace = c.Namespace,
        Confidence = (int)Math.Round(c.Confidence),
        Status = ControlObjectStatus.Approved,
        DomSignature = c.DomSignature,
        CreatedAt = c.CreatedAt.UtcDateTime,
        Code = c.Code,
    };

    private bool MatchesFilter(object obj)
    {
        if (obj is not ControlObjectListItem item) return false;
        if (string.IsNullOrWhiteSpace(_filterText)) return true;

        return (item.Name?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.DomSignature?.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void RaiseSummaryChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ApprovedCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(RejectedCount));
        ((AsyncRelayCommand)GenerateAllPendingCommand).RaiseCanExecuteChanged();
    }

    private Task AnalyzeCorpusAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "AnalyzeCorpus requested — SiteId: {SiteId} (Phase 13.4 PipelineOrchestrator not yet wired)",
            _siteId);
        return Task.CompletedTask;
    }

    private Task GenerateAllPendingAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "GenerateAllPending requested (Phase 13.4 PipelineOrchestrator not yet wired)");
        return Task.CompletedTask;
    }

    private void Import()
        => _logger.LogInformation("Import control objects requested (not yet implemented)");

    private void Export()
        => _logger.LogInformation("Export control objects requested (not yet implemented)");

    private void Approve(ControlObjectListItem? item)
    {
        if (item is null) return;
        item.Status = ControlObjectStatus.Approved;
        _logger.LogInformation("Control approved — Name: {Name}", item.Name);
        RaiseSummaryChanged();
        FilteredControlObjects.Refresh();
    }

    private void Reject(ControlObjectListItem? item)
    {
        if (item is null) return;
        item.Status = ControlObjectStatus.Rejected;
        _logger.LogInformation("Control rejected — Name: {Name}", item.Name);
        RaiseSummaryChanged();
        FilteredControlObjects.Refresh();
    }

    private Task RegenerateAsync(ControlObjectListItem? item, CancellationToken ct)
    {
        if (item is null) return Task.CompletedTask;
        _logger.LogInformation(
            "Regenerate requested — Name: {Name} (Phase 13.4 not yet wired)", item.Name);
        return Task.CompletedTask;
    }

    private void Delete(ControlObjectListItem? item)
    {
        if (item is null) return;
        ControlObjects.Remove(item);

        if (item.Status == ControlObjectStatus.Approved && !string.IsNullOrEmpty(item.Name))
        {
            try
            {
                _controlRegistry.DeleteControl(item.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Delete from registry failed — Name: {Name}", item.Name);
            }
        }

        _logger.LogInformation("Control deleted — Name: {Name}", item.Name);
        RaiseSummaryChanged();
    }

    private void CopyCode(ControlObjectListItem? item)
    {
        if (item is null || string.IsNullOrEmpty(item.Code)) return;
        try
        {
            Clipboard.SetText(item.Code);
            _logger.LogInformation("Copied code to clipboard — Name: {Name}", item.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Clipboard copy failed — Name: {Name}", item.Name);
        }
    }

    private void AddProperty()
    {
        if (SelectedControlObject is null) return;
        SelectedControlObject.Properties.Add(new ControlPropertyItem
        {
            Name = "NewProperty",
            ControlType = "Brinell.IControl",
            Selector = "",
        });
    }

    private void RemoveProperty(ControlPropertyItem? prop)
    {
        if (prop is null || SelectedControlObject is null) return;
        SelectedControlObject.Properties.Remove(prop);
    }
}
