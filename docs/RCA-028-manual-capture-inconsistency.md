# RCA-028: Manual Capture Path Divergence

**Date**: May 9, 2026  
**Severity**: High  
**Status**: Investigation Complete

## Problem Statement

When recording is enabled and user navigates away, both the top-level page and any iframes are recorded as separate session entries. However, when clicking the manual capture button, only the top-level page is captured even though `CaptureTrackedFramesForSessionAsync()` is invoked to emit iframe entries.

## Root Cause Analysis

Two distinct code paths produce different behavior:

### Automatic Recording Path (Working)
```
Navigation event triggered
  → OnPageTransition(url, snapshot, "top-level")
    → Recording.OnPageTransition() [deduped by source|url]
      → SessionSnapshots.Add(snapshot)

IFrame navigation event triggered
  → OnPageTransition(frameUrl, iframeSnapshot, "iframe")
    → Recording.OnPageTransition() [deduped by source|url]
      → SessionSnapshots.Add(iframeSnapshot)
```

### Manual Capture Path (Broken)
```
User clicks capture button
  → OnManualCaptureSnapshotAsync()
    → Capture top-level snapshot
    → Recording.SessionSnapshots.Add(snapshot)  [DIRECT ADD, bypasses OnPageTransition!]
      → Load inspector
    → CaptureTrackedFramesForSessionAsync()
      → For each frame: Recording.OnPageTransition(frameUrl, iframeSnapshot, "iframe")
```

**Key Difference**: Top-level manual capture bypasses `Recording.OnPageTransition()` and directly adds to `SessionSnapshots`. This:
1. Skips the deduplication check that both automatic path and iframe manual capture use
2. Uses a different API than the rest of the recording system
3. Creates inconsistent behavior between automatic and manual recording

## Solution

Replace direct `Recording.SessionSnapshots.Add()` call with `Recording.OnPageTransition()` for the top-level manual capture, matching the automatic recording path exactly.

## Expected Behavior After Fix

Both paths use identical code:
- Automatic page navigation → `Recording.OnPageTransition(url, snapshot, "top-level")`
- Manual page capture → `Recording.OnPageTransition(url, snapshot, "top-level")`
- Automatic iframe navigation → `Recording.OnPageTransition(frameUrl, iframeSnapshot, "iframe")`
- Manual iframe capture → `Recording.OnPageTransition(frameUrl, iframeSnapshot, "iframe")`

All go through the same deduplication and session management logic.
