# RCA-008: DOM Tree View — Filtering and Hover Highlight Don't Work

**Reported:** 2026-04-22
**Severity:** Medium
**Component:** `ViewModels/DomTreeViewModel.cs`, `Views/DomTreePanel.xaml`, `MainWindow.xaml`

---

## Symptoms

1. **Filtering doesn't work** — typing in the filter box has no visible effect on the tree.
2. **Hover-to-highlight doesn't work** — hovering over a tree node does not highlight the corresponding element in the browser.

## Root Cause

### Issue 1 — DomTreePanel DataContext Is Set to `DomTreeViewModel`, Not `InspectorViewModel`

The `DomTreePanel` in `MainWindow.xaml` has its DataContext bound to `Inspector.DomTree`:

**File:** `MainWindow.xaml`
```xml
<views:DomTreePanel Grid.Column="2" Width="300"
                    DataContext="{Binding Inspector.DomTree}"
                    .../>
```

The `DomTreePanel.xaml` binds the filter TextBox to `FilterText`:
```xml
<TextBox Text="{Binding FilterText, UpdateSourceTrigger=PropertyChanged}" .../>
```

This binding is correct — `DomTreeViewModel.FilterText` exists and `ApplyFilter()` runs when it changes. **However**, `ApplyFilter()` creates new `DomElement` copies via `FilterElement()` — it reconstructs the tree with only matching elements. The problem is that `DomElement` uses `init`-only properties:

**File:** `Models/DomElement.cs`
```csharp
public sealed class DomElement
{
    public string Tag { get; init; } = "";
    public List<DomElement> Children { get; init; } = [];
}
```

The `FilterElement` method creates new `DomElement` instances:
```csharp
return new DomElement
{
    Tag = element.Tag,
    // ... copies all properties
    Children = filteredChildren  // ← new list of filtered children
};
```

This should work for filtering the tree contents. The likely issue is that the `DomElement.FrameSource` property (added in RCA-004) is **not being copied** in `FilterElement`:

**File:** `ViewModels/DomTreeViewModel.cs`, lines 55–70
```csharp
return new DomElement
{
    Tag = element.Tag,
    Id = element.Id,
    ClassName = element.ClassName,
    Name = element.Name,
    Type = element.Type,
    DataTestId = element.DataTestId,
    Role = element.Role,
    AriaLabel = element.AriaLabel,
    Placeholder = element.Placeholder,
    TextContent = element.TextContent,
    BoundingBox = element.BoundingBox,
    Children = filteredChildren
    // FrameSource is MISSING — won't cause filter failure though
};
```

The actual filter issue is more subtle: the `RootElements` collection is updated (`Clear` + `Add`), which should trigger the TreeView to re-render. **The most likely cause is that the TreeView's `ItemsSource` binding is working but the tree is collapsed by default**, and the user doesn't notice the filtered results because all nodes start collapsed. The filter removes non-matching branches, so if the matching nodes are deep in the tree, they appear as a collapsed root with fewer children — which looks identical to the unfiltered tree.

**Alternate theory:** If `UpdateSourceTrigger=PropertyChanged` is not taking effect due to a WPF binding quirk with the filter TextBox losing focus, the filter setter may never fire.

### Issue 2 — No Hover-to-Highlight Wiring

There is **no code** that connects tree node hover events to browser element highlighting. The DOM tree panel is a plain TreeView with no `MouseEnter`/`MouseOver` event handlers on its items. The `ElementHighlightService` only injects hover behavior in the browser itself (via `mousemove` on `document`), not from the tree panel.

**File:** `Views/DomTreePanel.xaml` — no mouse event handlers on tree items
**File:** `Views/DomTreePanel.xaml.cs` — no hover event handling code

To highlight a browser element from the tree, the code would need to:
1. Detect which `DomElement` the mouse is over in the TreeView
2. Use the element's `BoundingBox` to position the overlay in the browser
3. Call `webView.ExecuteScriptAsync()` to move the highlight overlay to those coordinates

## Fix

### Issue 1 — Auto-Expand Filtered Tree + Copy FrameSource

1. Add `FrameSource` to the `FilterElement` copy:
```csharp
return new DomElement
{
    // ... existing properties ...
    FrameSource = element.FrameSource,
    Children = filteredChildren
};
```

2. Auto-expand all nodes when a filter is active. WPF TreeView expansion can be controlled via a style setter:
```xml
<TreeView.ItemContainerStyle>
    <Style TargetType="TreeViewItem">
        <Setter Property="IsExpanded" Value="{Binding DataContext.IsFilterActive,
                RelativeSource={RelativeSource AncestorType=UserControl}}"/>
    </Style>
</TreeView.ItemContainerStyle>
```

Add `IsFilterActive` property to `DomTreeViewModel`:
```csharp
public bool IsFilterActive => !string.IsNullOrWhiteSpace(_filterText);
```

### Issue 2 — Implement Tree-to-Browser Hover Highlighting

This requires a communication path from the DomTreePanel back to the BrowserView's WebView2. Options:

**Option A — Event on InspectorViewModel:**
```csharp
// InspectorViewModel
public event Action<BoundingBox?>? HighlightRequested;

public void RequestHighlight(DomElement? element)
{
    HighlightRequested?.Invoke(element?.BoundingBox);
}
```

Wire in MainWindow/MainViewModel to call `webView.ExecuteScriptAsync()` with JS that positions the overlay at the given bounding box coordinates.

**Option B — Direct JS call via BrowserViewModel accessor:**
Add a `HighlightElement(BoundingBox box)` method on `ElementHighlightService` that moves the overlay to specific coordinates.

Add mouse event handling to the TreeView items:
```xml
<TreeView.ItemContainerStyle>
    <Style TargetType="TreeViewItem">
        <EventSetter Event="MouseEnter" Handler="OnTreeItemMouseEnter"/>
        <EventSetter Event="MouseLeave" Handler="OnTreeItemMouseLeave"/>
    </Style>
</TreeView.ItemContainerStyle>
```

## Status

- [ ] `FrameSource` copied in `FilterElement`
- [ ] Filtered tree nodes auto-expanded
- [ ] Filter verified to update tree visually
- [ ] Tree node hover triggers browser element highlight
- [ ] Hover leave clears the browser highlight
