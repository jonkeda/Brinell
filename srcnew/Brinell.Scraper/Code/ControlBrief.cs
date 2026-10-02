using System.Globalization;
using System.Text;
using Brinell.Core.Scraping;
using Brinell.Scraper.Html;
using Brinell.Scraper.Projects;
using Brinell.Scraper.Service;

namespace Brinell.Scraper.Code;

/// <summary>A brief for making one control: its name, and the text the agent gets.</summary>
/// <param name="Name">The control's class name.</param>
/// <param name="Text">The brief: task, facts, hints.</param>
/// <param name="Cut">Whether the text was cut at <see cref="ControlBrief.MaxCharacters"/>.</param>
public sealed record ControlBriefText(string Name, string Text, bool Cut);

/// <summary>
/// What the agent gets to make a control from a recorded pattern (.my/ScraperEx/control-brief-plan.md):
/// the <b>task</b> (where to write, what to read and nothing else), the <b>facts</b> (where it occurs, an
/// example outline, its parts and in how many occurrences each is found) and the <b>hints</b>
/// (<see cref="ControlKind"/>'s reading: a base, members). The model decides; the hints help it.
/// </summary>
public static class ControlBrief
{
    /// <summary>The most characters a brief has (about 6k tokens); the rest is a tool call away.</summary>
    public const int MaxCharacters = 24_000;

    private const int ExampleLines = 80;
    private const int MaxParts = 30;
    private const int MaxPages = 10;
    private const int NodesPerPage = 8;
    private const int PartSources = 20;
    private const int MaxVariants = 10;

    private static readonly string[] FactAttributes =
        ["type", "readonly", "disabled", "placeholder", "name", "for", "href", "tabindex", "contenteditable", "aria-haspopup", "aria-expanded", "aria-owns", "aria-controls", "aria-checked", "aria-pressed", "aria-selected", "aria-label", "aria-labelledby", "aria-disabled", "aria-live"];

    /// <summary>The brief for the pattern with <paramref name="signature"/>.</summary>
    /// <param name="project">The scraped project.</param>
    /// <param name="signature">The pattern's signature (<see cref="NodeSignature"/>).</param>
    /// <param name="variant">Which variant of the pattern (<see cref="ControlKind.Variant"/>); every occurrence when null.</param>
    /// <param name="lifted">Whether the control starts at the pattern's node lifted to its root (<see cref="Pattern.Lifted"/>).</param>
    /// <param name="name">The control's name; the pattern's own when null.</param>
    /// <param name="header">Makes the scraped header for a page's node.</param>
    /// <param name="brinellRoot">The Brinell repository, to name the files to read by path; null when the project is outside it.</param>
    public static ControlBriefText Write(ScrapeProject project, string signature, string? variant, bool lifted, string? name, Func<string, int, string> header, string? brinellRoot)
    {
        var index = project.Index();
        var all = index.Pages
            .Select(p => (p.Key, Snapshot: index.Read(p.Latest)))
            .SelectMany(p => p.Snapshot.Nodes
                .Where(n => n.Kind == ScrapeNodeKind.Element && NodeSignature.Of(p.Snapshot, n) == signature)
                .Select(n => (Inner: n, Root: lifted ? PatternDetector.RootOf(p.Snapshot, n) : n))
                .DistinctBy(n => n.Root.Number)
                .Select(n => new Occurrence(p.Key, p.Snapshot, n.Root, ControlKind.Variant(p.Snapshot, n.Root), n.Inner)))
            .ToArray();
        var occurrences = all.Where(o => variant is null || o.Variant == variant).ToArray();
        if (occurrences.Length == 0)
            throw new ScraperProblem($"No recorded page has the pattern '{signature}'{(variant is null ? "" : $" as a {variant}")} on its newest capture.");

        // The occurrence with the most a test reads or acts on shows the most: an unlabelled one says less.
        var example = occurrences.MaxBy(o => ControlKind.Below(o.Snapshot, o.Root).Count)!;
        var readings = ControlKind.Below(example.Snapshot, example.Root);
        var interactive = readings.Where(r => r.Reading.Core != PartCore.Readable).ToArray();
        var cores = interactive.Select(r => r.Reading.Core).Distinct().ToArray();
        var hint = cores.Length switch
        {
            0 => Hints[PartCore.Readable],
            1 => Hints[cores[0]],
            _ => Component,
        };
        var className = name is { Length: > 0 } ? name : PatternName(project, signature, variant, lifted);
        var pages = occurrences.Select(o => o.Page).Distinct(StringComparer.Ordinal).Count();

        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"## {className} ({occurrences.Length}× on {Plural(pages, "page")})\n\n");

