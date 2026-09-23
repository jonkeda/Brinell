# Step 13.7 — Data Persistence: AnalysisResults & PageObjects Tables

## Objective

Add database tables required by the pipeline: `AnalysisResults` (control proposals + locator reports per analysis run) and `PageObjects` (generated PageObject code + validation per snapshot).

## Dependencies

- Phase 4 (existing SQLite schema in `CorpusService`)
- Step 13.1, 13.4 (consumers)

## Implementation

### Files

- Update: `Services/CorpusService.cs` (add migrations + accessors)
- New: `Migrations/2026_05_AnalysisResults.sql` (or inline SQL string)
- New: `Migrations/2026_05_PageObjects.sql`

### Schema

```sql
CREATE TABLE IF NOT EXISTS AnalysisResults (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    SiteId      INTEGER NOT NULL,
    AnalyzedAt  TEXT    NOT NULL,
    IsCurrent   INTEGER NOT NULL DEFAULT 1,
    Snapshots   INTEGER NOT NULL,
    LocalGroups INTEGER NOT NULL,
    ProposalsJson TEXT  NOT NULL,
    LocatorReportJson TEXT,
    FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS IX_AnalysisResults_Site ON AnalysisResults(SiteId, IsCurrent);

CREATE TABLE IF NOT EXISTS PageObjects (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    SiteId        INTEGER NOT NULL,
    SnapshotId    INTEGER NOT NULL,
    ClassName     TEXT    NOT NULL,
    Namespace     TEXT    NOT NULL,
    MainCode      TEXT    NOT NULL,
    ContainerCodesJson TEXT NOT NULL DEFAULT '[]',
    UsedControlsJson   TEXT NOT NULL DEFAULT '[]',
    Status        TEXT    NOT NULL,    -- NotGenerated|Generated|Error
    ValidationJson TEXT,
    GeneratedAt   TEXT    NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE CASCADE,
    FOREIGN KEY (SnapshotId) REFERENCES Snapshots(Id) ON DELETE CASCADE,
    UNIQUE(SnapshotId)
);
CREATE INDEX IF NOT EXISTS IX_PageObjects_Site ON PageObjects(SiteId);
```

### CorpusService accessors

```csharp
// AnalysisResults
public Task<long> StoreAnalysisResultAsync(long siteId, ControlObjectAnalysisResult result, CancellationToken ct = default);
public Task<ControlObjectAnalysisResult?> GetCurrentAnalysisResultAsync(long siteId, CancellationToken ct = default);
public Task UpdateProposalApprovalAsync(long siteId, string proposalName, ControlObjectStatus status, CancellationToken ct = default);

// PageObjects
public Task StorePageObjectAsync(PageGenerationResult result, CancellationToken ct = default);
public Task<List<PageGenerationResult>> GetPageObjectsAsync(long siteId, CancellationToken ct = default);
public Task<PageGenerationResult?> GetPageObjectBySnapshotAsync(long snapshotId, CancellationToken ct = default);
public Task DeletePageObjectAsync(long snapshotId, CancellationToken ct = default);
```

### IsCurrent semantics

`StoreAnalysisResultAsync`:

```sql
UPDATE AnalysisResults SET IsCurrent = 0 WHERE SiteId = @siteId;
INSERT INTO AnalysisResults (...) VALUES (..., 1);
```

This keeps a history of analysis runs while exposing the latest as "current".

### Approval persistence

When the user approves/rejects a proposal in the UI, `UpdateProposalApprovalAsync` mutates the `ProposalsJson` of the current row:

```csharp
var current = await GetCurrentAnalysisResultAsync(siteId, ct);
var proposal = current.Proposals.First(p => p.Name == proposalName);
proposal.Status = status;
// re-serialize and UPDATE the row
```

### Migration runner

In `CorpusService` constructor or `InitializeAsync`:

```csharp
private async Task RunMigrationsAsync()
{
    await ExecuteAsync(SCHEMA_ANALYSIS_RESULTS);
    await ExecuteAsync(SCHEMA_PAGE_OBJECTS);
}
```

Idempotent (`CREATE TABLE IF NOT EXISTS`).

### Cleanup

`DELETE FROM Sites WHERE Id = @id` cascades to `Snapshots`, `Controls`, `AnalysisResults`, `PageObjects`. Verify all FK declarations include `ON DELETE CASCADE`.

## Checklist

- [ ] `AnalysisResults` table created with `IsCurrent` flag and JSON columns
- [ ] `PageObjects` table created with unique constraint on `SnapshotId`
- [ ] Both tables cascade delete from `Sites`
- [ ] CorpusService accessors implemented (store/get/update/delete)
- [ ] `IsCurrent` updated transactionally so only one row per site is current
- [ ] Approval status mutates the current `AnalysisResults` row
- [ ] Migrations run idempotently on startup
- [ ] Existing tests still pass; new tests for accessors added
