# Step 4.6 — SPA-Aware Page Transition Detection

## Objective

Detect "virtual" page transitions in SPAs (React, Angular, Vue, Blazor) that don't trigger traditional navigation events.

## Dependencies

- Step 4.1 (DOM capture to trigger on transition)
- Phase 1 (WebView2 JS interop)

## Implementation

### MutationObserver injection

```javascript
const observer = new MutationObserver((mutations) => {
    const totalChanged = mutations.reduce(
        (sum, m) => sum + m.addedNodes.length + m.removedNodes.length, 0);
    if (totalChanged > threshold) {
        window.chrome.webview.postMessage({ type: 'pageTransition', url: location.href });
    }
});
observer.observe(document.body, { childList: true, subtree: true });
```

### Detection strategies

- **Threshold**: If >30% of visible elements changed → likely a "page transition".
- **URL change detection**: `hashchange` and `popstate` events + URL polling for `pushState` changes.
- **Wait for stable state**: After mutation detected, wait for:
  - No more mutations for 500ms
  - No pending XHR/fetch requests (intercept via `PerformanceObserver`)
  - No visible loading spinners (`[class*="loading"], [class*="spinner"]`)
- **Manual fallback**: "Capture This State" button for tricky SPAs where auto-detection fails.

### WebView2 ↔ WPF bridge

Use `WebMessageReceived` event to receive `pageTransition` messages from JS:

```csharp
webView.CoreWebView2.WebMessageReceived += (s, e) =>
{
    var msg = JsonSerializer.Deserialize<JsMessage>(e.WebMessageAsJson);
    if (msg?.Type == "pageTransition")
        OnPageTransitionDetected(msg.Url);
};
```

## Checklist

- [ ] MutationObserver injected after page load
- [ ] Large DOM changes (>30% threshold) trigger transition event
- [ ] `hashchange` and `popstate` events captured
- [ ] Stable-state detection waits for mutations to settle (500ms)
- [ ] Manual "Capture This State" button available
- [ ] WebView2 `WebMessageReceived` handler processes transition messages
