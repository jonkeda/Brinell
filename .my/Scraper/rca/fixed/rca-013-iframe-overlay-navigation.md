# RCA-013: IFrame Overlay Not Added After Page Navigation (UAT-4.2a)

**Reported:** 2026-05-04
**Severity:** Medium
**Component:** `Services/ElementHighlightService.cs`
**UAT Reference:** UAT-4.2a — iFrame Overlay Support

---

## Symptoms

When navigating to a new page that contains an iframe, the highlight overlay is not injected into the iframe. Hovering over elements inside the iframe does not produce the blue highlight or tooltip. The overlay only works if inspect mode is toggled off and on again after navigation.

## Root Cause

The overlay script injection into iframe contexts (`CoreWebView2Frame`) is wired up during the initial `EnableAsync` call, but when the browser navigates to a new page the frame objects are destroyed and recreated. The `FrameCreated` event handler that injects the overlay into new frames either:

1. Is not re-registered after a top-level navigation (the event subscription is lost when the old page unloads), or
2. The injection fires before the iframe's `contentDocument` is ready, causing the script to silently fail.

This means the fix from RCA-006 (injecting into iframes on initial load) does not survive page navigation — the re-injection path is missing or races with the iframe's document lifecycle.

## Expected Behavior

- After navigating to a new page, the overlay should be automatically re-injected into all iframes.
- The `FrameCreated` handler must be subscribed on the `CoreWebView2` instance (which survives navigation), not on per-page state.
- Injection into each frame should wait until the frame's `DOMContentLoaded` event fires.

## Fix

1. Subscribe to `CoreWebView2.FrameCreated` once during `EnableAsync` on the `CoreWebView2` instance (not per-navigation).
2. In the `FrameCreated` handler, listen for the frame's `DOMContentLoaded` event before injecting the overlay script.
3. On top-level `NavigationCompleted`, re-inject the overlay into the top-level document and enumerate existing frames to inject into any iframes that were already loaded before the `FrameCreated` handler could fire.

## Verification

- [X] Navigate to a page with iframes. Enable inspect mode. Overlay works inside iframes.
- [X] Navigate to a different page that also has iframes. Overlay is automatically injected — no need to toggle inspect mode.
- [X] Navigate back to the first page. Overlay works in iframes on return navigation.
- [X] Navigate to a page without iframes, then to a page with iframes. Overlay works in the iframes.
