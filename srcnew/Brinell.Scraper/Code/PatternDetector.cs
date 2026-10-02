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

    // Where a control's root stops: the page's structure, a row of a list or table, a dialog.
    private static readonly HashSet<string> Landmarks = new(StringComparer.Ordinal)
        { "html", "body", "main", "form", "fieldset", "section", "article", "nav", "header", "footer", "aside", "table", "thead", "tbody", "tr", "ul", "ol", "li", "dialog" };

    private const int MaxLift = 4;

    // Classes that say how an element looks or what state it is in, not what it is: a modifier
    // (v-btn--icon), spacing and layout utilities (pb-4, mx-6, d-flex, justify-center), themes and
    // states (theme--light, hydrated). A control is not named after them.
    private static readonly Regex Utility = new(
        @"--|^(m|p)[tbrlxya]?-|^(d|align|justify|text|flex|col|offset|order|float|overflow|elevation|rounded|theme|has|is|no|last|first)-|^(row|hydrated|interactive|active|disabled|selected|open|show|hidden)$",
        RegexOptions.CultureInvariant);

    /// <summary>The patterns, most widespread first.</summary>
    /// <param name="pages">Each page's key and its newest capture.</param>
    /// <param name="controls">The controls the project has, to say which pattern one already stands for.</param>
    /// <param name="max">The most patterns given.</param>
    /// <param name="testIds">The attributes that are test ids, preferred first, to name a pattern by.</param>
    public static IReadOnlyList<Pattern> Detect(IReadOnlyList<(string Page, ScrapeSnapshot Snapshot)> pages, IReadOnlyList<CodeClass> controls, int max = 30, IReadOnlyList<string>? testIds = null)
    {
        var signatures = new Dictionary<string, string?[]>(StringComparer.Ordinal);
        var occurrences = new Dictionary<string, List<(string Page, ScrapeSnapshot Snapshot, ScrapeNode Node)>>(StringComparer.Ordinal);
        foreach (var (page, snapshot) in pages)
        {
            var sizes = SubtreeSizes(snapshot);
            var all = signatures[page] = Signatures(snapshot);
            foreach (var node in snapshot.Nodes)
            {
                if (node.Kind != ScrapeNodeKind.Element || Skipped.Contains(node.Type) || sizes[node.Number] < 3) continue;
                var signature = all[node.Number]!;
                if (!occurrences.TryGetValue(signature, out var list)) occurrences[signature] = list = [];
                list.Add((page, snapshot, node));
            }
        }

        // An inner wrapper (a field's control box) starts higher up, at the root that also holds its label
        // (.my/ScraperEx/control-brief-plan.md, 3.3). A lifted pattern keeps the inner signature, which all
        // its occurrences share; the roots' own classes vary (a field's has-c, text-align-undefined). A root
        // that several patterns reach (its own signature, an inner one lifted to it) goes to the pattern
        // with the most occurrences.
        var claims = new Dictionary<(string Page, int Root), List<(string Signature, bool Lifted)>>();
        var roots = new Dictionary<(string Page, int Root), (string Page, ScrapeSnapshot Snapshot, ScrapeNode Node)>();
        var sizes2 = new Dictionary<(string Signature, bool Lifted), HashSet<(string, int)>>();
        foreach (var (signature, found) in occurrences)
        {
            var lift = KindOf(found[0].Snapshot, found[0].Node) == PatternKind.Repeated;
            foreach (var (page, snapshot, node) in found)
            {
                var root = lift ? Root(snapshot, node, signature, signatures[page]) : node;
                var claim = (signature, root.Number != node.Number);
                var at = (page, root.Number);
                if (!claims.TryGetValue(at, out var list)) claims[at] = list = [];
                list.Add(claim);
                roots[at] = (page, snapshot, root);
                if (!sizes2.TryGetValue(claim, out var set)) sizes2[claim] = set = [];
                set.Add(at);
            }
        }
        var rooted = new Dictionary<(string Signature, bool Lifted), List<(string Page, ScrapeSnapshot Snapshot, ScrapeNode Node)>>();
        foreach (var (at, list) in claims)
        {
            var best = list.MaxBy(c => sizes2[c].Count);
            if (!rooted.TryGetValue(best, out var group)) rooted[best] = group = [];
            group.Add(roots[at]);
        }

        // One pattern that is several controls (a field that is a select here and a text box there):
        // a pattern per variant (3.4).
        var groups = rooted.SelectMany(r =>
        {
            var variants = r.Value.GroupBy(f => ControlKind.Variant(f.Snapshot, f.Node), StringComparer.Ordinal).ToArray();
            return variants.Length == 1
                ? [(r.Key.Signature, r.Key.Lifted, Variant: (string?)null, Found: r.Value)]
                : variants.Select(v => (r.Key.Signature, r.Key.Lifted, Variant: (string?)v.Key, Found: v.ToList()));
        }).ToArray();

        var bySignature = controls.Where(c => c.Header?.Control is not null).GroupBy(c => c.Header!.Control!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().Name, StringComparer.Ordinal);
        var patterns = new List<(Pattern Pattern, HashSet<(string, int)> At, int Depth, ScrapeNode Example, HashSet<string> Common)>();
        foreach (var (signature, lifted, variant, found) in groups)
        {
            var first = found[0];
            var kind = KindOf(first.Snapshot, first.Node);
            var pageCount = found.Select(f => f.Page).Distinct(StringComparer.Ordinal).Count();
            var repeats = pageCount >= 2 || found.Count >= 3;
            if (!repeats && kind == PatternKind.Repeated) continue;
            var shared = SharedName(found.Select(f => f.Node).ToArray(), testIds ?? HtmlAttributes.TestIds);
            // A lifted pattern is named after its root, what the person sees.
            var parts = (lifted ? signatures[first.Page][first.Node.Number]! : signature).Split('|');
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
                shared ?? NameOf(parts, named))
            { Variant = variant, Lifted = lifted };
            // The classes every occurrence has: what tells this pattern from another of the same name.
            var common = found.Skip(1).Aggregate(Classes(first.Node).ToHashSet(StringComparer.Ordinal), (set, f) => { set.IntersectWith(Classes(f.Node)); return set; });
            patterns.Add((pattern, found.Select(f => (f.Page, f.Node.Number)).ToHashSet(), Ancestors(first.Snapshot, first.Node.Number).Count(), first.Node, common));
        }

        // A part of a pattern that occurs exactly where the pattern does (a card's body in every card) is
        // the same pattern: only the outer one is given.
        // Outer before inner: at equal spread, the shallower pattern first.
        var ordered = patterns.OrderByDescending(p => p.Pattern.Pages).ThenByDescending(p => p.Pattern.Occurrences).ThenBy(p => p.Depth).ThenBy(p => p.Pattern.Signature, StringComparer.Ordinal).ToList();
        var kept = new List<(Pattern Pattern, HashSet<(string, int)> At, int Depth, ScrapeNode Example, HashSet<string> Common)>();
        var snapshots = pages.ToDictionary(p => p.Page, p => p.Snapshot, StringComparer.Ordinal);
        foreach (var candidate in ordered)
        {
            // Only inside a repeated part: a form or a table that happens to hold one field per page is not that field.
            var inner = kept.Any(outer => outer.Pattern.Kind == PatternKind.Repeated && outer.Pattern.Occurrences == candidate.Pattern.Occurrences
                && candidate.At.All(at => Ancestors(snapshots[at.Item1], at.Item2).Any(a => outer.At.Contains((at.Item1, a)))));
            if (!inner) kept.Add(candidate);
            if (kept.Count == max) break;
        }
        // Patterns that come to the same name (a select and a text field, both .v-input) are told apart by
        // a class only one of them has, else by their variant; what is still the same is numbered.
        var distinct = kept.Select(k => k.Pattern).ToArray();
        foreach (var same in kept.Select((k, i) => (k, i)).GroupBy(x => x.k.Pattern.Name, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            foreach (var (k, i) in same)
            {
                var others = same.Where(o => o.i != i).SelectMany(o => o.k.Common).ToHashSet(StringComparer.Ordinal);
                var own = Classes(k.Example).FirstOrDefault(c => k.Common.Contains(c) && IsNamingClass(c) && !others.Contains(c));
                distinct[i] = own is not null ? k.Pattern with { Name = Pascal(own) }
                    : k.Pattern.Variant is { } variant ? k.Pattern with { Name = k.Pattern.Name + VariantWord(variant) }
                    : k.Pattern;
            }
        }
        var taken = new Dictionary<string, int>(StringComparer.Ordinal);
        return distinct.Select(p =>
        {
            var count = taken[p.Name] = taken.GetValueOrDefault(p.Name) + 1;
            return count == 1 ? p : p with { Name = p.Name + count };
        }).ToArray();
    }

    private static string VariantWord(string variant) => string.Concat(variant.Split('+').Select(core => core switch
    {
        "selector" => "Select",
        "clickable" => "Button",
        "readable" => "Label",
        _ => char.ToUpperInvariant(core[0]) + core[1..],
    }));

    /// <summary>
    /// Where a control made from a pattern starts: the pattern's node lifted to the root that also holds
    /// its label (as <see cref="Detect"/> does), for the brief and the check of a lifted pattern.
    /// </summary>
    public static ScrapeNode RootOf(ScrapeSnapshot snapshot, ScrapeNode node)
    {
        var all = SignatureCache.GetValue(snapshot, Signatures);
        return Root(snapshot, node, all[node.Number] ?? "", all);
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ScrapeSnapshot, string?[]> SignatureCache = new();

    private static string?[] Signatures(ScrapeSnapshot snapshot)
    {
        var all = new string?[snapshot.Nodes.Count + 1];
        foreach (var node in snapshot.Nodes)
            if (node.Kind == ScrapeNodeKind.Element)
                all[node.Number] = NodeSignature.Of(snapshot, node);
        return all;
    }

    // Walks up from a pattern's node while the ancestor is still this one control: it holds no other
    // occurrence, adds nothing interactive, and is not a landmark. The root is the highest such
    // ancestor that adds something of its own: a label or a message, or it is the block whose element the
    // node is (v-input__control in v-input). A plain wrapper above that is left out.
    private static ScrapeNode Root(ScrapeSnapshot snapshot, ScrapeNode node, string signature, string?[] signatures)
    {
        var current = node;
        var best = node;
        var inside = ControlKind.Subtree(snapshot, node).Select(n => n.Number).ToHashSet();
        for (var level = 0; level < MaxLift && current.Parent > 0; level++)
        {
            var up = snapshot[current.Parent];
            if (Landmarks.Contains(up.Type) || up.Role is "form" or "group" or "row" or "listitem" or "list" or "grid" or "navigation" or "dialog" or "main")
                break;
            var added = ControlKind.Subtree(snapshot, up).Where(n => !inside.Contains(n.Number)).ToArray();
            if (added.Any(n => n.Number != node.Number && signatures[n.Number] == signature)) break;
            var readings = added.Select(n => ControlKind.Of(snapshot, n)).OfType<CoreReading>().ToArray();
            if (readings.Any(r => r.Core != PartCore.Readable)) break;
            var block = Classes(current).Any(c => c.IndexOf("__", StringComparison.Ordinal) is > 0 and var at && Classes(up).Contains(c[..at]));
            foreach (var n in added) inside.Add(n.Number);
            current = up;
            if (block || readings.Length > 0) best = up;
        }
        return best;
    }

    private static string[] Classes(ScrapeNode node) => (node.Attribute("class") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

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

    /// <summary>Whether a class says what an element is: stable, and not a modifier, a utility or a state.</summary>
    internal static bool IsNamingClass(string name) => NodeSignature.IsStable(name) && !Utility.IsMatch(name);

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
public sealed record Pattern(string Signature, PatternKind Kind, int Pages, int Occurrences, IReadOnlyList<PatternExample> Examples, string? Control, string Name)
{
    /// <summary>
    /// When occurrences of one signature are different controls: which of them this is, by what a test
    /// does with it (<see cref="ControlKind.Variant"/>, such as <c>selector</c>); null when they are all alike.
    /// </summary>
    public string? Variant { get; init; }

    /// <summary>
    /// Whether its occurrences are the pattern's nodes lifted to their root (<see cref="PatternDetector.RootOf"/>):
    /// <see cref="Signature"/> is then the inner node's, and <see cref="Examples"/> are the roots.
    /// </summary>
    public bool Lifted { get; init; }
}

/// <summary>Where a pattern occurs: the page, the node's number and its outline line.</summary>
public sealed record PatternExample(string Page, int Node, string Line);
