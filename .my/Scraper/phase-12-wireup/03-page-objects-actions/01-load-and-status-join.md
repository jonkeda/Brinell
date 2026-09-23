# Step 12.W.1 — Load Page Objects with Status Join

## Objective

Fix `LoadPageObjects(siteId)` so that it no longer marks every row as `NotGenerated`. Instead, it should fetch the actual generation records from the corpus database via `CorpusService.GetPageObjects(siteId)` and join them by `SnapshotId` onto the existing snapshot list, populating status, timestamp, code, and control-object references for each row.

## Dependencies

- `CorpusService.GetPageObjects(siteId)` → `IReadOnlyList<PageObjectRecord>`
- `CorpusService.GetPageObjectBySnapshot(snapshotId)` → `PageObjectRecord?`
- `PageObjectListItem` model (already exists in VM)
- Snapshot list already loaded from site corpus

## Implementation

### Files

| Action | Path |
|--------|------|
| Modify | `Brinell.Scraper/ViewModels/PageObjectsTabViewModel.cs` |

### Code sketch

```csharp
private async Task LoadPageObjects(string siteId)
{
    var snapshots = _corpusService.GetSnapshots(siteId);
    var pageObjects = _corpusService.GetPageObjects(siteId);

    // Build lookup by SnapshotId for O(1) join
    var poLookup = pageObjects.ToDictionary(po => po.SnapshotId);

    var items = snapshots.Select(snap =>
    {
        var item = new PageObjectListItem
        {
            SnapshotId = snap.Id,
            PageTitle = snap.Title,
            Url = snap.Url
        };

        if (poLookup.TryGetValue(snap.Id, out var record))
        {
            item.Status = record.Status; // Generated | Error
            item.GeneratedAt = record.GeneratedAt;
            item.MainCode = record.Code;
            item.UsedControlObjects = record.UsedControlObjects;
        }
        else
        {
            item.Status = PageObjectStatus.NotGenerated;
        }

        return item;
    }).ToList();

    PageObjects = new ObservableCollection<PageObjectListItem>(items);
    OnPropertyChanged(nameof(PageObjects));
}
```

When the selected item changes, populate `ControlObjectReferences` for the detail pane:

```csharp
partial void OnSelectedPageObjectChanged(PageObjectListItem? value)
{
    if (value?.UsedControlObjects is { Count: > 0 } refs)
    {
        ControlObjectReferences = new ObservableCollection<string>(refs);
    }
    else
    {
        ControlObjectReferences = new ObservableCollection<string>();
    }
}
```

### Behavior

- On tab activation or site change, `LoadPageObjects` fetches both snapshot list and page-object records in one pass.
- Rows whose `SnapshotId` matches a `PageObjectRecord` display `Generated` (green) or `Error` (red) status plus the `GeneratedAt` timestamp.
- Rows with no matching record display `NotGenerated` (grey).
- Selecting a row with `UsedControlObjects` populates the detail pane's control-object reference list.
- If `CorpusService.GetPageObjects` throws, log the error and fall back to marking all rows `NotGenerated` (graceful degradation).

## Checklist

- [ ] Replace stub body of `LoadPageObjects` with join logic
- [ ] Add `ControlObjectReferences` observable collection property
- [ ] Wire `OnSelectedPageObjectChanged` to populate references
- [ ] Verify `PageObjectListItem` has `MainCode`, `UsedControlObjects`, `GeneratedAt` properties (add if missing)
- [ ] Confirm UI bindings for Status column render correctly for all three states
- [ ] Run unit tests for VM with mocked `CorpusService`
