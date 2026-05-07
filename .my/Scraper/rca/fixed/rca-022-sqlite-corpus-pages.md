# RCA-022: SQLite Corpus Store — Pages Not Persisted or Retrieved

**Reported:** 2026-05-04
**Severity:** High
**Component:** `Data/CorpusDatabase.cs`, `ViewModels/MainViewModel.cs`
**UAT Reference:** UAT-4.8 — SQLite Corpus Store

---

## Symptoms

Pages captured during recording or via the 📷 button are displayed in the "Corpus Pages" sidebar list during the current session, but:

1. Closing and reopening the app shows no previously recorded pages.
2. The SQLite database contains only a `Sites` table — there is no `Pages` table.
3. No code calls any save/load method for page snapshots.

## Root Cause

The `CorpusDatabase` class only has a `Sites` table. The `EnsureCreated()` method creates:

```sql
CREATE TABLE IF NOT EXISTS Sites (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    ...
);
```

There is no `Pages` table, no `SavePage` method, and no `GetPages` method. All page data exists only in memory via `Sidebar.CorpusPages` (an `ObservableCollection`) and `Recording.SessionSnapshots`.

When the app restarts:

- `SidebarViewModel.CorpusPages` starts empty
- `OnSiteSelected` sets `Sidebar.CorpusStats` from `ActiveSite.PageCount` (which is also always 0 from the DB since it's never updated)
- No code loads pages from the database

## Fix

### 1. Add Pages table to CorpusDatabase

```sql
CREATE TABLE IF NOT EXISTS Pages (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Url TEXT NOT NULL,
    Title TEXT NOT NULL DEFAULT '',
    CapturedAt TEXT NOT NULL DEFAULT (datetime('now')),
    ElementCount INTEGER NOT NULL DEFAULT 0,
    SnapshotJson TEXT NOT NULL,
    FOREIGN KEY (SiteId) REFERENCES Sites(Id)
);
```

### 2. Add CRUD methods

- `SavePage(long siteId, DomSnapshot snapshot)` — serialize snapshot to JSON, insert or update by URL
- `GetPages(long siteId)` — return list of pages for a site
- `DeletePage(long pageId)` — remove a page
- `UpdateSitePageCount(long siteId)` — update `Sites.PageCount` from actual page count

### 3. Wire up save/load

- When pages are transferred to corpus (analyze session), call `SavePage` for each
- When a site is selected (`OnSiteSelected`), call `GetPages` and populate `Sidebar.CorpusPages`
- When a page is manually captured via 📷, call `SavePage` immediately
- Update `Sites.PageCount` after any page save/delete

## Verification

- [X] Record 3 pages and analyze the session. Close the app, reopen, select the same site. All 3 pages appear in "Corpus Pages".
- [X] Use 📷 to capture a page outside recording. Close and reopen. The page persists.
- [X] Check the SQLite database file. A `Pages` table exists with rows containing serialized snapshot JSON.
- [X] The log shows "Corpus store — Site: {id}, Page: {name}, Elements: {count}, Size: {bytes} bytes" for each save.
- [X] `Sites.PageCount` reflects the actual number of stored pages.
