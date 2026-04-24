# Step 4.4 — Multi-Select Mode

## Objective

Let users pick multiple elements that should become control properties on the generated PageObject.

## Dependencies

- Step 4.3 (DOM tree view)
- Step 4.2 (browser overlay for Ctrl+click)

## Implementation

### Two selection modes

1. **TreeView checkboxes** — each `TreeViewItem` has a `CheckBox` in its template. Checked items are added to the selected set.
2. **Browser overlay Ctrl+click** — hold Ctrl and click elements in the browser to add/remove from selection. Selected elements get a persistent green border overlay.

### Selected elements collection

- `ObservableCollection<DomElement> SelectedElements` on the `InspectorViewModel`.
- Badge count shown in status bar: `4 selected │ DOM: 342 elements`.

### Bulk selection buttons

- **Select All Forms** — select all `<input>`, `<select>`, `<textarea>`, `<button>` elements.
- **Select All Inputs** — select only `<input>` elements.
- **Clear Selection** — deselect all.

## Checklist

- [ ] TreeView nodes have checkboxes that add/remove from SelectedElements
- [ ] Ctrl+click in browser toggles element selection (green border)
- [ ] Browser selection syncs with TreeView checkboxes
- [ ] Status bar shows selected count and total element count
- [ ] Select All Forms/Inputs buttons work correctly
- [ ] Clear Selection deselects all elements