        text.Append("### Task\n");
        var file = Path.Combine(project.Folder, project.Config.ControlsFolder, className + CodeGate.TemplateExtension);
        text.Append(CultureInfo.InvariantCulture, $"Write:      {file}   (new file; check_code generates {className}.gen.cs from it)\n");
        text.Append(CultureInfo.InvariantCulture, $"Namespace:  {ControlsNamespace(project)}\n");
        text.Append(CultureInfo.InvariantCulture, $"First line: {header(example.Page, example.Inner.Number)}\n");
        text.Append(CultureInfo.InvariantCulture, $"Class:      {hint.Declaration(className)}\n");
        text.Append("Read only:  ").Append(string.Join("\n            ", ReadOnly(hint, brinellRoot))).Append('\n');
        text.Append("Check:      check_code (generates the template's members, then builds the project)\n\n");

        text.Append("### Facts\n");
        text.Append(CultureInfo.InvariantCulture, $"Signature: {signature}{(lifted ? $"; the control starts higher, at the root that also holds its label ({example.Root.Type}{string.Concat(Classes(example.Root).Take(3).Select(c => "." + c))}), and its parts are relative to that root" : "")}\n");
        var variants = all.GroupBy(o => o.Variant, StringComparer.Ordinal).OrderByDescending(g => g.Count()).ToArray();
        if (variants.Length > 1)
        {
            text.Append("Variants of the pattern, by what is inside (what a test does with it):\n");
            foreach (var v in variants.Take(MaxVariants))
                text.Append(CultureInfo.InvariantCulture, $"  {v.Count(),4}×  {v.Key,-22} e.g. {v.First().Page} node {v.First().Root.Number}{(v.Key == variant ? "   ← this brief" : "")}\n");
            if (variants.Length > MaxVariants)
                text.Append(CultureInfo.InvariantCulture, $"  +{variants.Length - MaxVariants} more variants, {variants.Skip(MaxVariants).Sum(v => v.Count())}×\n");
            text.Append(variant is null ? "This brief is for all of them: one control, or say why to split it.\n" : "This brief is for the marked one; the model may still make one control for several, and say why.\n");
        }
        text.Append("Where: ").Append(Where(occurrences)).Append('\n');
        text.Append(CultureInfo.InvariantCulture, $"\nExample ({example.Page}, node {example.Root.Number}, with its parent):\n");
        text.Append(Outline(example.Snapshot, example.Root, ExampleLines));
        text.Append(CultureInfo.InvariantCulture, $"\nParts (relative to the control's root), found in how many of the {occurrences.Length}:\n");
        text.Append(Parts(example, occurrences, project.Config.TestIds));

        text.Append("\n### Hints\n");
        text.Append("What the elements are: ").Append(interactive.Length == 0 ? "nothing interactive; text to read" : string.Join("; ", interactive.Select(r => $"#{r.Node.Number} {r.Reading.Core.ToString().ToLowerInvariant()} ({r.Reading.Because})").Distinct().Take(8))).Append('\n');
        text.Append("Kind: ").Append(hint.Kind).Append(". Base: ").Append(hint.Base).Append('\n');
        text.Append("Members a test needs: ").Append(hint.Members).Append('\n');
        text.Append("These are a reading of the facts, not an order: where you see it differently, say why and do that.\n");
        text.Append(CultureInfo.InvariantCulture, $"\nMore: control_brief for another pattern; get_page_snapshot(page, node) for any occurrence above.\n");

