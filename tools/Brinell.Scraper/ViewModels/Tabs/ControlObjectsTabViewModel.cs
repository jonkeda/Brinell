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
    private readonly CorpusService _corpusService;
    private readonly ControlGenerationService? _controlGenerationService;
    private readonly ILogger<ControlObjectsTabViewModel> _logger;
    private readonly PipelineOrchestrator? _pipelineOrchestrator;

    private long _siteId;
    private string _siteNamespace = "Brinell.Generated";
    private string _filterText = "";
    private ControlObjectListItem? _selectedControlObject;
    private bool _isBusy;
    private string? _statusMessage;

    public ControlObjectsTabViewModel(
        IControlRegistry controlRegistry,
        CorpusService corpusService,
        ILogger<ControlObjectsTabViewModel> logger,
        PipelineOrchestrator? pipelineOrchestrator = null,
        ControlGenerationService? controlGenerationService = null)
    {
        _controlRegistry = controlRegistry;
        _corpusService = corpusService;
        _controlGenerationService = controlGenerationService;
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
            () => _pipelineOrchestrator is not null && !IsBusy);

        GenerateAllPendingCommand = new AsyncRelayCommand(
            GenerateAllPendingAsync,
            () => _pipelineOrchestrator is not null && !IsBusy && ApprovedCount > 0);

        ImportCommand = new RelayCommand(Import);
        ExportCommand = new RelayCommand(Export);

        ApproveCommand = new RelayCommand<ControlObjectListItem>(
            Approve, c => c is not null
                && c.Status != ControlObjectStatus.Approved
                && c.Status != ControlObjectStatus.Generated);
        RejectCommand = new RelayCommand<ControlObjectListItem>(
            Reject, c => c is not null
                && c.Status != ControlObjectStatus.Rejected
                && c.Status != ControlObjectStatus.Generated);
        RegenerateCommand = new AsyncRelayCommand<ControlObjectListItem>(
            RegenerateAsync, c => c is not null && _controlGenerationService is not null && !IsBusy);
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

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((AsyncRelayCommand)AnalyzeCorpusCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)GenerateAllPendingCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand<ControlObjectListItem>)RegenerateCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
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

    public void LoadControlObjects(long siteId, string? siteNamespace = null)
    {
        _siteId = siteId;
        if (!string.IsNullOrWhiteSpace(siteNamespace))
            _siteNamespace = siteNamespace;
        ControlObjects.Clear();

        var generated = _controlRegistry.GetAllControls();
        var analysisResult = _corpusService.GetCurrentAnalysisResult(siteId);

        // Index generated controls by name for O(1) lookup.
        var generatedByName = generated.ToDictionary(
            g => g.Name, g => g, StringComparer.OrdinalIgnoreCase);

        // Process proposals first.
        if (analysisResult is not null)
        {
            foreach (var proposal in analysisResult.Proposals)
            {
                generatedByName.TryGetValue(proposal.Name, out var matchingControl);

                var status = matchingControl is not null
                    ? ControlObjectStatus.Generated
                    : proposal.Status;

                ControlObjects.Add(new ControlObjectListItem
                {
                    Name = proposal.Name,
                    DomSignature = proposal.DomSignature,
                    Confidence = proposal.Confidence,
                    ExampleSnippet = proposal.ExampleSnippet,
                    Status = status,
                    IsGenerated = matchingControl is not null,
                    Proposal = proposal,
                    Code = matchingControl?.Code ?? "",
                    Namespace = matchingControl?.Namespace ?? "",
                    CreatedAt = matchingControl?.CreatedAt.UtcDateTime ?? default,
                });
            }
        }

        // Append any generated controls that have no matching proposal.
        foreach (var ctrl in generated)
        {
            if (ControlObjects.Any(i =>
                    string.Equals(i.Name, ctrl.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            ControlObjects.Add(MapFromGenerated(ctrl));
        }

        _logger.LogInformation(
            "Control Objects loaded — SiteId: {SiteId}, Count: {Count}, Proposals: {Proposals}, PipelineAvailable: {PipelineAvailable}",
            siteId, ControlObjects.Count, analysisResult?.Proposals.Count ?? 0, _pipelineOrchestrator is not null);

        RaiseSummaryChanged();
    }

    private static ControlObjectListItem MapFromGenerated(GeneratedControl c) => new()
    {
        Name = c.Name,
        Namespace = c.Namespace,
        Confidence = (int)Math.Round(c.Confidence),
        Status = ControlObjectStatus.Generated,
        IsGenerated = true,
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

    private async Task AnalyzeCorpusAsync(CancellationToken ct)
    {
        if (IsBusy || _pipelineOrchestrator is null) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Analyzing corpus for control objects\u2026";

            var result = await _pipelineOrchestrator.AnalyzeForControlObjectsAsync(_siteId, ct);

            _logger.LogInformation(
                "Analysis complete — SiteId: {SiteId}, Proposals: {Count}",
                _siteId, result.Proposals.Count);

            StatusMessage = $"Analysis complete \u2014 {result.Proposals.Count} proposals found.";

            LoadControlObjects(_siteId);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Analysis cancelled.";
            _logger.LogInformation("Corpus analysis cancelled — SiteId: {SiteId}", _siteId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Analysis failed: {ex.Message}";
            _logger.LogError(ex, "Corpus analysis failed — SiteId: {SiteId}", _siteId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task GenerateAllPendingAsync(CancellationToken ct)
    {
        if (IsBusy || _pipelineOrchestrator is null) return;

        try
        {
            IsBusy = true;

            var approved = ControlObjects
                .Where(x => x.Status == ControlObjectStatus.Approved && x.Proposal is not null)
                .Select(x => x.Proposal!)
                .ToList();

            StatusMessage = $"Generating {approved.Count} approved control(s)\u2026";

            var locator = _corpusService.GetCurrentAnalysisResult(_siteId)?.LocatorReport;
            await _pipelineOrchestrator.GenerateControlObjectsAsync(
                _siteId, approved, _siteNamespace, locator, ct);

            _logger.LogInformation(
                "Batch generation complete — SiteId: {SiteId}, Count: {Count}",
                _siteId, approved.Count);
            StatusMessage = "Generation complete.";

            LoadControlObjects(_siteId, _siteNamespace);
        }
        catch (Exceptions.LlmAuthRequiredException ex)
        {
            StatusMessage = "Authentication required — check your API key configuration.";
            _logger.LogWarning(ex, "LLM auth required during control generation — SiteId: {SiteId}", _siteId);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Generation cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Generation failed: {ex.Message}";
            _logger.LogError(ex, "Batch generation failed — SiteId: {SiteId}", _siteId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Import()
        => _logger.LogInformation("Import control objects requested (not yet implemented)");

    private void Export()
        => _logger.LogInformation("Export control objects requested (not yet implemented)");

    private void Approve(ControlObjectListItem? item)
    {
        if (item is null) return;
        item.Status = ControlObjectStatus.Approved;
        if (item.Proposal is not null)
            _corpusService.UpdateProposalApproval(_siteId, item.Name, ControlObjectStatus.Approved);
        _logger.LogInformation("Control approved — Name: {Name}", item.Name);
        RaiseSummaryChanged();
        FilteredControlObjects.Refresh();
    }

    private void Reject(ControlObjectListItem? item)
    {
        if (item is null) return;
        item.Status = ControlObjectStatus.Rejected;
        if (item.Proposal is not null)
            _corpusService.UpdateProposalApproval(_siteId, item.Name, ControlObjectStatus.Rejected);
        _logger.LogInformation("Control rejected — Name: {Name}", item.Name);
        RaiseSummaryChanged();
        FilteredControlObjects.Refresh();
    }

    private async Task RegenerateAsync(ControlObjectListItem? item, CancellationToken ct)
    {
        if (item is null || IsBusy || _controlGenerationService is null) return;

        if (item.Proposal is null)
        {
            _logger.LogWarning(
                "Cannot regenerate {Name}: no proposal data", item.Name);
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = $"Regenerating {item.Name}\u2026";
            item.Status = ControlObjectStatus.Approved;

            if (item.IsGenerated)
            {
                try { _controlRegistry.DeleteControl(item.Name); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Delete before regenerate failed — Name: {Name}", item.Name);
                }
            }

            var generated = await _controlGenerationService.GenerateControlAsync(
                item.Proposal, _siteNamespace, ct);

            item.Code = generated.Code;
            item.Namespace = generated.Namespace;
            item.CreatedAt = generated.CreatedAt.UtcDateTime;
            item.Status = ControlObjectStatus.Generated;
            item.IsGenerated = true;

            _logger.LogInformation("Regenerated control — Name: {Name}", item.Name);
            StatusMessage = $"Regenerated {item.Name}.";
        }
        catch (Exceptions.LlmAuthRequiredException ex)
        {
            StatusMessage = "Authentication required — check your API key configuration.";
            _logger.LogWarning(ex,
                "LLM auth required during regeneration — Name: {Name}", item.Name);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"Regeneration of {item.Name} cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Regeneration failed: {ex.Message}";
            _logger.LogError(ex, "Regeneration failed — Name: {Name}", item.Name);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Delete(ControlObjectListItem? item)
    {
        if (item is null) return;
        ControlObjects.Remove(item);

        if (item.Status == ControlObjectStatus.Generated && !string.IsNullOrEmpty(item.Name))
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
