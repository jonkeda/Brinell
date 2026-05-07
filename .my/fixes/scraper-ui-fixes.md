# Scraper UI Fixes

## FIX-001: Corpus screen has unnecessary margins

**Status:** Open

The margins around the corpus screen are not needed. The content should fill the full screen.

**Root cause:** In `MainWindow.xaml` (~L150), the content area Grid has a third `ColumnDefinition` hardcoded to `Width="300"` for the inspector panel. This reserves 300px even when the inspector `DockPanel` is collapsed via `BoolToVisibilityConverter`.

**Fix:** Change `Width="300"` to `Width="Auto"` on that column definition. The `DockPanel` inside already collapses to zero when `IsInspecting` is false. Optionally set `MaxWidth="300"` on the `DockPanel` itself to cap its open width.

**Files:** `MainWindow.xaml` L150

---

## FIX-002: Right sidebar empty when Inspect is closed

**Status:** Open

The right bar sidebar is empty when the Inspect panel is closed. It should either collapse or show relevant content.

**Root cause:** Same as FIX-001. The inspector `DockPanel` and `GridSplitter` both bind `Visibility` to `Inspector.IsInspecting` and correctly collapse, but the parent `ColumnDefinition Width="300"` keeps the space reserved. This leaves a 300px empty strip.

**Fix:** Same fix as FIX-001 — change the column width to `Auto`. Both issues share the same root cause.

**Files:** `MainWindow.xaml` L150

---

## FIX-003: Iframe page navigations are not recorded

**Status:** Open

Page changes that occur inside an iframe are not being recorded by the session recorder.

**Root cause:** In `BrowserView.xaml.cs` (~L85), `OnNavigationCompleted` fires `OnNavigationCompleted(e.IsSuccess, ...)` without checking whether the navigation was a top-level frame navigation or an iframe sub-navigation. There is no URL comparison against the last known top-level URL. The 2-second dedup in `RecordingViewModel.OnPageTransition` catches rapid same-URL repeats but not iframe navigations that happen later.

Additionally, `PageTransitionDetector` (JS-based MutationObserver/hashchange/popstate) exists but is never wired up — `RecordingStarted` never calls `PageTransitionDetector.StartAsync()`.

**Fix:** In `BrowserView.OnNavigationCompleted`, compare `WebView.CoreWebView2.Source` against the last known URL. Only call `_vm?.OnNavigationCompleted(...)` when the top-level URL actually changed. Store the previous URL in a field and update it on each real navigation. Alternatively, correlate `NavigationId` from `NavigationStarting` → `NavigationCompleted` to confirm top-level navigations.

**Files:** `BrowserView.xaml.cs` ~L47, ~L85

---

## FIX-004: Stop button doesn't show Resume button

**Status:** Open

Clicking the stop button doesn't show the Resume button. The stop button remains visible instead of toggling to Resume.

**Root cause:** In `MainWindow.xaml` (~L49), the Pause/Resume `StackPanel` has its `Visibility` bound to `Recording.IsRecording`. When `StopRecording()` sets `IsRecording = false`, this simultaneously hides the Stop button (correct) and collapses the entire Pause/Resume StackPanel — making the Resume button unreachable. The transition is always Stop → Record, never Stop → Resume.

**Fix:** Move the Resume button **outside** the `IsRecording`-gated `StackPanel`. Give it an independent visibility binding, e.g. visible when `IsRecording == false && SessionSnapshots.Count > 0`. Alternatively, add an `IsStopped` state property separate from `IsRecording` and bind the Resume button to that.

**Files:** `MainWindow.xaml` ~L39-53, `RecordingViewModel.cs` ~L82-89

---

## FIX-005: Prompt to clear session list on recording restart

**Status:** Open

If a recording is restarted, the user should be asked whether to clear the existing session list before continuing.

**Root cause:** In `MainViewModel.cs` (~L48-53), `RecordingStopped` immediately calls `Sidebar.ClearSession()` which clears `SessionPages` before the analyze prompt even appears. When a new recording starts, `StartRecording()` clears `SessionSnapshots` unconditionally with no user prompt.

**Fix:** Move `Sidebar.ClearSession()` from the `RecordingStopped` handler to the `RecordingStarted` handler, so session pages remain visible after stopping (for review). In `RecordingStarted`, check if `SessionPages.Count > 0` and show a confirmation dialog ("Clear previous session pages?") before clearing. If the user declines, keep the existing pages and append to them.

**Files:** `MainViewModel.cs` ~L47-53, `RecordingViewModel.cs` ~L72-80, `SidebarViewModel.cs` ~L56-60
