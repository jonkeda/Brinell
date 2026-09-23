# Step 07a-7 — Recording Stop Clears Session

## Objective

When recording stops, clear the "This Session" sidebar section and reset recording UI state. Optionally prompt "Analyze corpus now?".

## Current State

`RecordingViewModel.StopRecording()` fires `AnalyzePromptRequested` and `RecordingStopped` events, but nothing in `MainViewModel` handles the cleanup of sidebar state.

`MainViewModel.ToggleRecording()` (after Step 07a-6) sets `Sidebar.IsRecording = false` on stop, which hides the "This Session" section. But `SessionPages` still has stale data if the user starts a new recording.

## Changes

### 1. Update `MainViewModel.ToggleRecording()` — clear session on stop

```csharp
private void ToggleRecording()
{
    if (Recording.IsRecording)
    {
        Recording.StopRecording();
        Sidebar.ClearSession();    // clears SessionPages + sets IsRecording = false
    }
    else
    {
        Recording.StartRecording();
        Sidebar.IsRecording = true;
    }
}
```

`Sidebar.ClearSession()` already sets `IsRecording = false` and clears `SessionPages` (from Step 07a-1), so the single call handles both.

### 2. Handle `AnalyzePromptRequested`

When recording stops, `RecordingViewModel` fires `AnalyzePromptRequested`. Wire this in `MainViewModel` constructor:

```csharp
Recording.AnalyzePromptRequested += OnAnalyzePromptRequested;
```

```csharp
private void OnAnalyzePromptRequested()
{
    // Only prompt if pages were actually captured
    if (Recording.SessionSnapshots.Count == 0)
        return;

    // Show a message box asking whether to analyze
    // (MessageBox is acceptable for a tool app — no need for a custom dialog)
    var result = System.Windows.MessageBox.Show(
        $"{Recording.SessionSnapshots.Count} pages captured. Analyze corpus now?",
        "Recording Complete",
        System.Windows.MessageBoxButton.YesNo,
        System.Windows.MessageBoxImage.Question);

    if (result == System.Windows.MessageBoxResult.Yes)
    {
        // Future: switch to analysis view
        _logger.LogInformation("User chose to analyze corpus");
    }
}
```

### 3. Refresh corpus stats after recording

After recording stops, the corpus page count may have changed. Update the status bar:

```csharp
private void ToggleRecording()
{
    if (Recording.IsRecording)
    {
        Recording.StopRecording();
        Sidebar.ClearSession();

        // Refresh stats (page count may have changed if corpus storage is active)
        if (ActiveSite is not null)
        {
            Sidebar.CorpusStats = $"{ActiveSite.PageCount} pages · {ActiveSite.ControlCount} controls";
        }
    }
    else
    {
        Recording.StartRecording();
        Sidebar.IsRecording = true;
    }
}
```

Note: Until Step 4.8 (SQLite corpus store) is implemented, `ActiveSite.PageCount` won't actually update. This is forward-compatible wiring.

## Files Modified

| File | Action |
|------|--------|
| `ViewModels/MainViewModel.cs` | **Edit** — update `ToggleRecording`, add `OnAnalyzePromptRequested` handler |

## Verification

- Build succeeds
- Start recording → capture pages → stop recording:
  - "This Session" section clears
  - Sidebar returns to normal mode
  - If pages captured: "Analyze corpus now?" dialog appears
  - If no pages captured: no dialog
- Start a new recording → session is fresh (no stale data)
- All existing tests pass

## Checklist

- [ ] `ToggleRecording` calls `Sidebar.ClearSession()` on stop
- [ ] `AnalyzePromptRequested` event handled — shows prompt if pages were captured
- [ ] Corpus stats refreshed after recording stops
- [ ] New recording starts with empty session
- [ ] Build succeeds, tests pass
