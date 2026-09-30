using System.Diagnostics;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class PipelineOrchestrator
{
    private readonly ControlObjectAnalyzer _controlAnalyzer;
    private readonly ControlGenerationService _controlGenerator;
    private readonly PageGenerationService _pageGenerator;
    private readonly SkillService _skillService;
    private readonly CodeOutputService _codeOutput;
    private readonly CorpusService _corpus;
    private readonly IControlRegistry _registry;
    private readonly CorpusDatabase _db;
    private readonly ILogger<PipelineOrchestrator> _logger;

    public PipelineOrchestrator(
        ControlObjectAnalyzer controlAnalyzer,
        ControlGenerationService controlGenerator,
        PageGenerationService pageGenerator,
        SkillService skillService,
        CodeOutputService codeOutput,
        CorpusService corpus,
        IControlRegistry registry,
        CorpusDatabase db,
        ILogger<PipelineOrchestrator> logger)
    {
        _controlAnalyzer = controlAnalyzer;
        _controlGenerator = controlGenerator;
        _pageGenerator = pageGenerator;
        _skillService = skillService;
        _codeOutput = codeOutput;
        _corpus = corpus;
        _registry = registry;
        _db = db;
        _logger = logger;
    }

    public IProgress<PipelineProgress>? Progress { get; set; }

    public async Task<ControlObjectAnalysisResult> AnalyzeForControlObjectsAsync(
        long siteId, CancellationToken ct = default, Guid? runId = null)
    {
        runId ??= Guid.NewGuid();
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RunId"] = runId.Value,
            ["SiteId"] = siteId,
            ["Stage"] = "Analyze",
        });

        var sw = Stopwatch.StartNew();
        var snapshotCount = _corpus.ListSnapshots(siteId).Count(s => s.IsLatest);
        _logger.LogInformation(
            "Pipeline analyze start — Site: {SiteId}, LatestSnapshots: {Count}",
            siteId, snapshotCount);
        Report("Analyzing corpus for control objects", 0, snapshotCount);

        var result = await _controlAnalyzer.AnalyzeAsync(siteId, ct);

        sw.Stop();
        _logger.LogInformation(
            "Pipeline analyze done — Site: {SiteId}, Proposals: {Count}, Elapsed: {ElapsedMs} ms",
            siteId, result.Proposals.Count, sw.ElapsedMilliseconds);
        Report("Analysis complete", snapshotCount, snapshotCount);
        return result;
    }

    public async Task<List<GeneratedControl>> GenerateControlObjectsAsync(
        long siteId,
        List<ControlProposal> approved,
        string targetNamespace,
        LocatorReport? locator,
        CancellationToken ct = default,
        Guid? runId = null)
    {
        runId ??= Guid.NewGuid();
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RunId"] = runId.Value,
            ["SiteId"] = siteId,
            ["Stage"] = "GenerateControls",
        });

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "Pipeline generate controls start — Site: {SiteId}, Approved: {Count}, Namespace: {Namespace}",
            siteId, approved.Count, targetNamespace);
        Report("Generating control objects", 0, approved.Count);

        var controls = await _controlGenerator.GenerateAllApprovedAsync(
            approved, targetNamespace, locator, ct);

        Report("Regenerating site controls skill", approved.Count, approved.Count);
        var siteSlug = ResolveSiteSlug(siteId);
        await _skillService.GenerateSiteControlsSkillAsync(siteId, siteSlug, ct);

        sw.Stop();
        _logger.LogInformation(
            "Pipeline generate controls done — Site: {SiteId}, Generated: {Generated}/{Approved}, Elapsed: {ElapsedMs} ms",
            siteId, controls.Count, approved.Count, sw.ElapsedMilliseconds);

        return controls;
    }

    public async Task<List<PageGenerationResult>> GeneratePageObjectsAsync(
        long siteId,
        string targetNamespace,
        LocatorReport? locator,
        CancellationToken ct = default,
        Guid? runId = null)
    {
        runId ??= Guid.NewGuid();
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RunId"] = runId.Value,
            ["SiteId"] = siteId,
            ["Stage"] = "GeneratePages",
        });

        var sw = Stopwatch.StartNew();

        var summaries = _corpus.ListSnapshots(siteId)
            .Where(s => s.IsLatest)
            .GroupBy(s => s.PageName)
            .Select(g => g.OrderByDescending(s => s.CapturedAt).First())
            .ToList();

        _logger.LogInformation(
            "Pipeline generate pages start — Site: {SiteId}, Pages: {Count}, Namespace: {Namespace}",
            siteId, summaries.Count, targetNamespace);

        var results = new List<PageGenerationResult>();
        for (var i = 0; i < summaries.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var summary = summaries[i];
            Report($"Generating page object: {summary.PageName}", i, summaries.Count);

            var snapshot = _corpus.GetSnapshotById(summary.Id);
            if (snapshot is null)
            {
                _logger.LogWarning(
                    "Snapshot not found — Site: {SiteId}, SnapshotId: {SnapshotId}",
                    siteId, summary.Id);
                continue;
            }
            snapshot.PageName = summary.PageName;

            var result = await _pageGenerator.GeneratePageAsync(
                snapshot, targetNamespace, locator, containerGroups: null, ct);

            result.SiteId = siteId;
            result.SnapshotId = summary.Id;

            _corpus.StorePageObject(result);
            results.Add(result);
        }

        Report("Page object generation complete", summaries.Count, summaries.Count);
        sw.Stop();
        _logger.LogInformation(
            "Pipeline generate pages done — Site: {SiteId}, Generated: {Generated}/{Total}, Elapsed: {ElapsedMs} ms",
            siteId, results.Count, summaries.Count, sw.ElapsedMilliseconds);

        return results;
    }

    public async Task OutputAsync(
        long siteId, string outputPath, string targetNamespace,
        CancellationToken ct = default, Guid? runId = null)
    {
        runId ??= Guid.NewGuid();
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RunId"] = runId.Value,
            ["SiteId"] = siteId,
            ["Stage"] = "Output",
        });

        var sw = Stopwatch.StartNew();
        var controls = _registry.GetAllControls();
        var pages = _corpus.GetPageObjects(siteId);

        _logger.LogInformation(
            "Pipeline output start — Site: {SiteId}, Controls: {Controls}, Pages: {Pages}, Path: {Path}",
            siteId, controls.Count, pages.Count, outputPath);
        Report("Writing project to disk", 0, controls.Count + pages.Count);

        await _codeOutput.WriteProjectAsync(outputPath, targetNamespace, controls, pages, ct);

        sw.Stop();
        _logger.LogInformation(
            "Pipeline output done — Site: {SiteId}, Elapsed: {ElapsedMs} ms",
            siteId, sw.ElapsedMilliseconds);
        Report("Output complete", controls.Count + pages.Count, controls.Count + pages.Count);
    }

    public async Task RunFullPipelineAsync(
        long siteId,
        string targetNamespace,
        string outputPath,
        Func<List<ControlProposal>, Task<List<ControlProposal>>> approvalGate,
        CancellationToken ct = default)
    {
        var runId = Guid.NewGuid();
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RunId"] = runId,
            ["SiteId"] = siteId,
        });

        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "Pipeline full run start — Site: {SiteId}, RunId: {RunId}, Namespace: {Namespace}, Output: {Output}",
            siteId, runId, targetNamespace, outputPath);

        var analysis = await AnalyzeForControlObjectsAsync(siteId, ct, runId);
        var approved = await approvalGate(analysis.Proposals);

        await GenerateControlObjectsAsync(
            siteId, approved, targetNamespace, analysis.LocatorReport, ct, runId);
        await GeneratePageObjectsAsync(
            siteId, targetNamespace, analysis.LocatorReport, ct, runId);
        await OutputAsync(siteId, outputPath, targetNamespace, ct, runId);

        sw.Stop();
        _logger.LogInformation(
            "Pipeline full run done — Site: {SiteId}, RunId: {RunId}, Elapsed: {ElapsedMs} ms",
            siteId, runId, sw.ElapsedMilliseconds);
    }

    private void Report(string stage, int current, int total) =>
        Progress?.Report(new PipelineProgress(stage, current, total));

    private string ResolveSiteSlug(long siteId)
    {
        var site = _db.GetAllSites().FirstOrDefault(s => s.Id == siteId);
        return Slugify(site?.Name);
    }

    private static string Slugify(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "site";
        var chars = s.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return string.IsNullOrEmpty(slug) ? "site" : slug;
    }
}
