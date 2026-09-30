using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

/// <summary>
/// Implements the corpus query tools exposed to the Copilot SDK analyzer/generator agents.
/// All tools read the active site from <see cref="ISessionContext"/> and return JSON-serialized
/// results suitable for tool-call output. Also provides DOM formatting helpers used by
/// <see cref="PromptBuilder"/>.
/// </summary>
public sealed class CorpusTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly CorpusService _corpusService;
    private readonly IControlRegistry _controlRegistry;
    private readonly ISessionContext _sessionContext;
    private readonly ILogger<CorpusTools> _logger;

    public CorpusTools(
        CorpusService corpusService,
        IControlRegistry controlRegistry,
        ISessionContext sessionContext,
        ILogger<CorpusTools> logger)
    {
        _corpusService = corpusService;
        _controlRegistry = controlRegistry;
        _sessionContext = sessionContext;
        _logger = logger;
    }

    private long RequireSiteId() =>
        _sessionContext.CurrentSiteId
        ?? throw new InvalidOperationException(
            "No active site — open a workspace before invoking corpus tools.");

    public string ListRecordedPages()
    {
        var sw = Stopwatch.StartNew();
        var siteId = RequireSiteId();
        var pages = _corpusService.ListSnapshots(siteId)
            .Where(p => p.IsLatest)
            .Select(p => new
            {
                pageId = p.Id,
                pageName = p.PageName,
                pageUrl = p.PageUrl,
                elementCount = p.ElementCount,
                lastCapturedAt = p.CapturedAt,
            })
            .ToList();

        var json = JsonSerializer.Serialize(pages, JsonOptions);
        sw.Stop();
        _logger.LogInformation(
            "Tool list_recorded_pages — Site: {Site}, Pages: {Count}, Elapsed: {ElapsedMs} ms, ResultBytes: {Bytes}",
            siteId, pages.Count, sw.ElapsedMilliseconds, json.Length);
        return json;
    }

    public string GetPageSnapshot(string pageIdOrName)
    {
        var sw = Stopwatch.StartNew();
        var siteId = RequireSiteId();

        DomSnapshot? snapshot = null;
        if (long.TryParse(pageIdOrName, out var pageId))
            snapshot = _corpusService.GetSnapshotById(pageId);
        if (snapshot is null)
            snapshot = _corpusService.GetLatestSnapshot(siteId, pageIdOrName);

        if (snapshot is null)
        {
            _logger.LogInformation(
                "Tool get_page_snapshot — Site: {Site}, Query: {Q}, Result: not found",
                siteId, pageIdOrName);
            return JsonSerializer.Serialize(new { error = "page not found", query = pageIdOrName }, JsonOptions);
        }

        var elements = new List<object>();
        FlattenElements(snapshot.RootElement, parentPath: "", elements);

        var json = JsonSerializer.Serialize(new
        {
            pageUrl = snapshot.PageUrl,
            pageTitle = snapshot.PageTitle,
            elements,
        }, JsonOptions);

        sw.Stop();
        _logger.LogInformation(
            "Tool get_page_snapshot — Site: {Site}, Query: {Q}, Elements: {Count}, Elapsed: {ElapsedMs} ms, ResultBytes: {Bytes}",
            siteId, pageIdOrName, elements.Count, sw.ElapsedMilliseconds, json.Length);
        return json;
    }

    public string FindSimilarElements(string selector)
    {
        var sw = Stopwatch.StartNew();
        var siteId = RequireSiteId();

        // Lightweight selector parsing: tag, .class, #id, or substring fallback.
        var (tagFilter, idFilter, classFilter, substring) = ParseSelector(selector);

        var pages = _corpusService.ListSnapshots(siteId).Where(p => p.IsLatest).ToList();
        var matches = new List<object>();

        foreach (var page in pages)
        {
            var snap = _corpusService.GetSnapshotById(page.Id);
            if (snap is null) continue;
            CollectMatches(snap.RootElement, parentPath: "", page.Id,
                tagFilter, idFilter, classFilter, substring, matches);
        }

        var json = JsonSerializer.Serialize(matches, JsonOptions);
        sw.Stop();
        _logger.LogInformation(
            "Tool find_similar_elements — Site: {Site}, Selector: {Selector}, Matches: {Count}, " +
            "Elapsed: {ElapsedMs} ms, ResultBytes: {Bytes}",
            siteId, selector, matches.Count, sw.ElapsedMilliseconds, json.Length);
        return json;
    }

    public string GetGeneratedControls()
    {
        var sw = Stopwatch.StartNew();
        _ = RequireSiteId(); // tools must be called within an active site context
        var controls = _controlRegistry.GetAllControls()
            .Select(c => new
            {
                name = c.Name,
                domSignature = c.DomSignature,
                @namespace = c.Namespace,
                properties = ExtractProperties(c),
            })
            .ToList();

        var json = JsonSerializer.Serialize(controls, JsonOptions);
        sw.Stop();
        _logger.LogInformation(
            "Tool get_generated_controls — Controls: {Count}, Elapsed: {ElapsedMs} ms, ResultBytes: {Bytes}",
            controls.Count, sw.ElapsedMilliseconds, json.Length);
        return json;
    }

    public string SearchCorpus(string query)
    {
        var sw = Stopwatch.StartNew();
        var siteId = RequireSiteId();

        var elements = _corpusService.SearchElements(siteId, query);
        var results = elements.Select(el => new
        {
            pageId = (long?)null, // SearchElements does not return SnapshotId — best-effort
            xpath = el.Tag,
            snippet = SummarizeElement(el),
        }).ToList();

        var json = JsonSerializer.Serialize(results, JsonOptions);
        sw.Stop();
        _logger.LogInformation(
            "Tool search_corpus — Site: {Site}, Query: {Query}, Hits: {Count}, " +
            "Elapsed: {ElapsedMs} ms, ResultBytes: {Bytes}",
            siteId, query, results.Count, sw.ElapsedMilliseconds, json.Length);
        return json;
    }

    // --- Helpers ----------------------------------------------------------

    private static (string? tag, string? id, string? cls, string? sub) ParseSelector(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return (null, null, null, null);

        var s = selector.Trim();
        if (s.StartsWith('#')) return (null, s[1..], null, null);
        if (s.StartsWith('.')) return (null, null, s[1..], null);

        // tag, tag.class, tag#id
        var dotIdx = s.IndexOf('.');
        var hashIdx = s.IndexOf('#');

        string? tag = null, id = null, cls = null;
        if (dotIdx > 0 || hashIdx > 0)
        {
            var split = Math.Min(
                dotIdx > 0 ? dotIdx : int.MaxValue,
                hashIdx > 0 ? hashIdx : int.MaxValue);
            tag = s[..split];
            var rest = s[split..];
            if (rest.StartsWith('.')) cls = rest[1..];
            else if (rest.StartsWith('#')) id = rest[1..];
            return (tag, id, cls, null);
        }

        // Bare tag if alphanumeric-only, otherwise treat as substring.
        if (s.All(ch => char.IsLetterOrDigit(ch) || ch == '-'))
            return (s, null, null, null);
        return (null, null, null, s);
    }

    private static void CollectMatches(
        DomElement el, string parentPath, long pageId,
        string? tag, string? id, string? cls, string? substring,
        List<object> matches)
    {
        var path = string.IsNullOrEmpty(parentPath) ? el.Tag : $"{parentPath}/{el.Tag}";

        var tagOk = tag is null || el.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase);
        var idOk = id is null || (el.Id?.Equals(id, StringComparison.OrdinalIgnoreCase) ?? false);
        var clsOk = cls is null || (el.ClassName?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(c => c.Equals(cls, StringComparison.OrdinalIgnoreCase)) ?? false);
        var subOk = substring is null
            || (el.TextContent?.Contains(substring, StringComparison.OrdinalIgnoreCase) ?? false)
            || (el.AriaLabel?.Contains(substring, StringComparison.OrdinalIgnoreCase) ?? false)
            || (el.DataTestId?.Contains(substring, StringComparison.OrdinalIgnoreCase) ?? false);

        if (tagOk && idOk && clsOk && subOk
            && (tag is not null || id is not null || cls is not null || substring is not null))
        {
            matches.Add(new
            {
                pageId,
                xpath = path,
                html = SummarizeElement(el),
            });
        }

        foreach (var child in el.Children)
            CollectMatches(child, path, pageId, tag, id, cls, substring, matches);
    }

    private static void FlattenElements(DomElement el, string parentPath, List<object> output)
    {
        var path = string.IsNullOrEmpty(parentPath) ? el.Tag : $"{parentPath}/{el.Tag}";
        output.Add(new
        {
            xpath = path,
            tag = el.Tag,
            id = el.Id,
            className = el.ClassName,
            dataTestId = el.DataTestId,
            ariaLabel = el.AriaLabel,
            role = el.Role,
            text = el.TextContent,
        });
        foreach (var child in el.Children)
            FlattenElements(child, path, output);
    }

    private static IReadOnlyList<string> ExtractProperties(GeneratedControl c)
    {
        // GeneratedControl currently has no structured property list — surface signature only.
        // Future: parse properties from c.Code via Roslyn or extend the model.
        _ = c;
        return Array.Empty<string>();
    }

    private static string SummarizeElement(DomElement el)
    {
        var sb = new StringBuilder();
        sb.Append('<').Append(el.Tag);
        if (el.Id is not null) sb.Append(" id=\"").Append(el.Id).Append('"');
        if (el.ClassName is not null) sb.Append(" class=\"").Append(el.ClassName).Append('"');
        if (el.DataTestId is not null) sb.Append(" data-testid=\"").Append(el.DataTestId).Append('"');
        if (el.AriaLabel is not null) sb.Append(" aria-label=\"").Append(el.AriaLabel).Append('"');
        if (!string.IsNullOrWhiteSpace(el.TextContent))
            sb.Append('>').Append(Truncate(el.TextContent!, 80)).Append("</").Append(el.Tag).Append('>');
        else
            sb.Append(" />");
        return sb.ToString();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    // --- DOM formatting helpers (used by PromptBuilder) -------------------

    public static string FormatSnapshot(DomSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Page URL: {snapshot.PageUrl}");
        sb.AppendLine($"Page Title: {snapshot.PageTitle}");
        sb.AppendLine();
        FormatElement(sb, snapshot.RootElement, indent: 0);
        return sb.ToString();
    }

    public static void FormatElement(StringBuilder sb, DomElement el, int indent)
    {
        var pad = new string(' ', indent * 2);

        sb.Append($"{pad}<{el.Tag}");
        if (el.Id is not null) sb.Append($" id=\"{el.Id}\"");
        if (el.ClassName is not null) sb.Append($" class=\"{el.ClassName}\"");
        if (el.Name is not null) sb.Append($" name=\"{el.Name}\"");
        if (el.Type is not null) sb.Append($" type=\"{el.Type}\"");
        if (el.DataTestId is not null) sb.Append($" data-testid=\"{el.DataTestId}\"");
        if (el.Role is not null) sb.Append($" role=\"{el.Role}\"");
        if (el.AriaLabel is not null) sb.Append($" aria-label=\"{el.AriaLabel}\"");
        if (el.Placeholder is not null) sb.Append($" placeholder=\"{el.Placeholder}\"");

        if (el.Children.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(el.TextContent))
                sb.AppendLine($">{el.TextContent}</{el.Tag}>");
            else
                sb.AppendLine(" />");
        }
        else
        {
            sb.AppendLine(">");
            if (!string.IsNullOrWhiteSpace(el.TextContent))
                sb.AppendLine($"{pad}  {el.TextContent}");

            if (el.Children.Count > 50)
            {
                for (var i = 0; i < 10; i++)
                    FormatElement(sb, el.Children[i], indent + 1);
                sb.AppendLine($"{pad}  <!-- {el.Children.Count - 10} children omitted -->");
            }
            else
            {
                foreach (var child in el.Children)
                    FormatElement(sb, child, indent + 1);
            }

            sb.AppendLine($"{pad}</{el.Tag}>");
        }
    }
}
