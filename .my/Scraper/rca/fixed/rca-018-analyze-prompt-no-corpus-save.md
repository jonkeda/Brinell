# RCA-018: Analyze Prompt Does Not Add Pages to Corpus

**Reported:** 2026-05-04
**Severity:** High
**Component:** `ViewModels/MainViewModel.cs` — `OnAnalyzePromptRequested`

---

## Symptoms

After stopping a recording session with captured pages, the prompt "{N} pages captured. Analyze corpus now?" appears. Clicking "Yes" does nothing visible — no pages appear in the Corpus Pages sidebar, no analysis runs, and no feedback is given to the user.

## Root Cause

`OnAnalyzePromptRequested` is a stub that only logs when the user clicks Yes:

```csharp
private void OnAnalyzePromptRequested()
{
    if (Recording.SessionSnapshots.Count == 0)
        return;

    var result = System.Windows.MessageBox.Show(
        $"{Recording.SessionSnapshots.Count} pages captured. Analyze corpus now?",
        "Recording Complete",
        System.Windows.MessageBoxButton.YesNo,
        System.Windows.MessageBoxImage.Question);

    if (result == System.Windows.MessageBoxResult.Yes)
        _logger.LogInformation("User chose to analyze corpus");
}
```

There is no code to:
1. Persist the `SessionSnapshots` to the corpus database
2. Add the captured pages to the `Sidebar.CorpusPages` list
3. Trigger the LLM analysis pipeline

Additionally, the event ordering compounds the problem (see RCA-017): `RecordingStopped` fires **before** `AnalyzePromptRequested`, and `RecordingStopped` calls `Sidebar.ClearSession()` which wipes the UI list. By the time the analyze prompt appears, the session pages are already gone from the sidebar.

## Fix

1. **Persist snapshots to corpus** — When recording stops (regardless of analyze prompt answer), save all `SessionSnapshots` to the corpus via `CorpusDatabase` and add them to `Sidebar.CorpusPages`.
2. **Wire up analysis** — When the user clicks Yes, trigger the Copilot SDK analysis pipeline (Phase 5 functionality). If Phase 5 is not yet implemented, at minimum persist the pages and show a status message.
3. **Fix event ordering** — Ensure `AnalyzePromptRequested` fires and is answered **before** `ClearSession()` wipes the session state. Or decouple persistence from the prompt entirely (always persist on stop, prompt only controls whether analysis runs).

## Verification

- [ ] Record 3 pages. Click ⏹ Stop. Click "Yes" on the analyze prompt. All 3 pages appear under "Corpus Pages".
- [ ] Record 2 pages. Click ⏹ Stop. Click "No" on the analyze prompt. All 2 pages still appear under "Corpus Pages" (persistence is independent of analysis).
- [ ] The corpus stats update to reflect the new page count after recording stops.
