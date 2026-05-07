using System.Text;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

/// <summary>
/// Provides DOM element formatting utilities used by corpus query tools
/// and the prompt builder. The actual tool registration depends on the
/// Copilot SDK (step 5.1) — this class provides the query logic and formatting.
/// </summary>
public sealed class CorpusTools
{
    private readonly CorpusService _corpusService;
    private readonly IControlRegistry _controlRegistry;

    public CorpusTools(CorpusService corpusService, IControlRegistry controlRegistry)
    {
        _corpusService = corpusService;
        _controlRegistry = controlRegistry;
    }

    public string SearchCorpus(long siteId, string query, string? tag = null)
    {
        var results = _corpusService.SearchElements(siteId, query);

        if (tag is not null)
            results = results
                .Where(e => e.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (results.Count == 0)
            return "No matching elements found.";

        return FormatElementResults(results);
    }

    public string GetPageSnapshot(long siteId, string pageName)
    {
        var snapshot = _corpusService.GetLatestSnapshot(siteId, pageName);

        if (snapshot is null)
            return "No snapshot found for that page.";

        return FormatSnapshot(snapshot);
    }

    public string GetGeneratedControls()
    {
        var controls = _controlRegistry.GetAllControls();

        if (controls.Count == 0)
            return "No custom controls have been generated yet.";

        var sb = new StringBuilder();
        foreach (var ctrl in controls)
        {
            sb.AppendLine($"## {ctrl.Name}");
            sb.AppendLine($"DOM signature: {ctrl.DomSignature}");
            sb.AppendLine($"Namespace: {ctrl.Namespace}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public string ListRecordedPages(long siteId)
    {
        var pages = _corpusService.ListSnapshots(siteId);

        if (pages.Count == 0)
            return "No pages recorded yet.";

        var sb = new StringBuilder();
        sb.AppendLine("| ID | Page Name | URL | Elements | Captured |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var p in pages)
        {
            sb.AppendLine($"| {p.Id} | {p.PageName} | {p.PageUrl} | {p.ElementCount} | {p.CapturedAt:yyyy-MM-dd HH:mm} |");
        }
        return sb.ToString();
    }

    public static string FormatSnapshot(DomSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Page URL: {snapshot.PageUrl}");
        sb.AppendLine($"Page Title: {snapshot.PageTitle}");
        sb.AppendLine($"Element count: {CountElements(snapshot.RootElement)}");
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

    private static string FormatElementResults(IReadOnlyList<DomElement> elements)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Found {elements.Count} matching elements:");
        sb.AppendLine();

        foreach (var el in elements)
        {
            sb.Append($"<{el.Tag}");
            if (el.Id is not null) sb.Append($" id=\"{el.Id}\"");
            if (el.ClassName is not null) sb.Append($" class=\"{el.ClassName}\"");
            if (el.DataTestId is not null) sb.Append($" data-testid=\"{el.DataTestId}\"");
            if (el.AriaLabel is not null) sb.Append($" aria-label=\"{el.AriaLabel}\"");
            if (el.Role is not null) sb.Append($" role=\"{el.Role}\"");
            if (!string.IsNullOrWhiteSpace(el.TextContent))
                sb.AppendLine($">{el.TextContent}</{el.Tag}>");
            else
                sb.AppendLine(" />");
        }

        return sb.ToString();
    }

    private static int CountElements(DomElement element)
    {
        var count = 1;
        foreach (var child in element.Children)
            count += CountElements(child);
        return count;
    }
}
