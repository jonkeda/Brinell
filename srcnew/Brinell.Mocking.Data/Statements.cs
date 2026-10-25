using System.Text.RegularExpressions;

namespace Brinell.Mocking.Data;

/// <summary>Command text: normalized, classified as a write, and the objects it names.</summary>
public static class Statements
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex Writes = new(@"\b(INSERT|UPDATE|DELETE|MERGE)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Named = new(@"\b(?:FROM|JOIN|INTO|UPDATE|MERGE(?:\s+INTO)?|USING)\s+(?<name>(?>(?:\[[^\]]+\]|""[^""]+""|\w+)(?:\.(?:\[[^\]]+\]|""[^""]+""|\w+))*))(?!\()", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Procedures = new(@"\bEXEC(?:UTE)?\s+(?:@\w+\s*=\s*)?(?<name>(?:\[[^\]]+\]|""[^""]+""|\w+)(?:\.(?:\[[^\]]+\]|""[^""]+""|\w+))*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Strings = new(@"N?'(?:[^']|'')*'", RegexOptions.Compiled);
    private static readonly Regex Numbers = new(@"(?<![\w@\]\.$])-?\d+(?:\.\d+)?(?![\w\]])", RegexOptions.Compiled);

    /// <summary>
    /// The text with its literals as <c>?</c>: strings (<c>N'Hoorn'</c>) and numbers. A batch sent as text, or a constant EF
    /// inlines, carries values; the log keeps none.
    /// </summary>
    public static string WithoutLiterals(string text) => Numbers.Replace(Strings.Replace(text, "?"), "?");

    /// <summary>The text with its whitespace collapsed to single spaces.</summary>
    public static string Normalize(string text) => Whitespace.Replace(text, " ").Trim();

    /// <summary>Whether the command changes the database.</summary>
    public static bool IsWrite(string text) => Writes.IsMatch(text);

    /// <summary>
    /// The objects a command names: tables and views after <c>FROM</c>, <c>JOIN</c>, <c>INTO</c>, <c>UPDATE</c>,
    /// <c>MERGE</c> or <c>USING</c>, not a function called there (<c>OPENJSON(</c>); procedures after <c>EXEC</c>; as <c>schema.name</c> in lower case, <c>dbo</c> when none is written.
    /// What a view's or procedure's body touches comes from the schema snapshot's references.
    /// </summary>
    public static IEnumerable<string> Objects(string text)
    {
        var normalized = Normalize(text);
        foreach (Match match in Named.Matches(normalized).Cast<Match>().Concat(Procedures.Matches(normalized).Cast<Match>()))
        {
            var parts = match.Groups["name"].Value.Split('.').Select(part => part.Trim('[', ']', '"').ToLowerInvariant()).ToArray();
            if (parts[parts.Length - 1] is "sp_executesql" or "sys")
            {
                continue;
            }

            yield return parts.Length == 1 ? "dbo." + parts[0] : parts[parts.Length - 2] + "." + parts[parts.Length - 1];
        }
    }
}
