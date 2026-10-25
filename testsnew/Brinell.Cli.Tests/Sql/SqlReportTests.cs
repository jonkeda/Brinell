using Brinell.Cli.Sql;
using Brinell.Mocking.Data;

namespace Brinell.Cli.Tests.Sql;

// brinell sql report: commands joined to tests by their windows, the arrange before each, and the fan-out through a schema.
public sealed class SqlReportTests : IDisposable
{
    private static readonly DateTime Zero = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
    private readonly string _folder = Directory.CreateTempSubdirectory("brinell-sql-report-").FullName;

    [Fact]
    public void Windows_ComeFromTheClockRowAndTheTestSpans()
    {
        var windows = SqlReport.Windows([Timings(("A", 100, 50), ("B", 200, 100))]);

        Assert.Equal([new TestWindow("A", Zero.AddMilliseconds(100), Zero.AddMilliseconds(150)), new TestWindow("B", Zero.AddMilliseconds(200), Zero.AddMilliseconds(300))], windows);
    }

    [Fact]
    public void Commands_GoToTheTestTheyStartedIn_OrTheNextOnesArrange_OrNone()
    {
        var windows = SqlReport.Windows([Timings(("A", 100, 50), ("B", 200, 100))]);
        SqlLogEntry[] entries =
        [
            Entry(90, 5, "DELETE FROM Places"),     // before A: A's arrange
            Entry(110, 10, "SELECT * FROM Places"), // in A
            Entry(170, 10, "INSERT INTO Places"),   // between A and B: B's arrange
            Entry(152, 5, "SELECT * FROM Lines"),   // started in A, ended after it: A's
            Entry(250, 30, "SELECT * FROM Visits"), // in B
            Entry(400, 7, "SELECT 1"),              // after B: none
        ];

        var report = SqlReport.Of(entries, windows, null);

        var a = report.Tests.Single(test => test.Test == "A");
        Assert.Equal((15, 5, 50), (a.DatabaseMs, a.ArrangeDatabaseMs, a.TestMs));
        var b = report.Tests.Single(test => test.Test == "B");
        Assert.Equal((30, 10, 100, 50), (b.DatabaseMs, b.ArrangeDatabaseMs, b.TestMs, b.ArrangeMs));
        Assert.Equal(0.3, b.Share);
        Assert.Equal((1, 7.0), (report.Unattributed, report.UnattributedMs));
    }

    [Fact]
    public void FanOut_FollowsAProcedureToItsTables()
    {
        var schema = new SchemaSnapshot();
        schema.Objects["dbo.visitcount"] = new SchemaObject { Kind = "sql_stored_procedure", References = ["dbo.visits"] };
        schema.Objects["dbo.visits"] = new SchemaObject { Kind = "user_table" };
        schema.Objects["dbo.places"] = new SchemaObject { Kind = "user_table" };
        var windows = SqlReport.Windows([Timings(("A", 100, 50), ("B", 200, 50))]);

        var report = SqlReport.Of([Entry(110, 1, "SELECT * FROM Places"), Entry(210, 1, "EXEC dbo.VisitCount @id"), Entry(220, 1, "DELETE FROM [p] FROM [Places] AS [p]")], windows, schema);

        Assert.Equal(["dbo.places", "dbo.visitcount", "dbo.visits"], report.Tests.Single(test => test.Test == "B").Objects);
        Assert.Equal(new ObjectFanOut("dbo.places", 2, 1.0), report.Objects[0]);
        Assert.Contains(new ObjectFanOut("dbo.visits", 1, 0.5), report.Objects);
    }

    [Fact]
    public void Repeats_AreCommandsSentMoreThanTenTimesInATest()
    {
        var windows = SqlReport.Windows([Timings(("A", 0, 1000))]);

        var report = SqlReport.Of(Enumerable.Range(0, 11).Select(i => Entry(10 + i, 1, "SELECT * FROM Lines WHERE OrderId = @id")), windows, null);

        Assert.Equal([new Repeat("SELECT * FROM Lines WHERE OrderId = @id", 11)], report.Tests.Single().Repeats);
        Assert.Contains("Commands sent more than 10 times", report.ToMarkdown(), StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutTimings_TheLogsOwnTestNamesAreUsed_AndThereAreNoShares()
    {
        var entry = Entry(10, 2, "SELECT * FROM Places");
        entry.Test = "Tests.One";

        var report = SqlReport.Of([entry], [], null);

        Assert.Null(Assert.Single(report.Tests).Share);
        Assert.Contains("No timings were given", report.ToMarkdown(), StringComparison.Ordinal);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static SqlLogEntry Entry(double endMs, double ms, string text)
    {
        var entry = SqlLog.Entry("reader", text, null, Zero.AddMilliseconds(endMs - ms), TimeSpan.FromMilliseconds(ms));
        return entry;
    }

    // A timings file as Brinell.Core's Timing writes it: the header, the clock row, a test span per test.
    private string Timings(params (string Test, int StartMs, int DurationMs)[] tests)
    {
        var path = Path.Combine(_folder, $"timings-{Guid.NewGuid():N}.csv");
        var lines = new List<string>
        {
            "Run;Head;Test;Scenario;Step;Kind;Name;Page;Control;Action;StartMs;DurationMs;Attempts;Result",
            $"run;browser;;;;clock;{Zero:O};;;;0;0;;",
        };
        lines.AddRange(tests.Select(test => $"run;browser;{test.Test};;;test;{test.Test};;;;{test.StartMs};{test.DurationMs};;"));
        File.WriteAllLines(path, lines);
        return path;
    }
}
