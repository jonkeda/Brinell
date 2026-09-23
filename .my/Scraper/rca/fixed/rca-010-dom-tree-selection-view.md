# RCA-010: DOM Tree — Filter and "Select All" Produce No Visible Change

**Reported:** 2026-04-22
**Severity:** Medium
**Component:** `ViewModels/DomTreeViewModel.cs`, `ViewModels/InspectorViewModel.cs`, `Views/DomTreePanel.xaml`

---

## Problem Statement

The DOM tree panel has three ways to narrow what's shown — the filter text box, "Select Forms", and "Select Inputs". None of them produce a visible result. The tree looks identical before and after. This makes the inspector panel feel broken.

---

## Architecture: DOM Tree Data Flow

```
DomCaptureService.CaptureAsync()
       │
       ▼
DomSnapshot { RootElement: DomElement (recursive tree) }
       │
       ▼
InspectorViewModel.LoadSnapshot()
       │
       ├─ _snapshot stored (source of truth)
       ├─ SelectedElements.Clear()
       ├─ TotalElementCount = CountElements()
       └─ DomTree.LoadSnapshot()
              │
              ▼
       DomTreeViewModel
              │
              ├─ _snapshot stored (for re-filtering)
              ├─ RootElements = [snapshot.RootElement]
              └─ FilterText / ShowFilteredByTags() → ApplyFilter()
                     │
                     ▼
              TreeView in DomTreePanel.xaml
                     │
                     ├─ ItemsSource="{Binding RootElements}"
                     ├─ HierarchicalDataTemplate with Children binding
                     └─ ItemContainerStyle → IsExpanded bound to IsFilterActive
```

**Key insight:** `RootElements` is an `ObservableCollection<DomElement>`. When its contents change, WPF re-renders the `TreeView`. But WPF `TreeViewItem.IsExpanded` defaults to `false`, so a pruned tree with fewer nodes still *looks* the same — a single collapsed root.

---

## Root Cause Analysis

### Issue 1 — Filtered tree stays collapsed

**Before fix:** `ApplyFilter()` replaces `RootElements` with a pruned tree. The filter logic is correct — it removes branches that don't match. But `TreeViewItem.IsExpanded` defaults to `false`:

```
Full tree (collapsed):                  Filtered tree (collapsed):
▶ html                                 ▶ html
                                        (looks identical)
```

The user types "input" and sees no change. They assume the filter is broken.

**What the user expects:**
```
Filtered tree (expanded):
▼ html
  ▼ body
    ▼ form
      ▶ input id="name"
      ▶ input id="email"
      ▶ input type="submit"
```

### Issue 2 — "Select Forms/Inputs" don't touch the tree

**Before fix:** `InspectorViewModel.SelectByTags()`:
```csharp
private void SelectByTags(DomElement root, string[] tags)
{
    SelectedElements.Clear();
    CollectByTags(root, tags, SelectedElements);
    // ← nothing tells DomTree to update
}
```

`SelectedElements` is populated (the status bar count updates), but `DomTreeViewModel` is unaware. The tree continues showing the full DOM.

### Issue 3 — "Clear" leaves a stale tree

**Before fix:** `ClearSelection()`:
```csharp
public void ClearSelection()
{
    SelectedElements.Clear();
    // ← tree still shows filtered/pruned view
}
```

If the user clicked "Select Forms" (which now prunes the tree), then "Clear", the tree stayed pruned instead of restoring the full DOM.

### Issue 4 — `FrameSource` lost during copy

`FilterElement()` creates new `DomElement` instances (because `DomElement` uses `init`-only properties). The `FrameSource` property (added in RCA-004 for iframe support) was not included in the copy, so filtered iframe elements lost their source URL.

---

## Implementation

### Step 1 — Add `IsFilterActive` property to `DomTreeViewModel`

```csharp
private bool _isFilterActive;

public bool IsFilterActive
{
    get => _isFilterActive;
    private set => SetProperty(ref _isFilterActive, value);
}
```

This property drives auto-expansion. Set to `true` when any filter is active, `false` when showing the full tree.

**Set in three places:**
- `ApplyFilter()` — `true` when `_filterText` is non-empty, `false` when cleared
- `ShowFilteredByTags()` — always `true`
- `LoadSnapshot()` — always `false` (full tree, no filter)

### Step 2 — Auto-expand `TreeViewItem` when filtered

In `DomTreePanel.xaml`, add an `ItemContainerStyle` that binds `IsExpanded` to `IsFilterActive`:

```xml
<TreeView.ItemContainerStyle>
    <Style TargetType="TreeViewItem">
        <Setter Property="IsExpanded" 
                Value="{Binding DataContext.IsFilterActive, 
                        RelativeSource={RelativeSource AncestorType=TreeView}}"/>
    </Style>
</TreeView.ItemContainerStyle>
```

**Why `RelativeSource AncestorType=TreeView`?** Each `TreeViewItem`'s `DataContext` is a `DomElement` (from the `HierarchicalDataTemplate`). But `IsFilterActive` lives on the `DomTreeViewModel`, which is the `TreeView`'s `DataContext`. So we walk up to the `TreeView` to reach it.

**Behavior:**
| `IsFilterActive` | Effect |
|---|---|
| `false` | All nodes collapsed (normal browsing) |
| `true` | All nodes expanded (user sees filtered results immediately) |

### Step 3 — Add `ShowFilteredByTags()` to `DomTreeViewModel`

