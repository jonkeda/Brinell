# Step 12.W.4a — Re-Record Page & Re-Record All

## Objective

Wire the `ReRecordPageAsync` and `ReRecordAllAsync` stubs in `CorpusTabViewModel` so that triggering a re-record navigates the embedded browser to the page URL, switches to the Scraping tab, and starts the recording pipeline. `ReRecordAllAsync` iterates every page sequentially, waiting for each recording to complete before proceeding.

## Dependencies

- `CorpusTabViewModel.ReRecordRequested` event (already fires from `ReRecordPageAsync`)
- `WorkspaceViewModel` (owns `SelectedTabIndex`, `Scraping`, `Corpus`)
- `RecordingViewModel.StartRecordingCommand` and `RecordingViewModel.IsRecording`
- `BrowserViewModel.AddressUrl` (set) + `NavigateCommand`
- `CorpusTabViewModel.Pages` collection

## Implementation

### Files

| File | Action |
|------|--------|
| `WorkspaceViewModel.cs` | Subscribe `Corpus.ReRecordRequested`, add handler |
| `CorpusTabViewModel.cs` | Implement `ReRecordAllAsync`, add `IsBusy` property |
| `RecordingViewModel.cs` | Ensure `IsRecording` raises `PropertyChanged` (verify only) |

### Code sketch

**WorkspaceViewModel.cs** — subscribe in constructor:

```csharp
Corpus.ReRecordRequested += OnReRecordPageRequested;
```

```csharp
private async void OnReRecordPageRequested(PageItemViewModel page)
{
    SelectedTabIndex = 0; // Switch to Scraping tab
    Browser.AddressUrl = page.PageUrl;
    Browser.NavigateCommand.Execute(null);

    // Small delay to let navigation settle
    await Task.Delay(300);

    Scraping.Recording.StartRecordingCommand.Execute(null);
}
```

**CorpusTabViewModel.cs** — `IsBusy` property:

```csharp
private bool _isBusy;
public bool IsBusy
{
    get => _isBusy;
    private set => SetProperty(ref _isBusy, value);
}
```

**CorpusTabViewModel.cs** — `ReRecordAllAsync`:

```csharp
private async Task ReRecordAllAsync(CancellationToken ct)
{
    if (IsBusy) return;
    IsBusy = true;

    try
    {
        foreach (var page in Pages.ToList())
        {
            ct.ThrowIfCancellationRequested();

            ReRecordRequested?.Invoke(page);

            // Wait for recording to start
            await WaitForRecordingStarted(ct);

            // Wait for recording to finish
            await WaitForRecordingCompleted(ct);
        }
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task WaitForRecordingStarted(CancellationToken ct)
{
    // Poll until IsRecording becomes true (max ~5s)
    for (int i = 0; i < 50 && !_recordingViewModel.IsRecording; i++)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
    }
}

private async Task WaitForRecordingCompleted(CancellationToken ct)
{
    // Poll until IsRecording flips back to false (max ~60s)
    for (int i = 0; i < 600 && _recordingViewModel.IsRecording; i++)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
    }
}
```

> **Note:** `_recordingViewModel` must be injected or resolved via `WorkspaceViewModel`. If not directly accessible, raise a `ReRecordAllRequested` event that `WorkspaceViewModel` orchestrates instead, passing back a `Task` per page.

### Behavior

- Clicking "Re-Record" on a single page fires `ReRecordRequested`, which `WorkspaceViewModel` handles by switching tab, navigating, and starting recording.
- Clicking "Re-Record All" sets `IsBusy = true`, iterates pages sequentially, invoking the same per-page flow.
- Each iteration waits for `IsRecording` to become true then false before moving to the next page.
- If cancelled via `CancellationToken`, the loop exits cleanly and `IsBusy` resets.
- UI should bind button `IsEnabled` to `!IsBusy` to prevent double-invocation.

## Checklist

- [ ] `WorkspaceViewModel` subscribes `Corpus.ReRecordRequested += OnReRecordPageRequested`
- [ ] `OnReRecordPageRequested` sets `SelectedTabIndex = 0`, navigates browser, starts recording
- [ ] `CorpusTabViewModel.IsBusy` property added with `SetProperty`
- [ ] `ReRecordAllAsync` iterates all pages, waits per recording completion
- [ ] Cancellation token respected throughout
- [ ] Re-Record All button binds `IsEnabled` to `!IsBusy`
- [ ] Unsubscribe event in `Dispose` / teardown
