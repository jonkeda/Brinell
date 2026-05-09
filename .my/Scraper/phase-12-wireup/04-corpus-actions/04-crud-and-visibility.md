# Step 12.W.4d — CRUD Operations & Visibility Management

## Objective

Address the issue where corpus pages are already stored in the database but don't show up in the screen. Design and implement CRUD (Create, Read, Update, Delete) functions to manage page visibility, stale data, and reconcile the UI collection with database state.

## Problem Statement

- **Orphaned Records**: Pages exist in the database but are not in the current `Pages` collection due to stale caches or improper initialization.
- **Stale Data**: A page's snapshots or versions may be updated in the DB, but the UI still displays old cached data.
- **No Delete Path**: Pages and snapshots can accumulate without a way to remove them.
- **Incomplete Refresh**: Simply re-loading `Pages` doesn't reflect changes to existing page objects without UI recreation.

## Dependencies

- `CorpusService` — database access for page and snapshot CRUD
- `CorpusTabViewModel.Pages` — observable collection displayed on screen
- `CorpusTabViewModel._siteId` — current site identifier
- `PageItemViewModel` — model wrapping a page with snapshots
- `SnapshotVersionViewModel` — model wrapping a snapshot version

## Design

### CRUD Operations (CorpusService)

| Operation | Method | Purpose |
|-----------|--------|---------|
| Create | `AddSnapshotAsync(siteId, pageName, pageUrl, html, ct)` | Insert new snapshot; create page if needed |
| Read | `ListPagesByIdAsync(siteId, ct)` | Fetch all pages for a site with metadata |
| Read | `GetSnapshotsByPageIdAsync(pageId, ct)` | Fetch all snapshots for a page |
| Read | `GetSnapshotByIdAsync(snapshotId, ct)` | Fetch a single snapshot (with HTML) |
| Update | `UpdatePageAsync(pageId, pageName, pageUrl, ct)` | Update page metadata |
| Update | `UpdateSnapshotStatusAsync(snapshotId, status, ct)` | Mark snapshot as processed/error |
| Delete | `DeleteSnapshotAsync(snapshotId, ct)` | Remove a single snapshot |
| Delete | `DeletePageAsync(pageId, ct)` | Remove page and all snapshots |
| Cleanup | `DeleteStaleSnapshotsAsync(siteId, olderThanDays, ct)` | Archive old snapshots |

### UI Reconciliation (CorpusTabViewModel)

#### 1. **Initial Load with Orphan Detection**

```csharp
public async Task LoadPagesAsync(CancellationToken ct)
{
    try
    {
        var dbPages = await _corpusService.ListPagesByIdAsync(_siteId, ct);
        var dbPageIds = new HashSet<string>(dbPages.Select(p => p.Id));

        // Remove orphaned UI pages (in Pages but not in DB)
        var orphaned = Pages
            .Where(p => !dbPageIds.Contains(p.PageId))
            .ToList();
        
        foreach (var orphan in orphaned)
        {
            Pages.Remove(orphan);
            _logger.LogWarning("Removed orphaned page '{Page}' from UI.", orphan.PageName);
        }

        // Add/update pages from DB
        foreach (var dbPage in dbPages)
        {
            var existing = Pages.FirstOrDefault(p => p.PageId == dbPage.Id);
            if (existing != null)
            {
                // Refresh snapshots for existing page
                await existing.RefreshSnapshotsAsync(ct);
            }
            else
            {
                // Add new page from DB
                var snapshots = await _corpusService.GetSnapshotsByPageIdAsync(dbPage.Id, ct);
                var pageVm = new PageItemViewModel(dbPage.Id, dbPage.Name, dbPage.Url, snapshots);
                Pages.Add(pageVm);
                _logger.LogInformation("Added page '{Page}' from database.", dbPage.Name);
            }
        }

        _logger.LogInformation("Loaded {Count} pages from database.", dbPages.Count);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load pages from database.");
        throw;
    }
}
```

#### 2. **Refresh Snapshots for a Single Page**

```csharp
public async Task RefreshPageAsync(PageItemViewModel page, CancellationToken ct)
{
    try
    {
        var snapshots = await _corpusService.GetSnapshotsByPageIdAsync(page.PageId, ct);
        page.UpdateSnapshots(snapshots);
        _logger.LogInformation("Refreshed snapshots for page '{Page}'.", page.PageName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to refresh snapshots for page '{Page}'.", page.PageName);
        throw;
    }
}
```

#### 3. **Delete Snapshot (with UI sync)**