```csharp
public void ShowFilteredByTags(string[] tags)
{
    if (_snapshot is null) return;
    _filterText = string.Empty;
    OnPropertyChanged(nameof(FilterText));  // Clear filter box in UI
    IsFilterActive = true;
    RootElements.Clear();

    var filtered = FilterElementByTags(_snapshot.RootElement, tags);
    if (filtered is not null)
        RootElements.Add(filtered);
}
```

This prunes the tree to only show branches that contain elements matching the given tags. It clears the filter text box (so the user doesn't see a stale text filter while viewing a tag filter).

`FilterElementByTags()` is a recursive method similar to `FilterElement()` but matches by tag name instead of free text:

```csharp
private static DomElement? FilterElementByTags(DomElement element, string[] tags)
{
    var matches = Array.Exists(tags, t => 
        t.Equals(element.Tag, StringComparison.OrdinalIgnoreCase));
    var filteredChildren = new List<DomElement>();

    foreach (var child in element.Children)
    {
        var filtered = FilterElementByTags(child, tags);
        if (filtered is not null)
            filteredChildren.Add(filtered);
    }

    if (!matches && filteredChildren.Count == 0)
        return null;

    return new DomElement { /* all properties copied including FrameSource */ };
}
```

### Step 4 — Wire `SelectByTags` to update the tree

```csharp
private void SelectByTags(DomElement root, string[] tags)
{
    SelectedElements.Clear();
    CollectByTags(root, tags, SelectedElements);
    DomTree.ShowFilteredByTags(tags);  // ← new: prune tree to show matching elements
}
```

Now clicking "Select Forms" both populates `SelectedElements` (for the status bar count) and prunes the tree (for visual feedback).

### Step 5 — Wire `ClearSelection` to restore the tree

```csharp
public void ClearSelection()
{
    SelectedElements.Clear();
    if (_snapshot is not null)
        DomTree.LoadSnapshot(_snapshot);  // ← new: restore full tree
}
```

`LoadSnapshot` sets `IsFilterActive = false`, clears filter text, and shows the full DOM tree.

### Step 6 — Copy `FrameSource` in both filter methods

Both `FilterElement()` and `FilterElementByTags()` now include:
```csharp
FrameSource = element.FrameSource,
```

### Step 7 — Reset filter state on new snapshot

`LoadSnapshot()` now resets filter state:
```csharp
public void LoadSnapshot(DomSnapshot snapshot)
{
    _snapshot = snapshot;
    _filterText = string.Empty;
    OnPropertyChanged(nameof(FilterText));  // Clear filter box
    IsFilterActive = false;                 // Collapse tree
    RootElements.Clear();
    RootElements.Add(snapshot.RootElement);
}
```

---

## User Interaction Flow

```
 ┌─────────────────────────────────────────────────────────┐
 │                 DOM Tree Panel                          │
 │                                                         │
 │  🔎 [___filter text___]                                │
 │                                                         │
 │  ▼ html        ← full tree, collapsed by default       │
 │                                                         │
 ├─────────── User types "input" ──────────────────────────┤
 │                                                         │
 │  🔎 [input____________]                                │
 │                                                         │
 │  ▼ html        ← pruned + auto-expanded                │
 │    ▼ body                                               │
 │      ▼ form                                             │
 │        input id="name" class="form-control"             │
 │        input id="email"                                 │
 │        input type="submit"                              │
 │                                                         │
 ├─────────── User clears filter ──────────────────────────┤
 │                                                         │
 │  🔎 [________________]                                 │
 │                                                         │
 │  ▶ html        ← full tree restored, collapsed          │
 │                                                         │
 ├─────────── User clicks "Select Forms" ──────────────────┤
 │                                                         │
 │  🔎 [________________]  (filter box cleared)           │
 │                                                         │
 │  ▼ html        ← pruned to form elements, expanded     │
 │    ▼ body                                               │
 │      ▼ form                                             │
 │        input id="name"                                  │
 │        select id="country"                              │
 │        textarea id="bio"                                │
 │        button type="submit"                             │
 │                                                         │
 │  Status bar: "4 selected │ DOM: 342 elements"           │
 │                                                         │
 ├─────────── User clicks "Clear" ─────────────────────────┤
 │                                                         │
 │  ▶ html        ← full tree restored                     │
 │                                                         │
 │  Status bar: "0 selected │ DOM: 342 elements"           │
 └─────────────────────────────────────────────────────────┘
```

---

## Files Changed

| File | Change |
|---|---|
| `ViewModels/DomTreeViewModel.cs` | Added `IsFilterActive` property, `ShowFilteredByTags()`, `FilterElementByTags()`. `LoadSnapshot()` resets filter state. `FrameSource` copied in both filter methods. |
| `ViewModels/InspectorViewModel.cs` | `SelectByTags()` calls `DomTree.ShowFilteredByTags()`. `ClearSelection()` calls `DomTree.LoadSnapshot()`. |
| `Views/DomTreePanel.xaml` | Added `ItemContainerStyle` binding `TreeViewItem.IsExpanded` to `IsFilterActive`. |

## Status

- [x] `IsFilterActive` property added to `DomTreeViewModel`
- [x] `TreeViewItem.IsExpanded` bound to `IsFilterActive` — auto-expands when filtered
- [x] `ShowFilteredByTags()` added — prunes tree to matching tag branches
- [x] "Select Forms" and "Select Inputs" update the tree via `ShowFilteredByTags`
- [x] "Clear" restores full tree via `LoadSnapshot`
- [x] `FrameSource` copied in `FilterElement` and `FilterElementByTags`
- [x] All 94 tests passing
