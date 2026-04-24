# RCA-011: Inspect Mode — Persist Across Navigation + Toggle Button UX

**Reported:** 2026-04-22
**Severity:** High
**Component:** `ViewModels/MainViewModel.cs`, `ViewModels/BrowserViewModel.cs`, `Services/ElementHighlightService.cs`, `MainWindow.xaml`

---

## Problem Statement

Two related UX issues with inspect mode:

1. **Inspect doesn't survive navigation.** When the user enables inspect mode and then navigates (click a link, enter a URL, Back/Forward), the new page loads clean — no overlay, no Ctrl+click, stale DOM tree. The user expects inspect to stay on until they explicitly turn it off.

2. **Inspect button doesn't show state.** The toolbar uses a regular `Button` for inspect. There's no visual indicator that inspect is active. The user has to infer state from the DOM tree panel visibility, which is indirect and easy to miss.

---

## Architecture: How Inspect Mode Works

```
User clicks 🔍
       │
       ▼
MainViewModel.ToggleInspectAsync()
       │
       ├─ DomCaptureService.CaptureAsync(webView)
       │      └─ ExecuteScriptAsync(captureScript) → JSON → DomSnapshot
       │
       ├─ InspectorViewModel.LoadSnapshot(snapshot)
       │      ├─ DomTree.LoadSnapshot() → TreeView updates
       │      └─ TotalElementCount updated
       │
       ├─ ElementHighlightService.EnableAsync(webView)
       │      ├─ ExecuteScriptAsync(OverlayScript)    ← main frame
       │      └─ frame.ExecuteScriptAsync(IFrameOverlayScript)  ← each tracked iframe
       │
       └─ Inspector.IsInspecting = true
              └─ Binds to: DomTreePanel visibility, status bar, toggle button
```

**The fragile point:** All injected JavaScript lives in the page's DOM. WebView2 navigation destroys the document, which destroys the overlay, the event listeners, and the `window.__brinellOverlay` guard. The C# side (`_isActive = true`, `IsInspecting = true`) doesn't know the scripts are gone.

---

## Root Cause Analysis

### Issue 1 — No Re-Injection After Navigation

`BrowserView.OnNavigationCompleted` only updates loading/status state:

```csharp
private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
{
    _vm?.OnNavigationCompleted(e.IsSuccess, e.IsSuccess ? null : e.WebErrorStatus.ToString());
}
```

No code checks `Inspector.IsInspecting` after navigation. No code re-captures the DOM or re-injects the overlay.

**Chain of failure:**
1. User enables inspect → scripts injected, `_isActive = true`, `IsInspecting = true`
2. User clicks a link → WebView2 navigates → document destroyed → scripts gone
3. `NavigationCompleted` fires → status updated, nothing else
4. New page has no overlay, no event listeners
5. `_isActive` is still `true` → `EnableAsync()` would early-return if called
6. DOM tree still shows previous page's snapshot

### Issue 2 — `EnableAsync` Guard Blocks Re-Injection

```csharp
public async Task EnableAsync(CoreWebView2 webView)
{
    if (_isActive) return;  // ← blocks re-injection after navigation
    ...
}
```

After navigation, `_isActive` is `true` (never reset), so even if someone called `EnableAsync`, it would silently return.

### Issue 3 — Stale Tracked Frames

`ElementHighlightService._trackedFrames` holds references to frames from the previous page. After navigation, these frames are destroyed but the list isn't cleared. New iframes on the new page get added via `FrameCreated`, but the stale entries remain and cause silent exceptions during injection.

### Issue 4 — Button Doesn't Show State

```xml
<Button Command="{Binding InspectCommand}" Content="🔍" ToolTip="Inspect" Padding="6,2"/>
```

A regular `Button` has no checked/unchecked visual state. WPF `ToggleButton` does — it renders with a pressed/highlighted background when `IsChecked` is true.

---

## Implementation

### Change 1 — Toggle Button

Replace `Button` with `ToggleButton` in `MainWindow.xaml`. Bind `IsChecked` to `Inspector.IsInspecting` (one-way, since the command controls state):

```xml
<ToggleButton Command="{Binding InspectCommand}" Content="🔍" ToolTip="Inspect" Padding="6,2"
              IsChecked="{Binding Inspector.IsInspecting, Mode=OneWay}"/>
```

`Mode=OneWay` is important: clicking the button fires `InspectCommand` (which toggles `IsInspecting`), and the binding reflects the state. We don't want the `ToggleButton` to also try to set `IsInspecting` directly — that would bypass the command's async logic (DOM capture, overlay injection).

### Change 2 — `NavigationSucceeded` Event

Add to `BrowserViewModel`:

```csharp
public event Action? NavigationSucceeded;

public void OnNavigationCompleted(bool isSuccess, string? errorStatus)
{
    if (isSuccess)
    {
        _logger.LogInformation("Navigation completed: {Url}", AddressUrl);
        NavigationSucceeded?.Invoke();
    }
    else
    {
        _logger.LogWarning("Navigation failed: {Url}, Error: {ErrorStatus}", AddressUrl, errorStatus);
    }
    IsLoading = false;
    StatusText = isSuccess ? AddressUrl : $"Navigation failed: {errorStatus}";
}
```

### Change 3 — Re-Inspect on Navigation

In `MainViewModel`, subscribe and re-inspect:

