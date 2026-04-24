# RCA-004: IFrames Not Captured in DOM Snapshot

**Reported:** 2026-04-22
**Severity:** High
**Component:** `Services/DomCaptureService.cs`

---

## Symptoms

When navigating to a website that uses `<iframe>` elements, the content inside the iframes is not included in the DOM snapshot. The iframe elements themselves appear in the tree but their inner document contents are missing.

## Root Cause

The `CaptureScript` JavaScript only traverses `el.children` of the main document's DOM. When it encounters an `<iframe>` element, it captures the `<iframe>` tag and its attributes but does **not** cross into the iframe's `contentDocument`.

**File:** `Services/DomCaptureService.cs`, lines 82–100
```javascript
function captureElement(el) {
    const rect = el.getBoundingClientRect();
    return {
        tag: el.tagName.toLowerCase(),
        // ... attributes ...
        children: Array.from(el.children).map(captureElement)  // ← only traverses same-document children
    };
}
return JSON.stringify(captureElement(document.documentElement));
```

`el.children` on an `<iframe>` element returns an empty HTMLCollection — it does not cross the document boundary. The iframe's inner content lives in `el.contentDocument.documentElement`, which is a separate DOM tree.

Additionally, **cross-origin iframes** will throw a `SecurityError` when accessing `contentDocument` due to the Same-Origin Policy. Only same-origin iframes can be traversed from the parent document's JavaScript context.

## Fix

### Same-Origin IFrames

Add iframe traversal to the capture script. When the element is an `<iframe>`, attempt to access `contentDocument` and recurse into it:

```javascript
function captureElement(el) {
    const rect = el.getBoundingClientRect();
    let children = Array.from(el.children).map(captureElement);

    // Traverse into same-origin iframes
    if (el.tagName === 'IFRAME') {
        try {
            const iframeDoc = el.contentDocument;
            if (iframeDoc && iframeDoc.documentElement) {
                children = [captureElement(iframeDoc.documentElement)];
            }
        } catch (e) {
            // Cross-origin iframe — cannot access contentDocument
        }
    }

    return {
        tag: el.tagName.toLowerCase(),
        // ... existing attributes ...
        children: children
    };
}
```

### Cross-Origin IFrames

Cross-origin iframes cannot be accessed from the parent document's JS context. To capture them, WebView2 supports executing script in specific frames:

1. Use `CoreWebView2.FrameCreated` event to track iframe frames
2. For each frame, call `CoreWebView2Frame.ExecuteScriptAsync(CaptureScript)` to run the capture script inside the iframe's context
3. Stitch the results into the parent snapshot, matching by iframe element position

This is more complex and may warrant a phased approach:
- **Phase 1:** Same-origin iframe traversal (simple JS fix)
- **Phase 2:** Cross-origin iframe capture via `CoreWebView2Frame` API

### Model Change

Add an `IsIFrame` or `FrameSource` property to `DomElement` so the UI can distinguish iframe-sourced content:

```csharp
public string? FrameSource { get; set; }  // The iframe's src URL, if this subtree came from an iframe
```

## Affected Tests

- `DomCaptureServiceTests` — existing `ParseSnapshot` tests don't cover iframes
- **New tests needed:**
  - Parse a snapshot JSON containing an iframe with children
  - Verify iframe children are included in element count
  - Verify cross-origin iframe failure is handled gracefully (no crash)

## Status

- [ ] Same-origin iframe traversal added to CaptureScript
- [ ] Cross-origin iframe handling (graceful skip or WebView2 frame API)
- [ ] `FrameSource` property added to DomElement model
- [ ] Unit tests for iframe capture
- [ ] Tested with real iframe-heavy website
