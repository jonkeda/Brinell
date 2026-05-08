# Step 12.W.8c — Wire Recording Stop & Analyze Prompt

## Objective

Wire the recording stop flow: clear the "This Session" sidebar section, reset recording UI state, and prompt the user to analyze the corpus if pages were captured.

## Dependencies

- `RecordingViewModel.StopRecording()` — fires `RecordingStopped`, `AnalyzePromptRequested`
- `SidebarViewModel.ClearSession()` (from step 08a)
- `Recording.SessionSnapshots.Count` — captured page count

## Implementation

### Files

| File | Action |
|------|--------|
| `MainViewModel.cs` | Subscribe `AnalyzePromptRequested`, handle post-stop flow |

### Code sketch

**MainViewModel.cs — constructor subscription:**

```csharp
Recording.AnalyzePromptRequested += OnAnalyzePromptRequested;
```

**MainViewModel.cs — handler:**

```csharp
private void OnAnalyzePromptRequested()
{
    if (Recording.SessionSnapshots.Count == 0)
        return;

    var result = MessageBox.Show(
        $"{Recording.SessionSnapshots.Count} pages captured. Analyze corpus now?",
        "Recording Complete",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result == MessageBoxResult.Yes)
    {
        // Switch to Control Objects tab and trigger analysis
        SelectedTabIndex = 1; // Control Objects tab
        _ = ControlObjects.AnalyzeCorpusCommand.ExecuteAsync(null);
    }
}
```

### Flow

1. User clicks ⏺ (stop) or toolbar Stop button
2. `Recording.StopRecording()` fires `AnalyzePromptRequested`
3. `MainViewModel` shows MessageBox: "X pages captured. Analyze corpus now?"
4. If Yes → switch to Control Objects tab, trigger `AnalyzeCorpusAsync`
5. Sidebar session is cleared, red border removed

## Checklist

- [ ] Stopping recording clears sidebar "This Session" section
- [ ] `AnalyzePromptRequested` fires only when pages were captured
- [ ] MessageBox shows captured page count
- [ ] "Yes" switches to Control Objects tab and starts analysis
- [ ] "No" returns to normal state without further action