```csharp
// Constructor:
Browser.NavigationSucceeded += OnNavigationSucceeded;

private async void OnNavigationSucceeded()
{
    if (!Inspector.IsInspecting) return;

    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    var snapshot = await _domCapture.CaptureAsync(webView);
    Inspector.LoadSnapshot(snapshot);
    await _highlight.EnableAsync(webView, force: true);
    _logger.LogInformation("Inspect mode refreshed after navigation — {ElementCount} elements",
        Inspector.TotalElementCount);
}
```

The `force: true` parameter bypasses the `_isActive` guard and clears stale frames.

### Change 4 — `EnableAsync` Force Mode

Add `force` parameter to `ElementHighlightService.EnableAsync`:

```csharp
public async Task EnableAsync(CoreWebView2 webView, bool force = false)
{
    if (_isActive && !force) return;

    if (force)
        _trackedFrames.Clear();  // Stale frames from previous page

    _isActive = true;
    await webView.ExecuteScriptAsync(OverlayScript);

    foreach (var frame in _trackedFrames.ToArray())
    {
        try { await frame.ExecuteScriptAsync(IFrameOverlayScript); }
        catch { /* frame may have been destroyed */ }
    }

    _logger.LogDebug("Element highlight overlay enabled (top + {FrameCount} frames)", _trackedFrames.Count);
}
```

When `force = true`:
- `_trackedFrames` is cleared (old page's frames are dead)
- `_isActive` guard is bypassed (scripts need re-injection)
- New iframes will be tracked automatically via `FrameCreated` (already wired in `TrackFrames`)

---

## Lifecycle: Inspect Mode After Fix

```
                     ┌──────────────────────────────────────────────┐
                     │          INSPECT MODE LIFECYCLE              │
                     └──────────────────────────────────────────────┘

  User clicks 🔍 toggle button
       │
       ▼
  ToggleInspectAsync() ──── IsInspecting? ──── YES ──→ DisableAsync()
       │                                                  │
       NO                                          IsInspecting = false
       │                                          Toggle button unchecks
       ▼
  CaptureAsync() → LoadSnapshot() → EnableAsync() → IsInspecting = true
                                                     Toggle button checks
       │
       ▼
  ┌─── User browses ───────────────────────────────────────────────┐
  │                                                                 │
  │  Click link / enter URL / Back / Forward                        │
  │       │                                                         │
  │       ▼                                                         │
  │  NavigationCompleted (success)                                  │
  │       │                                                         │
  │       ▼                                                         │
  │  OnNavigationSucceeded()                                        │
  │       │                                                         │
  │       ├─ IsInspecting? NO → return (inspect was toggled off)    │
  │       │                                                         │
  │       ├─ CaptureAsync(webView) → new snapshot                   │
  │       ├─ LoadSnapshot() → tree updates to new page              │
  │       └─ EnableAsync(force: true)                               │
  │              ├─ Clear stale _trackedFrames                      │
  │              ├─ Inject OverlayScript into new page              │
  │              └─ New iframes auto-tracked via FrameCreated       │
  │                                                                 │
  │  User clicks 🔍 again → DisableAsync() → inspect off           │
  └─────────────────────────────────────────────────────────────────┘
```

---

## Edge Cases & Trade-offs

| Scenario | Behavior | Notes |
|---|---|---|
| Navigation fails (404, timeout) | `NavigationSucceeded` not fired, inspect stays on but shows previous snapshot | Acceptable — user sees the error page and can manually re-inspect |
| Rapid navigation (click 3 links fast) | Multiple `OnNavigationSucceeded` calls overlap; last one wins | DOM snapshot and overlay from the final page are what matter. Earlier calls harmlessly inject into pages that immediately navigate away |
| SPA client-side routing | `NavigationCompleted` doesn't fire for hash-only or pushState navigation | Known limitation — SPA routing doesn't trigger WebView2 navigation events. User can manually re-capture. Could be addressed later via `HistoryChanged` or a MutationObserver |
| Page with slow-loading dynamic content | DOM captured at `NavigationCompleted` may miss lazy-loaded elements | Acceptable — same behavior as initial inspect. User can re-capture |
| `target="_blank"` link while inspecting | RCA-007 navigates in-place → `NavigationCompleted` fires → re-inspect works | Correct behavior — inspect persists seamlessly |
| Toggle inspect off during navigation | `IsInspecting` set to `false` before `NavigationSucceeded` fires → guard returns early | Correct — no re-injection |

---

## Files Changed

| File | Change |
|---|---|
| `MainWindow.xaml` | `Button` → `ToggleButton` with `IsChecked="{Binding Inspector.IsInspecting, Mode=OneWay}"` |
| `ViewModels/BrowserViewModel.cs` | Added `NavigationSucceeded` event, fired on successful navigation |
| `ViewModels/MainViewModel.cs` | Added `OnNavigationSucceeded()` — re-captures DOM + re-injects overlay when inspect is active |
| `Services/ElementHighlightService.cs` | `EnableAsync(webView, force)` — `force=true` bypasses `_isActive` guard and clears stale `_trackedFrames` |

## Status

- [x] Inspect button changed to `ToggleButton` with visual state
- [x] `NavigationSucceeded` event added to `BrowserViewModel`
- [x] `MainViewModel` re-inspects on navigation when inspect is active
- [x] `ElementHighlightService.EnableAsync` supports `force` parameter
- [x] Stale tracked frames cleared on forced re-enable
- [x] All 94 tests passing
