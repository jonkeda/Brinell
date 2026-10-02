using System.Text.RegularExpressions;
using Brinell.Core.Scraping;
using Brinell.Scraper.Html;
using Brinell.Scraper.Projects;
using Brinell.Scraper.Service;

namespace Brinell.Scraper.Code;

/// <summary>
/// Finds what the recorded pages are built from, before any model looks: parts that repeat (on several
/// pages, or several times on one) are controls to make; forms, tables, lists, navigation, dialogs and
/// frames are structure a page object names. It works on each page's newest capture.
/// </summary>
public static class PatternDetector
{
    private static readonly HashSet<string> Skipped = new(StringComparer.Ordinal) { "html", "body", "head", "main" };

    // Classes that say how an element looks or what state it is in, not what it is: a modifier
    // (v-btn--icon), spacing and layout utilities (pb-4, mx-6, d-flex, justify-center), themes and
    // states (theme--light, hydrated). A control is not named after them.
    private static readonly Regex Utility = new(
        @"--|^(m|p)[tbrlxya]?-|^(d|align|justify|text|flex|col|order|float|overflow|elevation|rounded|theme|has|is|no|last|first)-|^(row|hydrated|interactive|active|disabled|selected|open|show|hidden)$",
        RegexOptions.CultureInvariant);

    /// <summary>The patterns, most widespread first.</summary>
    /// <param name="pages">Each page's key and its newest capture.</param>
    /// <param name="controls">The controls the project has, to say which pattern one already stands for.</param>
    /// <param name="max">The most patterns given.</param>
    /// <param name="testIds">The attributes that are test ids, preferred first, to name a pattern by.</param>
    public static IReadOnlyList<Pattern> Detect(IReadOnlyList<(string Page, ScrapeSnapshot Snapshot)> pages, IReadOnlyList<CodeClass> controls, int max = 30, IReadOnlyList<string>? testIds = null)
    {
        var occurrences = new Dictionary<string, List<(string Page, ScrapeSnapshot Snapshot, ScrapeNode Node)>>(StringComparer.Ordinal);
        foreach (var (page, snapshot) in pages)
        {
            var sizes = SubtreeSizes(snapshot);
            foreach (var node in snapshot.Nodes)
            {
                if (node.Kind != ScrapeNodeKind.Element || Skipped.Contains(node.Type) || sizes[node.Number] < 3) continue;
                var signature = NodeSignature.Of(snapshot, node);
                if (!occurrences.TryGetValue(signature, out var list)) occurrences[signature] = list = [];
                list.Add((page, snapshot, node));
            }
        }

        var bySignature = controls.Where(c => c.Header?.Control is not null).GroupBy(c => c.Header!.Control!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().Name, StringComparer.Ordinal);
        var patterns = new List<(Pattern Pattern, HashSet<(string, int)> At, int Depth)>();
        foreach (var (signature, found) in occurrences)
        {
            var first = found[0];
            var kind = KindOf(first.Snapshot, first.Node);
            var pageCount = found.Select(f => f.Page).Distinct(StringComparer.Ordinal).Count();
            var repeats = pageCount >= 2 || found.Count >= 3;
            if (!repeats && kind == PatternKind.Repeated) continue;
            var shared = SharedName(found.Select(f => f.Node).ToArray(), testIds ?? HtmlAttributes.TestIds);
            var parts = signature.Split('|');
            var named = NamingClass(parts, first.Node);
            // A wrapper or layout cell with nothing that says what it is (div|||div, td.pb-4) groups unrelated elements: not a control.
            if (kind == PatternKind.Repeated && shared is null && named is null && parts[1].Length == 0 && !parts[0].Contains('-')) continue;
            var pattern = new Pattern(
                signature,
                repeats && kind == PatternKind.Repeated ? PatternKind.Repeated : kind,
                pageCount,
                found.Count,
                found.Take(3).Select(f => new PatternExample(f.Page, f.Node.Number, SnapshotOutline.Line(f.Node))).ToArray(),
                bySignature.GetValueOrDefault(signature),
                shared ?? NameOf(parts, named));
            patterns.Add((pattern, found.Select(f => (f.Page, f.Node.Number)).ToHashSet(), Ancestors(first.Snapshot, first.Node.Number).Count()));
        }

        // A part of a pattern that occurs exactly where the pattern does (a card's body in every card) is
        // the same pattern: only the outer one is given.
        // Outer before inner: at equal spread, the shallower pattern first.
        var ordered = patterns.OrderByDescending(p => p.Pattern.Pages).ThenByDescending(p => p.Pattern.Occurrences).ThenBy(p => p.Depth).ThenBy(p => p.Pattern.Signature, StringComparer.Ordinal).ToList();
        var kept = new List<(Pattern Pattern, HashSet<(string, int)> At, int Depth)>();
        var snapshots = pages.ToDictionary(p => p.Page, p => p.Snapshot, StringComparer.Ordinal);
        foreach (var candidate in ordered)
        {
            var inner = kept.Any(outer => outer.Pattern.Occurrences == candidate.Pattern.Occurrences
                && candidate.At.All(at => Ancestors(snapshots[at.Item1], at.Item2).Any(a => outer.At.Contains((at.Item1, a)))));
            if (!inner) kept.Add(candidate);
            if (kept.Count == max) break;
        }
        // Two patterns can come to the same name (a field with and without its label): the later ones are numbered.
        var taken = new Dictionary<string, int>(StringComparer.Ordinal);
        return kept.Select(k =>
        {
            var count = taken[k.Pattern.Name] = taken.GetValueOrDefault(k.Pattern.Name) + 1;
            return count == 1 ? k.Pattern : k.Pattern with { Name = k.Pattern.Name + count };
        }).ToArray();
    }

