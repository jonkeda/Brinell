# Step 12.W.2 — Wire GenerateAll and RegenerateSelected

## Objective

Wire `GenerateAllAsync` to invoke the pipeline orchestrator for bulk page-object generation, and wire `RegenerateSelectedAsync` to regenerate a single page object. Both commands must report progress per-row, handle LLM-specific exceptions gracefully, and guard against concurrent invocations with an `IsBusy` flag.

## Dependencies

- `PipelineOrchestrator.GeneratePageObjectsAsync(siteId, ct, IProgress<PipelineProgress>?)`
- `PageGenerationService.GeneratePageAsync(siteId, snapshotId, ct)` → `PageGenerationResult`
- `CorpusService.GetPageObjectBySnapshot(snapshotId)` → `PageObjectRecord?`
- `LlmAuthRequiredException`, `LlmRateLimitedException` (defined in Brinell.Scraper or shared exceptions)
- `LoadPageObjects` (spec 01) for post-generation reload

## Implementation

### Files

| Action | Path |
|--------|------|
| Modify | `Brinell.Scraper/ViewModels/PageObjectsTabViewModel.cs` |

### Code sketch

**New properties:**

```csharp
[ObservableProperty]
private bool _isBusy;

[ObservableProperty]
private string _statusMessage = string.Empty;
```

**GenerateAllAsync:**

```csharp
[RelayCommand(CanExecute = nameof(CanGenerate))]
private async Task GenerateAllAsync(CancellationToken ct)
{
    IsBusy = true;
    StatusMessage = "Generating page objects…";

    var progress = new Progress<PipelineProgress>(p =>
    {
        // Update the matching row in-place
        var row = PageObjects.FirstOrDefault(r => r.SnapshotId == p.SnapshotId);
        if (row is not null)
        {
            row.Status = p.Success ? PageObjectStatus.Generated : PageObjectStatus.Error;
            row.GeneratedAt = p.Timestamp;
        }
        StatusMessage = $"Generated {p.CompletedCount}/{p.TotalCount}…";
    });

    try
    {
        await _pipelineOrchestrator.GeneratePageObjectsAsync(_siteId, ct, progress);
        StatusMessage = "Generation complete.";
    }
    catch (LlmAuthRequiredException)
    {
        StatusMessage = "LLM authentication required. Check API key in Settings.";
    }
    catch (LlmRateLimitedException ex)
    {
        StatusMessage = $"Rate limited. Retry after {ex.RetryAfter?.TotalSeconds:F0}s.";
    }
    catch (OperationCanceledException)
    {
        StatusMessage = "Generation cancelled.";
    }
    finally
    {
        IsBusy = false;
        // Reload from DB to ensure consistency
        await LoadPageObjects(_siteId);
    }
}

private bool CanGenerate() => !IsBusy && PageObjects.Any(p => p.Status == PageObjectStatus.NotGenerated);
```

**RegenerateSelectedAsync:**

```csharp
[RelayCommand(CanExecute = nameof(CanRegenerateSelected))]
private async Task RegenerateSelectedAsync(CancellationToken ct)
{
    var selected = SelectedPageObject;
    if (selected is null) return;

    IsBusy = true;
    StatusMessage = $"Regenerating '{selected.PageTitle}'…";

    try
    {
        var result = await _pageGenerationService.GeneratePageAsync(_siteId, selected.SnapshotId, ct);

        // Reload single row from DB
        var record = _corpusService.GetPageObjectBySnapshot(selected.SnapshotId);
        if (record is not null)
        {
            selected.Status = record.Status;
            selected.GeneratedAt = record.GeneratedAt;
            selected.MainCode = record.Code;
            selected.UsedControlObjects = record.UsedControlObjects;
        }

        StatusMessage = result.Success
            ? "Regeneration complete."
            : $"Regeneration failed: {result.ErrorMessage}";
    }
    catch (LlmAuthRequiredException)
    {
        StatusMessage = "LLM authentication required. Check API key in Settings.";
    }
    catch (LlmRateLimitedException ex)
    {
        StatusMessage = $"Rate limited. Retry after {ex.RetryAfter?.TotalSeconds:F0}s.";
    }
    catch (OperationCanceledException)
    {
        StatusMessage = "Regeneration cancelled.";
    }
    finally
    {
        IsBusy = false;
    }
}

private bool CanRegenerateSelected() => !IsBusy && SelectedPageObject is not null;
```

### Behavior

- Clicking "Generate All" disables the button (`IsBusy` guard) and begins bulk generation.
- As each page completes, the progress callback finds the matching row by `SnapshotId` and updates its `Status` in-place so the UI reflects real-time progress.
- `StatusMessage` shows a running count (`Generated 3/12…`).
- On completion, `LoadPageObjects` reloads from DB to ensure the view matches persisted state.
- "Regenerate Selected" operates on the currently selected row only; on success, it reloads that single row from the DB without disturbing the rest of the list.
- `LlmAuthRequiredException` surfaces a user-friendly auth message in `StatusMessage` (no crash dialog).
- `LlmRateLimitedException` surfaces the retry-after interval so the user knows when to retry.
- Cancellation (e.g., navigating away) sets `StatusMessage` to "cancelled" and clears `IsBusy`.
- `CanGenerate` and `CanRegenerateSelected` are re-evaluated when `IsBusy` or selection changes (call `GenerateAllCommand.NotifyCanExecuteChanged()` in the property setters).

## Checklist

- [ ] Add `IsBusy` and `StatusMessage` observable properties
- [ ] Replace `GenerateAllAsync` stub with orchestrator call + progress callback
- [ ] Replace `RegenerateSelectedAsync` stub with single-page generation + row reload
- [ ] Handle `LlmAuthRequiredException` and `LlmRateLimitedException` in both commands
- [ ] Call `NotifyCanExecuteChanged()` on both commands when `IsBusy` or `SelectedPageObject` changes
- [ ] Bind `StatusMessage` to status bar text in XAML
- [ ] Bind `IsBusy` to button `IsEnabled` (or rely on command CanExecute)
- [ ] Verify cancellation token flows from command infrastructure
- [ ] Unit test: progress callback updates correct row
- [ ] Unit test: auth exception surfaces message without throw
