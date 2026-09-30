using Brinell.Scraper.Exceptions;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class ControlObjectAnalyzer
{
    private readonly ControlGroupDetector _detector;
    private readonly CorpusService _corpus;
    private readonly ICopilotService _copilot;
    private readonly PromptBuilder _prompts;
    private readonly LlmRetryHelper _retryHelper;
    private readonly ILogger<ControlObjectAnalyzer> _logger;

    public ControlObjectAnalyzer(
        ControlGroupDetector detector,
        CorpusService corpus,
        ICopilotService copilot,
        PromptBuilder prompts,
        LlmRetryHelper retryHelper,
        ILogger<ControlObjectAnalyzer> logger)
    {
        _detector = detector;
        _corpus = corpus;
        _copilot = copilot;
        _prompts = prompts;
        _retryHelper = retryHelper;
        _logger = logger;
    }

    public async Task<ControlObjectAnalysisResult> AnalyzeAsync(
        long siteId, CancellationToken ct = default)
    {
        // Phase A — local detection across the latest snapshot of every page
        var summaries = _corpus.ListSnapshots(siteId).Where(s => s.IsLatest).ToList();
        var snapshots = new List<DomSnapshot>(summaries.Count);
        foreach (var summary in summaries)
        {
            var snap = _corpus.GetSnapshotById(summary.Id);
            if (snap is not null)
                snapshots.Add(snap);
        }

        var localGroups = new List<(DomSnapshot snap, List<ControlGroupSuggestion> groups)>();
        foreach (var s in snapshots)
            localGroups.Add((s, _detector.Detect(s.RootElement)));

        var aggregated = AggregatePatterns(localGroups);

        _logger.LogInformation(
            "Analyzing site {SiteId}: {SnapshotCount} pages, {LocalGroups} local groups",
            siteId, snapshots.Count, aggregated.Count);

        // Phase B — LLM cross-page synthesis
        var siteName = snapshots.Select(s => s.SiteName)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"Site {siteId}";

        var prompt = _prompts.BuildControlObjectAnalysisPrompt(siteName, aggregated, snapshots);

        List<ControlProposal> proposals;
        LocatorReport? locatorReport;
        try
        {
            (proposals, locatorReport) = await _retryHelper.WithRetryAsync<(List<ControlProposal>, LocatorReport?)>(
                call: (p, c) => _copilot.AnalyzeWithErrorHandlingAsync(p, _logger, c),
                initialPrompt: prompt,
                validate: response =>
                {
                    try
                    {
                        var (parsedProposals, parsedReport) =
                            AnalysisResultParser.ParseControlObjectAnalysis(response);
                        // Acceptable: any non-empty proposals OR an explicit empty list with valid JSON.
                        // Parser returns empty list when JSON is missing/invalid; distinguish by
                        // checking the response contains JSON markers.
                        var hasJson = response.Contains('{') && response.Contains('}');
                        if (parsedProposals.Count == 0 && !hasJson)
                            return (false, ((List<ControlProposal>, LocatorReport?))default!,
                                "No JSON object found in LLM response");
                        return (true, (parsedProposals, parsedReport), null);
                    }
                    catch (Exception ex)
                    {
                        return (false, ((List<ControlProposal>, LocatorReport?))default!,
                            $"Parse error: {ex.Message}");
                    }
                },
                maxRetries: 1,
                ct: ct,
                operation: $"AnalyzeControlObjects:Site{siteId}");
        }
        catch (LlmAuthRequiredException)
        {
            throw;
        }
        catch (LlmValidationException ex)
        {
            // Fallback per retry-matrix: return partial result (Proposals=[], local groups only).
            _logger.LogWarning(ex,
                "Analysis Phase B failed after retries — Site: {SiteId}. Returning empty proposals.",
                siteId);
            proposals = [];
            locatorReport = null;
        }
        catch (LlmRateLimitedException ex)
        {
            _logger.LogWarning(ex,
                "Analysis Phase B rate-limited — Site: {SiteId}. Returning empty proposals.",
                siteId);
            proposals = [];
            locatorReport = null;
        }
        catch (LlmTokenLimitException ex)
        {
            _logger.LogWarning(ex,
                "Analysis Phase B hit token limit — Site: {SiteId}. Returning empty proposals.",
                siteId);
            proposals = [];
            locatorReport = null;
        }

        _logger.LogInformation(
            "Analysis response parsed — Site: {SiteId}, Proposals: {ProposalCount}",
            siteId, proposals.Count);

        var result = new ControlObjectAnalysisResult
        {
            Proposals = proposals,
            LocatorReport = locatorReport,
            LocalGroupCount = aggregated.Count,
            SnapshotsAnalyzed = snapshots.Count,
            AnalyzedAt = DateTimeOffset.UtcNow
        };

        _corpus.StoreAnalysisResult(siteId, result);
        return result;
    }

    private static List<AggregatedPattern> AggregatePatterns(
        List<(DomSnapshot snap, List<ControlGroupSuggestion> groups)> input)
    {
        var aggregated = new List<AggregatedPattern>();

        foreach (var (snap, groups) in input)
        {
            foreach (var group in groups)
            {
                var element = group.Element;
                var tag = element.Tag.ToLowerInvariant();
                var classes = SplitClasses(element.ClassName);
                var childTags = element.Children
                    .Select(c => c.Tag.ToLowerInvariant())
                    .ToList();

                var match = aggregated.FirstOrDefault(p => IsExactMatch(p, tag, classes, childTags));
                var isFuzzy = false;
                if (match is null)
                {
                    match = aggregated.FirstOrDefault(p => IsFuzzyMatch(p, tag, classes, childTags));
                    isFuzzy = match is not null;
                }

                if (match is null)
                {
                    aggregated.Add(new AggregatedPattern
                    {
                        Tag = tag,
                        Classes = classes,
                        ChildTags = childTags,
                        Signature = BuildSignature(tag, classes, childTags),
                        Frequency = 1,
                        PageIds = [snap.PageName],
                        ExampleHtml = group.DisplayName,
                        LocalSuggestions = [group]
                    });
                }
                else
                {
                    match.Frequency++;
                    if (!match.PageIds.Contains(snap.PageName))
                        match.PageIds.Add(snap.PageName);
                    match.LocalSuggestions.Add(group);
                    if (isFuzzy) match.IsFuzzy = true;
                }
            }
        }

        return aggregated;
    }

    private static bool IsExactMatch(
        AggregatedPattern p, string tag, HashSet<string> classes, List<string> childTags)
    {
        if (p.Tag != tag) return false;
        if (p.Classes.Count != classes.Count) return false;
        if (!p.Classes.SetEquals(classes)) return false;
        if (p.ChildTags.Count != childTags.Count) return false;
        for (var i = 0; i < childTags.Count; i++)
            if (p.ChildTags[i] != childTags[i]) return false;
        return true;
    }

    private static bool IsFuzzyMatch(
        AggregatedPattern p, string tag, HashSet<string> classes, List<string> childTags)
    {
        if (p.Tag != tag) return false;

        // ≥80% class overlap (Jaccard-like, intersection over union)
        var union = new HashSet<string>(p.Classes);
        union.UnionWith(classes);
        if (union.Count == 0) return false;
        var intersection = p.Classes.Intersect(classes).Count();
        var overlap = (double)intersection / union.Count;
        if (overlap < 0.8) return false;

        // Same first 3 child tags
        var take = Math.Min(3, Math.Min(p.ChildTags.Count, childTags.Count));
        if (take == 0) return p.ChildTags.Count == 0 && childTags.Count == 0;
        for (var i = 0; i < take; i++)
            if (p.ChildTags[i] != childTags[i]) return false;
        return true;
    }

    private static HashSet<string> SplitClasses(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return new HashSet<string>(StringComparer.Ordinal);
        return new HashSet<string>(
            className.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.Ordinal);
    }

    private static string BuildSignature(string tag, HashSet<string> classes, List<string> childTags)
    {
        var classPart = classes.Count == 0
            ? ""
            : "." + string.Join(".", classes.OrderBy(c => c, StringComparer.Ordinal));
        var childPart = childTags.Count == 0
            ? ""
            : ">" + string.Join(">", childTags);
        return tag + classPart + childPart;
    }

    public sealed class AggregatedPattern
    {
        public required string Tag { get; init; }
        public required HashSet<string> Classes { get; init; }
        public required List<string> ChildTags { get; init; }
        public required string Signature { get; init; }
        public int Frequency { get; set; }
        public List<string> PageIds { get; init; } = [];
        public string ExampleHtml { get; init; } = "";
        public List<ControlGroupSuggestion> LocalSuggestions { get; init; } = [];
        public bool IsFuzzy { get; set; }
    }
}
