using System.Text.RegularExpressions;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public static class PromptTruncator
{
    // Heuristic char/token ratio used by the spec (chars / 4 ≈ tokens).
    private const int CharsPerToken = 4;

    public static int EstimateTokens(string prompt) =>
        string.IsNullOrEmpty(prompt) ? 0 : prompt.Length / CharsPerToken;

    public static string? TruncatePageObjectPrompt(
        string prompt, DomSnapshot snapshot, int maxChars)
    {
        _ = snapshot; // reserved — caller may use snapshot to reconstruct
        if (string.IsNullOrEmpty(prompt) || prompt.Length <= maxChars)
            return prompt;

        // Strip non-actionable HTML chunks (script/style/comments).
        var stripped = Regex.Replace(prompt, @"<script\b[^>]*>.*?</script>", "",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        stripped = Regex.Replace(stripped, @"<style\b[^>]*>.*?</style>", "",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        stripped = Regex.Replace(stripped, @"<!--.*?-->", "", RegexOptions.Singleline);

        if (stripped.Length <= maxChars)
            return stripped;

        // Collapse whitespace runs.
        var compact = Regex.Replace(stripped, @"[ \t]+", " ");
        compact = Regex.Replace(compact, @"\n\s*\n+", "\n");

        if (compact.Length <= maxChars)
            return compact;

        // Drop deeply-nested / non-actionable element lines (rough heuristic):
        // keep lines that mention an actionable tag, drop the rest.
        var lines = compact.Split('\n');
        var kept = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            if (IsActionableLine(line) || !LooksLikeHtmlLine(line))
                kept.Add(line);
        }
        var pruned = string.Join('\n', kept);
        if (pruned.Length <= maxChars)
            return pruned;

        return null;
    }

    private static bool IsActionableLine(string line) =>
        Regex.IsMatch(line,
            @"<\s*(input|button|select|textarea|a|img|label|form|nav|table)\b",
            RegexOptions.IgnoreCase);

    private static bool LooksLikeHtmlLine(string line) =>
        Regex.IsMatch(line, @"<\s*[a-zA-Z]");
}
