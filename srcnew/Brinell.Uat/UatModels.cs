namespace Brinell.Uat;

public sealed record UatSourceLocation(string? FilePath, int LineNumber)
{
    public override string ToString()
    {
        return FilePath is null ? LineNumber.ToString() : $"{FilePath}:{LineNumber}";
    }
}

public enum UatDiagnosticSeverity
{
    Error,
    Warning
}

public sealed record UatDiagnostic(
    UatDiagnosticSeverity Severity,
    string Code,
    string Message,
    UatSourceLocation Location)
{
    /// <summary>The value rewritten on the diagnostic's line, when there is exactly one right answer.</summary>
    public UatTextFix? Fix { get; init; }

    /// <summary>
    /// For a problem inside a callable phrase, reported at the call: the steps it passed through
    /// in the callables' bodies, outermost first. Empty otherwise.
    /// </summary>
    public IReadOnlyList<UatSourceLocation> Within { get; init; } = [];
}

public enum UatStepKeyword
{
    Given,
    When,
    Then,
    And,
    But
}

public sealed record UatDocument(
    string Title,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<UatStep> Background,
    IReadOnlyList<UatNamedDataTable> DataTables,
    IReadOnlyList<UatScenario> Scenarios,
    UatSourceLocation Source)
{
    /// <summary>The file's own callable phrases (<c>## Phrase:</c> sections).</summary>
    public IReadOnlyList<UatCallable> Callables { get; init; } = [];
}

public sealed record UatNamedDataTable(
    string Name,
    UatTable Table,
    UatSourceLocation Source);

public sealed record UatScenario(
    string Name,
    IReadOnlyList<string> Tags,
    IReadOnlyList<UatStep> Steps,
    UatSourceLocation Source,
    string? OutlineName = null,
    int? ExampleIndex = null);

public sealed record UatStep(
    UatStepKeyword Keyword,
    UatEffectiveStepKeyword EffectiveKeyword,
    string Text,
    UatTable? Table,
    UatSourceLocation Source,
    string? DocString = null)
{
    /// <summary>In a callable phrase's body: the call's table or doc string goes under this step.</summary>
    public UatPassedBlock Passes { get; init; }
}

public sealed record UatTable(
    IReadOnlyList<string> Columns,
    IReadOnlyList<UatTableRow> Rows,
    UatSourceLocation Source)
{
    public string GetValue(string column, int rowIndex = 0)
    {
        if (!TryGetValue(column, out var value, rowIndex))
        {
            throw new KeyNotFoundException($"Table does not contain column '{column}' at row {rowIndex}.");
        }

        return value;
    }

    public bool TryGetValue(string column, out string value, int rowIndex = 0)
    {
        value = string.Empty;

        if (rowIndex < 0 || rowIndex >= Rows.Count)
        {
            return false;
        }

        if (!Rows[rowIndex].Cells.TryGetValue(column, out var found))
        {
            return false;
        }

        value = found;
        return true;
    }
}

public sealed record UatTableRow(
    IReadOnlyDictionary<string, string> Cells,
    UatSourceLocation Source);

public sealed class UatParseResult
{
    public UatParseResult(UatDocument? document, IReadOnlyList<UatDiagnostic> diagnostics)
    {
        Document = document;
        Diagnostics = diagnostics;
    }

    public UatDocument? Document { get; }

    public IReadOnlyList<UatDiagnostic> Diagnostics { get; }

    public bool Success => Document is not null && Diagnostics.All(x => x.Severity != UatDiagnosticSeverity.Error);
}
