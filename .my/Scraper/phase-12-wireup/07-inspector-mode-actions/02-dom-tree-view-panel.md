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

    [RelayCommand]
    private async Task HoverNode(DomElementNode node)
    {
        HoveredElement = node.Element;
        // Highlight in browser via JS
        var script = $"window.__brinellHighlight('{node.Element.SelectorPath}');";
        if (ExecuteJsRequested is not null)
            await ExecuteJsRequested(script);
    }

    [RelayCommand]
    private async Task SelectNode(DomElementNode node)
    {
        // Scroll browser to element
        var script = $"document.querySelector('{node.Element.SelectorPath}')?.scrollIntoView({{block:'center'}});";
        if (ExecuteJsRequested is not null)
            await ExecuteJsRequested(script);
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

1. User enables Inspect mode → `DomCaptureService.CaptureAsync` is called
2. Snapshot loaded into `InspectorViewModel.LoadSnapshot`
3. Tree view appears in right panel
4. Hovering tree node → highlight element in browser (blue)
5. Clicking tree node → scroll browser to that element

## Checklist

- [ ] Inspector panel appears when inspect mode enabled
- [ ] DOM tree renders full hierarchy
- [ ] Filter text box narrows visible nodes
- [ ] Hovering tree node highlights element in browser
- [ ] Clicking tree node scrolls browser to element
- [ ] Element count shown in status area
