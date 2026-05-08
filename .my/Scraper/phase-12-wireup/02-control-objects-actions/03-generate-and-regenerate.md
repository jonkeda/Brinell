# Step 12.W.3 — Wire GenerateAllPending & RegenerateAsync

## Objective

Connect `GenerateAllPendingAsync` to the pipeline orchestrator for batch generation of approved proposals, and wire `RegenerateAsync` for single-item regeneration. Handle `LlmAuthRequiredException` with a user-facing auth message.

## Dependencies

- `PipelineOrchestrator.GenerateControlObjectsAsync(siteId, ct, progress?)` — batch generates all approved proposals
- `ControlGenerationService.GenerateControlAsync(proposal, ct)` — generates a single control (inject directly or extract from orchestrator)
- `LoadControlObjects(siteId)` — reload after generation
- `IsBusy` / `StatusMessage` from step 12.W.2
- `LlmAuthRequiredException` — thrown when API key is missing/expired

## Implementation

### Files

- **Modify**: `ControlObjectsTabViewModel.cs` — replace stubs for `GenerateAllPendingAsync` and `RegenerateAsync`
- **Possibly inject**: `IControlGenerationService` if not already available via orchestrator for single-item regen

### Code sketch

```csharp
// ControlObjectsTabViewModel.cs

private async Task GenerateAllPendingAsync(CancellationToken ct)
{
    if (IsBusy) return;

    try
    {
        IsBusy = true;

        var approvedCount = ControlObjects.Count(x => x.Status == ControlObjectStatus.Approved);
        StatusMessage = $"Generating {approvedCount} approved control(s)…";

        await _pipelineOrchestrator.GenerateControlObjectsAsync(_siteId, ct);

        _logger.LogInformation("Batch generation complete for site {SiteId}", _siteId);
        StatusMessage = "Generation complete.";

        await LoadControlObjects(_siteId);
    }
    catch (LlmAuthRequiredException ex)
    {
        StatusMessage = "Authentication required — check your API key configuration.";
        _logger.LogWarning(ex, "LLM auth required during generation");
    }
    catch (OperationCanceledException)
    {
        StatusMessage = "Generation cancelled.";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Generation failed: {ex.Message}";
        _logger.LogError(ex, "Batch generation failed for site {SiteId}", _siteId);
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task RegenerateAsync(ControlObjectItemViewModel item, CancellationToken ct)
{
    if (IsBusy) return;
    if (item.Proposal is null)
    {
        _logger.LogWarning("Cannot regenerate {Name}: no proposal data", item.Name);
        return;
    }

    try
    {
        IsBusy = true;
        StatusMessage = $"Regenerating {item.Name}…";
        item.Status = ControlObjectStatus.Approved; // Reset to approved during regen

        // Delete existing generated control if present
        if (item.IsGenerated)
        {
            _controlRegistry.DeleteControl(item.Name);
        }

        await _controlGenerationService.GenerateControlAsync(item.Proposal, ct);

        _logger.LogInformation("Regenerated control: {Name}", item.Name);
        item.Status = ControlObjectStatus.Generated;
        item.IsGenerated = true;
        StatusMessage = $"Regenerated {item.Name}.";
    }
    catch (LlmAuthRequiredException ex)
    {
        StatusMessage = "Authentication required — check your API key configuration.";
        _logger.LogWarning(ex, "LLM auth required during regeneration of {Name}", item.Name);
    }
    catch (OperationCanceledException)
    {
        StatusMessage = $"Regeneration of {item.Name} cancelled.";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Regeneration failed: {ex.Message}";
        _logger.LogError(ex, "Regeneration failed for {Name}", item.Name);
    }
    finally
    {
        IsBusy = false;
    }
}
```

### Behavior

- **GenerateAllPending**: generates all controls whose status is `Approved`. Uses orchestrator's batch method. On completion, reloads entire list so Generated status is reflected.
- **RegenerateAsync**: targets a single item. Deletes existing generated control first (if any), then re-generates from the proposal. Updates item status inline without full reload.
- Both methods guard with `IsBusy` to prevent concurrent operations.
- `LlmAuthRequiredException` surfaces a clear auth-required message (not a raw exception).
- Status message updates at each stage for user feedback.
- Cancellation is supported and handled gracefully.

## Checklist

- [ ] `GenerateAllPendingAsync` calls `_pipelineOrchestrator.GenerateControlObjectsAsync(_siteId, ct)`
- [ ] `GenerateAllPendingAsync` reloads list on success
- [ ] `RegenerateAsync` calls `_controlGenerationService.GenerateControlAsync(proposal, ct)`
- [ ] `RegenerateAsync` deletes existing control before regenerating
- [ ] `RegenerateAsync` updates item status to `Generated` on success
- [ ] `LlmAuthRequiredException` caught and surfaced as friendly message
- [ ] `IsBusy` guards both methods
- [ ] `IControlGenerationService` injected into VM (or accessed via orchestrator)
- [ ] Null proposal guard in `RegenerateAsync`
