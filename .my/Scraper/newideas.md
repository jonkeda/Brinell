# Scraper — Future Ideas

## Tabbed Browsing

Instead of navigating `target="_blank"` / `window.open()` links in-place (current behavior), support multiple tabs within the Scraper. Each tab would be its own `BrowserView` + `CoreWebView2` instance with full inspector/highlight/capture support. This would let the user keep the originating page open while inspecting the linked page — useful for workflows where the user needs to compare or cross-reference pages.

Currently handled by RCA-007: new-window requests navigate in-place, which is correct for a single-document tool but loses the originating page context.

## DOM Tree View Improvements

Two issues from RCA-008:

1. **Filter auto-expand** — Filtering the DOM tree removes non-matching branches, but results stay collapsed so the user can't tell anything changed. Filtered nodes should auto-expand. Also need to copy `FrameSource` in `FilterElement`.
2. **Tree-to-browser hover highlight** — Hovering a node in the DOM tree panel should highlight the corresponding element in the browser. Requires wiring `MouseEnter`/`MouseLeave` on `TreeViewItem` back to `ElementHighlightService` to position the overlay at the element's bounding box coordinates.

## Demo/Test HTML Pages

Create a set of local HTML test pages that exercise all Scraper features — forms, inputs, iframes (same-origin and cross-origin), `target="_blank"` links, `window.open()`, deeply nested DOM trees, various `data-testid`/`aria-label` patterns, etc. Serve them via a simple local HTTP server so the Scraper can navigate to them for repeatable manual and automated testing.

## Tag Search Redesign

The current "Select Forms" / "Select Inputs" buttons use hardcoded tag lists and prune the tree in-place (RCA-010). Redesign this into a more flexible tag search — e.g. a combo box or text input where the user can type or pick tag names (`input`, `button`, `a`, `div`, `iframe`, etc.) and the tree filters to show only branches containing those tags. Would replace the fixed buttons with a single, more powerful control.
