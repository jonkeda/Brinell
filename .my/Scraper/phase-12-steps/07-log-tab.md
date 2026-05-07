# Step 12.7 — Log Tab

## Objective

Promote the existing `LogViewerPanel` to a full tab. Underlying `LogViewerViewModel` is unchanged — just full vertical space and a tab-local toolbar.

## Dependencies

- Step 12.2 (Workspace shell)
- Existing `LogViewerViewModel`, `LogEntry`, sink wiring

## Implementation

### Files

- `Views/Tabs/LogTabView.xaml` (UserControl wrapping existing `LogViewerPanel`)

### Layout

```
DockPanel
├─ Toolbar (Top):
│   - Level filter ComboBox (All | Trace | Debug | Info | Warn | Error)
│   - Search TextBox (filters Message)
│   - [Clear] [Export]
│   - Auto-scroll CheckBox
├─ Status (Bottom): "{Total} entries | Showing: {Filtered} | Auto-scroll: {On/Off}"
└─ DataGrid (fill):
    Columns: Timestamp | Level | Source | Message
    - Row coloring by level (Warn=yellow, Error=red, others=default)
    - Selectable rows; Ctrl+C copies row text
```

### ViewModel additions (if not already present)

```csharp
// On LogViewerViewModel
public string SearchText { get; set; }     // filters FilteredEntries
public LogLevel LevelFilter { get; set; }
public bool AutoScroll { get; set; } = true;
public int FilteredCount { get; }

public ICommand ClearCommand { get; }
public ICommand ExportCommand { get; }     // CSV or .log to disk
```

- Add the missing properties/commands to existing `LogViewerViewModel` if absent.
- `FilteredEntries` ICollectionView with combined predicate (Level + SearchText).

### Behavior

- DataGrid `ItemsSource` bound to `FilteredEntries`.
- When `AutoScroll=true` and a new entry arrives, scroll to bottom.
- Export writes a CSV file via SaveFileDialog.
- Clear empties the underlying entries collection.

## Checklist

- [ ] `LogTabView` UserControl created (no new VM — reuse `LogViewerViewModel`)
- [ ] Toolbar exposes Level filter, search, Clear, Export, Auto-scroll
- [ ] Status line shows total + filtered counts + auto-scroll state
- [ ] Row coloring by level
- [ ] Auto-scroll behavior preserved
- [ ] Export to CSV works