    // A control's name: a test id or id all occurrences share (numbers left out: row-1, row-2 is Row),
    // else the first stable class, the role, the type.
    private static string? SharedName(IReadOnlyList<ScrapeNode> nodes, IReadOnlyList<string> testIds)
    {
        foreach (var attribute in testIds.Append("id"))
        {
            var names = nodes.Select(n => Pascal(n.Attribute(attribute))).Distinct(StringComparer.Ordinal).ToArray();
            if (names is [{ Length: > 0 } shared]) return shared;
        }
        return null;
    }

    // The first class that says what the element is, in its own order (the component's class usually comes first).
    private static string? NamingClass(string[] parts, ScrapeNode example)
    {
        var stable = parts[2].Split('.', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        return (example.Attribute("class") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(c => stable.Contains(c) && !Utility.IsMatch(c));
    }

    // A custom element's tag (b7-navbar-button) is its name; else its naming class, its role, its type.
    private static string NameOf(string[] parts, string? named)
    {
        foreach (var source in new[] { parts[0].Contains('-') ? parts[0] : null, named, parts[1], parts[0] })
            if (Pascal(source) is { Length: > 0 } name) return name;
        return "Part";
    }

    private static string Pascal(string? text) => text is null ? "" : string.Concat(
        Explore.DangerWords.Words(text).Where(w => !w.All(char.IsDigit)).Take(4).Select(w => char.ToUpperInvariant(w[0]) + w[1..]));

    private static PatternKind KindOf(ScrapeSnapshot snapshot, ScrapeNode node) => node switch
    {
        { Type: "form" } or { Role: "form" } => PatternKind.Form,
        { Type: "table" } or { Role: "grid" or "table" } => PatternKind.Table,
        { Type: "ul" or "ol" } or { Role: "list" or "listbox" or "menu" } when snapshot.ChildrenOf(node.Number).Count() >= 2 => PatternKind.List,
        { Type: "nav" } or { Role: "navigation" or "tablist" } => PatternKind.Navigation,
        { Type: "dialog" } or { Role: "dialog" or "alertdialog" } => PatternKind.Dialog,
        { Frame: not null } => PatternKind.Frame,
        _ => PatternKind.Repeated,
    };

    private static int[] SubtreeSizes(ScrapeSnapshot snapshot)
    {
        var sizes = new int[snapshot.Nodes.Count + 1];
        for (var i = snapshot.Nodes.Count - 1; i >= 0; i--)
        {
            var node = snapshot.Nodes[i];
            sizes[node.Number] += 1;
            if (node.Parent > 0) sizes[node.Parent] += sizes[node.Number];
        }
        return sizes;
    }

    private static IEnumerable<int> Ancestors(ScrapeSnapshot snapshot, int number)
    {
        for (var p = snapshot[number].Parent; p != 0; p = snapshot[p].Parent)
            yield return p;
    }
}

/// <summary>What a pattern looks like it is.</summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<PatternKind>))]
public enum PatternKind
{
    /// <summary>A part that repeats: a card, a row, a field with its label.</summary>
    Repeated,

    /// <summary>A form.</summary>
    Form,

    /// <summary>A table or grid.</summary>
    Table,

    /// <summary>A list or menu.</summary>
    List,

    /// <summary>Navigation or tabs.</summary>
    Navigation,

    /// <summary>A dialog.</summary>
    Dialog,

    /// <summary>A frame: a document of its own, a <c>FrameScope</c>.</summary>
    Frame,
}

/// <summary>A pattern: its signature, kind, on how many pages and how often, examples, the control that stands for it if one does, and the name a control for it gets.</summary>
public sealed record Pattern(string Signature, PatternKind Kind, int Pages, int Occurrences, IReadOnlyList<PatternExample> Examples, string? Control, string Name);

/// <summary>Where a pattern occurs: the page, the node's number and its outline line.</summary>
public sealed record PatternExample(string Page, int Node, string Line);
