# RCA-024: Workspace Open URL Latency and Start Page + New Site Clipping

**Reported:** 2026-05-09
**Severity:** Medium-High
**Component:**
- `MainWindow.xaml.cs`
- `ViewModels/WorkspaceViewModel.cs`
- `ViewModels/BrowserViewModel.cs`
- `Views/Tabs/ScrapingTabView.xaml.cs`
- `Views/StartPage.xaml`

---

## Summary of Reported Issues

1. On opening a site/workspace, the webview appears delayed or does not visibly open until tab switching; URL text is not shown immediately.
2. The `+ New Site` button is visually cut off on Start Page.

---

## Symptom Details

### S1 - Workspace opens but URL feedback is delayed

When a site is opened, users may see:

- delayed webview first render, or
- navigation only becoming obvious after interacting with tabs, and
- URL text in header not appearing immediately.

The user requested visible loading feedback (spinner) before URL text and immediate URL display.

### S2 - `+ New Site` button clipped

On narrower window widths, the right-side action button in Start Page header (`+ New Site`) is truncated.

---

## Root Cause Analysis

### RC1 - Header URL depends on late-bound async site load

`MainWindow` swaps to `WorkspacePage` immediately, then calls `WorkspaceViewModel.LoadAsync(...)`.
`ActiveSite` is assigned inside `LoadAsync` after async retrieval:

- `var site = await Task.Run(() => _db.GetAllSites().FirstOrDefault(...));`
- `ActiveSite = site;`

Top header URL text is bound to `ActiveSite.StartUrl`, so it is blank/late until `ActiveSite` is assigned.

### RC2 - Browser loading feedback starts only after navigation starts

`BrowserViewModel.IsLoading` is toggled in `OnNavigationStarting(...)` and reset in `OnNavigationCompleted(...)`.
There is no workspace-level loading indicator shown during pre-navigation phases:

- workspace/site hydration
- WebView2 initialization (`EnsureCoreWebView2Async`)
- pending navigation handoff

Result: perceived freeze/blank state before navigation events are emitted.

### RC3 - WebView initialization is tied to Scraping tab view load

`ScrapingTabView` initializes browser host only when view is loaded (`TryInitializeBrowser`).
If tab content creation/activation is delayed by WPF tab behavior or UI timing, pending navigation may feel delayed.

### RC4 - Start Page layout uses fixed width with disabled horizontal scrolling

In `StartPage.xaml`:

- main content stack has fixed `Width="900"`
- root `ScrollViewer` has `HorizontalScrollBarVisibility="Disabled"`

At smaller window widths, content beyond viewport width is clipped with no horizontal recovery path, so right-side header controls (including `+ New Site`) are cut off.

---

## Proposed Fixes

### F1 - Show URL immediately at workspace open

On open request (before async load completes), set an immediate display URL in workspace VM:

- add `DisplayUrl` property in `WorkspaceViewModel`
- set from known input early:
  - `navigateUrl` if provided
  - otherwise site start URL as soon as site is resolved

Bind header URL text to `DisplayUrl` instead of directly to `ActiveSite.StartUrl`.

### F2 - Add workspace-level loading state + spinner before URL text

Add `IsOpeningSite` bool to `WorkspaceViewModel`:

- `true` at start of `LoadAsync`
- `false` after browser navigation request is queued (or first navigation complete event)

In `WorkspacePage.xaml` header row:

- add small `ProgressBar IsIndeterminate="True"` (or spinner glyph) before URL text
- show spinner when `IsOpeningSite == true`
- keep URL text visible immediately (from `DisplayUrl`)

This satisfies: spinner before URL text and immediate URL visibility.

### F3 - Improve first-load responsiveness signal from Browser VM

Add/init state in `BrowserViewModel` for startup/loading phases:

- optional `IsInitializingBrowser`
- status values like `Initializing browser...`, `Preparing navigation...`

Set this around `BrowserView.Initialize(...)`/`EnsureCoreWebView2Async(...)` to reduce blank-state ambiguity.

### F4 - Prevent Start Page clipping of `+ New Site`

Make Start Page responsive:

1. Replace fixed stack width (`Width="900"`) with responsive sizing:
   - `MaxWidth="900"` and no fixed width
2. Keep center alignment via outer columns, but allow shrink on narrow windows.
3. For safety, change horizontal scroll behavior to `Auto` on root `ScrollViewer` if any wide content remains.
4. Ensure header grid uses flexible space for left title and preserves right button visibility.

---

## Verification Checklist

- [ ] On site open, header URL is shown immediately (before navigation completes).
- [ ] Spinner/indeterminate indicator is visible while opening/loading and disappears after ready.
- [ ] Webview first-load no longer appears stalled without feedback.
- [ ] Opening directly into workspace no longer requires tab switch to perceive progress.
- [ ] On narrow window widths, `+ New Site` is fully visible and clickable.
- [ ] Start Page remains usable across resize down to minimum supported window width.

---

## Implementation Notes

- This RCA does not require architectural change; it is primarily UI state management and responsive layout hardening.
- If desired, add a lightweight telemetry/log marker for site-open phases:
  - `WorkspaceOpenStart`
  - `WebViewInitDone`
  - `FirstNavigationStart`
  - `FirstNavigationComplete`

This will separate true WebView slowness from missing UI feedback.
