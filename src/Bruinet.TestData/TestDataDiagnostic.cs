namespace Bruinet.TestData;

/// <summary>
/// One problem in a test-set file.
/// </summary>
/// <param name="File">The file.</param>
/// <param name="Line">The 1-based line, when known.</param>
/// <param name="Code">A stable code (<see cref="TestDataCodes"/>).</param>
/// <param name="Path">The JSON path, e.g. <c>server[0].status</c>, when known.</param>
/// <param name="Message">What is wrong, in words a person can act on.</param>
public sealed record TestDataDiagnostic(string File, int? Line, string Code, string? Path, string Message)
{
    /// <summary><c>file:line: CODE path: message</c>, the format editors and models read.</summary>
    public override string ToString()
    {
        var location = Line is { } line ? $"{File}:{line}" : File;
        var where = Path is { Length: > 0 } ? $"{Path}: " : string.Empty;
        return $"{location}: {Code} {where}{Message}";
    }
}

/// <summary>The diagnostic codes.</summary>
public static class TestDataCodes
{
    /// <summary>A field the type does not have.</summary>
    public const string UnknownField = "DATA001";

    /// <summary>A required field is missing.</summary>
    public const string MissingField = "DATA002";

    /// <summary>A value does not fit the field's type (a wrong enum name, a null where none is allowed).</summary>
    public const string WrongValue = "DATA003";

    /// <summary>The file is not valid JSON.</summary>
    public const string Syntax = "DATA004";

    /// <summary>The file is empty.</summary>
    public const string Empty = "DATA005";

    /// <summary>A rule failed.</summary>
    public const string Rule = "DATA010";

    /// <summary>Two items share an id.</summary>
    public const string DuplicateId = "DATA011";

    /// <summary>A rule threw instead of reporting.</summary>
    public const string RuleCrashed = "DATA019";

    /// <summary>A reference names a test set that does not exist.</summary>
    public const string UnknownTestSet = "DATA020";
}

/// <summary>The diagnostics of one validation run.</summary>
/// <param name="Diagnostics">Everything found, in file order.</param>
public sealed record TestDataReport(IReadOnlyList<TestDataDiagnostic> Diagnostics)
{
    /// <summary>Whether nothing was found.</summary>
    public bool IsValid => Diagnostics.Count == 0;

    /// <summary>One diagnostic per line, then a count.</summary>
    public string Format()
        => string.Join(Environment.NewLine, Diagnostics.Select(d => d.ToString()).Append($"{Diagnostics.Count} error(s)."));
}
