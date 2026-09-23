# RCA-007: New-Window Content Escapes Scraper (Cannot Inspect `target="_blank"` Pages)

**Reported:** 2026-04-22
**Severity:** High
**Component:** `Views/BrowserView.xaml.cs`

---

## Symptoms

Clicking a `target="_blank"` link or triggering `window.open()` opens a new browser window. This window **does** appear — it is not lost or invisible. However:

1. The new window is a **bare Chromium shell** — no address bar, no Back button, no Scraper toolbar.
2. The Scraper's **DOM inspector cannot reach it** — highlight overlay, Ctrl+click selection, and DOM capture only operate on the WebView2 instance inside `BrowserView`. The new window is a separate WebView2 instance that the Scraper has no reference to.
3. The user has to **manually copy the URL** from the new window, close it, paste it into the Scraper's address bar, and navigate. This breaks the workflow entirely.
4. If many links use `target="_blank"` (common on documentation sites, admin panels, dashboards), the user is constantly fighting this.

## Root Cause

WebView2 raises `CoreWebView2.NewWindowRequested` when a page requests a new window. If the event is **not handled**, WebView2 opens a new top-level window with its own `CoreWebView2` instance. The Scraper has no connection to this instance:

- `ElementHighlightService` only has `TrackFrames` wired to the original `CoreWebView2`
- `DomCaptureService` only captures from the original `CoreWebView2`
- `InspectorViewModel` only displays DOM trees from the original capture
- The address bar doesn't update — it still shows the previous page's URL
- Back/forward history is unaffected — the user can't navigate back from the new window

The Scraper is a **single-document tool** — all inspection, capture, and highlighting is designed around one WebView2 instance. Content that escapes into a separate window is effectively invisible to the tool.

**File:** `Views/BrowserView.xaml.cs` — no `NewWindowRequested` handler existed.

## Fix

Subscribe to `NewWindowRequested`, suppress the new window, and navigate the current WebView in-place:

```csharp
WebView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;

private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
{
    e.Handled = true;
    WebView.CoreWebView2.Navigate(e.Uri);
}
```

This keeps all navigation within the single Scraper WebView, which means:

- **DOM inspector works** — highlight overlay, Ctrl+click, and capture operate on the navigated page
- **Address bar updates** — `SourceChanged` fires normally, showing the new URL
- **Back button works** — the user can press Back to return to the originating page
- **Frame tracking works** — `ElementHighlightService.TrackFrames` covers any iframes on the new page
- **No orphan windows** — no bare Chromium shells accumulate

## Trade-offs

| Scenario | Behavior | Impact |
|---|---|---|
| `target="_blank"` links | Navigate in-place; Back returns to origin | Correct — this is the desired scraping UX |
| `window.open()` for OAuth popups | Opens in-place; opener loses `window.open()` return ref, so `postMessage` between opener and popup breaks | Acceptable — Scraper doesn't need to complete OAuth flows |
| `window.open()` with size/position params | Params ignored; content loads full-size | Correct — Scraper doesn't need popup geometry |
| Page that opens a link AND expects to stay on current page | Current page is replaced; user must press Back | Minor friction, but consistent with single-document model |
| Downloads via new-window redirect | `DownloadStarting` fires normally after in-place navigation | Works correctly |

For a scraping/inspection tool, navigating in-place is the correct design. Every page the user visits must be reachable by the Scraper's tooling.

## Status

- [x] `NewWindowRequested` handler added to `BrowserView`
- [x] `e.Handled = true` suppresses default new-window behavior
- [x] Target URL navigated in the current WebView
- [x] All 94 tests passing
