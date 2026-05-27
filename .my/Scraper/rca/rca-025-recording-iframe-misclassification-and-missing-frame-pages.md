# RCA-025: Recording Misclassifies Top-Level Pages as [iframe] and Does Not Persist Home IFrame as Separate Page

**Reported:** 2026-05-09
**Severity:** High
**Component:**
- `ViewModels/Tabs/ScrapingTabViewModel.cs`
- `ViewModels/RecordingViewModel.cs`
- `Views/BrowserView.xaml.cs`
- `Services/DomCaptureService.cs`

---

## Summary of Reported Issues

1. While recording and navigating Home -> Catalog -> Checkout -> Support, recorded items are stored as `[iframe] ...` even though they are top-level pages.
2. The iframe inside Home is not stored as a separate page.

---

## Symptom Details

### S1 - Top-level pages are labeled as iframe pages

Session list shows entries like:

- `[iframe] Phase 14 Demo Site - Home`
- `[iframe] Phase 14 Demo Site - Catalog`
- `[iframe] Phase 14 Demo Site - Checkout`
- `[iframe] Phase 14 Demo Site - Support`

instead of plain top-level page names.

### S2 - Home iframe content is not a separate recorded page

The iframe content is embedded in the page capture but does not appear as an independent page entry in the recording list.

---

## Evidence

Observed logs in runtime output show page captures logged as `[iframe]` using top-level URLs, e.g.:

- `Page captured: [iframe] Phase 14 Demo Site - Home (file:///.../index.html)`
- `Page captured: [iframe] Phase 14 Demo Site - Catalog (file:///.../pages/catalog.html)`

This matches user-visible session list behavior.

---

## Root Cause Analysis

### RC1 - IFrame transition capture uses top-level URL instead of frame URL

In `ScrapingTabViewModel.CaptureTransitionAsync(...)`:

- `snapshot.PageName = isIFrameNavigation ? "[iframe] ..." : ...`
- `transitionUrl = snapshot.PageUrl` when no override is provided

For iframe navigation events, `snapshot.PageUrl` is still the main document URL (top-level WebView source), not the frame source URL.

Result: iframe-originated captures are stored under top-level page URL but with `[iframe]` label.

### RC2 - URL-only deduplication causes first capture in 2-second window to "win"

`RecordingViewModel.OnPageTransition(...)` deduplicates by:

- same URL, and
- within 2 seconds

When iframe-triggered capture happens first for a page URL, later top-level capture for the same URL within the dedupe window is skipped.

Result: page ends up recorded as `[iframe] ...` entry for that URL.

### RC3 - Frame navigation callback has no frame identity/URL payload

`BrowserView` currently raises `IFrameNavigationSucceeded` without frame URL/context.

Result: recording logic cannot distinguish:

- which iframe navigated
- what frame URL should be used as page identity

and falls back to top-level page URL.

### RC4 - Current DOM capture model merges iframe DOM into parent snapshot by design

`DomCaptureService` captures iframe content as child DOM under `<iframe>` nodes (same-origin directly; cross-origin via frame merge logic).

Result: iframe content is part of parent snapshot, not persisted as separate snapshot/page unless explicitly modeled as separate capture path.

---

## Why Home IFrame Is Not Stored as Separate Page

Because iframe captures currently reuse the parent page URL and do not create a frame-specific page identity, they are treated as transitions/captures of the parent page rather than independent pages.

The current model is "single snapshot tree with embedded frame DOM", not "separate snapshot per frame URL".

---

## Proposed Fixes

### F1 - Include frame URL/context in iframe navigation event pipeline

Update `BrowserView` / `BrowserViewModel` event contract:

- replace/extend `IFrameNavigationSucceeded` with payload containing frame URL and frame name/id if available.

Example contract:

- `event Action<string?>? IFrameNavigationSucceededWithUrl`

### F2 - Use frame URL for iframe recording identity

In `ScrapingTabViewModel.CaptureTransitionAsync(...)` for iframe transitions:

- set `transitionUrl` to frame URL payload when available
- set page name as `[iframe] {frameTitleOrHost}` only for true frame captures

This prevents frame captures from masquerading as top-level page captures.

### F3 - Improve dedupe key to include navigation type

Adjust `RecordingViewModel.OnPageTransition(...)` dedupe key from URL-only to:

- `(url, sourceType)` where `sourceType = top-level | iframe`

or

- include frame URL when available.

This avoids iframe capture suppressing top-level capture for the same parent URL.

### F4 - Decide and implement desired persistence model for iframes

Two valid options:

1. **Embedded-only model (current + clarified):**
   - Keep iframe DOM only as child nodes in parent snapshot.
   - Do not expect separate page entries for frames.
   - Remove/disable `[iframe]` pseudo-page entries from session list.

2. **Separate-page model (requested behavior):**
   - Persist frame captures as separate session/corpus entries keyed by frame URL.
   - Keep parent snapshot embedding as well.

Given user request, option 2 is preferred.

### F5 - Add explicit guard: never prefix top-level capture with `[iframe]`

If transition source is unknown or frame URL missing, default to top-level classification (no `[iframe]` prefix) to avoid false labeling.

---

## Verification Checklist

- [ ] Navigate Home -> Catalog -> Checkout -> Support during recording: top-level entries are not labeled `[iframe]`.
- [ ] Home page iframe generates separate frame entry only when frame URL identity is available.
- [ ] Top-level and iframe captures for same parent page can coexist (no accidental dedupe suppression).
- [ ] Parent snapshot still contains iframe DOM subtree.
- [ ] Session list shows expected naming and no misleading `[iframe]` labels on top-level pages.

---

## Suggested Follow-up Work Item

Implement frame-aware recording identity end-to-end:

1. Frame navigation payload in view layer.
2. Transition URL selection logic in scraping tab.
3. Dedupe key expansion in recording VM.
4. UAT update for iframe-as-separate-page behavior.
