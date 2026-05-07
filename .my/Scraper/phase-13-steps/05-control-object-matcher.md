# Step 13.5 — ControlObject Matcher

## Objective

Implement `ControlObjectMatcher` — given a page snapshot and the site's registered ControlObjects, identify which DOM elements should be expressed as references to existing controls (vs. inline containers or direct controls).

## Dependencies

- Phase 4 (`DomSnapshot`, `DomElement`)
- Step 13.2 (`IControlRegistry` populated)

## Implementation

### Files

- `Services/ControlObjectMatcher.cs`
- `Models/ControlObjectMatch.cs`
- `Services/CssSignatureParser.cs` (helper)

### Models

```csharp
public class ControlObjectMatch
{
    public DomElement Element { get; set; } = default!;
    public GeneratedControl Control { get; set; } = default!;
    public double Score { get; set; }     // 0..1
    public string Reason { get; set; } = "";
}
```

### Service

```csharp
public class ControlObjectMatcher
{
    private readonly CssSignatureParser _parser;

    public List<ControlObjectMatch> MatchAll(
        DomSnapshot snapshot, IReadOnlyList<GeneratedControl> controls)
    {
        var matches = new List<ControlObjectMatch>();
        foreach (var element in WalkActionable(snapshot.RootElement))
            foreach (var control in controls)
            {
                var score = ScoreMatch(element, control);
                if (score >= 0.75)
                    matches.Add(new ControlObjectMatch { Element = element,
                        Control = control, Score = score,
                        Reason = $"Matched signature {control.DomSignature} ({score:P0})" });
            }

        // Deduplicate: per element, keep highest-scoring match
        return matches
            .GroupBy(m => m.Element.XPath)
            .Select(g => g.OrderByDescending(m => m.Score).First())
            .ToList();
    }

    private double ScoreMatch(DomElement element, GeneratedControl control)
    {
        var signature = _parser.Parse(control.DomSignature);
        if (!TagMatches(element, signature)) return 0;
        var classScore = ClassOverlap(element, signature);   // 0..1
        var childScore = ChildStructureMatch(element, signature);   // 0..1
        return 0.4 * (TagMatches(element, signature) ? 1 : 0)
             + 0.3 * classScore
             + 0.3 * childScore;
    }
}
```

### `CssSignatureParser`

Parses simple CSS-like signatures used in `GeneratedControl.DomSignature`:

| Token | Meaning |
|---|---|
| `tag` | Tag name match |
| `.class` | Class must be present |
| `#id` | ID must equal |
| `[attr]` | Attribute presence |
| `[attr=value]` | Attribute equals value |
| `>` | Direct child |
| `+` | Adjacent sibling |
| ` ` (space) | Descendant |

Output: `ParsedSignature { Tag, Classes[], Id?, Attributes[], Children[] }`.

### Scoring rules

| Signal | Weight |
|---|---|
| Tag match (required) | 0.4 |
| Class set overlap (Jaccard) | 0.3 |
| First-3-children tag chain match | 0.3 |

Threshold for match: ≥ 0.75.

### Walk policy

Only consider elements in `actionable` set (form, table, nav, fieldset, div with `role`, custom widget classes), to avoid scoring every `<span>`.

### DI registration

```csharp
services.AddSingleton<CssSignatureParser>();
services.AddSingleton<ControlObjectMatcher>();
```

## Checklist

- [ ] `ControlObjectMatch` model added
- [ ] `CssSignatureParser` handles tag, class, id, attribute, child combinator
- [ ] `MatchAll` returns one best match per element above threshold
- [ ] Threshold (0.75) and weights are constants/settings
- [ ] Walk restricted to actionable elements
- [ ] Deterministic across runs (same snapshot → same matches)
- [ ] Services registered in DI
- [ ] Unit tests cover: tag-only, class-overlap, child-chain, no-match cases
