# Step 4.10 — Snapshot Diff

## Objective

Compare two snapshots of the same page to see what changed — added, removed, and modified elements.

## Dependencies

- Step 4.8 (snapshot storage with history)
- Step 4.9 (corpus browser to trigger diff view)

## Implementation

### DomDiffService

```csharp
public sealed class DomDiffService
{
    public DomDiffResult Compare(DomSnapshot before, DomSnapshot after)
    {
        // Match elements by id > data-testid > name > structural path
        // Categorize: Added, Removed, Changed, Unchanged
    }
}

public class DomDiffResult
{
    public List<DomElement> Added { get; init; } = [];
    public List<DomElement> Removed { get; init; } = [];
    public List<DomElementChange> Changed { get; init; } = [];
    public int UnchangedCount { get; init; }
}
```

### Element matching priority

1. `id` attribute
2. `data-testid` attribute
3. `name` attribute
4. Structural path (tag + position in parent)

### Diff view

- Inline diff with color coding: green = added, red = removed, yellow = changed.
- Shows element-level changes with attribute-level detail.
- Accessible from Corpus Browser when a page has multiple snapshots.

## Checklist

- [ ] `DomDiffService` matches elements by id > data-testid > name > structural path
- [ ] Added, removed, and changed elements correctly categorized
- [ ] Diff view color-coded: green/red/yellow
- [ ] Attribute-level changes shown for modified elements
- [ ] Accessible from Corpus Browser when page has 2+ snapshots