        var brief = text.ToString();
        return brief.Length <= MaxCharacters
            ? new ControlBriefText(className, brief, false)
            : new ControlBriefText(className, brief[..MaxCharacters] + "\n… cut at the brief's budget: get_page_snapshot for the rest.\n", true);
    }

    /// <summary>The namespace of the project's controls: its namespace with the pages folder swapped for the controls folder.</summary>
    public static string ControlsNamespace(ScrapeProject project)
    {
        var pages = "." + project.Config.PagesFolder.Replace('/', '.').Replace('\\', '.');
        var controls = project.Config.ControlsFolder.Replace('/', '.').Replace('\\', '.');
        return project.Namespace.EndsWith(pages, StringComparison.Ordinal)
            ? project.Namespace[..^pages.Length] + "." + controls
            : project.Namespace + "." + controls;
    }

    private sealed record Occurrence(string Page, ScrapeSnapshot Snapshot, ScrapeNode Root, string Variant, ScrapeNode Inner);

    private static string[] Classes(ScrapeNode node) => (node.Attribute("class") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private sealed record Hint(string Kind, string Base, string BaseFile, string Example, string Members, Func<string, string> Declaration);

    private static Hint Simple(string kind, string @base, string example, string members) =>
        new(kind, $"{@base}<TScope>", $"srcnew/Brinell.Html/Controls/{@base}.tpl.cs", example, members,
            name => $"public partial class {name}<TScope> : {@base}<TScope> where TScope : IHtmlScope<TScope>   (usings: Brinell.Core.Locators, Brinell.Html.Controls, Brinell.Html.Interfaces; constructors (scope, Locator) and (scope, string selectorOrId) as in the example)");

    private static readonly IReadOnlyDictionary<PartCore, Hint> Hints = new Dictionary<PartCore, Hint>
    {
        [PartCore.Text] = Simple("a simple control: text", "FocusableControlBase", "srcnew/Brinell.Html/Controls/Text/TextInputControl.tpl.cs", "SetText(text) and Clear() on the input; GetText()"),
        [PartCore.Selector] = Simple("a simple control: selector", "SelectorControlBase", "srcnew/Brinell.Html/Controls/Selection/SelectControl.tpl.cs", "SelectItem(text): open it, then the option with that text; GetSelectedText(); Open()"),
        [PartCore.Toggle] = Simple("a simple control: toggle", "ToggleControlBase", "srcnew/Brinell.Html/Controls/Toggle/CheckBoxControl.cs", "Check(), Uncheck(), IsChecked()"),
        [PartCore.Range] = Simple("a simple control: range", "RangeControlBase", "srcnew/Brinell.Html/Controls/Range/RangeInputControl.cs", "SetValue(value), GetValue()"),
        [PartCore.Date] = Simple("a simple control: date", "FocusableControlBase", "srcnew/Brinell.Html/Controls/DateTime/DateInputControl.tpl.cs", "SetDate(date), GetDate()"),
        [PartCore.Clickable] = Simple("a simple control: clickable", "ClickableControlBase", "srcnew/Brinell.Html/Controls/Buttons/ButtonControl.tpl.cs", "Click(); GetText()"),
        [PartCore.Readable] = Simple("a simple control: text to read", "ControlBase", "srcnew/Brinell.Html/Controls/Display/LabelControl.cs", "GetText(); a Get*Core per thing it shows"),
    };

    private static readonly Hint Component = new(
        "a component: several parts a test uses", "ContainerBase<TParent, TSelf>", "srcnew/Brinell.Html/Controls/ContainerBase.cs",
        "samples/Todo/tests/Brinell.Samples.Todo.Web.UITests/Pages/Parts.cs",
        "a control per part (each with its own core, from the parts above), and members for what a test does with the whole",
        name => $"public partial class {name}<TParent> : ContainerBase<TParent, {name}<TParent>> where TParent : IHtmlScope<TParent>   (as TodoStatus in the example: Self, a constructor taking the parent and a locator, a property per part)");

    private static IEnumerable<string> ReadOnly(Hint hint, string? root)
    {
        string Path(string relative) => root is null ? relative + " (in the Brinell repository)" : System.IO.Path.Combine(root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        yield return Path(".github/skills/html-control/SKILL.md");
        yield return Path(hint.BaseFile) + "   (the base: its *Core methods)";
        yield return Path(hint.Example) + "   (a control of this kind)";
    }

    private static string PatternName(ScrapeProject project, string signature, string? variant, bool lifted)
    {
        var index = project.Index();
        var pages = index.Pages.Select(p => (p.Key, index.Read(p.Latest))).ToArray();
        var patterns = PatternDetector.Detect(pages, [], int.MaxValue, project.Config.TestIds).Where(p => p.Signature == signature && p.Lifted == lifted).ToArray();
        return (patterns.FirstOrDefault(p => p.Variant == variant) ?? patterns.FirstOrDefault())?.Name ?? "Part";
    }

    private static string Where(IReadOnlyList<Occurrence> occurrences)
    {
        var byPage = occurrences.GroupBy(o => o.Page, StringComparer.Ordinal).ToArray();
        var shown = byPage.Take(MaxPages).Select(g => $"{g.Key} (node {string.Join(", ", g.Take(NodesPerPage).Select(o => o.Root.Number))}{(g.Count() > NodesPerPage ? $", +{g.Count() - NodesPerPage}" : "")})");
        return string.Join("; ", shown) + (byPage.Length > MaxPages ? $"; +{byPage.Length - MaxPages} more pages" : "");
    }

    // The node's parent, then its subtree: every element, with the attributes that say what it is.
    private static string Outline(ScrapeSnapshot snapshot, ScrapeNode root, int maxLines)
    {
        var text = new StringBuilder();
        var depth = new Dictionary<int, int>();
        var start = 0;
        if (root.Parent > 0)
        {
            text.Append(Line(snapshot[root.Parent])).Append('\n');
            start = 1;
        }
        var lines = start;
        foreach (var node in ControlKind.Subtree(snapshot, root))
        {
            var level = node.Number == root.Number ? start : depth[node.Parent] + 1;
            depth[node.Number] = level;
            if (lines == maxLines)
            {
                text.Append("  … more: get_page_snapshot for the rest\n");
                break;
            }
            text.Append(' ', level * 2).Append(Line(node)).Append('\n');
            lines++;
        }
        return text.ToString();
    }

    private static string Line(ScrapeNode node)
    {
        if (node.Kind == ScrapeNodeKind.ShadowRoot) return $"#{node.Number} (shadow root)";
        var line = new StringBuilder().Append('#').Append(node.Number).Append(' ').Append(node.Type);
        if (node.Attribute("id") is { Length: > 0 } id) line.Append(" #").Append(id);
        if (node.Attribute("class") is { Length: > 0 } classes)
            line.Append(' ').Append(string.Concat(classes.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(6).Select(c => "." + c)));
        if (node.Role is { Length: > 0 } role) line.Append(" role=").Append(role);
        foreach (var key in FactAttributes)
            if (node.Attribute(key) is { } value)
                line.Append(' ').Append(key).Append(value.Length > 0 ? "=" + Short(value, 40) : "");
        foreach (var data in node.Attributes.Where(a => a.Key.StartsWith("data-", StringComparison.Ordinal)).Take(3))
            line.Append(' ').Append(data.Key).Append('=').Append(Short(data.Value, 30));
        if (node.Name is { Length: > 0 } name && node.Attribute("aria-label") is null) line.Append(" name=\"").Append(Short(name, 40)).Append('"');
        if (node.Text is { Length: > 0 } text && text.Trim('​', ' ').Length > 0) line.Append(" \"").Append(Short(text.Trim(), 50)).Append('"');
        return line.ToString();
    }

    // Each element a test reads or acts on, in the example and then in other occurrences (a part only some
    // have), with a locator relative to the pattern's node and in how many occurrences it finds something.
    private static string Parts(Occurrence example, IReadOnlyList<Occurrence> occurrences, IReadOnlyList<string> testIds)
    {
        var text = new StringBuilder();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var sources = new[] { example }.Concat(occurrences.Where(o => o != example).Take(PartSources - 1));
        foreach (var (occurrence, node, reading) in sources.SelectMany(o => ControlKind.Below(o.Snapshot, o.Root).Select(b => (o, b.Node, b.Reading))))
        {
            if (node.Number == occurrence.Root.Number) continue;
            if (Locator(node, testIds) is not { } locator || !seen.Add(locator)) continue;
            if (seen.Count > MaxParts)
            {
                text.Append("  … more parts: get_page_snapshot for the rest\n");
                break;
            }
            var selector = SnapshotSelector.Parse(locator);
            var found = selector is null ? 0 : occurrences.Count(o => selector.Under(o.Snapshot, o.Root).Count > 0);
            var many = selector is null ? 0 : selector.Under(occurrence.Snapshot, occurrence.Root).Count;
            var facts = Line(node);
            var where = occurrence == example ? "" : $" (seen on {occurrence.Page}, node {node.Number})";
            text.Append(CultureInfo.InvariantCulture, $"  {locator,-36} {found,4} of {occurrences.Count,-4} {reading.Core.ToString().ToLowerInvariant()} ({reading.Because}){(many > 1 ? $", {many} in one occurrence" : "")}   {facts[(facts.IndexOf(' ', StringComparison.Ordinal) + 1)..]}{where}\n");
        }
        return seen.Count == 0 ? "  (none: nothing below the node is read or acted on)\n" : text.ToString();
    }

    // A CSS locator for a part, inside the pattern's node: a test id; else its tag with a class that says
    // what it is; an input's type; a role or an ARIA attribute; else the tag.
    private static string? Locator(ScrapeNode node, IReadOnlyList<string> testIds)
    {
        foreach (var testId in testIds)
            if (node.Attribute(testId) is { Length: > 0 } value)
                return $"[{testId}={Quote(value)}]";
        var naming = (node.Attribute("class") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(PatternDetector.IsNamingClass);
        if (naming is not null) return $"{node.Type}.{naming}";
        if (node.Type == "input") return node.Attribute("type") is { Length: > 0 } type ? $"input[type={type}]" : "input";
        if (node.Role is { Length: > 0 } role) return $"{node.Type}[role={role}]";
        if (node.Attribute("aria-haspopup") is not null) return $"{node.Type}[aria-haspopup]";
        return node.Type.Length > 0 ? node.Type : null;
    }

    private static string Quote(string value) => value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_') ? value : "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string Short(string value, int max) => value.Length <= max ? value : value[..max] + "…";

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
