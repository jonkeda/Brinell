using System.Globalization;
using System.Text;
using Brinell.Core.Scraping;
using Brinell.Scraper.Html;
using Brinell.Scraper.Projects;
using Brinell.Scraper.Recordings;

namespace Brinell.Scraper.Code;

/// <summary>
/// A page object drafted from a recorded page by rules, with no model: a member per suggestion with a
/// stable locator, from every state of the page in its newest session, and the unstable ones listed in a
/// comment for a person. The draft is a <c>partial</c> class in <c>&lt;Name&gt;.scraped.cs</c>; a person's
/// additions go in the other part, <c>&lt;Name&gt;.cs</c>, which a draft never touches.
/// </summary>
public static class PageDrafter
{
    /// <summary>The ending of a drafted file.</summary>
    public const string Extension = ".scraped.cs";

    // Folders of a path that say nothing about the page.
    private static readonly HashSet<string> Folders = new(StringComparer.OrdinalIgnoreCase) { "pages", "page", "views", "app", "index" };

    /// <summary>A page's class name from its key: <c>/invoices/{id}/edit</c> is <c>InvoiceEditPage</c>, <c>/</c> is <c>HomePage</c>.</summary>
    public static string ClassName(string key)
    {
        var path = key.Split('#')[^1];
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var words = new List<string>();
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = Path.GetFileNameWithoutExtension(segments[i]);
            if (segment == "{id}" || Folders.Contains(segment)) continue;
            // The collection a record is in names one record: invoices/{id} is an invoice.
            if (i + 1 < segments.Length && segments[i + 1] == "{id}") segment = Singular(segment);
            words.AddRange(DangerWordsSplit(segment));
        }
        var name = string.Concat(words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
        if (name.Length == 0) name = "Home";
        if (char.IsDigit(name[0])) name = "Page" + name;
        return name.EndsWith("Page", StringComparison.Ordinal) ? name : name + "Page";
    }

    /// <summary>Drafts one page: the members of all its states in the session of its newest capture.</summary>
    public static PageDraft Draft(ScrapeProject project, RecordingIndex index, RecordedPage page, string className)
    {
        var session = page.Latest.Session;
        var blazor = project.Config.Platform == "blazor";
        var merged = new List<ControlSuggestion>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        // Oldest first, so the page as it first shows names the members.
        foreach (var capture in page.Captures.Where(c => c.Session == session).Reverse())
        {
            foreach (var suggestion in ControlAdvisor.Suggest(index.Read(capture), project.Config.TestIds, blazor))
            {
                if (seen.Add($"{suggestion.Scope}|{suggestion.Locator}")) merged.Add(suggestion);
            }
        }

        var members = Unique(merged.Where(m => m.Stable).ToList(), className);
        var unstable = merged.Where(m => !m.Stable).ToArray();
        var latest = index.Read(page.Latest);
        var header = new ScrapedHeader { Page = page.Key, From = $"{session}/{page.Latest.Event.Snapshot}", Hash = SnapshotFile.ContentHash(latest) };
        var path = Path.Combine(project.Config.PagesFolder, className + Extension);
        return new PageDraft(page.Key, className, path, Code(project, className, header, members, unstable, blazor), members, unstable);
    }

    private static string Code(ScrapeProject project, string className, ScrapedHeader header, IReadOnlyList<ControlSuggestion> members, IReadOnlyList<ControlSuggestion> unstable, bool blazor)
    {
        var baseClass = blazor ? "BlazorPageObjectBase" : "HtmlPageObjectBase";
        var namespaces = new SortedSet<string>(StringComparer.Ordinal) { "Brinell.Core.Locators", "Brinell.Html.Interfaces", blazor ? "Brinell.Blazor.Pages" : "Brinell.Html.Pages" };
        foreach (var member in members) namespaces.Add(member.Namespace);

        // A member in a frame is on the frame's scope: FrameScope<Page>, FrameScope<FrameScope<Page>> for a frame in a frame.
        var byName = members.ToDictionary(m => m.Name, StringComparer.Ordinal);
        string ParentType(ControlSuggestion m) =>
            m.Scope is { } scope && byName.TryGetValue(scope, out var frame) ? $"FrameScope<{ParentType(frame)}>" : className;

        var code = new StringBuilder();
        code.AppendLine(header.ToString());
        foreach (var ns in namespaces) code.AppendLine(CultureInfo.InvariantCulture, $"using {ns};");
        code.AppendLine();
        code.AppendLine(CultureInfo.InvariantCulture, $"namespace {project.Namespace};");
        code.AppendLine();
        code.AppendLine(CultureInfo.InvariantCulture, $"/// <summary>The page {Xml(header.Page!)}: drafted by the Scraper from a recording. Draft again to update; add to the other part, {className}.cs.</summary>");
        code.AppendLine(CultureInfo.InvariantCulture, $"public sealed partial class {className}(IHtmlTestContext context) : {baseClass}<{className}>(context)");
        code.AppendLine("{");
        for (var i = 0; i < members.Count; i++)
        {
            var m = members[i];
            var parent = m.Scope is { } scope && byName.ContainsKey(scope) ? scope : "this";
            if (i > 0) code.AppendLine();
            code.AppendLine(CultureInfo.InvariantCulture, $"    public {m.Control}<{ParentType(m)}> {m.Name} => new({parent}, {m.Locator});");
        }
        if (unstable.Count > 0)
        {
            code.AppendLine();
            code.AppendLine("    // Not drafted: no stable locator. Give the element a test id, or pick a locator by hand.");
            foreach (var m in unstable)
                code.AppendLine(CultureInfo.InvariantCulture, $"    // {m.Control} {m.Name}: {m.Locator} ({m.Why})");
        }
        code.AppendLine("}");
        return code.ToString();
    }

    // What a page object has already: a member of that name would hide it.
    private static readonly string[] Reserved =
    [
        "AssertIdle", "AssertLoaded", "AssertTitle", "Context", "DefaultLocatorStrategy", "FindElement", "FindElements", "GetTitle", "IsBusy",
        "IsLoaded", "IsReady", "Name", "Page", "ProbeReadiness", "Self", "TakeScreenshot", "TryFindElement", "Url", "WaitBusy", "WaitLoaded", "WaitReady", "WaitTitle",
    ];

    // Names are unique in the class, never the class's own and never a base member's.
    private static List<ControlSuggestion> Unique(List<ControlSuggestion> members, string className)
    {
        var used = new HashSet<string>(Reserved, StringComparer.Ordinal) { className };
        for (var i = 0; i < members.Count; i++)
        {
            var name = members[i].Name;
            var unique = name;
            for (var n = 2; !used.Add(unique); n++) unique = name + n.ToString(CultureInfo.InvariantCulture);
            members[i] = members[i] with { Name = unique };
        }
        return members;
    }

    private static string Singular(string word) =>
        word.EndsWith("ies", StringComparison.OrdinalIgnoreCase) ? word[..^3] + "y"
        : word.EndsWith("ses", StringComparison.OrdinalIgnoreCase) ? word[..^2]
        : word.EndsWith('s') && !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase) ? word[..^1]
        : word;

    private static IEnumerable<string> DangerWordsSplit(string text) => Explore.DangerWords.Words(text);

    private static string Xml(string text) => text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
}

/// <summary>A drafted page object: the page, its class, where it goes, its code, its members and those left out.</summary>
public sealed record PageDraft(string Key, string ClassName, string Path, string Code, IReadOnlyList<ControlSuggestion> Members, IReadOnlyList<ControlSuggestion> Unstable);
