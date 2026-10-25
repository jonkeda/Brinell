using Xunit;

namespace Bruinell.Bdd.Tests;

public sealed class BddTableBindingTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Bind_BuildsRecords_WithDefaultsForUnlistedColumns()
    {
        var todos = Table("""
            | Title        | Status     | Due Date |
            | Call plumber | InProgress | +2d      |
            | Buy milk     | open       |          |
            """).Bind<Todo>(Now);

        Assert.Equal(
            [
                new Todo("Call plumber", Status.InProgress, new DateOnly(2026, 3, 12)),
                new Todo("Buy milk", Status.Open, null),
            ],
            todos);
    }

    [Fact]
    public void Bind_AnUnknownColumn_NamesTheFieldsAndASuggestion()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Titel |
            | x     |
            """).Bind<Todo>());

        Assert.Contains("column 'Titel' is not a field of Todo", exception.Message);
        Assert.Contains("Did you mean 'Title'?", exception.Message);
    }

    [Fact]
    public void Bind_AMissingRequiredColumn_IsAnError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Status |
            | Open   |
            """).Bind<Todo>());

        Assert.Contains("needs column(s) Title", exception.Message);
    }

    [Fact]
    public void Bind_AWrongEnumName_ListsTheNames()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Title | Status |
            | x     | Opne   |
            """).Bind<Todo>());

        Assert.Contains("'Opne' is not a Status. Did you mean Open? It is one of Open, InProgress, Done", exception.Message);
    }

    [Fact]
    public void Bind_ARelativeDateWithoutNow_IsAnError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Title | Due Date |
            | x     | today    |
            """).Bind<Todo>());

        Assert.Contains("no 'now'", exception.Message);
    }

    [Fact]
    public void AssertContains_ComparesOnlyTheListedColumns()
    {
        Table("""
            | Title    | Status |
            | Buy milk | Open   |
            """).AssertContains(Rows());
    }

    [Fact]
    public void AssertContains_AMissingRow_ShowsExpectedActualAndTheClosestDifference()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Title    | Status |
            | Buy milk | Done   |
            """).AssertContains(Rows()));

        Assert.Contains("1 row(s) missing", exception.Message);
        Assert.Contains("<- missing (closest differs in Status: Open)", exception.Message);
        Assert.Contains("| File taxes | Done   |", exception.Message);
    }

    [Fact]
    public void AssertContains_ShowsDatesAsTheyAreWritten()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Title    | Due Date    |
            | Buy milk | 13-Mar-2026 |
            """).AssertContains(Rows()));

        Assert.Contains("closest differs in Due Date: 12-Mar-2026", exception.Message);
    }

    [Fact]
    public void AssertExactly_AnExtraRow_IsReported()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Title    |
            | Buy milk |
            """).AssertExactly(Rows()));

        Assert.Contains("1 extra", exception.Message);
        Assert.Contains("| File taxes |   <- extra", exception.Message);
    }

    [Fact]
    public void AssertExactly_TheSameRowsInAnotherOrder_Pass()
    {
        Table("""
            | Title      |
            | File taxes |
            | Buy milk   |
            """).AssertExactly(Rows());
    }

    [Fact]
    public void AssertDoesNotContain_AMatchingRow_IsReported()
    {
        Table("""
            | Title |
            | Walk  |
            """).AssertDoesNotContain(Rows());

        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Status |
            | Done   |
            """).AssertDoesNotContain(Rows()));

        Assert.Contains("found 1", exception.Message);
    }

    [Fact]
    public void AssertContains_ABlankStringIsShownQuoted()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Table("""
            | Title |
            | x     |
            """).AssertContains([new Todo("", Status.Open)]));

        Assert.Contains("| \"\"    |", exception.Message);
    }

    private static List<Todo> Rows() =>
    [
        new("Buy milk", Status.Open, new DateOnly(2026, 3, 12)),
        new("File taxes", Status.Done),
    ];

    private static BddTable Table(string rows)
    {
        var lines = rows.ReplaceLineEndings("\n").Split('\n').ToList();
        var columns = lines[0].Count(c => c == '|') - 1;
        lines.Insert(1, "|" + string.Concat(Enumerable.Repeat(" --- |", columns)));
        var parse = BddMarkdownParser.Parse($"# Feature: T\n\n## Scenario: S\n\n```gherkin\nGiven a table\n{string.Join("\n", lines)}\n```\n");
        return parse.Document!.Scenarios[0].Steps[0].Table!;
    }

    public enum Status
    {
        Open,
        InProgress,
        Done,
    }

    public sealed record Todo(string Title, Status Status = Status.Open, DateOnly? DueDate = null);
}
