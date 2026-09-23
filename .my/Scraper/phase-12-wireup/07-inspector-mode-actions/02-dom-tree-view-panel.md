# Step 12.W.7b — Wire DOM Tree View Panel

## Objective

Wire the DOM Inspector tree view panel to display the captured DOM snapshot as a hierarchical tree, with hover-highlight sync to the browser and click-to-scroll behavior.

## Dependencies

- `DomCaptureService.CaptureAsync(webView)` → `DomSnapshot`
- `InspectorViewModel` (or create) — owns tree data and selection
- `BrowserViewModel.IsInspectMode` (from step 07a)
- `CoreWebView2.ExecuteScriptAsync` for highlight/scroll commands

## Implementation

### Files

| File | Action |
|------|--------|
| `ViewModels/InspectorViewModel.cs` | Create — holds `DomSnapshot`, `FilterText`, `TreeRoot`, hover/select logic |
| `Views/InspectorPanel.xaml` | Create — TreeView with filter TextBox |
| `ScrapingTabViewModel.cs` | Expose `Inspector` property, trigger capture on inspect mode |

### Code sketch

**InspectorViewModel.cs:**

```csharp
public sealed partial class InspectorViewModel : ViewModelBase
{
    private readonly ILogger<InspectorViewModel> _logger;

    [ObservableProperty] private DomSnapshot? _snapshot;
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private DomElement? _hoveredElement;
    [ObservableProperty] private int _totalElementCount;

    public ObservableCollection<DomElementNode> TreeRoot { get; } = [];

    public event Func<string, Task>? ExecuteJsRequested;

    public void LoadSnapshot(DomSnapshot snapshot)
    {
        Snapshot = snapshot;
        TotalElementCount = CountElements(snapshot.RootElement);
        TreeRoot.Clear();
        TreeRoot.Add(DomElementNode.FromElement(snapshot.RootElement));
    }

    partial void OnFilterTextChanged(string value)
    {
        // Apply filter to tree nodes — collapse non-matching, expand matching
        ApplyFilter(TreeRoot, value);
    }

    // Frame index: -1 = top frame, >=0 = index into TrackedFrames
    public event Func<string, int, Task>? ExecuteJsRequested;

    [RelayCommand]
    private async Task HoverNode(DomElementNode node)
    {
        HoveredElement = node.Element;
        // Highlight in browser via JS — route to correct frame for iframe elements.
        var script = $"window.__brinellHighlight('{node.Element.SelectorPath}');";
        var frameIndex = node.FrameIndex; // -1 for top frame, >=0 for an iframe
        if (ExecuteJsRequested is not null)
            await ExecuteJsRequested(script, frameIndex);
    }

    [RelayCommand]
    private async Task SelectNode(DomElementNode node)
    {
        // Scroll browser to element — must target the correct frame.
        var script = $"document.querySelector('{node.Element.SelectorPath}')?.scrollIntoView({{block:'center'}});";
        var frameIndex = node.FrameIndex;
        if (ExecuteJsRequested is not null)
            await ExecuteJsRequested(script, frameIndex);
    }
}
```

**InspectorPanel.xaml** (key structure):

```xml
<DockPanel>
    <TextBox DockPanel.Dock="Top" Text="{Binding FilterText, UpdateSourceTrigger=PropertyChanged}"
             Watermark="Filter by tag, id, class…" Margin="4"/>
    <TextBlock DockPanel.Dock="Bottom" Text="{Binding TotalElementCount, StringFormat='{}{0} elements'}"
              Margin="4" FontSize="11" Foreground="Gray"/>
    <TreeView ItemsSource="{Binding TreeRoot}">
        <TreeView.ItemTemplate>
            <HierarchicalDataTemplate ItemsSource="{Binding Children}">
                <TextBlock Text="{Binding DisplayText}"/>
            </HierarchicalDataTemplate>
        </TreeView.ItemTemplate>
    </TreeView>
</DockPanel>
```

### Interaction flow

1. User enables Inspect mode → `DomCaptureService.CaptureAsync(webView, _highlight.TrackedFrames)` is called
   - Pass `TrackedFrames` so cross-origin iframe DOM is merged into the snapshot
2. Snapshot loaded into `InspectorViewModel.LoadSnapshot`
   - Assign `FrameIndex` to each `DomElementNode` during tree construction:
     - Top-frame elements: `FrameIndex = -1`
     - Elements inside the Nth cross-origin iframe (merged by `CaptureFramesAsync`): `FrameIndex = N`
3. Tree view appears in right panel (iframe subtrees shown as children of their `<iframe>` node)
4. Hovering tree node → highlight element in browser (blue)
   - `ExecuteJsRequested(script, frameIndex)` is routed by `ScrapingTabViewModel`:
     - `frameIndex == -1` → `webView.ExecuteScriptAsync(script)`
     - `frameIndex >= 0` → `_highlight.TrackedFrames[frameIndex].ExecuteScriptAsync(script)`
5. Clicking tree node → scroll browser to that element (same frame routing as above)

## Checklist

- [ ] Inspector panel appears when inspect mode enabled
- [ ] `DomCaptureService.CaptureAsync` called with `_highlight.TrackedFrames`
- [ ] DOM tree renders full hierarchy, including merged cross-origin iframe subtrees
- [ ] `DomElementNode.FrameIndex` set correctly during tree construction (-1 = top, N = iframe)
- [ ] Filter text box narrows visible nodes
- [ ] Hovering tree node highlights element in correct frame (main or iframe)
- [ ] Clicking tree node scrolls browser to element in correct frame
- [ ] Element count shown in status area
