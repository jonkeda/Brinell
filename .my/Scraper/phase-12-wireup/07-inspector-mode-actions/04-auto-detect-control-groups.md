# Step 12.W.7d — Wire Auto-Detect Control Groups

## Objective

Wire the control group detection heuristics so that after a DOM capture, the system automatically identifies forms, tables, lists, and nav regions and presents them as suggestions the user can accept or reject.

## Dependencies

- `InspectorViewModel` (from steps 07b/07c) — owns snapshot and selection
- `ControlGroupDetector` service (implements detection heuristics from phase-04 step 4.5)
- `DomSnapshot` loaded after capture

## Implementation

### Files

| File | Action |
|------|--------|
| `Services/ControlGroupDetector.cs` | Create or verify — detection heuristics |
| `ViewModels/InspectorViewModel.cs` | Add `DetectedGroups`, accept/reject logic |
| `Views/InspectorPanel.xaml` | Add suggestion list below tree or as popup |

### Code sketch

**ControlGroupDetector.cs:**

```csharp
public sealed class ControlGroupDetector
{
    public IReadOnlyList<DetectedGroup> Detect(DomSnapshot snapshot)
    {
        var groups = new List<DetectedGroup>();

        foreach (var el in Flatten(snapshot.RootElement))
        {
            if (el.Tag == "form")
                groups.Add(new DetectedGroup("FormContainer", el, GetFormChildren(el)));
            else if (el.Tag == "table" && HasTheadAndTbody(el))
                groups.Add(new DetectedGroup("TableContainer", el, GetTableCells(el)));
            else if (el.Tag is "ul" or "ol" && el.Children.Count >= 2)
                groups.Add(new DetectedGroup("ListContainer", el, el.Children));
            else if (el.Tag == "nav")
                groups.Add(new DetectedGroup("NavigationContainer", el, GetLinks(el)));
            else if (el.Tag == "fieldset")
                groups.Add(new DetectedGroup(GetLegendName(el), el, el.Children));
            else if (el.Role is "dialog" or "form" or "tablist")
                groups.Add(new DetectedGroup($"{el.Role}Container", el, el.Children));
        }

        return groups;
    }
}

public sealed record DetectedGroup(string SuggestedName, DomElement Container, IReadOnlyList<DomElement> Children);
```

**InspectorViewModel.cs:**

```csharp
public ObservableCollection<DetectedGroupViewModel> DetectedGroups { get; } = [];

public void RunAutoDetection()
{
    if (Snapshot is null) return;

    var groups = _controlGroupDetector.Detect(Snapshot);
    DetectedGroups.Clear();

    foreach (var g in groups)
    {
        DetectedGroups.Add(new DetectedGroupViewModel
        {
            Name = g.SuggestedName,
            Container = g.Container,
            Children = g.Children,
            IsAccepted = null // pending
        });
    }

    _logger.LogInformation("Auto-detected {Count} control groups", groups.Count);
}

[RelayCommand]
private void AcceptGroup(DetectedGroupViewModel group)
{
    group.IsAccepted = true;
    // Add all children to SelectedElements
    foreach (var child in group.Children)
        if (!SelectedElements.Contains(child))
            SelectedElements.Add(child);
    OnPropertyChanged(nameof(SelectionStatus));
}

[RelayCommand]
private void RejectGroup(DetectedGroupViewModel group)
{
    group.IsAccepted = false;
}
```

### UI — Suggestion banner

After DOM capture, if groups are detected, show a banner:

```
╔═══════════════════════════════════════════════╗
║ Found 2 forms, 1 navigation, 1 table         ║
║ [Accept All]  [Review Individually]  [Dismiss]║
╚═══════════════════════════════════════════════╝
```

"Review Individually" expands the list with per-group Accept/Reject buttons.

## Checklist

- [ ] `ControlGroupDetector` identifies forms, tables, lists, navs, fieldsets, role-based containers
- [ ] Detection runs automatically after DOM capture completes
- [ ] Suggestion banner shows detected group summary
- [ ] User can accept/reject each group individually
- [ ] Accepted groups auto-add their child elements to selection
- [ ] "Accept All" bulk-accepts all detected groups
