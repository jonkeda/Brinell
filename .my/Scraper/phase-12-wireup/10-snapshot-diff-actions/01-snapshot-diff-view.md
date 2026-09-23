# Step 12.W.10 — Wire Snapshot Diff View

## Objective

Wire the snapshot diff view so that from the Corpus Browser, selecting a page with multiple snapshots enables a "View Diff" action that opens a color-coded comparison of two snapshot versions.

## Dependencies

- `DomDiffService.Compare(DomSnapshot before, DomSnapshot after)` → `DomDiffResult`
- `CorpusService.GetSnapshotById(id)` — loads full snapshot for comparison
- `CorpusTabViewModel.SelectedPage` — page with multiple versions
- `SnapshotVersionViewModel` — version items with `SnapshotId`

## Implementation

### Files

| File | Action |
|------|--------|
| `Services/DomDiffService.cs` | Verify exists — element matching + diff result |
| `ViewModels/DiffViewModel.cs` | Create — holds diff result for display |
| `Views/DiffView.xaml` | Create — color-coded tree diff display |
| `CorpusTabViewModel.cs` | Add `ViewDiffCommand`, open diff for two versions |

### Code sketch

**DomDiffService.cs** (verify/create):

```csharp
public sealed class DomDiffService
{
    public DomDiffResult Compare(DomSnapshot before, DomSnapshot after)
    {
        var result = new DomDiffResult();
        MatchAndDiff(before.RootElement, after.RootElement, result);
        return result;
    }

    private void MatchAndDiff(DomElement before, DomElement after, DomDiffResult result)
    {
        // Match by: id > data-testid > name > structural path (tag + index)
        // Categorize each element as Added, Removed, Changed, or Unchanged
    }
}

public sealed class DomDiffResult
{
    public List<DomElement> Added { get; init; } = [];
    public List<DomElement> Removed { get; init; } = [];
    public List<DomElementChange> Changed { get; init; } = [];
    public int UnchangedCount { get; init; }
    public string Summary => $"+{Added.Count} −{Removed.Count} ~{Changed.Count} ({UnchangedCount} unchanged)";
}

public sealed class DomElementChange
{
    public DomElement Before { get; init; } = default!;
    public DomElement After { get; init; } = default!;
    public List<string> ChangedAttributes { get; init; } = [];
}
```

**DiffViewModel.cs:**

```csharp
public sealed partial class DiffViewModel : ViewModelBase
{
    [ObservableProperty] private DomDiffResult? _diffResult;
    [ObservableProperty] private string _beforeLabel = "";
    [ObservableProperty] private string _afterLabel = "";

    public ObservableCollection<DiffLineViewModel> DiffLines { get; } = [];

    public void LoadDiff(DomSnapshot before, DomSnapshot after, DomDiffResult result)
    {
        BeforeLabel = $"{before.PageName} ({before.CapturedAt:g})";
        AfterLabel = $"{after.PageName} ({after.CapturedAt:g})";
        DiffResult = result;

        DiffLines.Clear();
        foreach (var added in result.Added)
            DiffLines.Add(new DiffLineViewModel(added, DiffKind.Added));
        foreach (var removed in result.Removed)
            DiffLines.Add(new DiffLineViewModel(removed, DiffKind.Removed));
        foreach (var changed in result.Changed)
            DiffLines.Add(new DiffLineViewModel(changed.After, DiffKind.Changed, changed.ChangedAttributes));
    }
}

public enum DiffKind { Added, Removed, Changed }
```

**CorpusTabViewModel.cs — ViewDiff command:**

```csharp
[RelayCommand(CanExecute = nameof(CanViewDiff))]
private async Task ViewDiffAsync()
{
    if (SelectedPage is null) return;

    var versions = SelectedPage.Versions;
    if (versions.Count < 2) return;

    // Compare latest two versions
    var before = await _corpusService.GetSnapshotById(versions[^2].SnapshotId);
    var after = await _corpusService.GetSnapshotById(versions[^1].SnapshotId);

    if (before is null || after is null) return;

    var result = _domDiffService.Compare(before, after);

    Diff.LoadDiff(before, after, result);
    IsDiffViewVisible = true;
}

private bool CanViewDiff => SelectedPage?.Versions.Count >= 2;
```

### Diff view color coding

| Color | Meaning |
|-------|---------|
| Green background | Added element |
| Red background | Removed element |
| Yellow background | Changed element (attribute differences shown inline) |

### DiffView.xaml (key structure):

```xml
<DockPanel>
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="8">
        <TextBlock Text="{Binding BeforeLabel}" FontWeight="Bold"/>
        <TextBlock Text=" → " Margin="4,0"/>
        <TextBlock Text="{Binding AfterLabel}" FontWeight="Bold"/>
    </StackPanel>
    <TextBlock DockPanel.Dock="Top" Text="{Binding DiffResult.Summary}" Margin="8,0"/>
    <ListView ItemsSource="{Binding DiffLines}">
        <ListView.ItemTemplate>
            <DataTemplate>
                <Border Background="{Binding DiffColor}" Padding="4,2">
                    <TextBlock Text="{Binding DisplayText}"/>
                </Border>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</DockPanel>
```

## Checklist

- [ ] "View Diff" button enabled when page has 2+ snapshot versions
- [ ] `DomDiffService` matches elements by id > data-testid > name > structural path
- [ ] Added, removed, changed elements correctly categorized
- [ ] Diff view shows color-coded results (green/red/yellow)
- [ ] Attribute-level changes shown for modified elements
- [ ] Summary line shows counts (+added −removed ~changed)
- [ ] Before/after labels show page name and capture timestamp
