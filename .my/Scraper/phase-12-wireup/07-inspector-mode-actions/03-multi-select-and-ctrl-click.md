# Step 12.W.7c — Wire Multi-Select & Ctrl+Click

## Objective

Wire multi-select mode so users can pick elements via TreeView checkboxes or Ctrl+click in the browser, building the `SelectedElements` collection that feeds code generation.

## Dependencies

- `InspectorViewModel` (from step 07b) — owns tree and snapshot
- `CoreWebView2.WebMessageReceived` — receives Ctrl+click messages from overlay JS
- Phase-04 Step 4.4 multi-select design

## Implementation

### Files

| File | Action |
|------|--------|
| `ViewModels/InspectorViewModel.cs` | Add `SelectedElements`, selection toggle logic, bulk actions |
| `Resources/inspect-overlay.js` | Extend — post `elementSelected` message on Ctrl+click |
| `ScrapingTabViewModel.cs` | Subscribe `WebMessageReceived`, route to inspector |

### Code sketch

**InspectorViewModel.cs:**

```csharp
public ObservableCollection<DomElement> SelectedElements { get; } = [];

public string SelectionStatus => $"{SelectedElements.Count} selected │ DOM: {TotalElementCount} elements";

[RelayCommand]
private void ToggleSelection(DomElementNode node)
{
    node.IsSelected = !node.IsSelected;
    if (node.IsSelected)
        SelectedElements.Add(node.Element);
    else
        SelectedElements.Remove(node.Element);

    OnPropertyChanged(nameof(SelectionStatus));
    // Update green highlight in browser
    _ = UpdateBrowserSelectionHighlights();
}

public void OnBrowserElementSelected(string selectorPath)
{
    var node = FindNodeByPath(TreeRoot, selectorPath);
    if (node is not null)
        ToggleSelection(node);
}

[RelayCommand]
private void SelectAllForms()
{
    SelectByPredicate(el => el.Tag is "input" or "select" or "textarea" or "button");
}

[RelayCommand]
private void SelectAllInputs()
{
    SelectByPredicate(el => el.Tag == "input");
}

[RelayCommand]
private void ClearSelection()
{
    foreach (var node in GetAllNodes(TreeRoot))
        node.IsSelected = false;
    SelectedElements.Clear();
    OnPropertyChanged(nameof(SelectionStatus));
    _ = UpdateBrowserSelectionHighlights();
}
```

**ScrapingTabViewModel.cs** — WebMessage routing:

```csharp
private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
{
    var msg = JsonSerializer.Deserialize<JsMessage>(e.WebMessageAsJson);
    if (msg?.Type == "elementSelected")
    {
        Inspector.OnBrowserElementSelected(msg.SelectorPath);
    }
}
```

**inspect-overlay.js** — Ctrl+click handler:

```javascript
document.addEventListener('click', (e) => {
    if (!e.ctrlKey || !window.__brinellInspectActive) return;
    e.preventDefault();
    e.stopPropagation();
    const path = generateSelectorPath(e.target);
    window.chrome.webview.postMessage({ type: 'elementSelected', selectorPath: path });
}, true);
```

### UI elements

- TreeView nodes get CheckBox in their template
- Status bar: `"4 selected │ DOM: 342 elements"`
- Toolbar buttons: Select All Forms, Select All Inputs, Clear Selection

## Checklist

- [ ] TreeView checkboxes toggle selection
- [ ] Ctrl+click in browser toggles element selection (green highlight)
- [ ] Browser selection syncs with TreeView checkboxes bidirectionally
- [ ] Status bar shows selected count and total element count
- [ ] "Select All Forms" / "Select All Inputs" bulk actions work
- [ ] "Clear Selection" deselects all and removes green highlights
