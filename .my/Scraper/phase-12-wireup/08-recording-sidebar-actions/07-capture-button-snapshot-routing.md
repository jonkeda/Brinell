# Step 12.W.8f — Capture Button Snapshot Routing

## Objective

Change the Capture DOM Snapshot button behavior so the captured snapshot is routed into the correct destination based on the current recording state, rather than only loading into the inspector form.

## Change Scope

### 1) Current behavior — capture loads inspector only

The capture button currently:
- Captures a DOM snapshot.
- Loads it into the inspector (`Inspector.LoadSnapshot`) and activates the inspector panel.

The snapshot is not persisted or added to any recording/corpus collection.

### 2) Required behavior — route capture based on recording state

**When recording is active (`Recording.IsRecording == true`):**
- Captured snapshot is added to the recording session (`Recording.OnPageTransition` / `Recording.SessionSnapshots`), exactly as an auto-captured page.
- Snapshot also loads into the inspector so the user can immediately inspect it.
- The new entry appears in the "This Session" sidebar list.

**When recording is not active:**
- Captured snapshot is stored directly into the corpus for the active site (`CorpusService.StoreSnapshot`).
- Snapshot also loads into the inspector.
- The corpus list in the session panel refreshes to reflect the new entry.

### 3) Additional follow-up item

- Item 3 was not specified in the request and should be added once clarified.

## UX Design

- No dialog or confirmation is required for the routing decision; the button follows the current recording state silently.
- The "This Session" or corpus list should update immediately after capture.
- The inspector opens automatically after capture in both paths (existing behavior is correct for this part).
- Status bar or log should reflect which path was taken (e.g., "Snapshot added to session" vs "Snapshot saved to corpus").

## Technical Design

### Routing logic in `ScrapingTabViewModel.OnManualCaptureSnapshotAsync`

Current flow:

```
capture → Inspector.LoadSnapshot → Inspector.IsInspecting = true
```

New flow:

```
capture
  if Recording.IsRecording
    → Recording.OnPageTransition(url, snapshot)   // dedup-aware, adds to session
  else
    → CorpusService.StoreSnapshot(Session.SiteId, snapshot)
    → Session.Load(Session.SiteId, Session.SiteHeader)   // refresh corpus list
  → Inspector.LoadSnapshot(snapshot)
  → Inspector.IsInspecting = true
```

### Guard conditions

- If `Session.SiteId <= 0`, skip corpus store and log a warning.
- If `Recording.IsRecording` but the dedup window rejects the snapshot (same URL too recent), still load into inspector — the user explicitly requested a capture.

### Suggested status reporting

Extend `Browser.StatusText` or a new `Session.LastCaptureStatus` property with a short message:
- `"Snapshot added to This Session"` (recording path)
- `"Snapshot saved to corpus"` (corpus path)
- `"Snapshot loaded in inspector"` (no site context / guard hit)

## Files

| File | Action |
|------|--------|
| `tools/Brinell.Scraper/ViewModels/Tabs/ScrapingTabViewModel.cs` | Update `OnManualCaptureSnapshotAsync` with routing logic |
| `tools/Brinell.Scraper/Views/Tabs/ScrapingTabView.xaml` | Optional: update button tooltip to reflect context-aware behavior |

## Acceptance Criteria

1. While recording, pressing Capture adds a snapshot to This Session and opens the inspector.
2. While not recording, pressing Capture stores the snapshot to corpus and refreshes the corpus list.
3. Inspector opens with the captured snapshot in both paths.
4. Status text or log reflects which routing path was taken.
5. Guard conditions prevent silent failures when site context is missing.

## Test Impact

Add/adjust unit tests covering:
- `OnManualCaptureSnapshotAsync` while recording routes to `Recording.SessionSnapshots`.
- `OnManualCaptureSnapshotAsync` while not recording routes to `CorpusService` and refreshes session panel.
- Inspector is loaded in both paths.
- Guard: no corpus store when `SiteId <= 0`.
