# Step 12.W.4g — Corpus Store Consolidation Plan (Cutover)

## Objective

Remove the incorrect dual-store model and keep one correct implementation only.

No backward compatibility. No migration/safeguard layer.

## Decision

Use `CorpusService` snapshot model as the only corpus source.

- Keep: `Sites` metadata
- Remove: `Pages` as corpus storage
- Read/write corpus only via `Snapshots` + related snapshot tables

## Cutover Scope

### Keep

- `CorpusService` tables and flows:
  - `Snapshots`
  - `Elements`
  - `AnalysisResults`
  - `PageObjects`

### Remove

- `CorpusDatabase.Pages` corpus flows
- `Sites.PageCount` as source of truth for corpus count
- all `SavePage/GetPages/GetPageSnapshot/UpdateSitePageCount` corpus call paths

## Implementation Plan

## Phase 1 — Read-path Fix

### Goal

Start page and Corpus tab must read from the same source immediately.

### Tasks

1. Change Start page count to snapshot-derived count per site.
2. Stop using `SiteInfo.PageCount` for corpus display logic.
3. Keep count rendering based on `CorpusService` query results.

### Files

- `tools/Brinell.Scraper/ViewModels/StartPageViewModel.cs`
- `tools/Brinell.Scraper/Services/CorpusService.cs`
- `tools/Brinell.Scraper/Models/SiteInfo.cs` (remove/de-emphasize count authority)

## Phase 2 — Write-path Fix

### Goal

All corpus writes go to snapshot storage only.

### Tasks

1. Manual capture writes: use `CorpusService.StoreSnapshot` only.
2. Import writes: use `CorpusService.StoreSnapshot` only.
3. Analyze/session transfer writes: use `CorpusService.StoreSnapshot` only.
4. Delete calls to `CorpusDatabase.SavePage` and related page-table update logic.

### Files

- `tools/Brinell.Scraper/ViewModels/MainViewModel.cs`
- `tools/Brinell.Scraper/Data/CorpusDatabase.cs`

## Phase 3 — Legacy Removal

### Goal

Delete the wrong model so it cannot be used again.

### Tasks

1. Remove `Pages` CRUD methods from `CorpusDatabase`.
2. Remove `PageCount` update methods tied to `Pages`.
3. Remove any remaining references in ViewModels.

### Files

- `tools/Brinell.Scraper/Data/CorpusDatabase.cs`
- all call sites referencing `Pages` APIs

## Phase 4 — Tests

### Goal

Prove single-source behavior.

### Tests

1. Start page count equals snapshot count for active site.
2. Corpus tab list count equals snapshot-derived grouped page count.
3. Manual capture/import/session analyze create snapshot-backed corpus rows.
4. Full suite remains green.

### Candidate test files

- `tools/Brinell.Scraper.Tests/Services/CorpusServiceCrudTests.cs`
- `tools/Brinell.Scraper.Tests/ViewModels/CorpusTabViewModelReconciliationTests.cs`
- new integration test(s) under `tools/Brinell.Scraper.Tests/ViewModels/`

## Acceptance Criteria

1. One corpus source in code: snapshots.
2. No code path writes corpus content into `Pages`.
3. Start page and Corpus tab cannot diverge due to storage split.
4. All tests pass.

## Execution Order

1. Phase 1 (read-path fix)
2. Phase 2 (write-path fix)
3. Phase 3 (legacy removal)
4. Phase 4 (tests and validation)
