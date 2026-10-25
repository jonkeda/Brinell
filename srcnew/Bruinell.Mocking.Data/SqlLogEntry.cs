namespace Bruinell.Mocking.Data;

/// <summary>
/// One command a test sent, as the log keeps it: when, how long, what kind, its text and the objects it names. Parameter
/// names only, never their values, and the number of rows, never the rows: a log of a database with real data is safe
/// to keep and share.
/// </summary>
public sealed class SqlLogEntry
{
    /// <summary>The run, as <see cref="SqlLog.Run"/>.</summary>
    public string? Run { get; set; }

    /// <summary>The test that sent it, when known where it was sent; else joined by time (<c>bruinell sql report</c>).</summary>
    public string? Test { get; set; }

    /// <summary>When it started, UTC.</summary>
    public DateTime Start { get; set; }

    /// <summary>How long it took, in milliseconds; a reader's until it was closed, so reading its rows counts.</summary>
    public double Ms { get; set; }

    /// <summary><c>reader</c>, <c>non-query</c>, <c>scalar</c>, <c>error</c> or <c>open</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The text, whitespace collapsed; a stored procedure called as one, as its <c>EXEC</c>.</summary>
    public string? Text { get; set; }

    /// <summary>The parameters' names.</summary>
    public string[]? Parameters { get; set; }

    /// <summary>Rows read by a reader.</summary>
    public int? Rows { get; set; }

    /// <summary>Rows a write affected.</summary>
    public int? Affected { get; set; }

    /// <summary>The provider's error number, for a command that failed.</summary>
    public int? Error { get; set; }

    /// <summary>The objects the text names (<see cref="Statements.Objects"/>), as <c>schema.name</c>.</summary>
    public string[]? Objects { get; set; }

    /// <summary>When it ended, UTC.</summary>
    public DateTime End => Start.AddMilliseconds(Ms);
}
