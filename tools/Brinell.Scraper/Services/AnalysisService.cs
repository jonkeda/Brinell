using System.Diagnostics;
using System.Text;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

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
        long siteId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var pages = _corpusService.ListSnapshots(siteId);

        _logger.LogInformation("Analysis started — Pages: {PageCount}", pages.Count);

        _copilotService.CurrentSiteId = siteId;
        var prompt = BuildAnalysisPrompt(pages);
        var response = await _copilotService.AnalyzeAsync(prompt, ct);
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
