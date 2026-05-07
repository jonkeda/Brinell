# Step 5.6 — Analysis Pass (Phase 5A)

## Objective

The analyzer agent examines the full corpus to detect repeated UI patterns and propose custom controls. This runs before any code generation — the user must approve proposed controls before generation proceeds.

## Dependencies

- Step 5.1 (Copilot SDK with analyzer agent)
- Step 5.2 (`brinell-conventions` skill loaded)
- Step 5.3 (corpus query tools registered)
- Step 4.8 (SQLite corpus with stored snapshots)

## Implementation

### AnalysisService

```csharp
// Services/AnalysisService.cs
public sealed class AnalysisService
{
    private readonly ICopilotService _copilotService;
    private readonly CorpusService _corpusService;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        ICopilotService copilotService,
        CorpusService corpusService,
        ILogger<AnalysisService> logger)
    {
        _copilotService = copilotService;
        _corpusService = corpusService;
        _logger = logger;
    }

    public async Task<AnalysisResult> AnalyzeCorpusAsync(
        int siteId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var pages = await _corpusService.ListSnapshotsAsync(siteId);

        _logger.LogInformation(
            "Analysis started — Pages: {PageCount}", pages.Count);

        // Build corpus summary for the analyzer
        var prompt = BuildAnalysisPrompt(pages);

        // Send to analyzer agent — it will query corpus via tools
        var response = await _copilotService.AnalyzeAsync(prompt, ct);

        // Parse structured response
        var result = AnalysisResultParser.Parse(response);

        sw.Stop();
        _logger.LogInformation(
            "Analysis completed — Patterns found: {PatternCount}, " +
            "Custom controls proposed: {ControlCount}, Elapsed: {ElapsedMs} ms",
            result.LocatorReport is not null ? 1 : 0,
            result.ProposedControls.Count,
            sw.ElapsedMilliseconds);

        return result;
    }

    private static string BuildAnalysisPrompt(IReadOnlyList<SnapshotSummary> pages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Analyze the following site corpus for repeated UI patterns.");
        sb.AppendLine("Use the available tools to query the corpus.");
        sb.AppendLine();
        sb.AppendLine("## Instructions");
        sb.AppendLine();
        sb.AppendLine("1. Call `list_recorded_pages()` to see all available pages");
        sb.AppendLine("2. Call `get_page_snapshot(pageId)` for each page to inspect its DOM");
        sb.AppendLine("3. Call `find_similar_elements(selector)` to detect repeated patterns");
        sb.AppendLine("4. Call `get_generated_controls()` to see existing custom controls");
        sb.AppendLine();
        sb.AppendLine("## Expected Output");
        sb.AppendLine();
        sb.AppendLine("Return a JSON object with this schema:");
        sb.AppendLine("```json");
        sb.AppendLine("{");
        sb.AppendLine("  \"proposedControls\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"name\": \"PascalCaseControlName\",");
        sb.AppendLine("      \"domSignature\": \"css-like pattern\",");
        sb.AppendLine("      \"frequency\": 12,");
        sb.AppendLine("      \"confidence\": 92,");
        sb.AppendLine("      \"exampleSnippet\": \"<div>...</div>\",");
        sb.AppendLine("      \"suggestedProperties\": [\"Prop1\", \"Prop2\"]");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"locatorReport\": {");
        sb.AppendLine("    \"stableAttributes\": [\"data-testid\", \"aria-label\"],");
        sb.AppendLine("    \"unstableAttributes\": [\"id (dynamic on N pages)\"],");
        sb.AppendLine("    \"recommendations\": \"summary text\"");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine($"The corpus contains {pages.Count} recorded pages.");

        return sb.ToString();
    }
}
```

### AnalysisResult model

```csharp
// Models/AnalysisResult.cs
public sealed class AnalysisResult
{
    public List<ControlProposal> ProposedControls { get; init; } = [];
    public LocatorReport? LocatorReport { get; init; }
}

