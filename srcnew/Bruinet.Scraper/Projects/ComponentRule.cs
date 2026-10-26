using Bruinet.Core.Utilities;

namespace Bruinet.Scraper.Projects;

/// <summary>
/// One row of <c>## Components</c>: elements the person names, what they are, and the control's name.
/// The Scraper's rules read most things from the page; this is for what they cannot tell.
/// </summary>
/// <param name="Match">The elements: <c>selector: …</c>, or <c>text: …</c> for an element with that text.</param>
/// <param name="Kind">What they are: <c>collection</c>, <c>component</c>, or a core (<c>text</c>, <c>selector</c>, <c>toggle</c>, <c>range</c>, <c>date</c>, <c>clickable</c>, <c>readable</c>).</param>
/// <param name="Name">The control's name.</param>
public sealed record ComponentRule(ElementMatch Match, string Kind, string Name)
{
    /// <summary>The kinds a row may give.</summary>
    public static readonly IReadOnlyList<string> Kinds = ["collection", "component", "text", "selector", "toggle", "range", "date", "clickable", "readable"];

    /// <summary>Reads a row of <c>## Components</c>.</summary>
    /// <exception cref="FormatException">The kind is not one of <see cref="Kinds"/>, or the name is empty.</exception>
    public static ComponentRule Read(MarkdownTableRow row)
    {
        var kind = row.Cells.GetValueOrDefault("Kind", "").Trim().ToLowerInvariant();
        if (!Kinds.Contains(kind))
            throw new FormatException($"Line {row.LineNumber}: Kind is one of {string.Join(", ", Kinds)}, not '{kind}'.");
        var name = row.Cells.GetValueOrDefault("Name", "").Trim();
        if (name.Length == 0)
            throw new FormatException($"Line {row.LineNumber}: a component needs a Name.");
        return new ComponentRule(ElementMatch.Parse(row.Cells.GetValueOrDefault("Match", ""), row.LineNumber), kind, name);
    }
}
