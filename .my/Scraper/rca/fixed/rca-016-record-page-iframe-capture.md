# RCA-016: Record Page Button Does Not Capture IFrame Content

**Reported:** 2026-05-04
**Severity:** Medium
**Component:** `ViewModels/MainViewModel.cs` — `RecordPageAsync`

---

## Symptoms

Clicking the 📷 Record Page button captures the top-level page but does not include the DOM content of iframes. The captured snapshot's element tree shows the `<iframe>` element as a leaf node with no children, even when the iframe contains a full document that is visible on screen.

## Root Cause

`RecordPageAsync` calls `_domCapture.CaptureAsync(webView)` which executes the capture script in the top-level document context. The `DomCaptureService.CaptureScript` does traverse same-origin iframes via `el.contentDocument`, so same-origin iframe content **is** included.

However, the WebView2 page in question uses **cross-origin iframes** (e.g. ExactOnline embedded in a different-origin host). The capture script's `try/catch` around `el.contentDocument` silently fails for cross-origin frames — the browser blocks access to `contentDocument` due to the same-origin policy.

To capture cross-origin iframe content, the capture script must be injected and executed **inside each iframe's own execution context** via `CoreWebView2Frame.ExecuteScriptAsync`, then the results stitched together into the parent snapshot.

## Fix

1. After capturing the top-level DOM, enumerate `_trackedFrames` from `ElementHighlightService` (or maintain a parallel frame list in `DomCaptureService`).
2. For each tracked `CoreWebView2Frame`, execute the capture script inside that frame via `frame.ExecuteScriptAsync`.
3. Merge the iframe capture results into the parent snapshot by replacing the empty `<iframe>` leaf node with the iframe's captured element tree.
4. Mark merged iframe subtrees with an `InIframe = true` flag so downstream processing knows the origin context.

## Verification

- [X] Navigate to a page with a cross-origin iframe. Click 📷 Record Page. The captured snapshot includes the iframe's DOM content.
- [X] Navigate to a page with a same-origin iframe. Click 📷. Both top-level and iframe content are captured (no regression).
- [ ] The DOM tree view shows iframe children nested under the `<iframe>` element node.
