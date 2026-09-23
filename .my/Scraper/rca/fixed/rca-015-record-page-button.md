# RCA-015: Add Manual "Record Page" Button

**Reported:** 2026-05-04
**Severity:** Low (Enhancement)
**Component:** `MainWindow.xaml`, `ViewModels/MainWindowViewModel.cs`, `Services/RecordingService.cs`

---

## Problem

There is no way to manually capture a single page snapshot on demand. The current recording workflow requires starting a full recording session (⏺ Record), navigating to pages to trigger automatic captures, and then stopping the session (⏹ Stop). This is cumbersome when the user wants to:

- Capture a single specific page without starting a session
- Re-capture a page that didn't capture correctly during a session
- Capture a page that loaded via a mechanism the transition detector doesn't catch (e.g. JavaScript-driven content swap, iframe navigation, or delayed AJAX rendering)

## Proposed Solution

Add a 📷 "Record Page" button to the toolbar that captures the current page's DOM snapshot immediately and adds it to the corpus.

### Behavior

1. **Outside a recording session:** The button captures the current page and adds it directly to the corpus. A brief toast or status bar message confirms: "Page captured: {name}".
2. **During a recording session:** The button captures the current page and adds it to both the "This Session" list and the corpus. This allows the user to force a capture at any point during recording.
3. **Duplicate handling:** If the current URL already exists in the corpus, prompt the user: "This page is already recorded. Overwrite?" with Yes/No. During a recording session, skip the prompt and always capture (the deduplication window still applies).

### UI Placement

- Add a 📷 button to the toolbar, positioned after the ⏺ Record button.
- The button is enabled whenever a site is open and the browser has a loaded page (not blank or error).
- Tooltip: "Record this page (capture DOM snapshot)"

### Implementation

1. Add `RecordPageCommand` to `MainWindowViewModel` that calls `RecordingService.CaptureCurrentPageAsync()`.
2. `CaptureCurrentPageAsync` triggers `DomCaptureService.CaptureAsync()`, stores the result via `CorpusStore.SavePageAsync()`, and updates the sidebar.
3. Wire the button in `MainWindow.xaml` with the 📷 icon and command binding.

## Verification

- [X] With a site open (no recording session), click 📷 Record Page. The current page is captured and appears in the corpus. The status bar confirms the capture.
- [X] Navigate to the same URL and click 📷 again. A prompt asks whether to overwrite. Click Yes — the page is re-captured. Click No — no capture occurs.
- [X] Start a recording session. Click 📷 Record Page. The page appears in both "This Session" and the corpus.
- [X] Navigate to a page that doesn't auto-capture (e.g. content loaded via AJAX after initial load). Click 📷 and confirm the fully-rendered DOM is captured.
- [X] The button is disabled on the start screen (no site open) and on navigation error pages.
