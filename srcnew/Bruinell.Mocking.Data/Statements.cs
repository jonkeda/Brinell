using System.Text.RegularExpressions;

namespace Bruinell.Mocking.Data;

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
    public static IEnumerable<string> Objects(string text) => Names(text).Select(name => (name.Schema ?? "dbo") + "." + name.Name);

    /// <summary>The objects a command names as <see cref="Objects"/> finds them, in lower case, the schema null where none is written.</summary>
    public static IEnumerable<(string? Schema, string Name)> Names(string text)
    {
        var normalized = Normalize(text);
        foreach (Match match in Named.Matches(normalized).Cast<Match>().Concat(Procedures.Matches(normalized).Cast<Match>()))
        {
            var parts = match.Groups["name"].Value.Split('.').Select(part => part.Trim('[', ']', '"').ToLowerInvariant()).ToArray();
            if (parts[parts.Length - 1] is "sp_executesql" or "sys")
            {
                continue;
            }

            yield return parts.Length == 1 ? (null, parts[0]) : (parts[parts.Length - 2], parts[parts.Length - 1]);
        }
    }

    private static readonly Regex Insert = new(@"INSERT INTO (?<table>\S+) \((?<columns>[^)]*)\) VALUES \((?<values>[^)]*)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SelectStart = new(@"^SELECT (DISTINCT )?(TOP\([^)]*\) )?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// The text's identity for replay's matching: normalized, an <c>INSERT</c>'s and a leading <c>SELECT</c>'s columns
    /// sorted. Their order says nothing about the query (EF orders them by property name, so renaming a property
    /// reorders them); replay gives the recorded columns back in the order the live query asks for (<see cref="Projection"/>).
    /// </summary>
    public static string Key(string text)
    {
        var normalized = Insert.Replace(Normalize(text), insert =>
            $"INSERT INTO {insert.Groups["table"].Value} ({string.Join(", ", Split(insert.Groups["columns"].Value).OrderBy(column => column, StringComparer.Ordinal))}) VALUES (...)");
        return Projection(normalized) is { } projection
            ? projection.Prefix + string.Join(", ", projection.Columns.OrderBy(column => column, StringComparer.Ordinal)) + projection.Tail
            : normalized;
    }

    /// <summary>
    /// The parameter that fills each column of the text's <c>INSERT</c>s, by table and column: EF numbers an insert's
    /// parameters in its column order, so replay matches a parameter by the column it fills, not its name.
    /// </summary>
    public static IReadOnlyDictionary<string, string> InsertParameters(string text)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match insert in Insert.Matches(Normalize(text)))
        {
            var table = insert.Groups["table"].Value;
            var columns = Split(insert.Groups["columns"].Value).ToList();
            var values = Split(insert.Groups["values"].Value).ToList();
            for (var i = 0; i < Math.Min(columns.Count, values.Count); i++)
            {
                if (values[i].StartsWith("@", StringComparison.Ordinal))
                {
                    parameters[table + "." + columns[i]] = values[i];
                }
            }
        }

        return parameters;
    }

    /// <summary>
    /// A leading <c>SELECT</c>'s columns: the text before them, each column as written, and the text from <c>FROM</c> on;
    /// null for any other statement. Commas and <c>FROM</c> inside parentheses belong to a column.
    /// </summary>
    public static Projected? Projection(string normalized)
    {
        var select = SelectStart.Match(normalized);
        if (!select.Success)
        {
            return null;
        }

        var columns = new List<string>();
        var depth = 0;
        var start = select.Length;
        for (var i = start; i < normalized.Length; i++)
        {
            switch (normalized[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    columns.Add(normalized.Substring(start, i - start).Trim());
                    start = i + 1;
                    break;
                case ' ' when depth == 0 && string.Compare(normalized, i, " FROM ", 0, 6, StringComparison.OrdinalIgnoreCase) == 0:
                    columns.Add(normalized.Substring(start, i - start).Trim());
                    return new Projected(select.Value, columns, normalized.Substring(i));
            }
        }

        return null;
    }

    private static IEnumerable<string> Split(string list)
        => list.Split(',').Select(part => part.Trim()).Where(part => part.Length > 0);
}

/// <summary>A <c>SELECT</c>'s columns between the text before them and the text from <c>FROM</c> on.</summary>
public sealed record Projected(string Prefix, IReadOnlyList<string> Columns, string Tail);
