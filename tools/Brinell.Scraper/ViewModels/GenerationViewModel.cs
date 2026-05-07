using System.Collections.ObjectModel;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class GenerationViewModel : ViewModelBase
{
    private readonly PageGenerationService _pageGenerationService;
    private readonly ILogger<GenerationViewModel> _logger;
    private string _statusText = "";
    private bool _isGenerating;
    private int _completedCount;
    private int _failedCount;
    private int _totalCount;

    public GenerationViewModel(
        PageGenerationService pageGenerationService,
        ILogger<GenerationViewModel> logger)
    {
        _pageGenerationService = pageGenerationService;
        _logger = logger;

        GenerateBatchCommand = new AsyncRelayCommand(GenerateBatchAsync, () => !IsGenerating);
    }

    public ObservableCollection<PageGenerationResult> Results { get; } = [];

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        set => SetProperty(ref _isGenerating, value);
    }

    public int CompletedCount
    {
        get => _completedCount;
        set => SetProperty(ref _completedCount, value);
    }

    public int FailedCount
    {
        get => _failedCount;
        set => SetProperty(ref _failedCount, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    public ICommand GenerateBatchCommand { get; }

    public async Task GenerateBatchAsync(
        IReadOnlyList<DomSnapshot> snapshots,
        string namespaceName,
        LocatorReport? locatorReport,
        CancellationToken ct = default)
    {
        IsGenerating = true;
        TotalCount = snapshots.Count;
        CompletedCount = 0;
        FailedCount = 0;
        Results.Clear();
        StatusText = $"Generating 0/{TotalCount}...";

        try
        {
            var results = await _pageGenerationService.GenerateBatchAsync(
                snapshots, namespaceName, locatorReport, ct);

            foreach (var result in results)
            {
                Results.Add(result);
                if (result.Validation.IsValid)
                    CompletedCount++;
                else
                    FailedCount++;
                StatusText = $"Generated {CompletedCount + FailedCount}/{TotalCount}";
            }

            StatusText = $"Complete — {CompletedCount} succeeded, {FailedCount} failed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch generation failed");
            StatusText = $"Generation failed: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private Task GenerateBatchAsync(CancellationToken ct)
    {
        _logger.LogInformation("Batch generation requested (requires snapshot selection)");
        return Task.CompletedTask;
    }
}
