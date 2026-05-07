# Step 12.5 — Page Objects Tab

## Objective

Dedicated workspace for managing generated PageObjects: list, status, generated code, validation results, and ControlObject usage per page.

## Dependencies

- Step 12.2 (Workspace shell), Step 12.4 (Control Objects tab — for cross-navigation)
- `PageGenerationService`, `IControlRegistry`, `CodeValidator`
- Phase 13.4 (`PipelineOrchestrator.GeneratePageObjectsAsync`)

## Implementation

### Files

- `Views/Tabs/PageObjectsTabView.xaml`
- `ViewModels/PageObjectsTabViewModel.cs`
- `Models/PageObjectListItem.cs`
- `Models/PageObjectPropertyItem.cs`
- `Models/ControlObjectReference.cs`
- `Models/ValidationEntry.cs`
- `Models/PageObjectStatus.cs`

### Models

```csharp
public enum PageObjectStatus { NotGenerated, Generated, Error }

public class PageObjectListItem : ViewModelBase
{
    public long SnapshotId { get; set; }
    public string PageName { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public int ElementCount { get; set; }
    public string ClassName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public PageObjectStatus Status { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string MainCode { get; set; } = "";
    public List<string> ContainerCodes { get; } = new();
    public ObservableCollection<PageObjectPropertyItem> Properties { get; } = new();
    public ObservableCollection<ControlObjectReference> UsedControlObjects { get; } = new();
    public ValidationResult? Validation { get; set; }
}

public class PageObjectPropertyItem
{
    public string Name { get; set; } = "";
    public string ControlType { get; set; } = "";
    public string Locator { get; set; } = "";
    public bool IsCustomControlObject { get; set; }
}

public class ControlObjectReference
{
    public string Name { get; set; } = "";
    public string DomSignature { get; set; } = "";
}

public class ValidationEntry
{
    public string Category { get; set; } = "";   // Syntax|Types|Locators|Compilation
    public string Severity { get; set; } = "";   // OK|Warning|Error
    public string Message { get; set; } = "";
}
```

### `PageObjectsTabViewModel`

```csharp
public class PageObjectsTabViewModel : ViewModelBase
{
    public ObservableCollection<PageObjectListItem> PageObjects { get; } = new();
    public ICollectionView FilteredPageObjects { get; }
    public PageObjectListItem? SelectedPageObject { get; set; }
    public string FilterText { get; set; } = "";

    public int TotalCount { get; }
    public int GeneratedCount { get; }
    public int PendingCount { get; }
    public int ErrorCount { get; }

    public IAsyncCommand GenerateAllCommand { get; }
    public IAsyncCommand RegenerateSelectedCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }

    public ICommand CopyCodeCommand { get; }
    public ICommand OpenSourcePageCommand { get; }     // → switch to Scraping tab
    public ICommand NavigateToControlObjectCommand { get; }  // → Control Objects tab

    public void LoadPageObjects(long siteId);
}
```

### Layout

```
DockPanel
├─ Toolbar (Top): [⚡ Generate All] [🔄 Regenerate Selected]
│                 [📤 Export] [📂 Open Output Folder]
└─ Grid (fill)
    ├─ Col0 (300px): Filter + ListBox (status icon | name | element count | status)
    │                + summary
    └─ Col1 (fill): Detail (ScrollViewer)
        1. Header card (class name, namespace, source URL+count, status, ctrl-obj count, generated date)
        2. Properties DataGrid (read-only) — Name | Type | Locator;
           IsCustomControlObject styled differently
        3. Generated Code (mono TextBox) + Expanders for container code blocks
           + [Copy] [Regenerate]
        4. Validation: list of ValidationEntry with category icons
        5. Used ControlObjects: ItemsControl with [Open in Control Objects tab] button
```

### Behavior

- `LoadPageObjects` joins `Snapshots` + `PageObjects` table (Phase 13.5) for the active site.
- Generate calls `PipelineOrchestrator.GeneratePageObjectsAsync` and updates list rows.
- Regenerate works per-snapshot.
- `OpenSourcePageCommand` raises an event handled by `WorkspaceViewModel` to switch to Scraping tab and load that page in the browser.
- `NavigateToControlObjectCommand` raises an event handled by `WorkspaceViewModel` to switch tabs and select the referenced control.
- Filter matches page name and URL.

### Context menu

- Generate / Regenerate / Copy Code / Open Source Page / Export Page Object / Delete

## Checklist

- [ ] Models added
- [ ] ViewModel exposes filtered list, selection, summary, all commands
- [ ] View has toolbar, left list, 5-section detail
- [ ] Validation result rendered with per-category icons
- [ ] Cross-tab navigation events wired through `WorkspaceViewModel`
- [ ] Context menu present
