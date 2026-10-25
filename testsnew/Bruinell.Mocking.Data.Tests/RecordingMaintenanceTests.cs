using System.Text.Json.Nodes;
using Bruinell.Mocking.Data.Replay;

namespace Bruinell.Mocking.Data.Tests;

/// <summary>
/// Re-records made readable and gated (M1 to M5 of .my/sql-replay/plan.md, step 5): the summary of what changed, pairing
/// and binding as replay does, the review file, and recording over an older recording after the app changed.
/// </summary>
public sealed class RecordingMaintenanceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bruinell", "MaintenanceTests", Guid.NewGuid().ToString("N"));

    public RecordingMaintenanceTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Diff_ShowsEachKindOfChange_OnALineOfItsCommand()
    {
        var before = Recording(
            Stamp(("dbo.places", "a"), ("dbo.kinds", "k")),
            Read("SELECT Id, Name FROM dbo.Places", ["Id", "Name"], [1, "Hoorn"], [2, "Enkhuizen"]),
            Write("UPDATE dbo.Places SET Name = @name", 2),
            Read("SELECT Kind FROM dbo.Kinds", ["Kind"], ["city"]),
            Error("INSERT INTO dbo.Places (Id) VALUES (@id)", 2627));
        var after = Recording(
            Stamp(("dbo.places", "b"), ("dbo.visits", "v")),
            Read("SELECT Id, Name FROM dbo.Places", ["Id", "Label"], [1, "Hoorn"], [2, "Medemblik"], [3, "Edam"]),
            Write("UPDATE dbo.Places SET Name = @name", 1),
            Read("SELECT COUNT(*) FROM dbo.Visits", ["Count"], [4]),
            Write("INSERT INTO dbo.Places (Id) VALUES (@id)", 1));

        var lines = RecordingDiff.Compare(before, after);

        Assert.Equal(
        [
            "stamp: - dbo.kinds",
            "stamp: dbo.places changed",
            "stamp: + dbo.visits",
            "+ reader SELECT COUNT(*) FROM dbo.Visits",
            "- reader SELECT Kind FROM dbo.Kinds",
            "~ reader SELECT Id, Name FROM dbo.Places: columns +Label -Name; rows 2 -> 3",
            "~ non-query UPDATE dbo.Places SET Name = @name: affected 2 -> 1",
            "~ non-query INSERT INTO dbo.Places (Id) VALUES (@id): error 2627 -> none; affected -1 -> 1",
        ], lines);
    }

    [Fact]
    public void Diff_ValuesThatDifferOnlyByAVolatileValue_AreNoChange()
    {
        Recording Run(string id) => Recording(
            null,
            Write("INSERT INTO dbo.Places (Id, Name) VALUES (@id, @name)", 1, ("@id", id), ("@name", "Hoorn")),
            Read("SELECT Id, Name FROM dbo.Places", ["Id", "Name"], [id, "Hoorn"]));
        var before = Run("11111111-1111-1111-1111-111111111111");
        var after = Run("22222222-2222-2222-2222-222222222222");
        after.MarkVolatile(before);

        Assert.Empty(RecordingDiff.Compare(before, after));
        Assert.Equal(["@id"], after.Exchanges[0].Volatile);
    }

    [Fact]
    public void Hash_AndDiff_IgnoreVolatileValuesAndHowOftenAReadRepeats()
    {
        Recording Run(string id, int repeats) => Recording(
            null,
            [
                Write("INSERT INTO dbo.Places (Id) VALUES (@id)", 1, ("@id", id)),
                .. Enumerable.Range(0, repeats).Select(_ => Read("SELECT Id FROM dbo.Places", ["Id"], [id])),
                Write("DELETE FROM dbo.Log", 0),
            ]);
        var before = Run("11111111-1111-1111-1111-111111111111", 1);
        var after = Run("22222222-2222-2222-2222-222222222222", 3);
        before.Exchanges[0].Volatile = ["@id"];
        after.Exchanges[0].Volatile = ["@id"];
        var changed = Run("22222222-2222-2222-2222-222222222222", 3);
        changed.Exchanges[0].Volatile = ["@id"];
        changed.Exchanges[^1].RecordsAffected = 5;

        Assert.Empty(RecordingDiff.Compare(before, after));
        Assert.Equal(Hash(before), Hash(after));
        Assert.NotEqual(Hash(before), Hash(changed));
    }

    [Fact]
    public void Diff_AWriteAddedEarly_IsOneAddedCommand_AndLaterOnesStillPair()
    {
        var before = Recording(null, Read("SELECT 1", ["One"], [1]), Write("UPDATE dbo.A SET X = 1", 1), Read("SELECT 2", ["Two"], [2]));
        var after = Recording(null, Write("DELETE FROM dbo.Log", 0), Read("SELECT 1", ["One"], [1]), Write("UPDATE dbo.A SET X = 1", 1), Read("SELECT 2", ["Two"], [2]));

        Assert.Equal(["+ non-query DELETE FROM dbo.Log"], RecordingDiff.Compare(before, after));
    }

    [Fact]
    public void Review_PassesAfterAccept_FailsOnAChange_AndASameReRecordNeedsNone()
    {
        var path = Path.Combine(_folder, "bunit", "A.json");
        Recording(null, Read("SELECT 1", ["One"], [1])).Save(path);
        new SchemaSnapshot().Save(Path.Combine(_folder, "schema.json"));

        Assert.Equal(["new bunit/A.json"], RecordingReview.Unreviewed(_folder));
        RecordingReview.Accept(_folder);
        Assert.Empty(RecordingReview.Unreviewed(_folder));

        Recording(null, Read("SELECT 1", ["One"], [1])).Save(path);
        Assert.Empty(RecordingReview.Unreviewed(_folder));

        Recording(null, Read("SELECT 1", ["One"], [2])).Save(path);
        Assert.Equal(["changed bunit/A.json"], RecordingReview.Unreviewed(_folder));

        File.Delete(path);
        Assert.Equal(["gone bunit/A.json"], RecordingReview.Unreviewed(_folder));
    }

    [Fact]
    public void Record_OverARecordingWithoutOneOfItsCommands_Saves()
    {
        var path = Path.Combine(_folder, "B.json");
        Recording(null, Read("SELECT 1", ["One"], [1])).Save(path);
        var session = new ReplaySession(ReplayMode.Record, path);
        session.Record(Read("SELECT 1", ["One"], [1]));
        session.Record(Read("SELECT 2", ["Two"], [2]));

        session.Finish();

        Assert.Equal(2, Mocking.Data.Replay.Recording.Load(path)!.Exchanges.Count);
    }

    private static Recording Recording(Stamp? stamp, params Exchange[] exchanges)
    {
        var recording = new Recording { Stamp = stamp };
        for (var i = 0; i < exchanges.Length; i++)
        {
            exchanges[i].Index = i;
            recording.Exchanges.Add(exchanges[i]);
        }

        return recording;
    }

    private string Hash(Recording recording)
    {
        var path = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".json");
        recording.Save(path);
        return RecordingReview.Hash(File.ReadAllText(path));
    }

    private static Stamp Stamp(params (string Name, string Hash)[] objects)
        => new() { Objects = objects.ToDictionary(item => item.Name, item => item.Hash, StringComparer.OrdinalIgnoreCase) };

    private static Exchange Read(string text, string[] columns, params object[][] rows) => new()
    {
        Kind = "reader",
        Text = text,
        Results =
        [
            new RecordedResult(
                [.. columns.Select(name => new RecordedColumn(name, "System.Object", "sql_variant"))],
                [.. rows.Select(row => (IReadOnlyList<JsonNode?>)[.. row.Select(Values.ToJson)])]),
        ],
    };

    private static Exchange Write(string text, int affected, params (string Name, object Value)[] parameters)
    {
        var exchange = new Exchange { Kind = "non-query", Text = text, RecordsAffected = affected };
        foreach (var (name, value) in parameters)
        {
            exchange.Parameters[name] = Values.ToJson(value);
        }

        return exchange;
    }

    private static Exchange Error(string text, int number)
        => new() { Kind = "non-query", Text = text, Error = new RecordedError("Microsoft.Data.SqlClient.SqlException", number, 1, 14, "Violation", null) };
}
