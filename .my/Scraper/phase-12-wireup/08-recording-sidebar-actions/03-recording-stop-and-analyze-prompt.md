# Step 12.W.8c — Wire Recording Stop & Analyze Prompt

## Objective

Wire the recording stop flow: stop capture and prompt the user to transfer/analyze recorded pages. Keep "This Session" visible until the user chooses to analyze, then clear session state as part of transfer.

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

**MainViewModel.cs — handler (implemented):**

```csharp
private void OnAnalyzePromptRequested()
{
    var capturedCount = Recording.SessionSnapshots.Count;
    if (capturedCount == 0)
        return;

    var result = System.Windows.MessageBox.Show(
        $"{capturedCount} pages captured. Transfer to corpus and analyze now?",
        "Recording Complete",
        System.Windows.MessageBoxButton.YesNo,
        System.Windows.MessageBoxImage.Question);

    if (result == System.Windows.MessageBoxResult.Yes)
        AnalyzeSession();
}
```

### Flow

1. User clicks ⏹ Stop.
2. `Recording.StopRecording()` flips recording state and fires `AnalyzePromptRequested`.
3. `MainViewModel` shows: "X pages captured. Transfer to corpus and analyze now?"
4. If Yes → `AnalyzeSession()` stores session snapshots into corpus, refreshes sidebar corpus, then clears session (`Recording.ClearSnapshots()` + `Sidebar.ClearSession()`).
5. If No → session pages remain visible for review/retry; red border is removed because recording is stopped.

## IFrame Validation

Session snapshots transferred during Analyze include iframe data because capture paths already use:

- `DomCaptureService.CaptureAsync(webView, _highlight.TrackedFrames)`
- same-origin iframe traversal in capture script
- cross-origin frame capture via `CoreWebView2Frame.ExecuteScriptAsync(...)`

## Learned Notes (from previous implementation)

- Do not clear session immediately on stop; it prevents review and forces re-recording if the user clicks No.
- Keep transfer-to-corpus explicit inside Analyze action to preserve user intent and avoid accidental corpus pollution.
- Manual record duplicate checks should consider iframe source differences (RCA-019) to avoid false overwrite prompts when only embedded app content changed.

## Checklist

- [x] Stop recording removes recording mode (and border) without immediate session wipe
- [x] `AnalyzePromptRequested` only leads to action when pages were captured
- [x] MessageBox shows captured page count
- [x] "Yes" transfers session pages to corpus and clears session
- [x] "No" keeps session pages for user review
