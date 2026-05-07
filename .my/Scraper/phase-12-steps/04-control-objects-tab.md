# Step 12.4 — Control Objects Tab

## Objective

Replace the minimal `ControlsManagerView` with a full workspace for analyzing, approving, generating, editing, and inspecting Brinell ControlObjects.

## Dependencies

- Step 12.2 (Workspace shell)
- `IControlRegistry`, `AnalysisService`, `ControlGenerationService`
- Phase 13.1 (`ControlObjectAnalyzer` — when wired) and Phase 13.4 (`PipelineOrchestrator`)

## Implementation

### Files

- `Views/Tabs/ControlObjectsTabView.xaml`
- `ViewModels/ControlObjectsTabViewModel.cs`
- `Models/ControlObjectListItem.cs`
- `Models/ControlPropertyItem.cs`
- `Models/ControlObjectStatus.cs`

### Models

```csharp
public enum ControlObjectStatus { Pending, Approved, Rejected }

public class ControlObjectListItem : ViewModelBase
{
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    public int Confidence { get; set; }
    public ControlObjectStatus Status { get; set; }
    public string DomSignature { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Code { get; set; } = "";
    public string ExampleSnippet { get; set; } = "";
    public ObservableCollection<ControlPropertyItem> Properties { get; } = new();
    public int UsedByPageCount { get; set; }
}

public class ControlPropertyItem : ViewModelBase
{
    public string Name { get; set; } = "";
    public string ControlType { get; set; } = "";
    public string Selector { get; set; } = "";
}
```

### `ControlObjectsTabViewModel`

```csharp
public class ControlObjectsTabViewModel : ViewModelBase
{
    public ObservableCollection<ControlObjectListItem> ControlObjects { get; } = new();
    public ICollectionView FilteredControlObjects { get; }
    public ControlObjectListItem? SelectedControlObject { get; set; }

    public string FilterText { get; set; } = "";

    // Summary
    public int TotalCount { get; }
    public int ApprovedCount { get; }
    public int PendingCount { get; }
    public int RejectedCount { get; }

    // Toolbar commands
    public IAsyncCommand AnalyzeCorpusCommand { get; }
    public IAsyncCommand GenerateAllPendingCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }

    // Per-item commands
    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }
    public IAsyncCommand RegenerateCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CopyCodeCommand { get; }

    public void LoadControlObjects(long siteId);
}
```

### Layout

```
DockPanel
├─ Toolbar (Top): [🔬 Analyze Corpus] [⚡ Generate All Pending]
│                 [📥 Import] [📤 Export]
└─ Grid (fill)
    ├─ Col0 (300px): Left panel
    │   - Filter TextBox
    │   - ListBox (FilteredControlObjects):
    │       icon (status) | name | "{confidence}%" | status label
    │   - Summary block at bottom (Total/Approved/Pending/Rejected)
    └─ Col1 (fill): Detail (ScrollViewer)
        - Header card: name, namespace, confidence ProgressBar,
          status icon, DOM signature (mono), created date
        - Properties DataGrid (Name | Type | Locator) — editable + Add/Remove
        - Generated Code: read-only TextBox (mono) + [Copy] [Regenerate]
        - DOM Preview: read-only TextBox (mono) bound to ExampleSnippet
```

### Behavior

- `LoadControlObjects` pulls approved controls from `IControlRegistry` plus pending proposals from latest analysis result (Phase 13.1).
- `AnalyzeCorpusCommand` calls `PipelineOrchestrator.AnalyzeForControlObjectsAsync(siteId)` and merges new proposals into the list.
- `GenerateAllPendingCommand` invokes `PipelineOrchestrator.GenerateControlObjectsAsync(approved)`.
- Approve/Reject mutates `Status` and persists to `AnalysisResults` table.
- Regenerate re-runs LLM for the single proposal, updating `Code`.
- Filter is case-insensitive over name and DOM signature.

### Context menu on items

- Approve / Reject / Regenerate / Copy Code / Delete

## Checklist

- [ ] Models added (`ControlObjectListItem`, `ControlPropertyItem`, `ControlObjectStatus`)
- [ ] ViewModel exposes filtered list, selection, summary counts, all commands
- [ ] View has toolbar, left list, detail with 4 sections (header, properties, code, DOM)
- [ ] Approve/Reject/Regenerate/Delete wired with persistence
- [ ] Analyze/Generate buttons call pipeline orchestrator
- [ ] Context menu present