```csharp
public async Task DeleteSnapshotAsync(PageItemViewModel page, SnapshotVersionViewModel snapshot, CancellationToken ct)
{
    if (await _dialogService.ConfirmAsync("Delete this snapshot permanently?") != true)
        return;

    try
    {
        await _corpusService.DeleteSnapshotAsync(snapshot.SnapshotId, ct);
        page.RemoveSnapshot(snapshot);
        _logger.LogInformation("Deleted snapshot {SnapshotId}.", snapshot.SnapshotId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete snapshot {SnapshotId}.", snapshot.SnapshotId);
        throw;
    }
}
```

#### 4. **Delete Page (with UI sync)**

```csharp
public async Task DeletePageAsync(PageItemViewModel page, CancellationToken ct)
{
    if (await _dialogService.ConfirmAsync($"Delete page '{page.PageName}' and all snapshots permanently?") != true)
        return;

    try
    {
        await _corpusService.DeletePageAsync(page.PageId, ct);
        Pages.Remove(page);
        if (SelectedPage == page)
            SelectedPage = null;

        _logger.LogInformation("Deleted page '{Page}' and all snapshots.", page.PageName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete page '{Page}'.", page.PageName);
        throw;
    }
}
```

### View Model Extensions

#### PageItemViewModel

```csharp
public class PageItemViewModel : ViewModelBase
{
    public string PageId { get; }
    public string PageName { get; }
    public string PageUrl { get; }
    
    private ObservableCollection<SnapshotVersionViewModel> _snapshots;
    public ObservableCollection<SnapshotVersionViewModel> Snapshots
    {
        get => _snapshots;
        private set => SetProperty(ref _snapshots, value);
    }

    // Refresh snapshots from database
    public async Task RefreshSnapshotsAsync(CancellationToken ct)
    {
        var refreshed = await _corpusService.GetSnapshotsByPageIdAsync(PageId, ct);
        UpdateSnapshots(refreshed);
    }

    // Replace snapshot collection (preserves UI items where IDs match)
    public void UpdateSnapshots(IEnumerable<SnapshotModel> newSnapshots)
    {
        var newIds = new HashSet<string>(newSnapshots.Select(s => s.Id));
        var existing = Snapshots.ToDictionary(s => s.SnapshotId);

        // Remove stale snapshots
        foreach (var stale in Snapshots.Where(s => !newIds.Contains(s.SnapshotId)).ToList())
            Snapshots.Remove(stale);

        // Add/update snapshots
        foreach (var snapshot in newSnapshots)
        {
            if (existing.TryGetValue(snapshot.Id, out var vm))
            {
                vm.UpdateFromModel(snapshot);
            }
            else
            {
                Snapshots.Add(new SnapshotVersionViewModel(snapshot));
            }
        }
    }

    public void RemoveSnapshot(SnapshotVersionViewModel snapshot)
    {
        Snapshots.Remove(snapshot);
    }
}
```

#### SnapshotVersionViewModel

```csharp
public class SnapshotVersionViewModel : ViewModelBase
{
    public string SnapshotId { get; }
    
    private string _pageName;
    public string PageName
    {
        get => _pageName;
        set => SetProperty(ref _pageName, value);
    }

    private DateTime _capturedAt;
    public DateTime CapturedAt
    {
        get => _capturedAt;
        set => SetProperty(ref _capturedAt, value);
    }

    private int _elementCount;
    public int ElementCount
    {
        get => _elementCount;
        set => SetProperty(ref _elementCount, value);
    }

    private SnapshotStatus _status;
    public SnapshotStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    // Update this VM from database model
    public void UpdateFromModel(SnapshotModel model)
    {
        PageName = model.PageName;
        CapturedAt = model.CapturedAt;
        ElementCount = model.ElementCount;
        Status = model.Status;
    }
}

public enum SnapshotStatus { Pending, Processed, Error, Archived }
```

## Implementation Plan

### Phase 1: Core CRUD (CorpusService) ✅ COMPLETE

- [x] Implement `ListPagesBySiteId` — fetch all distinct pages per site
- [x] Implement `GetSnapshotsByPageName` — fetch all snapshots for a page
- [x] Implement `DeletePageByName` — cascade delete all snapshots for a page
- [x] Implement `DeleteStaleSnapshots` — delete snapshots older than N days

**Implementation notes:**
- All CRUD methods are in `CorpusService.cs` with logging and transaction support
- `ListPagesBySiteId` returns `PageMetadata` (new model in `Models/PageMetadata.cs`)
- `DeletePageByName` performs cascade delete with Elements table cleanup
- Methods support error recovery via try/catch in calling code

### Phase 2: UI Reconciliation (CorpusTabViewModel) ✅ COMPLETE

