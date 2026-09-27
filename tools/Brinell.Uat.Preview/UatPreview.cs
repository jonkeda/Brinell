using System.Reflection;
using System.Net;

namespace Brinell.Uat.Preview;

/// <summary>What the preview knows besides the file's text.</summary>
public sealed record UatPreviewOptions
{
    /// <summary>Why the file is only parsed, not bound, shown at the top; null when binding.</summary>
    public string? ParseOnlyReason { get; init; }

    /// <summary>
    /// The suite's catalogs: with them, each step shows what it binds to, the page it runs on,
    /// and the gate's diagnostic when it binds to nothing. Null to parse only.
    /// </summary>
    public UatCatalogSet? Catalogs { get; init; }
}

/// <summary>A rendered file: the HTML, and every diagnostic it shows.</summary>
/// <param name="Html">An <c>&lt;article&gt;</c>, every block with its <c>data-line</c>.</param>
/// <param name="Diagnostics">The parse (and, when bound, bind) diagnostics, in line order.</param>
public sealed record UatPreviewResult(string Html, IReadOnlyList<UatDiagnostic> Diagnostics);

/// <summary>
/// Renders a feature or step file as the preview shows it (P1, P3): the engine's parse of the file,
/// so the preview shows what the gate sees. Markdown around the scenarios goes through Markdig, with
/// raw HTML off; each block carries <c>data-line</c>, the file's 1-based line, for scroll sync.
/// </summary>
public static class UatPreview
{
    /// <summary>Renders <paramref name="text"/>, the content of <paramref name="filePath"/>.</summary>
    /// <param name="text">The file's text, which may be unsaved.</param>
    /// <param name="filePath">The file's path, which says whether it is a feature or a step file.</param>
    /// <param name="options">What else the preview knows.</param>
    public static UatPreviewResult Render(string text, string filePath, UatPreviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return new UatPreviewWriter(text, filePath, options ?? new UatPreviewOptions()).Write();
    }

    /// <summary>
    /// The stylesheet: every colour a CSS variable (<c>--uat-*</c>), with light, dark and
    /// high-contrast values; a host overrides the variables to follow its theme.
    /// </summary>
    public static string Stylesheet { get; } = ReadStylesheet();

    /// <summary>A whole page around a rendered file, with the stylesheet inline.</summary>
    /// <param name="result">The rendered file.</param>
    /// <param name="title">The page's title.</param>
    public static string Page(UatPreviewResult result, string title)
    {
        ArgumentNullException.ThrowIfNull(result);
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <meta name="color-scheme" content="light dark">
            <title>{WebUtility.HtmlEncode(title)}</title>
            <style>
            {Stylesheet}
            </style>
            </head>
            <body>
            {result.Html}
            </body>
            </html>
            """;
    }

    private static string ReadStylesheet()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Brinell.Uat.Preview.preview.css")
            ?? throw new InvalidOperationException("The preview stylesheet is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
