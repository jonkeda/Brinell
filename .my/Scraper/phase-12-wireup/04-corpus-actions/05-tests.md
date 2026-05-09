# Step 12.W.4e — Tests for CRUD & Visibility Reconciliation

## Objective

Define test coverage for the Phase 12.W.4d implementation so DB visibility issues (pages in DB not shown in UI) are prevented from regressing.

## Scope

- `CorpusService` CRUD methods:
  - `ListPagesBySiteId(siteId)`
  - `GetSnapshotsByPageName(siteId, pageName)`
  - `DeletePageByName(siteId, pageName)`
  - `DeleteStaleSnapshots(siteId, olderThanDays)`
- `CorpusTabViewModel` reconciliation and delete flows:
  - `LoadPagesWithReconciliationAsync(ct)`
  - `RefreshPageAsync(page, ct)`
  - `DeleteSnapshotAsync(page, snapshot, ct)`
  - `DeletePageAsync(page, ct)`
- UI command wiring in `CorpusTabView.xaml`:
  - `RefreshPageCommand`
  - `DeleteSnapshotCommand`
  - `DeletePageCommand`

## Test Strategy

### 1. Unit Tests: CorpusService

Use SQLite temp DB and isolated service instance per test.

#### `ListPagesBySiteId`

- Returns distinct pages for a site.
- Does not return pages from other sites.
- Returns latest timestamp per page.
- Handles empty site with empty result.

#### `GetSnapshotsByPageName`

- Returns all snapshots for the given page ordered by `CapturedAt` descending.
- Returns empty when page does not exist.
- Does not leak snapshots from other pages/sites.

#### `DeletePageByName`

- Deletes all snapshots for the target page.
- Deletes corresponding indexed elements rows.
- Does not delete snapshots for other pages.
- Safe when page has no snapshots.

#### `DeleteStaleSnapshots`

- Deletes only snapshots older than cutoff.
- Returns correct deleted count.
- Leaves newer snapshots untouched.
- Cleans up related elements rows.

### 2. Unit Tests: CorpusTabViewModel

Use mocked `CorpusService` responses where possible; for delete integration behavior, use in-memory or temp DB service.

#### `LoadPagesWithReconciliationAsync`

- Removes orphaned UI pages not present in DB.
- Adds DB pages missing in UI.
- Refreshes versions for existing pages.
- Preserves totals (`TotalPages`, `TotalSnapshots`, `TotalElements`, `TotalSizeBytes`).

#### `RefreshPageAsync`

- Replaces stale versions with DB truth.
- Updates version numbering and latest marker.
- Updates page URL when latest snapshot URL differs.

#### `DeleteSnapshotAsync`

- On confirmation yes: removes snapshot from DB and UI.
- On confirmation no: no changes applied.
- If selected snapshot deleted, selected version updates to latest or null.

#### `DeletePageAsync`

- On confirmation yes: removes page and snapshots from DB and UI.
- Clears `SelectedPage` when deleted page is selected.
- On confirmation no: no changes applied.

### 3. Command Wiring Tests / UI Behavior

For ViewModel-level command tests:

- `RefreshPageCommand` executable only when `SelectedPage != null`.
- `DeleteSnapshotCommand` executable only when `SelectedPage != null` and row parameter is provided.
- `DeletePageCommand` executable only when `SelectedPage != null`.

For smoke UI verification (manual):

- Toolbar buttons trigger expected actions.
- Page context menu entries call the expected commands.
- Per-row delete button deletes the row snapshot (after confirm).

## Proposed Test Files

- `tests/Brinell.Scraper.Tests/Services/CorpusServiceCrudTests.cs`
- `tests/Brinell.Scraper.Tests/ViewModels/CorpusTabViewModelReconciliationTests.cs`
- `tests/Brinell.Scraper.Tests/ViewModels/CorpusTabViewModelCommandTests.cs`

## Data Fixtures

Use deterministic fixture builders:

- Site A, Site B
- Page names:
  - `Home`
  - `Products`
  - `Checkout`
- Snapshot timeline with explicit timestamps:
  - old (90 days)
  - mid (14 days)
  - recent (1 day)

## Acceptance Criteria

- Visibility bug covered: pages directly inserted in DB appear after refresh/reconciliation.
- Delete operations validated for both DB and UI collection state.
- Stale cleanup validated with exact row-count assertions.
- Commands tested for execution guards.
- Test suite passes locally and in CI.

## Execution

- Run targeted tests:

```powershell
dotnet test tests/Brinell.Scraper.Tests/Brinell.Scraper.Tests.csproj --filter "CorpusServiceCrudTests|CorpusTabViewModelReconciliationTests|CorpusTabViewModelCommandTests"
```

- Run full suite:

```powershell
dotnet test
```

## Next Step

Implement the above test files, then validate they fail-before/fix-after for at least one visibility regression scenario.