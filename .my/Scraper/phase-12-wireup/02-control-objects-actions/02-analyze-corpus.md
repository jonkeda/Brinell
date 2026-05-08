# Step 12.W.2 — Wire AnalyzeCorpusAsync

## Objective

Connect the `AnalyzeCorpusAsync` command to the pipeline orchestrator, add busy-state tracking to disable UI during the operation, and reload the control objects list on completion.

## Dependencies

- `PipelineOrchestrator.AnalyzeForControlObjectsAsync(siteId, ct, progress?)` — performs LLM analysis
- `LoadControlObjects(siteId)` — already wired per step 12.W.1
- `IsBusy` property pattern (add if not present)

## Implementation

### Files

- **Modify**: `ControlObjectsTabViewModel.cs` — replace stub body of `AnalyzeCorpusAsync`, add `IsBusy` and `StatusMessage` properties

### Code sketch

```csharp
// ControlObjectsTabViewModel.cs

private bool _isBusy;
public bool IsBusy
{
    get => _isBusy;
    set => SetProperty(ref _isBusy, value);
}

private string? _statusMessage;
public string? StatusMessage
{
    get => _statusMessage;
    set => SetProperty(ref _statusMessage, value);
}

private async Task AnalyzeCorpusAsync(CancellationToken ct)
{
    if (IsBusy) return;

    try
    {
        IsBusy = true;
        StatusMessage = "Analyzing corpus for control objects…";

        var result = await _pipelineOrchestrator.AnalyzeForControlObjectsAsync(_siteId, ct);

        _logger.LogInformation(
            "Analysis complete: {Count} proposals identified",
            result.Proposals.Count);

        StatusMessage = $"Analysis complete — {result.Proposals.Count} proposals found.";

        await LoadControlObjects(_siteId);
    }
    catch (OperationCanceledException)
    {
        StatusMessage = "Analysis cancelled.";
        _logger.LogInformation("Corpus analysis cancelled by user");
    }
    catch (Exception ex)
    {
        StatusMessage = $"Analysis failed: {ex.Message}";
        _logger.LogError(ex, "Corpus analysis failed for site {SiteId}", _siteId);
    }
    finally
    {
        IsBusy = false;
    }
}
```

### Behavior

- Clicking Analyze sets `IsBusy = true`, disabling Analyze/Generate/Regenerate buttons.
- Status message updates to show progress text.
- On success, logs proposal count, updates status message, and reloads the list.
- On cancellation (user navigates away or cancels), logs and shows cancelled message.
- On exception, logs error and displays the exception message in status bar.
- `IsBusy` resets to false in all paths via `finally`.
- Command `CanExecute` should return `!IsBusy` (bind to `IsBusy` via `ObservesProperty` or relay command pattern).

## Checklist

- [ ] `IsBusy` property added with `INotifyPropertyChanged`
- [ ] `StatusMessage` property added
- [ ] Stub body replaced with `_pipelineOrchestrator.AnalyzeForControlObjectsAsync(_siteId, ct)`
- [ ] `LoadControlObjects(_siteId)` called after successful analysis
- [ ] `OperationCanceledException` caught separately
- [ ] General exception caught, logged, surfaced to UI
- [ ] `IsBusy` reset in `finally`
- [ ] Analyze button disabled while `IsBusy` is true
