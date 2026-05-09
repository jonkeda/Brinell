# Step 12.W.8g - Capture Always Persists Snapshot and Add Session Transfer Button

## Objective

Update the capture and session flow so:

1. Capturing via the `Capture DOM Snapshot` button always creates a snapshot record.
2. Add a `Transfer to Corpus` button beneath `This Session` that transfers session snapshots to corpus, matching the existing `Yes` behavior from the stop-recording prompt.

## Change Scope

### 1) Capture button always creates a snapshot

Current behavior:
- Capture routes differently based on recording state and can be inspector-only in guard cases.

Required behavior:
- Every capture button click must result in a persisted snapshot (no inspector-only outcome).
- If recording is active, snapshot must be persisted in session flow and remain eligible for transfer.
- If recording is inactive, snapshot must be persisted to corpus immediately.
- Missing site context must not silently drop persistence; either:
  - block capture with clear user-facing status, or
  - require selecting/creating an active site before allowing capture.

### 2) Add `Transfer to Corpus` button under `This Session`

Current behavior:
- Session transfer to corpus happens through stop-recording prompt (`Yes` path).

Required behavior:
- Add explicit `Transfer to Corpus` button under `This Session` list.
- Button performs the same transfer operation as prompt `Yes`:
  - store all session snapshots into corpus for current site
  - refresh corpus list/stats
  - clear session snapshots
- Button should work even when recording is stopped, as long as session contains snapshots.

## UX Design

### Capture button

- Capture always persists; no no-op behavior.
- On failure preconditions (for example no active site), show clear status text and disable button where possible.

### Transfer button

- Placement: directly below `This Session` actions.
- Label: `Transfer to Corpus`.
- Enabled only when:
  - `Session.SiteId > 0`
  - `Recording.SessionSnapshots.Count > 0`
- Optional confirmation dialog:
  - `Transfer X snapshots to corpus now?`

## Technical Design

### Capture persistence rules

In `OnManualCaptureSnapshotAsync`:
- Always execute a persistence path.
- Recording ON:
  - keep current session add path (`Recording.OnPageTransition`) and ensure snapshot is retained for transfer.
- Recording OFF:
  - store to corpus (`CorpusService.StoreSnapshot`) and refresh session corpus panel.
- Remove inspector-only fallback persistence outcome.

### Transfer command

Introduce command in `SessionPanelViewModel` and wire callback in `ScrapingTabViewModel`:

- `TransferSessionToCorpusCommand`
- `CanTransferSessionToCorpus`

Callback behavior (same as analyze `Yes`):
1. Guard `Session.SiteId > 0` and non-empty session snapshots.
2. Persist each session snapshot to corpus.
3. Refresh session panel corpus (`Session.Load(...)`).
4. Clear recording snapshots (`Recording.ClearSnapshots()`).
5. Update status/log.

Refactor suggestion:
- Extract common transfer logic currently in `AnalyzeSession()` into one shared private method and reuse it for:
  - stop prompt `Yes`
  - `Transfer to Corpus` button

## Files (expected)

| File | Action |
|------|--------|
| `tools/Brinell.Scraper/ViewModels/Tabs/SessionPanelViewModel.cs` | Add transfer command + can-execute state |
| `tools/Brinell.Scraper/ViewModels/Tabs/ScrapingTabViewModel.cs` | Wire transfer callback and share transfer logic with analyze flow |
| `tools/Brinell.Scraper/Views/Tabs/ScrapingTabView.xaml` | Add `Transfer to Corpus` button under `This Session` |

## Acceptance Criteria

1. Every click on `Capture DOM Snapshot` results in a persisted snapshot path (session or corpus).
2. No inspector-only capture path remains.
3. `Transfer to Corpus` button appears beneath `This Session`.
4. Clicking `Transfer to Corpus` performs the same transfer result as stop-prompt `Yes`.
5. After transfer: corpus refreshed, session cleared, status/log updated.
6. Transfer button is disabled when there are no session snapshots or no active site.

## Test Impact

Add/adjust tests for:
- manual capture while recording persists into session snapshots.
- manual capture while not recording persists to corpus.
- transfer command enabled/disabled state transitions.
- transfer command stores all session snapshots, refreshes corpus, clears session.
- stop-prompt `Yes` and transfer button use the same shared transfer behavior.
