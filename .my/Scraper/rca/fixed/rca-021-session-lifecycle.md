# RCA-021: Session List Not Cleared After Analysis / Incorrect Session Lifecycle

**Reported:** 2026-05-04
**Severity:** High
**Component:** `ViewModels/MainViewModel.cs`, `ViewModels/SidebarViewModel.cs`, `MainWindow.xaml`

---

## Symptoms

Multiple related session lifecycle issues:

1. **Session list not cleared after analysis** — After stopping recording and clicking "Yes" to analyze, the "This Session" list remains visible with stale pages.
2. **Session cleared too early** — The `RecordingStopped` handler currently transfers pages to corpus and clears the session immediately, before the user can review or analyze.
3. **Session section not visible when pages exist** — The "This Session" sidebar section is only visible when `IsRecording` is true. After recording stops, session pages should remain visible until explicitly analyzed/cleared.
4. **No analyze button outside recording** — When recording is stopped but session pages still exist, there's no way to trigger analysis. An "Analyze Session" button should appear.

## Root Cause

### Session cleared on stop instead of on analyze

The `RecordingStopped` handler transfers pages to corpus and calls `ClearSession()` immediately:

```csharp
Recording.RecordingStopped += () =>
{
    foreach (var snapshot in Recording.SessionSnapshots)
        Sidebar.CorpusPages.Add(...);
    Sidebar.ClearSession();  // ← clears too early
};
```

The correct lifecycle should be:

1. **Stop recording** → recording state ends (red border gone, ⏺ button returns), but session pages remain visible
2. **User reviews** → session pages stay in sidebar, analyze button appears
3. **Analyze** → pages transfer to corpus, session list clears

### Session visibility tied to recording state

In `MainWindow.xaml`, the "This Session" section is bound to `Sidebar.IsRecording`:

```xml
<StackPanel Visibility="{Binding Sidebar.IsRecording, Converter={StaticResource BoolToVisibility}}">
```

This hides the section as soon as recording stops, even if session pages exist.

### No standalone analyze action

The analyze prompt only fires as part of `StopRecording()`. There's no command to analyze the session independently.

## Fix

### 1. Add `HasSessionPages` property to SidebarViewModel

```csharp
public bool HasSessionPages => SessionPages.Count > 0;
```

Update it when pages are added/removed.

### 2. Change session section visibility

Bind to `HasSessionPages` instead of `IsRecording`:

```xml
<StackPanel Visibility="{Binding Sidebar.HasSessionPages, Converter={StaticResource BoolToVisibility}}">
```

### 3. Don't transfer/clear on stop

`RecordingStopped` should only set `IsRecording = false`. Pages stay in session list.

### 4. Add AnalyzeSessionCommand

Add a command that:

- Transfers session pages to corpus
- Updates corpus stats
- Clears the session list
- Triggers analysis (Phase 5 TODO)

### 5. Add analyze button to sidebar

Show an "Analyze Session ▶" button below the session list when `HasSessionPages && !IsRecording`.

## Verification

- [X] Record 3 pages. Click ⏹ Stop. The "This Session" list remains visible with all 3 pages. The recording UI (red border, stop/pause buttons) disappears.
- [X] An "Analyze Session" button appears below the session list.
- [X] Click "Analyze Session". Pages transfer to "Corpus Pages", session list clears, corpus stats update.
- [X] Start a new recording. "This Session" is empty — no stale pages.
- [X] Record pages, stop, then start recording again without analyzing. The previous session pages are still visible plus new pages are added.
- [X] Session section is hidden only when there are zero session pages.
