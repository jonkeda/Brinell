# Step 13.6 — Pipeline Orchestrator

## Objective

Implement `PipelineOrchestrator` — the single entry point that the UI tabs call to drive the full Scrape → Analyze → Approve → Generate Controls → Generate Pages → Output flow.

## Dependencies

- Step 13.1 (`ControlObjectAnalyzer`)
- Step 13.2 (`ControlGenerationService.GenerateAllApprovedAsync`)
- Step 13.3 (`SkillService`)
- Step 13.4 (`PageGenerationService`)
- Step 13.5 (`ControlObjectMatcher`)
- Phase 7 (`CodeOutputService`)

## Implementation

### Files

- `Services/PipelineOrchestrator.cs`
- `Models/PipelineProgress.cs`

### Service

```csharp
public class PipelineOrchestrator
{
    private readonly ControlObjectAnalyzer _controlAnalyzer;
    private readonly ControlGenerationService _controlGenerator;
    private readonly PageGenerationService _pageGenerator;
    private readonly SkillService _skillService;
    private readonly CodeOutputService _codeOutput;
    private readonly CorpusService _corpus;
    private readonly IControlRegistry _registry;
    private readonly SiteService _sites;
    private readonly ILogger<PipelineOrchestrator> _logger;

    public IProgress<PipelineProgress>? Progress { get; set; }

    public Task<ControlObjectAnalysisResult> AnalyzeForControlObjectsAsync(
        long siteId, CancellationToken ct = default)
        => _controlAnalyzer.AnalyzeAsync(siteId, ct);

    public async Task<List<GeneratedControl>> GenerateControlObjectsAsync(
        long siteId, List<ControlProposal> approved,
        string targetNamespace, LocatorReport? locator,
        CancellationToken ct = default)
    {
        Report("Generating control objects", 0, approved.Count);
        var controls = await _controlGenerator.GenerateAllApprovedAsync(
            approved, targetNamespace, locator, ct);

        var site = await _sites.GetAsync(siteId, ct);
        await _skillService.GenerateSiteControlsSkillAsync(siteId, site.Slug, ct);
        return controls;
    }

    public async Task<List<PageGenerationResult>> GeneratePageObjectsAsync(
        long siteId, string targetNamespace, LocatorReport? locator,
        CancellationToken ct = default)
    {
        var snapshots = await _corpus.GetLatestSnapshotsAsync(siteId, ct);
        var results = new List<PageGenerationResult>();
        for (int i = 0; i < snapshots.Count; i++)
        {
            Report($"Generating page object: {snapshots[i].PageName}", i, snapshots.Count);
            var result = await _pageGenerator.GeneratePageAsync(
                snapshots[i], targetNamespace, locator, ct: ct);
            await _corpus.StorePageObjectAsync(result, ct);
            results.Add(result);
        }
        return results;
    }

    public async Task OutputAsync(
        long siteId, string outputPath, string targetNamespace,
        CancellationToken ct = default)
    {
        var controls = await _registry.GetControlsAsync(siteId, ct);
        var pages = await _corpus.GetPageObjectsAsync(siteId, ct);
        await _codeOutput.WriteProjectAsync(outputPath, targetNamespace, controls, pages, ct);
    }

    public async Task RunFullPipelineAsync(
        long siteId, string targetNamespace, string outputPath,
        Func<List<ControlProposal>, Task<List<ControlProposal>>> approvalGate,
        CancellationToken ct = default)
    {
        var analysis = await AnalyzeForControlObjectsAsync(siteId, ct);
        var approved = await approvalGate(analysis.Proposals);
        await GenerateControlObjectsAsync(siteId, approved, targetNamespace,
            analysis.LocatorReport, ct);
        await GeneratePageObjectsAsync(siteId, targetNamespace, analysis.LocatorReport, ct);
        await OutputAsync(siteId, outputPath, targetNamespace, ct);
    }

    private void Report(string stage, int current, int total) =>
        Progress?.Report(new PipelineProgress(stage, current, total));
}

public record PipelineProgress(string Stage, int Current, int Total);
```

### State machine (UI-visible)

```
Empty → CorpusReady → ProposalsPending → ControlsGenerated
      → PagesGenerated → OutputComplete
```

The orchestrator does not own state — UI tabs (12.4, 12.5) drive transitions and persist intermediate state via `AnalysisResults` and `PageObjects` tables (Step 13.7).

### DI registration

```csharp
services.AddSingleton<PipelineOrchestrator>();
```

### UI consumers

| Tab | Method called |
|---|---|
| Control Objects → Analyze Corpus | `AnalyzeForControlObjectsAsync` |
| Control Objects → Generate All Pending | `GenerateControlObjectsAsync` |
| Page Objects → Generate All | `GeneratePageObjectsAsync` |
| Page Objects → Open Output Folder (after) | `OutputAsync` |

## Checklist

- [ ] `PipelineOrchestrator` exposes per-stage methods + full pipeline runner
- [ ] Skill regeneration runs after every control generation pass
- [ ] Page generation persists results via `CorpusService.StorePageObjectAsync`
- [ ] `IProgress<PipelineProgress>` exposed for UI status reporting
- [ ] Approval gate is a delegate, not a UI dependency
- [ ] Logging at each stage start/end with counts and elapsed time
- [ ] Service registered in DI
