using System.Data.Common;
using Bruinet.Mocking.Data.Replay;
using Microsoft.Data.Sqlite;

namespace Bruinet.Mocking.Data.Tests;

/// <summary>
/// The schema snapshot on SQLite (Q1 to Q6 of .my/sql-replay/plan.md, step 7): objects from <c>sqlite_master</c>, a
/// table's hash with its indexes and triggers, references parsed from views and triggers, reference data, the stamp, and
/// the stale check; on a database file of their own, no server needed.
/// </summary>
public sealed class SqliteSchemaTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bruinet", "SqliteSchemaTests", Guid.NewGuid().ToString("N"));

    public SqliteSchemaTests()
    {
        Directory.CreateDirectory(_folder);
        Execute("""
            CREATE TABLE Places (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL UNIQUE);
            CREATE TABLE Kinds (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL DEFAULT 'town' CHECK (length(Name) > 0));
            CREATE TABLE Audit (Id INTEGER PRIMARY KEY AUTOINCREMENT, Note TEXT NOT NULL);
            CREATE INDEX IX_Kinds_Name ON Kinds (Name);
            CREATE VIEW PlaceNames AS SELECT Name FROM Places;
            CREATE VIEW LongNames AS WITH n AS (SELECT Name FROM PlaceNames) SELECT Name FROM n WHERE length(Name) > 5;
            CREATE TRIGGER Places_Audit AFTER INSERT ON Places BEGIN INSERT INTO Audit (Note) VALUES ('place'); END;
            INSERT INTO Places (Id, Name) VALUES (1, 'Hoorn'), (2, 'Enkhuizen');
            INSERT INTO Kinds (Id, Name) VALUES (1, 'city'), (2, 'town');
            ANALYZE;
            """);
    }

    private string Database => Path.Combine(_folder, "places.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task Objects_AreTheTablesAndViewsInMain_NotTheCatalogsIndexesOrTriggers()
    {
        var snapshot = await Read();

        Assert.Equal(["main.audit", "main.kinds", "main.longnames", "main.placenames", "main.places"], snapshot.Objects.Keys);
        Assert.Equal("table", snapshot.Objects["main.places"].Kind);
        Assert.Equal("view", snapshot.Objects["main.placenames"].Kind);
        Assert.Equal("main", snapshot.DefaultSchema);
    }

    [Fact]
    public async Task Hash_ChangesWithAColumnAnIndexOrATrigger_OnlyForItsTable()
    {
        var before = await Read();

        Execute("ALTER TABLE Kinds ADD COLUMN Code TEXT");
        var column = await Read();
        Execute("CREATE INDEX IX_Places_Both ON Places (Id, Name)");
        var index = await Read();
        Execute("DROP TRIGGER Places_Audit; CREATE TRIGGER Places_Audit AFTER INSERT ON Places BEGIN INSERT INTO Audit (Note) VALUES ('new place'); END;");
        var trigger = await Read();
        Execute("DROP VIEW LongNames; DROP VIEW PlaceNames; CREATE VIEW PlaceNames AS SELECT Name, Id FROM Places;");
        var view = await Read();

        Assert.NotEqual(before.Objects["main.kinds"].Hash, column.Objects["main.kinds"].Hash);
        Assert.Equal(before.Objects["main.places"].Hash, column.Objects["main.places"].Hash);
        Assert.NotEqual(column.Objects["main.places"].Hash, index.Objects["main.places"].Hash);
        Assert.NotEqual(index.Objects["main.places"].Hash, trigger.Objects["main.places"].Hash);
        Assert.Equal(before.Objects["main.audit"].Hash, trigger.Objects["main.audit"].Hash);
        Assert.NotEqual(trigger.Objects["main.placenames"].Hash, view.Objects["main.placenames"].Hash);
        Assert.Equal(trigger.Objects["main.places"].Hash, view.Objects["main.places"].Hash);
    }

    [Fact]
    public async Task References_FollowViewsAndTriggers_AndDropNamesThatAreNoObject()
    {
        var snapshot = await Read();

        Assert.Equal(["main.places"], snapshot.Objects["main.placenames"].References);
        Assert.Equal(["main.placenames"], snapshot.Objects["main.longnames"].References);
        Assert.Contains("main.places", snapshot.Expand(["main.longnames"]));
        Assert.Equal(["main.audit"], snapshot.Objects["main.places"].References);
    }

    [Fact]
    public async Task ReferenceData_ChangedRowsChangeTheHash_TheSameRowsInAnotherOrderDont()
    {
        var before = await Read("Kinds");

        Execute("DELETE FROM Kinds; INSERT INTO Kinds (Id, Name) VALUES (2, 'town'), (1, 'city');");
        var reordered = await Read("Kinds");
        Execute("UPDATE Kinds SET Name = 'village' WHERE Id = 2");
        var changed = await Read("Kinds");

        Assert.Equal(before.Objects["main.kinds"].Hash, reordered.Objects["main.kinds"].Hash);
        Assert.NotEqual(before.Objects["main.kinds"].Hash, changed.Objects["main.kinds"].Hash);
        Assert.Equal(before.Objects["main.places"].Hash, changed.Objects["main.places"].Hash);
    }

    [Fact]
    public async Task Stamp_TakesEfsQuotedNamesInMain_AndNotItsCatalogProbe()
    {
        var path = Path.Combine(_folder, "recordings", "Stamp.json");
        var session = new ReplaySession(ReplayMode.Record, path, (await Read()).ToSchema());
        session.Record(new Exchange { Kind = "reader", Text = "SELECT \"p\".\"Name\" FROM \"Places\" AS \"p\"" });
        session.Record(new Exchange { Kind = "scalar", Text = "SELECT COUNT(*) FROM \"sqlite_master\" WHERE \"type\" = 'table' AND \"rootpage\" IS NOT NULL;" });

        session.Finish();

        Assert.Equal(["main.audit", "main.places"], Recording.Load(path)!.Stamp!.Objects.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Stale_AfterAColumnIsAdded_IsOnlyTheRecordingThatReadsItsTable()
    {
        var recordings = Path.Combine(_folder, "recordings");
        var schema = (await Read()).ToSchema();
        await RecordAsync(Path.Combine(recordings, "bunit", "Places.json"), schema, "SELECT Id, Name FROM Places");
        await RecordAsync(Path.Combine(recordings, "bunit", "Kinds.json"), schema, "SELECT Id, Name FROM Kinds");

        Execute("ALTER TABLE Kinds ADD COLUMN Code TEXT");
        var states = RecordingCheck.Check(recordings, (await Read()).ToSchema());

        var stale = Assert.Single(states, state => state.IsStale);
        Assert.EndsWith("Kinds.json", stale.Path);
        Assert.Equal(["main.kinds changed"], stale.Differences);
        Assert.Equal(2, states.Count);
    }

    // Through the ADO.NET adapter on the real file, as a suite records.
    private async Task RecordAsync(string path, Schema schema, string sql)
    {
        var session = new ReplaySession(ReplayMode.Record, path, schema);
        using (var connection = new ReplayingDbConnection(new SqliteConnection($"Data Source={Database}"), session))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
            }
        }

        session.Finish();
    }

    private async Task<SchemaSnapshot> Read(params string[] data)
    {
        using var connection = new SqliteConnection($"Data Source={Database}");
        await connection.OpenAsync();
        return await SchemaSnapshot.ReadAsync(connection, new SnapshotOptions { Data = data });
    }

    private void Execute(string sql)
    {
        using var connection = new SqliteConnection($"Data Source={Database}");
        connection.Open();
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