public sealed class ControlProposal
{
    public string Name { get; init; } = "";
    public string DomSignature { get; init; } = "";
    public int Frequency { get; init; }
    public int Confidence { get; init; }
    public string ExampleSnippet { get; init; } = "";
    public List<string> SuggestedProperties { get; init; } = [];
    public bool IsApproved { get; set; }
}

public sealed class LocatorReport
{
    public List<string> StableAttributes { get; init; } = [];
    public List<string> UnstableAttributes { get; init; } = [];
    public string Recommendations { get; init; } = "";
}
```

### AnalysisResultParser

```csharp
// Services/AnalysisResultParser.cs
public static class AnalysisResultParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static AnalysisResult Parse(string llmResponse)
    {
        // Extract JSON from markdown code fences if present
        var json = ExtractJson(llmResponse);

        if (json is null)
            return new AnalysisResult(); // empty result on parse failure

        return JsonSerializer.Deserialize<AnalysisResult>(json, JsonOptions)
            ?? new AnalysisResult();
    }

    private static string? ExtractJson(string response)
    {
        // Try to find JSON in ```json fences
        var match = Regex.Match(response, @"```json\s*\n(.*?)```",
            RegexOptions.Singleline);
        if (match.Success)
            return match.Groups[1].Value.Trim();

        // Try to find raw JSON object
        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start >= 0 && end > start)
            return response[start..(end + 1)];

        return null;
    }
}
```

### AnalysisViewModel (wire up existing stub)

```csharp
// ViewModels/AnalysisViewModel.cs
public sealed class AnalysisViewModel : ViewModelBase
{
    private readonly AnalysisService _analysisService;
    private readonly ILogger<AnalysisViewModel> _logger;

    public ObservableCollection<ControlProposal> ProposedControls { get; } = [];
    public LocatorReport? LocatorReport { get => _locatorReport; set => SetProperty(ref _locatorReport, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsAnalyzing { get => _isAnalyzing; set => SetProperty(ref _isAnalyzing, value); }

    public ICommand AnalyzeCommand { get; }
    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }
    public ICommand ApproveAllCommand { get; }
    public ICommand GenerateControlsCommand { get; }

    public async Task RunAnalysisAsync(int siteId, CancellationToken ct = default)
    {
        IsAnalyzing = true;
        StatusText = "Analyzing corpus...";

        try
        {
            var result = await _analysisService.AnalyzeCorpusAsync(siteId, ct);

            ProposedControls.Clear();
            foreach (var ctrl in result.ProposedControls)
                ProposedControls.Add(ctrl);

            LocatorReport = result.LocatorReport;
            StatusText = $"Found {result.ProposedControls.Count} control patterns";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            StatusText = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private void ApproveControl(ControlProposal proposal)
    {
        proposal.IsApproved = true;
        _logger.LogInformation("Control approved — Name: {ControlName}", proposal.Name);
    }

    private void RejectControl(ControlProposal proposal)
    {
        proposal.IsApproved = false;
        _logger.LogInformation("Control rejected — Name: {ControlName}", proposal.Name);
    }
}
```

## Checklist

- [ ] `AnalysisService.AnalyzeCorpusAsync()` sends corpus summary to analyzer agent
- [ ] Analyzer agent uses tools to query corpus (not inline data)
- [ ] Analysis prompt instructs LLM to return structured JSON
- [ ] `AnalysisResult` model with `ProposedControls` and `LocatorReport`
- [ ] `ControlProposal` includes: name, DOM signature, frequency, confidence, example snippet, suggested properties
- [ ] `LocatorReport` includes: stable/unstable attributes, recommendations
- [ ] `AnalysisResultParser` extracts JSON from markdown fences or raw response
- [ ] `AnalysisViewModel` presents results for user approval
- [ ] User can approve/reject individual controls
- [ ] Approve All button marks all proposals as approved
- [ ] Analysis start/completion logged with page count, pattern count, elapsed time
- [ ] Control approval/rejection logged with control name
