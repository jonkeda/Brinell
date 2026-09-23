using System.Text.RegularExpressions;

namespace Brinell.Scraper.Services;

public static class CodeBlockParser
{
    private static readonly Regex CodeBlockRegex = new(
        @"```(?:csharp|cs)\s*\n(.*?)```",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractCSharpBlocks(string llmResponse)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
            return [];

        var matches = CodeBlockRegex.Matches(llmResponse);

        if (matches.Count > 0)
        {
            return matches
                .Select(m => m.Groups[1].Value.Trim())
                .Where(block => !string.IsNullOrWhiteSpace(block))
                .ToList();
        }

        return TryExtractUnfencedCode(llmResponse);
    }

    private static IReadOnlyList<string> TryExtractUnfencedCode(string response)
    {
        var lines = response.Split('\n');
        var codeLines = new List<string>();
        var inCode = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (!inCode && IsCSharpLine(trimmed))
                inCode = true;

            if (inCode)
                codeLines.Add(line);
        }

        if (codeLines.Count == 0)
            return [];

        return [string.Join('\n', codeLines).Trim()];
    }

    private static bool IsCSharpLine(string line) =>
        line.StartsWith("using ", StringComparison.Ordinal) ||
        line.StartsWith("namespace ", StringComparison.Ordinal) ||
        line.StartsWith("public ", StringComparison.Ordinal) ||
        line.StartsWith("private ", StringComparison.Ordinal) ||
        line.StartsWith("internal ", StringComparison.Ordinal) ||
        line.StartsWith("sealed ", StringComparison.Ordinal) ||
        line.StartsWith("//", StringComparison.Ordinal) ||
        line.StartsWith("[", StringComparison.Ordinal) ||
        line.StartsWith("{", StringComparison.Ordinal);

    public static IReadOnlyList<string> SplitByClassDeclarations(string code)
    {
        var lines = code.Split('\n');
        var classes = new List<string>();
        var preamble = new List<string>();
        var current = new List<string>();
        var braceDepth = 0;
        var inClass = false;
        var preambleCollected = false;

        foreach (var line in lines)
        {
            if (!inClass && !preambleCollected &&
                Regex.IsMatch(line, @"^\s*(public|internal|sealed|abstract|static)\s+.*(class|record|struct)\s+"))
            {
                preamble.AddRange(current);
                current.Clear();
                preambleCollected = true;
                inClass = true;
            }
            else if (!inClass && preambleCollected &&
                Regex.IsMatch(line, @"^\s*(public|internal|sealed|abstract|static)\s+.*(class|record|struct)\s+"))
            {
                inClass = true;
                // Prepend preamble to this class
                current.InsertRange(0, preamble);
            }

            current.Add(line);

            braceDepth += line.Count(c => c == '{') - line.Count(c => c == '}');

            if (inClass && braceDepth == 0 && current.Count > 1)
            {
                classes.Add(string.Join('\n', current).Trim());
                current.Clear();
                inClass = false;
            }
        }

        if (current.Count > 0)
        {
            var remaining = string.Join('\n', current).Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                if (!preambleCollected)
                    classes.Add(remaining);
                else if (classes.Count > 0)
                    classes[^1] += "\n" + remaining;
            }
        }

        return classes.Count > 0 ? classes : [code];
    }
}
