# Step 12.W.4b — Generate Page Object

## Objective

Wire the `GeneratePageObjectAsync(version, ct)` stub in `CorpusTabViewModel` to invoke `PageGenerationService.GeneratePageAsync` for a single snapshot, updating the version's status on success or failure. If the pipeline service is unavailable, the command should present a disabled state with an explanatory tooltip.

## Dependencies

- `PageGenerationService.GeneratePageAsync(siteId, snapshotId, ct)` — returns generated page object or throws
- `CorpusTabViewModel._siteId` — current site identifier
- `SnapshotVersionViewModel.SnapshotId`, `.HasPageObject`, `.PageObjectStatus`
- `PipelineOrchestrator` (nullable — used to gate availability)

## Implementation

### Files

| File | Action |
|------|--------|
| `CorpusTabViewModel.cs` | Implement `GeneratePageObjectAsync`, add service field |
| `SnapshotVersionViewModel.cs` | Ensure `HasPageObject` and `PageObjectStatus` are settable with change notification |

### Code sketch

**CorpusTabViewModel.cs** — constructor injection:

```csharp
private readonly PageGenerationService? _pageGenerationService;

public CorpusTabViewModel(
    ...,
    PageGenerationService? pageGenerationService = null)
{
    _pageGenerationService = pageGenerationService;
}

public bool CanGeneratePageObject => _pageGenerationService is not null;
```

**CorpusTabViewModel.cs** — implementation:

```csharp
private async Task GeneratePageObjectAsync(SnapshotVersionViewModel version, CancellationToken ct)
{
    if (_pageGenerationService is null)
    {
        _logger.LogWarning("PageGenerationService not available — generation disabled.");
        return;
    }

    version.PageObjectStatus = PageObjectStatus.Generating;

    try
    {
        await _pageGenerationService.GeneratePageAsync(_siteId, version.SnapshotId, ct);

        version.HasPageObject = true;
        version.PageObjectStatus = PageObjectStatus.Generated;

        _logger.LogInformation("Page object generated for snapshot {SnapshotId}.", version.SnapshotId);
    }
    catch (OperationCanceledException)
    {
        version.PageObjectStatus = PageObjectStatus.None;
        throw;
    }
    catch (Exception ex)
    {
        version.PageObjectStatus = PageObjectStatus.Error;
        _logger.LogError(ex, "Failed to generate page object for snapshot {SnapshotId}.", version.SnapshotId);
    }
}
```

**SnapshotVersionViewModel.cs** — status enum and properties:

```csharp
public enum PageObjectStatus { None, Generating, Generated, Error }

private PageObjectStatus _pageObjectStatus;
public PageObjectStatus PageObjectStatus
{
    get => _pageObjectStatus;
    set => SetProperty(ref _pageObjectStatus, value);
}

private bool _hasPageObject;
public bool HasPageObject
{
    get => _hasPageObject;
    set => SetProperty(ref _hasPageObject, value);
}
```

### Behavior

- Clicking "Generate" on a snapshot version calls `GeneratePageObjectAsync(version, ct)`.
- Status transitions: `None` → `Generating` → `Generated` (success) or `Error` (failure).
- On cancellation, status resets to `None`.
- If `PageGenerationService` is null (not registered), `CanGeneratePageObject` is `false`; the button should bind `IsEnabled` to this and show a tooltip: "Pipeline service not configured".
- Errors are logged but do not throw to the UI; the `Error` status drives visual feedback (e.g., red icon).

## Checklist

- [ ] `PageGenerationService?` injected into `CorpusTabViewModel` constructor
- [ ] `CanGeneratePageObject` property exposed for binding
- [ ] `GeneratePageObjectAsync` calls `GeneratePageAsync(_siteId, version.SnapshotId, ct)`
- [ ] On success: `HasPageObject = true`, `PageObjectStatus = Generated`
- [ ] On failure: `PageObjectStatus = Error`, error logged
- [ ] On cancel: `PageObjectStatus = None`, exception re-thrown
- [ ] `SnapshotVersionViewModel` has `PageObjectStatus` enum property with change notification
- [ ] Generate button disabled + tooltip when service unavailable
