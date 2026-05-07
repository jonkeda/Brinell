# RCA-017: Session Pages Disappear When Recording Stops

**Reported:** 2026-05-04
**Severity:** High
**Component:** `ViewModels/MainViewModel.cs`, `ViewModels/SidebarViewModel.cs`

---

## Symptoms

During a recording session, 3 pages (including iframe transitions) are captured and appear in the "This Session" sidebar list. When the user clicks ⏹ Stop, the session pages immediately vanish from the sidebar. The "Corpus Pages" section remains empty — the captured pages are lost.

## Root Cause

The `RecordingStopped` event handler in `MainViewModel` calls `Sidebar.ClearSession()` unconditionally:

```csharp
Recording.RecordingStopped += () =>
{
    Sidebar.ClearSession();
    // ...
};
```

`SidebarViewModel.ClearSession()` removes all items from `SessionPages` and sets `IsRecording = false`, which hides the "This Session" section entirely.

The captured snapshots exist in `Recording.SessionSnapshots` (an `ObservableCollection<DomSnapshot>`), but `ClearSession()` wipes the sidebar list **before** the user has a chance to see the "Analyze" prompt result, and regardless of the answer, the pages are never transferred to the "Corpus Pages" list.

The flow is:

1. `StopRecording()` fires `RecordingStopped` → `Sidebar.ClearSession()` wipes the list
2. `StopRecording()` fires `AnalyzePromptRequested` → user sees the prompt, but pages are already gone from the UI
3. Even if the user clicks "Yes", `OnAnalyzePromptRequested` only logs — it never moves snapshots to the corpus

## Fix

1. **Move session pages to corpus on stop** — Before clearing the session, transfer all `SessionSnapshots` to the corpus sidebar:
   ```csharp
   Recording.RecordingStopped += () =>
   {
       // Move session pages to corpus before clearing
       foreach (var snapshot in Recording.SessionSnapshots)
       {
           Sidebar.CorpusPages.Add(new SidebarPageItem
           {
               Name = snapshot.PageName,
               Url = snapshot.PageUrl,
               StatusIcon = "📄"
           });
       }
       Sidebar.ClearSession();
       // Update stats
   };
   ```
2. **Show analyze prompt before clearing** — Alternatively, defer `ClearSession()` until after the analyze prompt is answered, so the user can still see what was captured.

## Verification

- [X] Record 3 pages. Click ⏹ Stop. The 3 pages now appear under "Corpus Pages" with 📄 icons.
- [X] The "This Session" section disappears (recording UI is cleaned up).
- [X] The corpus stats update to reflect the new page count.
- [ ] Start a new recording. The "This Session" list is empty — no stale pages.
