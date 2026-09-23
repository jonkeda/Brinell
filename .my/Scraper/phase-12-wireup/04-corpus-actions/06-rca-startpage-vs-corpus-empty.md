# RCA 12.W.4f — Start Page Count Shows 13, Corpus List Is Empty

## Incident Summary

When opening site **ExactOnline**:

1. Start page card shows **13 pages**.
2. User expects those pages to appear in Corpus tab.
3. Corpus tab page list is empty.

## Expected Behavior

- The page count shown on Start page and the pages shown in Corpus tab represent the same corpus source (or are explicitly labeled as different sources).

## Actual Behavior

- Start page count is populated from `Sites.PageCount` in `CorpusDatabase`.
- Corpus tab list is populated from `Snapshots` via `CorpusService.ListSnapshots(siteId)`.
- If pages exist only in `Pages` table and not in `Snapshots`, Start page can show non-zero count while Corpus tab is empty.

## Evidence

### A) Start page count source

`StartPageViewModel.ToCardItem()` maps card count from `SiteInfo.PageCount`:

- `PageCount = site.PageCount`

So Start page reflects `Sites.PageCount` value from `CorpusDatabase.GetAllSites()`.

### B) Workspace load path to Corpus

`WorkspaceViewModel.LoadAsync(siteId)` calls:

- `Corpus.Load(siteId)`

### C) Corpus list source

`CorpusTabViewModel.Load(siteId)` uses:

- `_corpusService.ListSnapshots(siteId)`
- Groups snapshots by `PageName`
- Builds UI pages from snapshot groups

If `ListSnapshots(siteId)` returns empty, corpus page list is empty.

### D) Start page count maintenance path

Main flows update site count using `CorpusDatabase` pages table:

- `_db.SavePage(...)`
- `_db.UpdateSitePageCount(siteId)`

This keeps `Sites.PageCount` in sync with `Pages`, not with `Snapshots`.

## Root Cause

**Primary root cause:** data-source divergence.

The UI currently mixes two different persistence models for “corpus pages”:

- **Model 1 (legacy/DB pages):** `CorpusDatabase` (`Sites`, `Pages`) drives Start page count.
- **Model 2 (snapshot corpus):** `CorpusService` (`Snapshots`, `Elements`) drives Corpus tab list.

There is no guaranteed synchronization or migration layer between `Pages` and `Snapshots` for existing site data. Therefore, page counts and corpus list can disagree.

## Contributing Factors

1. **No reconciliation bridge** between `Pages` and `Snapshots` at workspace load.
2. **No UX hint** that Start page count and Corpus list may come from different stores.
3. **No guardrail test** (until now) asserting Start page count aligns with Corpus list source.

## Why ExactOnline Shows 13 but Empty Corpus

Most likely, ExactOnline has 13 records in `Pages` (and/or `Sites.PageCount=13`), but zero records in `Snapshots` for that `SiteId`. Corpus tab only reads `Snapshots`, so it renders empty.

## Impact

- User trust issue: visible contradiction in core workflow.
- Operational confusion: users may think data is lost.
- Increased support/debug time due to ambiguous source of truth.

## Corrective Actions

### Immediate (hotfix-safe)

1. Add startup reconciliation on `WorkspaceViewModel.LoadAsync(siteId)`:
   - If `Sites.PageCount > 0` and `CorpusService.ListSnapshots(siteId).Count == 0`, run one-time hydration from `Pages` into `Snapshots` (or show guided migration prompt).
2. Add warning/status note in Corpus tab when:
   - Start-page count > 0 but snapshot corpus count == 0.
3. Add telemetry log event for mismatch detection:
   - `CorpusSourceMismatch(siteId, pageCount, snapshotCount)`.

### Medium-term

1. Define a **single source of truth** for corpus pages.
2. Deprecate or map `Sites.PageCount` to snapshot-derived count.
3. Update Start page card to use same source as Corpus tab.

### Long-term

1. Introduce explicit schema/version migration pipeline.
2. Add regression tests that assert count/list consistency across Start page and Corpus tab.

## Verification Plan

1. Prepare site with `Sites.PageCount > 0` and empty `Snapshots`.
2. Open site and confirm mismatch is detected.
3. Execute migration/reconciliation.
4. Re-open Corpus tab:
   - expected: pages displayed and count aligned.
5. Run test suite including corpus CRUD/reconciliation tests.

## Proposed Owner Tasks

- [ ] Implement mismatch detection in workspace load.
- [ ] Implement one-time migration or fallback rendering.
- [ ] Align Start page count source with Corpus tab source.
- [ ] Add integration test: `StartCount_Equals_CorpusListCount_ForActiveSite`.

## Decision Record

Until source unification is complete, treat `Snapshots` as authoritative for Corpus tab rendering, and explicitly reconcile or communicate mismatch from `Pages`-based site counts.
