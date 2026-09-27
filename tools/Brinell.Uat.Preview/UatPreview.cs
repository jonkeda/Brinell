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

    /// <summary>The file's scenarios' last runs, by scenario name: each step shows its status, a failed one its message and screenshot.</summary>
    public IReadOnlyDictionary<string, UatScenarioRun> Runs { get; init; } = new Dictionary<string, UatScenarioRun>();

    /// <summary>
    /// The URL a screenshot is shown from, for its path; null when it cannot be shown. The IDE maps
    /// a virtual host to the results folder, since a webview does not load <c>file:</c> URLs.
    /// </summary>
    public Func<string, string?> ImageUrl { get; init; } = path => new Uri(path).AbsoluteUri;
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

    /// <summary>
    /// The page an IDE's webview loads once: the stylesheet, a bar with the suite picker and the bind
    /// state, and the script that takes <c>update</c>, <c>scrollTo</c> and <c>theme</c> messages and
    /// sends <c>reveal</c> and <c>pickSuite</c>. The same page in Visual Studio and VS Code.
    /// </summary>
    public static string HostPage { get; } = $"""
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset="utf-8">
        <meta name="color-scheme" content="light dark">
        <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src https: data:; style-src 'unsafe-inline'; script-src 'unsafe-inline'">
        <title>Scenario Preview</title>
        <style>
        {ReadResource("preview.css")}
        </style>
        </head>
        <body>
        <header class="uat-bar"><span id="uat-state" class="uat-state" data-state="parse-only">Parsed only</span><select id="uat-suite" class="uat-suite" hidden aria-label="Suite"></select></header>
        <main id="uat-root"></main>
        <script>
        {ReadResource("preview-host.js")}
        </script>
        </body>
        </html>
        """;

    private static string ReadStylesheet() => ReadResource("preview.css");

    private static string ReadResource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Brinell.Uat.Preview." + name)
            ?? throw new InvalidOperationException($"The preview's {name} is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
