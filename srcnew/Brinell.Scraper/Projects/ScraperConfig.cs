using System.Text.RegularExpressions;
using Brinell.Core.Utilities;
using Brinell.Scraper.Html;

namespace Brinell.Scraper.Projects;

/// <summary>
/// A scraped project's settings, from its <c>scraper.config.md</c>: the <c>## App</c> field table,
/// tables for URL aliases, test-id attributes and text masks, and Explore's (<see cref="ExploreSettings"/>).
/// </summary>
public sealed record ScraperConfig
{
    /// <summary>The file name that makes a project scrapeable.</summary>
    public const string FileName = "scraper.config.md";

    /// <summary>The platform recorded: <c>html</c> or <c>blazor</c>.</summary>
    public string Platform { get; init; } = "html";

    /// <summary>Where a recording starts.</summary>
    public string StartUrl { get; init; } = "";

    /// <summary>Other addresses of the same app (another country's host). A page elsewhere, such as a sign-in provider's, is not the app's.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>The namespace generated code goes in; the project's name when empty.</summary>
    public string Namespace { get; init; } = "";

    /// <summary>The folder, in the project, generated page objects go to.</summary>
    public string PagesFolder { get; init; } = "Pages";

    /// <summary>The folder, in the project, generated controls go to.</summary>
    public string ControlsFolder { get; init; } = "Controls";

    /// <summary>Whether a screenshot is stored beside each snapshot.</summary>
    public bool Screenshots { get; init; }

    /// <summary>The attributes that are test ids, in the order a locator prefers them.</summary>
    public IReadOnlyList<string> TestIds { get; init; } = HtmlAttributes.TestIds;

    /// <summary>Text masked before anything is written.</summary>
    public IReadOnlyList<TextMask> Masks { get; init; } = [];

    /// <summary>The chat agent (or mode) the Scraper's prompts run in, by default (<c>## Agent</c>, <c>Agent</c>); null for the view's default.</summary>
    public string? ChatAgent { get; init; }

    /// <summary>The model the Scraper's prompts run with, by default (<c>## Agent</c>, <c>Model</c>); null for the IDE's own.</summary>
    public string? ChatModel { get; init; }

    /// <summary>The app's own components, as the person names them (<c>## Components</c>): these win over the Scraper's own reading.</summary>
    public IReadOnlyList<ComponentRule> Components { get; init; } = [];

    /// <summary>How Explore may go through the app: its budget and guards.</summary>
    public ExploreSettings Explore { get; init; } = new();

    /// <summary>Reads a config file's text.</summary>
    /// <exception cref="FormatException">A field's value is not valid; the message names its line.</exception>
    public static ScraperConfig Parse(string markdown)
    {
        var sections = MarkdownTables.ReadSections(markdown);
        var config = new ScraperConfig();
        if (sections.TryGetValue("App", out var app))
        {
            foreach (var row in app.Rows)
            {
                var value = row.Cells.GetValueOrDefault("Value", "");
                config = row.Cells.GetValueOrDefault("Field", "") switch
                {
                    "Platform" => config with { Platform = PlatformName(value, row.LineNumber) },
                    "StartUrl" => config with { StartUrl = value },
                    "Namespace" => config with { Namespace = value },
                    "Pages" => config with { PagesFolder = value },
                    "Controls" => config with { ControlsFolder = value },
                    "Screenshots" => config with { Screenshots = Bool(value, row.LineNumber) },
                    var other => throw new FormatException($"Line {row.LineNumber}: '{other}' is not a field of ## App (Platform, StartUrl, Namespace, Pages, Controls, Screenshots)."),
                };
            }
        }
        if (sections.TryGetValue("Aliases", out var aliases))
            config = config with { Aliases = aliases.Rows.Select(r => r.Cells.GetValueOrDefault("Url", "")).Where(u => u.Length > 0).ToArray() };
        if (sections.TryGetValue("Test ids", out var testIds))
            config = config with { TestIds = testIds.Rows.Select(r => r.Cells.GetValueOrDefault("Attribute", "")).Where(a => a.Length > 0).ToArray() };
        if (sections.TryGetValue("Masks", out var masks))
            config = config with { Masks = masks.Rows.Select(Mask).ToArray() };
        if (sections.TryGetValue("Agent", out var agent))
        {
            foreach (var row in agent.Rows)
            {
                var value = row.Cells.GetValueOrDefault("Value", "").Trim();
                config = row.Cells.GetValueOrDefault("Field", "") switch
                {
                    "Agent" => config with { ChatAgent = value.Length > 0 ? value : null },
                    "Model" => config with { ChatModel = value.Length > 0 ? value : null },
                    var other => throw new FormatException($"Line {row.LineNumber}: '{other}' is not a field of ## Agent (Agent, Model)."),
                };
            }
        }
        if (sections.TryGetValue("Components", out var components))
            config = config with { Components = components.Rows.Select(ComponentRule.Read).ToArray() };
        return config with { Explore = ExploreSettings.Read(sections) };
    }

    /// <summary>Reads a config file.</summary>
    public static ScraperConfig Read(string path) => Parse(File.ReadAllText(path));

    private static string PlatformName(string value, int line) => value.ToLowerInvariant() switch
    {
        "html" or "blazor" => value.ToLowerInvariant(),
        _ => throw new FormatException($"Line {line}: Platform is html or blazor, not '{value}'."),
    };

    private static bool Bool(string value, int line) =>
        bool.TryParse(value, out var b) ? b : throw new FormatException($"Line {line}: '{value}' is not true or false.");

    private static TextMask Mask(MarkdownTableRow row)
    {
        var pattern = Unquote(row.Cells.GetValueOrDefault("Pattern", ""));
        try
        {
            return new TextMask(new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)), Unquote(row.Cells.GetValueOrDefault("Replacement", "***")));
        }
        catch (ArgumentException e)
        {
            throw new FormatException($"Line {row.LineNumber}: '{pattern}' is not a regular expression: {e.Message}", e);
        }
    }

    // A cell may put its value in backticks, so a leading or trailing space survives the table.
    private static string Unquote(string cell) =>
        cell.Length >= 2 && cell[0] == '`' && cell[^1] == '`' ? cell[1..^1] : cell;
}

/// <summary>Text that is masked in a snapshot: every match of <paramref name="Pattern"/> becomes <paramref name="Replacement"/>.</summary>
public sealed record TextMask(Regex Pattern, string Replacement);
