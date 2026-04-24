# RCA-006: IFrame Inspection — Highlight and Inspector Don't Work Inside IFrames

**Reported:** 2026-04-22
**Severity:** High
**Component:** `Services/ElementHighlightService.cs`, `Services/DomCaptureService.cs`

---

## Symptoms

When the page contains `<iframe>` elements and inspect mode is active:
1. Hovering over elements **inside** the iframe does not show the blue highlight overlay or tooltip.
2. Ctrl+clicking inside the iframe does not select individual elements.
3. The entire `<iframe>` element is highlighted/selected as a single block instead.

## Root Cause

### Highlight Overlay Only Injected into Top-Level Document

The `ElementHighlightService.OverlayScript` is injected via `webView.ExecuteScriptAsync()`, which runs in the **top-level document context only**. The `mousemove` and `click` event listeners are attached to `document` of the parent page.

When the user hovers over an iframe, `document.elementFromPoint()` returns the `<iframe>` element itself — it cannot "see through" into the iframe's inner document. The iframe is a separate browsing context with its own `document`, so:

- `mousemove` events inside the iframe **do not bubble** to the parent document
- `document.elementFromPoint()` in the parent returns the `<iframe>` element, not the inner element
- The overlay highlights the entire iframe as a single block

**File:** `Services/ElementHighlightService.cs`, lines 115–130
```javascript
document.addEventListener('mousemove', function(e) {
    const el = document.elementFromPoint(e.clientX, e.clientY);
    // el is the <iframe> element, never the inner content
    ...
}, true);
```

### DOM Capture Already Traverses Same-Origin IFrames (RCA-004 Fix)

The `DomCaptureService.CaptureScript` was updated to traverse into same-origin iframe `contentDocument`. So the DOM tree shows iframe children correctly, but the highlight overlay and click handlers are not injected into those iframes.

## Fix

### Inject Overlay Script into All Frames

Use WebView2's `CoreWebView2Frame` API to inject the overlay script into each iframe:

```csharp
public async Task EnableAsync(CoreWebView2 webView)
{
    if (_isActive) return;
    _isActive = true;
    
    // Inject into top-level document
    await webView.ExecuteScriptAsync(OverlayScript);
    
    // Inject into all existing frames
    foreach (var frame in _trackedFrames)
    {
        try { await frame.ExecuteScriptAsync(OverlayScript); }
        catch { /* frame may have been destroyed */ }
    }
    
    _logger.LogDebug("Element highlight overlay enabled");
}
```

Track frames via `CoreWebView2.FrameCreated`:
```csharp
public void TrackFrames(CoreWebView2 webView)
{
    webView.FrameCreated += (_, args) =>
    {
        _trackedFrames.Add(args.Frame);
        args.Frame.Destroyed += (_, _) => _trackedFrames.Remove(args.Frame);
        
        if (_isActive)
        {
            _ = args.Frame.ExecuteScriptAsync(OverlayScript);
        }
    };
}
```

### Coordinate Mapping for Nested Messages

When `window.chrome.webview.postMessage` fires from inside an iframe, the bounding box coordinates are relative to the iframe's viewport, not the parent page. The parent needs to offset these by the iframe's position. Add the iframe's offset to the message:

```javascript
// Inside the iframe's overlay script, include the iframe's offset
const frameRect = window.frameElement?.getBoundingClientRect();
// Add frameRect.x/y to reported positions
```

### Alternative: CSS pointer-events Approach

A simpler (but less precise) approach: when inspect mode is active, set `pointer-events: none` on all iframes so clicks/hovers pass through to the parent. But this prevents interaction with iframe content entirely, so the frame injection approach is better.

## Status

- [ ] Frame tracking added to ElementHighlightService
- [ ] Overlay script injected into all tracked frames
- [ ] Coordinate offset mapping for iframe-sourced messages
- [ ] Disable script injected into all frames on toggle-off
- [ ] Tested with same-origin and cross-origin iframes
