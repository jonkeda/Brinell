using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public sealed class ControlObjectMatcher
{
    private const double Threshold = 0.75;
    private const double TagWeight = 0.4;
    private const double ClassWeight = 0.3;
    private const double ChildWeight = 0.3;

    private static readonly HashSet<string> ActionableTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "form", "table", "nav", "fieldset", "ul", "ol",
    };

    private static readonly string[] WidgetClassPatterns =
    {
        "form", "table", "nav", "menu", "card", "dialog", "panel", "widget",
    };

    private readonly CssSignatureParser _parser;

    public ControlObjectMatcher(CssSignatureParser parser)
    {
        _parser = parser;
    }

    public List<ControlObjectMatch> MatchAll(
        DomSnapshot snapshot, IReadOnlyList<GeneratedControl> controls)
    {
        var matches = new List<ControlObjectMatch>();
        if (snapshot.RootElement is null || controls.Count == 0)
            return matches;

        foreach (var (element, xpath) in WalkActionable(snapshot.RootElement))
        {
            foreach (var control in controls)
            {
                var (score, reason) = ScoreMatch(element, control);
                if (score >= Threshold)
                {
                    matches.Add(new ControlObjectMatch
                    {
                        Element = element,
                        Control = control,
                        Score = score,
                        Reason = reason,
                        XPath = xpath,
                    });
                }
            }
        }

        return matches
            .GroupBy(m => m.XPath)
            .Select(g => g.OrderByDescending(m => m.Score).ThenBy(m => m.Control.Name).First())
            .OrderBy(m => m.XPath, StringComparer.Ordinal)
            .ToList();
    }

    private (double Score, string Reason) ScoreMatch(DomElement element, GeneratedControl control)
    {
        var signature = _parser.Parse(control.DomSignature);
        if (!TagMatches(element, signature))
            return (0, "");

        var classScore = ClassOverlap(element, signature);
        var childScore = ChildStructureMatch(element, signature);
        var score = TagWeight + ClassWeight * classScore + ChildWeight * childScore;
        var reason = $"Matched signature {control.DomSignature} ({score:P0})";
        return (score, reason);
    }

    private static bool TagMatches(DomElement element, ParsedSignature signature)
    {
        if (string.IsNullOrEmpty(signature.Tag) || signature.Tag == "*")
            return true;
        return string.Equals(element.Tag, signature.Tag, StringComparison.OrdinalIgnoreCase);
    }

    private static double ClassOverlap(DomElement element, ParsedSignature signature)
    {
        if (signature.Classes.Count == 0)
            return 1.0;

        var elementClasses = SplitClasses(element.ClassName);
        if (elementClasses.Count == 0)
            return 0;

        var sigSet = new HashSet<string>(signature.Classes, StringComparer.OrdinalIgnoreCase);
        var elSet = new HashSet<string>(elementClasses, StringComparer.OrdinalIgnoreCase);

        var intersection = sigSet.Intersect(elSet, StringComparer.OrdinalIgnoreCase).Count();
        var union = sigSet.Union(elSet, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static double ChildStructureMatch(DomElement element, ParsedSignature signature)
    {
        var sigChildTags = FlattenChildTags(signature).Take(3).ToList();
        if (sigChildTags.Count == 0)
            return 1.0;

        var elChildTags = element.Children.Take(3).Select(c => c.Tag).ToList();
        if (elChildTags.Count == 0)
            return 0;

        var matched = 0;
        for (var i = 0; i < sigChildTags.Count && i < elChildTags.Count; i++)
        {
            if (string.Equals(sigChildTags[i], elChildTags[i], StringComparison.OrdinalIgnoreCase))
                matched++;
        }
        return (double)matched / sigChildTags.Count;
    }

    private static IEnumerable<string> FlattenChildTags(ParsedSignature signature)
    {
        var current = signature;
        while (current.Children.Count > 0)
        {
            var next = current.Children[0];
            if (!string.IsNullOrEmpty(next.Tag))
                yield return next.Tag;
            current = next;
        }
    }

    private static List<string> SplitClasses(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return [];
        return className
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static IEnumerable<(DomElement Element, string XPath)> WalkActionable(DomElement root)
    {
        var counters = new Dictionary<DomElement, Dictionary<string, int>>();
        var stack = new Stack<(DomElement Element, string ParentPath)>();
        stack.Push((root, ""));

        while (stack.Count > 0)
        {
            var (el, parentPath) = stack.Pop();
            var path = string.IsNullOrEmpty(parentPath) ? "/" + el.Tag : parentPath + "/" + el.Tag;

            if (IsActionable(el))
                yield return (el, path);

            // Push children in reverse so traversal order is left-to-right.
            for (var i = el.Children.Count - 1; i >= 0; i--)
            {
                var child = el.Children[i];
                stack.Push((child, $"{path}[{i + 1}]"));
            }
        }
    }

    private static bool IsActionable(DomElement el)
    {
        if (ActionableTags.Contains(el.Tag))
            return true;

        if (string.Equals(el.Tag, "div", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(el.Role))
            return true;

        if (!string.IsNullOrWhiteSpace(el.ClassName))
        {
            foreach (var token in SplitClasses(el.ClassName))
            {
                foreach (var pattern in WidgetClassPatterns)
                {
                    if (token.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }

        return false;
    }
}
