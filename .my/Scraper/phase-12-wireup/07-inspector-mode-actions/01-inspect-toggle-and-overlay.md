# Step 12.W.7a — Wire Inspect Toggle & Element Overlay

## Objective

Wire the 🔍 Inspect toolbar button to inject/remove the highlight overlay JavaScript into WebView2, so hovering elements in the browser shows a blue highlight with locator tooltip.

## Dependencies

- `BrowserViewModel.IsInspectMode` (toggle property, exists or add)
- `DomCaptureService` (already implemented)
- `CoreWebView2.ExecuteScriptAsync` for JS injection
- Phase-04 Step 4.2 overlay JS (already written)

## Implementation

### Files

| File | Action |
|------|--------|
| `BrowserViewModel.cs` | Add `IsInspectMode` property, `ToggleInspectCommand` |
| `MainViewModel.cs` or `ScrapingTabViewModel.cs` | Subscribe `IsInspectMode` change → inject/remove overlay JS |
| `Resources/inspect-overlay.js` | Embedded resource — overlay script from phase-04 step 4.2 |

### Code sketch

**BrowserViewModel.cs:**

```csharp
[ObservableProperty]
private bool _isInspectMode;

[RelayCommand]
private void ToggleInspect()
{
    IsInspectMode = !IsInspectMode;
}

partial void OnIsInspectModeChanged(bool value)
{
    InspectModeChanged?.Invoke(value);
}

public event Action<bool>? InspectModeChanged;
```

**ScrapingTabViewModel.cs** (or MainViewModel):

```csharp
Browser.InspectModeChanged += async (enabled) =>
{
    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    // Delegate to ElementHighlightService — it injects the overlay into the
    // main frame AND all tracked iframes (via IFrameOverlayScript).
    if (enabled)
        await _highlight.EnableAsync(webView);
    else
        await _highlight.DisableAsync(webView);
};
```

> ⚠️ **Do NOT call `webView.ExecuteScriptAsync(...)` directly here.**
> `ElementHighlightService.EnableAsync/DisableAsync` already handles both the top-level
> frame and every tracked `CoreWebView2Frame` (populated via `TrackFrames()` wired up in
> `BrowserView.xaml.cs`). Bypassing the service leaves iframe content without an overlay.

### Overlay behavior

- Blue border + light blue background on hovered element
- Tooltip below element: `tag#id .class — Suggested: Locator.ByXxx("...")`
- Green border for selected elements (from multi-select, step 07c)
- Overlay div is MutationObserver-safe (does not trigger app MutationObservers)

## IFrame coverage (existing — must be preserved)

The existing `ElementHighlightService` already handles iframes:
- `TrackFrames(webView)` subscribes to `CoreWebView2.FrameCreated`; each new frame is added to `_trackedFrames` and removed on `frame.Destroyed`.
- `frame.DOMContentLoaded` auto-injects `IFrameOverlayScript` whenever an iframe re-navigates (so the overlay survives SPA navigation inside iframes).
- `EnableAsync` / `DisableAsync` inject/remove the overlay in every tracked frame.

No new code is needed for iframe overlay support — wiring through `ElementHighlightService` is sufficient.

## Checklist

- [ ] 🔍 Inspect button toggles `IsInspectMode`
- [ ] `ElementHighlightService.EnableAsync/DisableAsync` called (not raw `ExecuteScriptAsync`)
- [ ] Overlay injected into main frame AND all tracked iframes
- [ ] Hovering elements in main frame and inside iframes shows blue highlight
- [ ] Tooltip shows tag, id, aria-label, suggested locator
- [ ] Overlay does not interfere with page functionality
