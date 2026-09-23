# RCA-026: Manual "Capture DOM Snapshot" Does Not Always Add to Session and Is Not Aligned with Recording Flow

**Reported:** 2026-05-09
**Severity:** High
**Component:**
- `ViewModels/Tabs/ScrapingTabViewModel.cs`
- `ViewModels/RecordingViewModel.cs`
- `ViewModels/SessionPanelViewModel.cs`
- `Services/DomCaptureService.cs`

---

## Summary of Reported Issues

1. Clicking **Capture DOM Snapshot** should always add a snapshot to **This Session**, regardless of recording toggle state.
2. Manual capture should record the page in the same way as normal recording, including iframe content.

---

## Expected Behavior

### E1 - Session-first manual capture

When user clicks **Capture DOM Snapshot**, the captured snapshot is always appended to session history and visible in the session list.

### E2 - Recording-toggle independence

Manual capture behavior is identical whether recording is ON or OFF.

### E3 - Same capture fidelity as normal recording

Manual capture uses the same DOM capture path as recording capture, including merged iframe DOM.

---

## Actual Behavior

### A1 - Behavior differs by recording state

Current `OnManualCaptureSnapshotAsync` has branching behavior:

- If recording is ON: snapshot is added to session.
- If recording is OFF and site exists: snapshot is persisted to corpus and session is reloaded.
- If recording is OFF and no site exists: snapshot falls back to session.

Result: manual capture does not consistently add to session.

### A2 - User mental model mismatch

Users expect the Capture button to behave like an explicit "add this page to session now" action.

Result: snapshots can appear in corpus but not in session timeline when recording is OFF.

---

## Root Cause Analysis

### RC1 - Manual capture command conflates two intents

`OnManualCaptureSnapshotAsync` currently mixes:

- session recording intent, and
- corpus persistence intent.

The command is framed as a direct capture action, but implementation routes to corpus when recording is OFF.

### RC2 - Recording state is used as a routing switch for manual command

A UI toggle intended for continuous auto-record flow also controls one-shot capture destination.

Result: same user action has different output destination.

### RC3 - Session list source is disconnected from corpus-only write path

Session list reflects `Recording.SessionSnapshots`. Writing directly to corpus does not guarantee equivalent session entry.

Result: user does not see captured page in "This Session" after manual capture in OFF state.

### RC4 - No explicit parity contract between manual and normal recording

Although manual path already uses `DomCaptureService.CaptureAsync(webView, _highlight.TrackedFrames)` (iframe-inclusive), behavior parity is not codified for destination and processing semantics.

Result: implementation drift and ambiguous expectations.

---

## Why IFrame Inclusion Must Be Explicit

Normal recording depends on `DomCaptureService` with tracked frames to merge iframe DOM into snapshot tree. Manual capture must keep this exact call contract to avoid lower-fidelity captures.

Current code already passes `_highlight.TrackedFrames`; this must be preserved as a non-regression requirement.

---

## Proposed Fixes

### F1 - Make manual capture always add to session

In `OnManualCaptureSnapshotAsync`, remove corpus-only routing from normal button path.

- Always add snapshot to `Recording.SessionSnapshots` (or equivalent session pipeline method).
- Keep status text consistent: "Snapshot added to This Session".

### F2 - Preserve recording parity through shared capture helper

Refactor manual and navigation-triggered recording paths to share one capture-to-session routine, so naming/classification and metadata handling stay consistent.

### F3 - Keep iframe-inclusive capture contract

Ensure manual capture continues to call:

- `CaptureAsync(webView, _highlight.TrackedFrames)`

and add tests to lock this behavior.

### F4 - Decouple corpus persistence from Capture button

If needed, add a separate explicit action for corpus persistence (for example, "Save Snapshot to Corpus"), rather than overloading Capture.

### F5 - Clarify dedupe expectation for manual action

Define whether repeated manual clicks should always append (no dedupe) or follow dedupe. Recommended: manual capture should always append because it is explicit user intent.

---

## Verification Checklist

- [ ] With recording OFF, click Capture DOM Snapshot: snapshot appears in session list.
- [ ] With recording ON, click Capture DOM Snapshot: snapshot appears in session list.
- [ ] OFF and ON paths produce equivalent session entry shape (name/url/timestamp conventions).
- [ ] Manual capture snapshot contains iframe DOM subtree (same fidelity as normal recording).
- [ ] No unexpected corpus-only save occurs from Capture button.
- [ ] Existing explicit corpus workflows continue to function through their own commands.

---

## Suggested Follow-up Work Item

Implement manual-capture parity end-to-end:

1. Session-first routing in `OnManualCaptureSnapshotAsync`.
2. Shared helper for capture-to-session semantics.
3. Unit tests for OFF/ON parity and iframe inclusion.
4. Optional dedicated corpus-save command if product still needs direct save behavior.
