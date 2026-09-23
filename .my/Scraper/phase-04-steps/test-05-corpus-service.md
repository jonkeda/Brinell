# Test 4.5 — CorpusService Tests

**Covers:** Step 4.8 — `CorpusService` (SQLite CRUD for sites, snapshots, and elements)

**File:** `Brinell.Scraper.Tests/Data/CorpusServiceTests.cs`

## Test Inventory (12 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `CreateSite_PersistsAndReturns` | `CreateSiteAsync(name, startUrl, ns, outputPath)` returns a `SiteCorpus` with non-zero `Id` and correct fields |
| 2 | `GetSite_ReturnsCorrectSite` | After creating a site, `GetSiteAsync(name)` returns the same site with matching `Name`, `StartUrl`, `Namespace`, `OutputPath` |
| 3 | `GetSite_ReturnsNull_WhenNotFound` | `GetSiteAsync("nonexistent")` returns null |
| 4 | `ListSites_ReturnsAll` | After creating 3 sites, `ListSitesAsync()` returns a list of 3 |
| 5 | `StoreSnapshot_PersistsSnapshot` | After storing a `DomSnapshot`, `GetLatestSnapshotAsync(siteId, pageName)` returns it with correct `PageUrl`, `PageTitle`, `CapturedAt` |
| 6 | `StoreSnapshot_IndexesElements` | After storing a snapshot with 5 elements, `SearchElementsAsync(siteId, "input")` returns the indexed `<input>` elements |
| 7 | `StoreSnapshot_ReRecord_MarksOldAsHistory` | Store snapshot A for "LoginPage", then store snapshot B for "LoginPage" — `GetLatestSnapshotAsync` returns B; querying history shows A with `IsLatest = 0` |
| 8 | `GetLatestSnapshot_ReturnsLatest` | After storing two snapshots for the same page, only the one with `IsLatest = 1` is returned |
| 9 | `GetLatestSnapshot_ReturnsNull_WhenNoSnapshots` | `GetLatestSnapshotAsync(siteId, "NoSuchPage")` returns null |
| 10 | `ListSnapshots_ReturnsAllForSite` | After storing 3 snapshots for a site, `ListSnapshotsAsync(siteId)` returns 3 `SnapshotSummary` items |
| 11 | `SearchElements_ByTag` | After indexing elements, `SearchElementsAsync(siteId, "button")` returns all `<button>` elements across pages |
| 12 | `SearchElements_ByDataTestId` | After indexing elements with `DataTestId`, `SearchElementsAsync(siteId, "submit-btn")` returns the matching element |

## Notes

- Use in-memory SQLite (`DataSource=:memory:`) for isolated, fast tests. Each test creates a fresh connection.
- `CorpusService` constructor takes a connection string or `SqliteConnection` — pass the in-memory connection.
- Call `EnsureCreated()` or equivalent schema initialization before each test.
- Test class should implement `IDisposable` to close connections and call `SqliteConnection.ClearAllPools()`.
- `DomSnapshot` test instances should have a realistic `RootElement` tree (2–3 nested elements) with various attributes.
- Test 6: verify that individual elements are stored in the `Elements` table with correct `Tag`, `ElementId`, `DataTestId`, `ParentPath`.
- Test 7: verify `IsLatest` flag management — old snapshot should have `IsLatest = 0`, new should have `IsLatest = 1`.
- `SnapshotSummary` is a lightweight projection (no full `DomJson`) with `PageName`, `PageUrl`, `CapturedAt`, `ElementCount`, `IsLatest`.
- `ILogger<CorpusService>` dependency can use `NullLogger<CorpusService>.Instance`.
- No WPF dependencies — no STA thread required.
