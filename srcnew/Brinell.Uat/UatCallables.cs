using System.Text.RegularExpressions;

namespace Brinell.Uat;

/// <summary>How often a callable phrase runs its body for one call.</summary>
public enum UatCallableKind
{
    /// <summary><c>## Phrase:</c> - once.</summary>
    Once,

    /// <summary><c>## Phrase for each row:</c> - once per row of the call's table.</summary>
    EachRow,
}

/// <summary>What a body step takes from under the call.</summary>
public enum UatPassedBlock
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>The call's table (<c>&lt;table&gt;</c>).</summary>
    Table,

    /// <summary>The call's doc string (<c>&lt;doc string&gt;</c>).</summary>
    DocString,
}

/// <summary>
/// A named sequence of steps written in scenario language: <c>## Phrase: a project {name}</c>.
/// Bound where it is called; it has no run-time existence of its own.
/// </summary>
/// <param name="Kind">Once, or once per row of the call's table.</param>
/// <param name="Phrase">The heading text, with <c>{x}</c> placeholders.</param>
/// <param name="Declared">The <c>Parameters</c> table's rows.</param>
/// <param name="Steps">The body, with <c>&lt;x&gt;</c> placeholders left in.</param>
/// <param name="Source">The heading's line.</param>
public sealed record UatCallable(
    UatCallableKind Kind,
    string Phrase,
    IReadOnlyList<UatCallableParameterDeclaration> Declared,
    IReadOnlyList<UatStep> Steps,
    UatSourceLocation Source)
{
    /// <summary>The heading's placeholders, in order.</summary>
    public IReadOnlyList<string> HeadingParameters { get; } = UatCommandPattern.ParameterNames(Phrase);

    /// <summary>
    /// The folder it is written in, under the scenario root (<see cref="UatNamespaces"/>): scenarios
    /// there and below see it, and its body binds there. <c>""</c> for the root.
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>The full path of the scenario file it is written in, when only that file sees it; null for a phrase file's.</summary>
    public string? LocalTo { get; init; }

    /// <summary>The heading as a call reads it: <c>Phrase for each row: ...</c> or <c>Phrase: ...</c>.</summary>
    public override string ToString() => $"{(Kind == UatCallableKind.EachRow ? "Phrase for each row" : "Phrase")}: {Phrase}";
}

/// <summary>A row of a callable's <c>Parameters</c> table.</summary>
/// <param name="Name">The parameter, or a column of a per-row callable, or <c>table</c>.</param>
/// <param name="TypeName">As the phrase list writes types: <c>number</c>, <c>date</c>, <c>TodoStatus</c>.</param>
/// <param name="Example">A value that must read as the type; null when the table has no example.</param>
/// <param name="Source">The row's line.</param>
public sealed record UatCallableParameterDeclaration(string Name, string TypeName, string? Example, UatSourceLocation Source);

/// <summary>A <c>*.phrases.md</c> file: callable phrases seen by every scenario in its folder and below.</summary>
/// <param name="Title">The <c># Phrases:</c> heading's text.</param>
/// <param name="Callables">Its phrases.</param>
/// <param name="Source">The file.</param>
public sealed record UatPhrasesDocument(string Title, IReadOnlyList<UatCallable> Callables, UatSourceLocation Source);

/// <summary>A parsed phrase file and its problems.</summary>
public sealed record UatPhrasesParseResult(UatPhrasesDocument? Document, IReadOnlyList<UatDiagnostic> Diagnostics)
{
    /// <summary>Whether it parsed without errors.</summary>
    public bool Success => Document is not null && Diagnostics.All(x => x.Severity != UatDiagnosticSeverity.Error);
}

