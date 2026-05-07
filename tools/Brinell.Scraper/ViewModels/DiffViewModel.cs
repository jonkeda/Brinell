using Brinell.Scraper.Models;

namespace Brinell.Scraper.ViewModels;

public sealed class DiffViewModel : ViewModelBase
{
    public string Title { get; init; } = "Snapshot Diff";
    public int AddedCount { get; init; }
    public int RemovedCount { get; init; }
    public int ChangedCount { get; init; }
    public int UnchangedCount { get; init; }
    public List<string> Added { get; init; } = [];
    public List<string> Removed { get; init; } = [];
    public List<string> Changed { get; init; } = [];

    public static DiffViewModel FromResult(DomDiffResult result, string pageName)
    {
        return new DiffViewModel
        {
            Title = $"Diff — {pageName}",
            AddedCount = result.Added.Count,
            RemovedCount = result.Removed.Count,
            ChangedCount = result.Changed.Count,
            UnchangedCount = result.UnchangedCount,
            Added = result.Added.Select(FormatElement).ToList(),
            Removed = result.Removed.Select(FormatElement).ToList(),
            Changed = result.Changed.Select(FormatChange).ToList(),
        };
    }

    private static string FormatElement(DomElement el)
    {
        var parts = new List<string> { $"<{el.Tag}>" };
        if (el.Id is not null) parts.Add($"id=\"{el.Id}\"");
        if (el.ClassName is not null) parts.Add($"class=\"{el.ClassName}\"");
        if (el.DataTestId is not null) parts.Add($"data-testid=\"{el.DataTestId}\"");
        if (el.AriaLabel is not null) parts.Add($"aria-label=\"{el.AriaLabel}\"");
        if (el.TextContent is { Length: > 0 } text)
            parts.Add($"\"{(text.Length > 40 ? text[..37] + "..." : text)}\"");
        return string.Join("  ", parts);
    }

    private static string FormatChange(DomElementChange change)
    {
        var attrs = string.Join(", ", change.ChangedAttributes);
        return $"{FormatElement(change.Before)} → [{attrs}]";
    }
}
