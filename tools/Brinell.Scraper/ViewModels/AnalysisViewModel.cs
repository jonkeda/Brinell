using System.Collections.ObjectModel;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class AnalysisViewModel : ViewModelBase
{
    private readonly AnalysisService _analysisService;
    private readonly ILogger<AnalysisViewModel> _logger;
    private LocatorReport? _locatorReport;
    private string _statusText = "";
    private bool _isAnalyzing;

    public AnalysisViewModel(
        AnalysisService analysisService,
        ILogger<AnalysisViewModel> logger)
    {
        _analysisService = analysisService;
        _logger = logger;

        ApproveCommand = new RelayCommand<ControlProposal>(ApproveControl);
        RejectCommand = new RelayCommand<ControlProposal>(RejectControl);
        ApproveAllCommand = new RelayCommand(ApproveAll, () => ProposedControls.Count > 0);
        AnalyzeCommand = new AsyncRelayCommand(ct => RunAnalysisAsync(0, ct));
    }

    public ObservableCollection<ControlProposal> ProposedControls { get; } = [];

    public LocatorReport? LocatorReport
    {
        get => _locatorReport;
        set => SetProperty(ref _locatorReport, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set => SetProperty(ref _isAnalyzing, value);
    }

    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }
    public ICommand ApproveAllCommand { get; }
    public ICommand AnalyzeCommand { get; }

    public async Task RunAnalysisAsync(long siteId, CancellationToken ct = default)
    {
        IsAnalyzing = true;
        StatusText = "Analyzing corpus...";

        try
        {
            var result = await _analysisService.AnalyzeCorpusAsync(siteId, ct);

            ProposedControls.Clear();
            foreach (var ctrl in result.ProposedControls)
                ProposedControls.Add(ctrl);

            LocatorReport = result.LocatorReport;
            StatusText = $"Found {result.ProposedControls.Count} control patterns";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            StatusText = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private void ApproveControl(ControlProposal? proposal)
    {
        if (proposal is null) return;
        proposal.IsApproved = true;
        _logger.LogInformation("Control approved — Name: {ControlName}", proposal.Name);
    }

    private void RejectControl(ControlProposal? proposal)
    {
        if (proposal is null) return;
        proposal.IsApproved = false;
        _logger.LogInformation("Control rejected — Name: {ControlName}", proposal.Name);
    }

    private void ApproveAll()
    {
        foreach (var proposal in ProposedControls)
            proposal.IsApproved = true;
        _logger.LogInformation("All controls approved — Count: {Count}", ProposedControls.Count);
    }
}
