using System.Text;

namespace Brinell.Scraper.Services;

public sealed class ParsedSignature
{
    public string Tag { get; init; } = "";
    public List<string> Classes { get; init; } = [];
    public string? Id { get; init; }
    public List<(string Name, string? Value)> Attributes { get; init; } = [];
    public List<ParsedSignature> Children { get; init; } = [];
}

public sealed class CssSignatureParser
{
    public ParsedSignature Parse(string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return new ParsedSignature();

        var segments = SplitSegments(signature);
        var root = ParseSegment(segments[0]);
        var current = root;
        for (var i = 1; i < segments.Count; i++)
        {
            var child = ParseSegment(segments[i]);
            current.Children.Add(child);
            current = child;
        }
        return root;
    }

    private static List<string> SplitSegments(string signature)
    {
        // Split by combinators: ' ', '>', '+'. Treat all as ordered child segments.
        var segments = new List<string>();
        var sb = new StringBuilder();
        var inBracket = 0;
        foreach (var ch in signature.Trim())
        {
            if (ch == '[') inBracket++;
            else if (ch == ']') inBracket = Math.Max(0, inBracket - 1);

            if (inBracket == 0 && (ch == ' ' || ch == '>' || ch == '+'))
            {
                if (sb.Length > 0)
                {
                    segments.Add(sb.ToString());
                    sb.Clear();
                }
                continue;
            }
            sb.Append(ch);
        }
        if (sb.Length > 0) segments.Add(sb.ToString());
        return segments;
    }

    private static ParsedSignature ParseSegment(string segment)
    {
        string tag = "";
        string? id = null;
        var classes = new List<string>();
        var attrs = new List<(string Name, string? Value)>();

        var i = 0;
        // Read leading tag (letters, digits, '-', '*')
        while (i < segment.Length && IsTagChar(segment[i]))
        {
            tag += segment[i];
            i++;
        }

        while (i < segment.Length)
        {
            var ch = segment[i];
            if (ch == '.')
            {
                i++;
                var start = i;
                while (i < segment.Length && IsIdentChar(segment[i])) i++;
                if (i > start) classes.Add(segment[start..i]);
            }
            else if (ch == '#')
            {
                i++;
                var start = i;
                while (i < segment.Length && IsIdentChar(segment[i])) i++;
                if (i > start) id = segment[start..i];
            }
            else if (ch == '[')
            {
                i++;
                var start = i;
                while (i < segment.Length && segment[i] != ']') i++;
                var inner = segment[start..i];
                if (i < segment.Length) i++; // skip ']'
                var eq = inner.IndexOf('=');
                if (eq < 0)
                {
                    attrs.Add((inner.Trim(), null));
                }
                else
                {
                    var name = inner[..eq].Trim();
                    var value = inner[(eq + 1)..].Trim().Trim('"', '\'');
                    attrs.Add((name, value));
                }
            }
            else
            {
                i++;
            }
        }

        return new ParsedSignature
        {
            Tag = tag,
            Classes = classes,
            Id = id,
            Attributes = attrs,
        };
    }

    private static bool IsTagChar(char c) => char.IsLetterOrDigit(c) || c == '-' || c == '*';
    private static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '-' || c == '_';
}
