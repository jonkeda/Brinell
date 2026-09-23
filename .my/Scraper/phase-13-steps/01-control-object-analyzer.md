# Step 13.1 — ControlObject Analyzer

## Objective

Implement `ControlObjectAnalyzer` — the two-phase service that scans the corpus for repeating DOM patterns and proposes ControlObjects with confidence scores and a `LocatorReport`.

## Dependencies

- Phase 4 (DOM capture, `CorpusService`)
- Phase 5 (existing `ControlGroupDetector`, `AnalysisResultParser`, `PromptBuilder`)
- Step 13.8 (`ICopilotService` for Phase B LLM calls)

## Implementation

### Files

- `Services/ControlObjectAnalyzer.cs`
- `Models/ControlObjectAnalysisResult.cs`
- Update: `Services/PromptBuilder.cs` (add `BuildControlObjectAnalysisPrompt`)

### Result model

```csharp
public class ControlObjectAnalysisResult
{
    public List<ControlProposal> Proposals { get; set; } = new();
    public LocatorReport? LocatorReport { get; set; }
    public int LocalGroupCount { get; set; }
    public int SnapshotsAnalyzed { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
```

### Service skeleton

```csharp
public class ControlObjectAnalyzer
{
    private readonly ControlGroupDetector _detector;
    private readonly CorpusService _corpus;
    private readonly ICopilotService _copilot;
    private readonly AnalysisResultParser _parser;
    private readonly PromptBuilder _prompts;
    private readonly ILogger<ControlObjectAnalyzer> _logger;

    public async Task<ControlObjectAnalysisResult> AnalyzeAsync(
        long siteId, CancellationToken ct = default)
    {
        // Phase A — local detection
        var snapshots = await _corpus.GetLatestSnapshotsAsync(siteId, ct);
        var localGroups = new List<(DomSnapshot snap, List<ControlGroupSuggestion> groups)>();
        foreach (var s in snapshots)
            localGroups.Add((s, _detector.Detect(s.RootElement)));

        var aggregated = AggregatePatterns(localGroups);

        // Phase B — LLM cross-page analysis
        var prompt = _prompts.BuildControlObjectAnalysisPrompt(siteId, aggregated, snapshots);
        var response = await _copilot.AnalyzeAsync(prompt, ct);
        var (proposals, locatorReport) = _parser.ParseControlObjectAnalysis(response);

        return new ControlObjectAnalysisResult
        {
            Proposals = proposals,
            LocatorReport = locatorReport,
            LocalGroupCount = aggregated.Count,
            SnapshotsAnalyzed = snapshots.Count,
        };
    }

    private List<AggregatedPattern> AggregatePatterns(
        List<(DomSnapshot, List<ControlGroupSuggestion>)> input)
    {
        // Key by structural signature: tag + class set + child tag chain
        // Count frequency across pages, collect example snippet, page IDs
    }
}
```

### Aggregation rules

| Match | Behavior |
|---|---|
| Exact: same tag + class set + immediate child tag sequence | Same pattern; bump frequency |
| Fuzzy: same tag + ≥80% class overlap + same first 3 child tags | Same pattern; mark `IsFuzzy=true` |
| Otherwise | Distinct pattern |

`AggregatedPattern` carries: `Signature`, `Frequency`, `PageIds[]`, `ExampleHtml`, `LocalSuggestions[]`.

### `BuildControlObjectAnalysisPrompt`

Inputs: site name, `aggregated`, `snapshots`.

Prompt structure (see Phase 13 spec §13.6 "ControlObject Analysis Prompt"):

- Site context + page count
- Available tools list (`list_recorded_pages`, `get_page_snapshot`, `find_similar_elements`)
- Pre-aggregated patterns formatted as compact JSON
- Required JSON response schema:
  ```json
  {
    "proposedControls": [
      { "name": "...", "domSignature": "...", "frequency": 0,
        "confidence": 0, "exampleSnippet": "...",
        "suggestedProperties": [{"name":"","controlType":"","selector":""}] }
    ],
    "locatorReport": { "stableAttributes": [], "unstableAttributes": [], "recommendations": "" }
  }
  ```

### Persistence

- Persist `ControlObjectAnalysisResult` to new `AnalysisResults` table (Step 13.7).
- Re-running analysis upserts a row keyed by `(SiteId, AnalyzedAt)` and updates "current" pointer.

### DI registration

```csharp
services.AddSingleton<ControlObjectAnalyzer>();
```

## Checklist

- [ ] `ControlObjectAnalysisResult` model added
- [ ] `ControlObjectAnalyzer` runs Phase A locally then Phase B via `ICopilotService.AnalyzeAsync`
- [ ] Aggregation merges exact + fuzzy matches and tracks frequency
- [ ] `PromptBuilder.BuildControlObjectAnalysisPrompt` produces prompt matching schema
- [ ] `AnalysisResultParser` parses JSON response into proposals + locator report
- [ ] Result is persisted in `AnalysisResults` table
- [ ] Service registered in DI
- [ ] Logging: `Analyzing site {SiteId}: {SnapshotCount} pages, {LocalGroups} local groups`
