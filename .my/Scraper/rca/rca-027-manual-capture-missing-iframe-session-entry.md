# RCA-027: Manual Capture Records Only Top-Level Page and Misses IFrame Session Entry

**Reported:** 2026-05-09
**Severity:** High
**Component:**
- `ViewModels/Tabs/ScrapingTabViewModel.cs`
- `ViewModels/RecordingViewModel.cs`
- `Services/DomCaptureService.cs`
- `Services/ElementHighlightService.cs`

---

## Summary of Reported Issue

When clicking **Capture DOM Snapshot** on a page that contains an iframe, only the top-level page is recorded in session. The iframe is not recorded as its own session entry.

---

## Expected Behavior

1. Manual capture should record the page in the same way as normal recording behavior.
2. If iframe context exists, iframe recording should also be present as a session item (with iframe identity), not only merged inside the top-level page capture.

---

## Actual Behavior

1. Manual capture creates exactly one session snapshot for the top-level page.
2. No iframe-specific session entry is created by the manual capture action.

---

## Root Cause Analysis

### RC1 - Manual capture path bypasses transition recording pipeline

`OnManualCaptureSnapshotAsync` captures DOM and appends a single snapshot directly to `Recording.SessionSnapshots`.

It does not call the frame-aware transition flow that classifies source type and URL identity.

### RC2 - No frame iteration / frame identity emission in manual capture

Manual capture uses `CaptureAsync(webView, _highlight.TrackedFrames)` which merges iframe DOM into the top-level snapshot tree, but it does not emit separate recording events per iframe URL/context.

Result: iframe content is embedded, but no separate iframe recording row is produced.

### RC3 - Recording list semantics are event-driven, not DOM-subtree-driven

Session rows are driven by transitions/additions into `Recording.SessionSnapshots`. A merged iframe subtree in one snapshot does not automatically become an additional session row.

### RC4 - Regression against user expectation after session-first manual capture change

RCA-026 aligned manual capture destination to session, but did not implement iframe session-entry parity with normal iframe transition recording behavior.

---

## Impact

- Users cannot reliably confirm iframe recording from a manual capture action.
- Session timeline under-represents iframe coverage even when iframe DOM exists inside the top-level snapshot.
- UAT expectation "page + iframe recorded" fails for one-click manual capture.

---

## Proposed Fixes

### F1 - Introduce shared capture-and-record helper with source context

Create a shared path used by both manual capture and normal recording that supports:

- top-level snapshot add,
- optional iframe snapshot adds,
- source typing (`top-level` / `iframe`),
- consistent naming and dedupe behavior.

### F2 - Add iframe session-entry emission for manual capture

After manual top-level capture:

1. enumerate tracked frames,
2. resolve frame URL/context (when available),
3. add iframe session entries using the same source-aware recording contract as normal iframe transitions.

### F3 - Preserve merged iframe DOM in top-level snapshot

Keep current `CaptureAsync(webView, _highlight.TrackedFrames)` behavior so top-level snapshot still contains iframe subtree content.

### F4 - Define manual-capture dedupe contract

Decide and implement expected behavior for repeated manual captures:

- recommended: always append on explicit click,
- but for iframe entries still allow short-window dedupe only for exact same iframe source key if needed.

### F5 - Add tests for manual iframe recording parity

Add/extend tests in scraper tab viewmodel tests for:

- manual capture creates top-level session row,
- manual capture on iframe page creates iframe session row,
- iframe row uses iframe source identity,
- no regression to existing normal recording flow.

---

## Verification Checklist

- [ ] On iframe page, click Capture DOM Snapshot once: session shows top-level page row.
- [ ] Same action also creates iframe session row when frame URL/context is available.
- [ ] Top-level snapshot still contains iframe DOM subtree.
- [ ] Manual OFF/ON recording toggle states do not change iframe session-entry behavior.
- [ ] Existing iframe transition recording remains intact.

---

## Suggested Follow-up Work Item

Implement manual-capture iframe parity end-to-end:

1. shared record helper with source identity,
2. manual iframe emission logic,
3. source-aware dedupe rules,
4. targeted unit tests and UAT update.