/// <summary>
/// Where a step came from when a callable phrase put it there: the call, and for a per-row
/// callable the row. Frames chain outwards to the step written in the scenario.
/// </summary>
/// <param name="Callable">The callable.</param>
/// <param name="Call">The calling step, as written (or as expanded, inside another callable).</param>
/// <param name="Row">The 1-based row of the call's table, for a per-row callable.</param>
/// <param name="Values">What each placeholder was replaced with.</param>
/// <param name="Parent">The frame of the call's own call, if it is inside a callable too.</param>
public sealed record UatCallFrame(
    UatCallable Callable,
    UatStep Call,
    int? Row,
    IReadOnlyDictionary<string, string> Values,
    UatCallFrame? Parent)
{
    /// <summary>The call's line, or its row's line for a per-row callable.</summary>
    public UatSourceLocation Location =>
        Row is { } row && Call.Table is { } table && row - 1 < table.Rows.Count ? table.Rows[row - 1].Source : Call.Source;

    /// <summary>1 for a call written in the scenario.</summary>
    public int Depth => (Parent?.Depth ?? 0) + 1;

    /// <summary>The frames from the scenario's step inwards, ending with this one.</summary>
    public IReadOnlyList<UatCallFrame> Chain
    {
        get
        {
            List<UatCallFrame> frames = [];
            for (var frame = this; frame is not null; frame = frame.Parent)
            {
                frames.Insert(0, frame);
            }

            return frames;
        }
    }

    /// <summary>The call as a trace shows it: the step, and <c>row 2 (Harbour, 12.5)</c> for a row.</summary>
    public string Describe()
        => Row is { } row && Call.Table is { } table && row - 1 < table.Rows.Count
            ? $"row {row} ({string.Join(", ", table.Columns.Select(column => table.Rows[row - 1].Cells[column]))})"
            : $"{Call.Keyword} {Call.Text}";
}

// The placeholders of an outline or a callable body, <name>, replaced by values.
internal static class UatOutline
{
    private static readonly Regex PlaceholderRegex = new("<([^<>]+)>", RegexOptions.Compiled);

    private static readonly Regex QuotedPlaceholderRegex = new("(\"?)<([^<>]+)>(\"?)", RegexOptions.Compiled);

    // A call's values keep their quotes, so a body that writes "<name>" gets the value without
    // them when quoteAware: "Riverside", not ""Riverside"". Outline cells are used as written.
    public static UatStep Substitute(UatStep step, IReadOnlyDictionary<string, string> values, bool quoteAware = false)
    {
        UatTable? table = null;
        if (step.Table is not null)
        {
            var rows = step.Table.Rows
                .Select(row => new UatTableRow(
                    row.Cells.ToDictionary(x => x.Key, x => Substitute(x.Value, values, quoteAware), StringComparer.Ordinal),
                    row.Source))
                .ToArray();
            table = new UatTable(step.Table.Columns, rows, step.Table.Source);
        }

        return step with
        {
            Text = Substitute(step.Text, values, quoteAware),
            Table = table,
            DocString = step.DocString is null ? null : Substitute(step.DocString, values, quoteAware),
        };
    }

    public static string Substitute(string text, IReadOnlyDictionary<string, string> values, bool quoteAware = false)
    {
        if (!quoteAware)
        {
            return PlaceholderRegex.Replace(text, match => values.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
        }

        return QuotedPlaceholderRegex.Replace(text, match =>
        {
            if (!values.TryGetValue(match.Groups[2].Value, out var value))
            {
                return match.Value;
            }

            var open = match.Groups[1].Value;
            var close = match.Groups[3].Value;
            return open.Length > 0 && close.Length > 0
                ? $"\"{Unquote(value)}\""
                : open + value + close;
        });
    }

    public static IEnumerable<string> Placeholders(UatStep step)
    {
        foreach (Match match in PlaceholderRegex.Matches(step.Text))
        {
            yield return match.Groups[1].Value;
        }

        if (step.DocString is not null)
        {
            foreach (Match match in PlaceholderRegex.Matches(step.DocString))
            {
                yield return match.Groups[1].Value;
            }
        }

        if (step.Table is null)
        {
            yield break;
        }

        foreach (var row in step.Table.Rows)
        {
            foreach (var cell in row.Cells.Values)
            {
                foreach (Match match in PlaceholderRegex.Matches(cell))
                {
                    yield return match.Groups[1].Value;
                }
            }
        }
    }

    public static string? WholePlaceholder(string text)
    {
        var trimmed = Unquote(text.Trim());
        var match = PlaceholderRegex.Match(trimmed);
        return match.Success && match.Index == 0 && match.Length == trimmed.Length ? match.Groups[1].Value : null;
    }

    private static string Unquote(string value)
        => value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;
}
