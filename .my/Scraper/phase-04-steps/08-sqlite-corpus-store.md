# Step 4.8 — SQLite Corpus Store

## Objective

Store all DOM snapshots in a per-site SQLite database with element indexing for cross-page pattern queries.

## Dependencies

- Step 4.1 (DomSnapshot / DomElement models)
- NuGet: `Microsoft.Data.Sqlite` (already referenced)

## Implementation

### Schema

```sql
CREATE TABLE Sites (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    StartUrl TEXT NOT NULL,
    Namespace TEXT NOT NULL,
    OutputPath TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    LastRecordedAt TEXT
);

CREATE TABLE SiteAliases (
    SiteId INTEGER NOT NULL REFERENCES Sites(Id),
    AliasUrl TEXT NOT NULL,
    PRIMARY KEY (SiteId, AliasUrl)
);

CREATE TABLE Snapshots (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL REFERENCES Sites(Id),
    PageName TEXT NOT NULL,
    PageUrl TEXT NOT NULL,
    PageTitle TEXT,
    CapturedAt TEXT NOT NULL,
    DomJson TEXT NOT NULL,
    ElementCount INTEGER NOT NULL,
    SnapshotSizeBytes INTEGER NOT NULL,
    IsLatest INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE Elements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SnapshotId INTEGER NOT NULL REFERENCES Snapshots(Id),
    Tag TEXT NOT NULL,
    ElementId TEXT,
    ClassName TEXT,
    DataTestId TEXT,
    AriaLabel TEXT,
    Role TEXT,
    TextContent TEXT,
    ParentPath TEXT,
    AttributesJson TEXT
);

CREATE INDEX IX_Elements_Tag ON Elements(Tag);
CREATE INDEX IX_Elements_DataTestId ON Elements(DataTestId);
CREATE INDEX IX_Snapshots_SiteId ON Snapshots(SiteId);
CREATE INDEX IX_Snapshots_PageName ON Snapshots(SiteId, PageName);
```

### CorpusService

```csharp
public sealed class CorpusService
{
    Task<SiteCorpus> CreateSiteAsync(string name, string startUrl, string ns, string outputPath);
    Task<SiteCorpus?> GetSiteAsync(string name);
    Task<IReadOnlyList<SiteCorpus>> ListSitesAsync();
    Task StoreSnapshotAsync(SiteCorpus site, DomSnapshot snapshot);
    Task<DomSnapshot?> GetLatestSnapshotAsync(int siteId, string pageName);
    Task<IReadOnlyList<SnapshotSummary>> ListSnapshotsAsync(int siteId);
    Task<IReadOnlyList<DomElement>> SearchElementsAsync(int siteId, string query);
}
```

### Storage details

- Database location: `%APPDATA%\Brinell.Scraper\corpus\{site-name}.db`
- Re-recording a page: mark old snapshot as `IsLatest = 0`, insert new as `IsLatest = 1`.
- Index individual elements in `Elements` table for cross-page pattern queries (Phase 5).

## Checklist

- [ ] Per-site SQLite database created at `%APPDATA%` location
- [ ] `Sites`, `SiteAliases`, `Snapshots`, `Elements` tables created
- [ ] `CorpusService` CRUD operations implemented
- [ ] Re-recording marks old snapshot as historical, stores new as latest
- [ ] Elements individually indexed for cross-page queries
- [ ] Indexes created for efficient lookups
