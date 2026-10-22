namespace Brinell.Html.Interfaces;

/// <summary>
/// What a page's readiness probe reads, in one look: the document's load state and the busy
/// attribute on its body.
/// </summary>
/// <param name="ReadyState"><c>document.readyState</c>: "loading", "interactive" or "complete".</param>
/// <param name="HasBody">Whether the document has a body yet.</param>
/// <param name="Busy">The body's busy attribute; null when it has none.</param>
public sealed record PageProbe(string? ReadyState, bool HasBody, string? Busy)
{
    /// <summary>A document being replaced by a navigation: nothing is ready.</summary>
    public static PageProbe Navigating { get; } = new("loading", false, null);

    /// <summary>Whether the document has loaded.</summary>
    public bool IsComplete => string.Equals(ReadyState, "complete", StringComparison.Ordinal);
}

/// <summary>
/// A context that answers a page's probe in one round trip. <c>HtmlPageObjectBase</c> uses it
/// when its context has it, and otherwise asks for idle and the body's attribute apart.
/// </summary>
public interface IHtmlPageProbe
{
    /// <summary>The document's load state and the body's <paramref name="busyAttribute"/>, read once.</summary>
    PageProbe ProbePage(string busyAttribute);
}
