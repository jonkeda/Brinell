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
    UatSourceLocation Location);

public enum UatStepKeyword
{
    Given,
    When,
    Then,
    And,
    But
}

public enum UatEffectiveStepKeyword
{
    Given,
    When,
    Then
}

public sealed record UatDocument(
    string Title,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<UatStep> Background,
    IReadOnlyList<UatNamedDataTable> DataTables,
    IReadOnlyList<UatScenario> Scenarios,
    UatSourceLocation Source);

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
    UatSourceLocation Source);

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
