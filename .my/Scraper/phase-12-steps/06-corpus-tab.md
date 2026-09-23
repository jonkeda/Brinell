# Step 12.6 — Corpus Tab

## Objective

Replace `CorpusBrowserView` with a full corpus management workspace: page grouping, version history, snapshot diff, DOM preview, element stats, and per-page generation status.

## Dependencies

- Step 12.2 (Workspace shell)
- Existing `CorpusService`, `DiffWindow`, `DomTreePanel`

## Implementation

### Files

- `Views/Tabs/CorpusTabView.xaml`
- `ViewModels/CorpusTabViewModel.cs`
- `Models/CorpusPageGroup.cs`
- `Models/SnapshotVersionRow.cs`

### Models

```csharp
public class CorpusPageGroup : ViewModelBase
{
    public string PageName { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public ObservableCollection<SnapshotVersionRow> Versions { get; } = new();
    public SnapshotVersionRow LatestSnapshot => Versions[0];
    public int TotalElements => LatestSnapshot.ElementCount;

    // Generation status (for left-list icons)
    public bool HasControlObjects { get; set; }
    public bool ControlObjectsPending { get; set; }
    public PageObjectStatus PageObjectStatus { get; set; }
}

public class SnapshotVersionRow : ViewModelBase
{
    public long SnapshotId { get; set; }
    public int VersionNumber { get; set; }   // 1, 2, 3 ...
    public bool IsLatest { get; set; }
    public DateTime CapturedAt { get; set; }
    public int ElementCount { get; set; }
    public long SnapshotSizeBytes { get; set; }
    public bool HasPageObject { get; set; }
}
```

### `CorpusTabViewModel`

```csharp
public class CorpusTabViewModel : ViewModelBase
{
    public ObservableCollection<CorpusPageGroup> Pages { get; } = new();
    public ICollectionView FilteredPages { get; }
    public CorpusPageGroup? SelectedPage { get; set; }
    public SnapshotVersionRow? SelectedVersion { get; set; }
    public string FilterText { get; set; } = "";

    // Totals
    public int TotalPages { get; }
    public int TotalSnapshots { get; }
    public int TotalElements { get; }
    public long TotalSizeBytes { get; }

    // Toolbar
    public IAsyncCommand ReRecordAllCommand { get; }
    public IAsyncCommand RefreshCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand DeleteSelectedCommand { get; }

    // Per-version
    public ICommand ViewVersionCommand { get; }
    public ICommand CompareCommand { get; }            // (v1, v2) → DiffWindow
    public IAsyncCommand GeneratePageObjectCommand { get; }

    // Per-page context
    public IAsyncCommand ReRecordPageCommand { get; }
    public ICommand ExportPageCommand { get; }
    public ICommand DeleteAllVersionsCommand { get; }
    public ICommand OpenInBrowserCommand { get; }      // → Scraping tab

    public void Load(long siteId);
}
```

### Layout

```
DockPanel
├─ Toolbar (Top): [📷 Re-Record All] [🔄 Refresh] [📤 Export]
│                 [📥 Import] [🗑 Delete Selected]
└─ Grid (fill)
    ├─ Col0 (300px): Filter + ListBox of CorpusPageGroup
    │   - icon | page name | "{N} elem" | "{N} vers."
    │   - extra status icons: control-objects (✅/⏳/—), page-object (✅/⏳/❌)
    │   - totals block at bottom
    └─ Col1 (fill): Detail (ScrollViewer)
        1. Page header (name, URL, version count, latest date)
        2. Version History DataGrid: Version | Date | Elems | Size | [View]
                                    | HasPageObject | [Generate]
           + [Compare v↔v] buttons
        3. DOM Preview: hosts existing DomTreePanel for SelectedVersion
        4. Element Stats: tag breakdown, with-id/with-class counts,
                          stable vs unstable locator estimate
```

### Behavior

- `Load(siteId)` queries `CorpusService` for snapshots grouped by `PageName`, ordered by `CapturedAt DESC`.
- Generation-status flags joined from `Controls` (signature match) and `PageObjects` (Phase 13.5).
- `CompareCommand` opens existing `DiffWindow` with the two selected version IDs.
- `OpenInBrowserCommand` raises event → `WorkspaceViewModel` switches to Scraping tab and navigates the browser to `PageUrl`.
- Element stats computed from snapshot DOM (count by tag, id/class presence, locator stability heuristic).

## Checklist

- [ ] Models added
- [ ] ViewModel exposes pages, versions, totals, all commands
- [ ] View has 4-section detail panel (header, versions table, DOM preview, stats)
- [ ] Version table shows page-object status + Generate button per version
- [ ] Compare opens existing DiffWindow
- [ ] Re-record / Open-in-browser cross-tab events wired
- [ ] Filter matches name and URL
