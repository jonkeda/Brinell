namespace Brinell.Scraper.Html;

/// <summary>What an Html node keeps in <see cref="Brinell.Core.Scraping.ScrapeNode.Attributes"/>.</summary>
public static class HtmlAttributes
{
    /// <summary>
    /// The attributes kept by name, besides every <c>data-*</c> and <c>aria-*</c> one. <c>value</c> is never
    /// kept: what a user typed is not recorded.
    /// </summary>
    public static readonly IReadOnlyList<string> Kept =
    [
        "id", "class", "name", "type", "placeholder", "title", "alt", "for", "href",
        "disabled", "readonly", "checked", "selected", "required", "contenteditable", "tabindex",
    ];

    /// <summary>The test-id attributes in the order a locator prefers them, when the config names none.</summary>
    public static readonly IReadOnlyList<string> TestIds = ["data-testid", "data-test", "data-test-id", "data-qa", "data-cy"];

    /// <summary>True when an attribute of this name is kept.</summary>
    public static bool IsKept(string name) =>
        name.StartsWith("data-", StringComparison.Ordinal)
        || (name.StartsWith("aria-", StringComparison.Ordinal) && name != "aria-label")
        || Kept.Contains(name);
}