- [x] Implement `LoadPagesWithReconciliationAsync` — orphan detection + add missing pages
- [x] Implement `RefreshPageAsync` — single page refresh from DB
- [x] Implement `DeleteSnapshotAsync` — UI + DB sync with confirmation
- [x] Implement `DeletePageAsync` — UI + DB sync with confirmation
- [x] Add CRUD command bindings (`RefreshPageCommand`, `DeleteSnapshotCommand`, `DeletePageCommand`)
- [x] Update `RefreshAsync` to use reconciliation with fallback

**Implementation notes:**
- `LoadPagesWithReconciliationAsync` detects orphaned pages and logs removal count
- `RefreshPageAsync` uses `UpdatePageSnapshots` to merge DB state without UI recreation
- Both delete operations show MessageBox confirmation dialogs
- Commands properly handle CanExecute state changes when selection changes

### Phase 3: View Model Updates ✅ COMPLETE

- [x] Add `UpdatePageSnapshots` to reconcile snapshot collections
- [x] Add `AddPageFromDatabaseAsync` to hydrate new pages from DB
- [x] Add properties and status management to `SnapshotVersionRow`
- [x] Create `PageMetadata` model for page-level metadata

**Implementation notes:**
- `UpdatePageSnapshots` preserves existing UI objects while updating properties
- Version numbering re-calculated on refresh to keep consistency
- `SnapshotVersionRow.VersionLabel` already existed; reused for display

### Phase 4: UI Wiring ✅ COMPLETE

- [x] Add "Refresh Page" button to toolbar (bind to `RefreshPageCommand`)
- [x] Add "Delete Page" button to toolbar (bind to `DeletePageCommand`)
- [x] Add "Delete Snapshot" button in DataGrid Actions column (bind to `DeleteSnapshotCommand`)
- [x] Add "Refresh Page" and "Delete Page" context menu items for page list
- [x] Update DeleteSnapshotCommand to accept SnapshotVersionRow parameter
- [x] Wire CommandParameter bindings in DataGrid template

**Implementation notes:**
- Toolbar buttons added after main Refresh button with tooltips
- Page context menu updated with two new entries
- DataGrid Delete button passes current row as CommandParameter
- All commands properly disabled when no selection

**Build Status:** ✅ Successful (no compilation errors)

## Code References

### CorpusService CRUD Methods
- `ListPagesBySiteId(siteId)` — returns `List<PageMetadata>`
- `GetSnapshotsByPageName(siteId, pageName)` — returns `List<SnapshotSummary>`
- `DeletePageByName(siteId, pageName)` — void, transaction-safe
- `DeleteStaleSnapshots(siteId, olderThanDays)` — returns count deleted

### CorpusTabViewModel Reconciliation Methods
- `LoadPagesWithReconciliationAsync(ct)` — full sync with orphan removal
- `RefreshPageAsync(page, ct)` — single page update from DB
- `DeleteSnapshotAsync(page, snapshot, ct)` — with confirmation dialog
- `DeletePageAsync(page, ct)` — with confirmation dialog
- `UpdatePageSnapshots(page, snapshots)` — private helper for smart merge
- `AddPageFromDatabaseAsync(dbPage, ct)` — private helper to hydrate new page

### New Commands
- `RefreshPageCommand` → calls `RefreshPageAsync` on `SelectedPage`
- `DeleteSnapshotCommand` → calls `DeleteSnapshotAsync` on `SelectedVersion`
- `DeletePageCommand` → calls `DeletePageAsync` on `SelectedPage`

## Testing Scenarios

1. **Orphan Cleanup**: 
   - Insert pages directly in DB
   - Call `LoadPagesWithReconciliationAsync`
   - Verify UI adds missing pages with correct snapshots

2. **Stale Data Refresh**:
   - Update a snapshot's metadata in DB (e.g., mark as historical)
   - Call `RefreshPageAsync`
   - Verify UI reflects version number changes

3. **Snapshot Delete**:
   - Select a non-latest snapshot
   - Click Delete
   - Confirm dialog appears, then deletes from DB and UI

4. **Page Delete**:
   - Select a page with multiple snapshots
   - Confirm deletion dialog shows correct count
   - All snapshots deleted from DB and UI removed

5. **Stale Snapshot Cleanup**:
   - Call `DeleteStaleSnapshots(siteId, 30)` via service directly
   - Verify old snapshots removed from UI on next refresh

## Status: FULLY IMPLEMENTED ✅

All four phases complete:
- **Phase 1** ✅: CRUD service methods (CorpusService.cs)
- **Phase 2** ✅: Visibility reconciliation (CorpusTabViewModel.cs)
- **Phase 3** ✅: View model updates (PageMetadata.cs)
- **Phase 4** ✅: UI command wiring (CorpusTabView.xaml)

**Build Status:** ✅ Successful (no compilation errors)

