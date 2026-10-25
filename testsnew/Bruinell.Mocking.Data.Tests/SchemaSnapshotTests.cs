using Bruinell.Mocking.Data.Replay;
using Microsoft.Data.SqlClient;

namespace Bruinell.Mocking.Data.Tests;

/// <summary>
/// The snapshot's gaps closed (S1 to S4 of .my/sql-replay/plan.md, step 3): triggers in their table, unqualified names in
/// a body resolved, dynamic SQL marked and listed by hand, reference data hashed; on a database of their own.
/// </summary>
[Trait("Category", "SqlServer")]
public sealed class SchemaSnapshotTests : IAsyncLifetime
{
    private readonly string _database = "BruinellSnapshotTests_" + Guid.NewGuid().ToString("N")[..8];
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bruinell", "SnapshotTests", Guid.NewGuid().ToString("N"));

    private string Connection => $"Server={SqlServerTests.Server};Database={_database};Integrated Security=True;TrustServerCertificate=True";

    public async Task InitializeAsync()
    {
        if (new SqlServerFactAttribute().Skip is not null)
        {
            return;
        }

        await Execute(SqlServerTests.Master, $"CREATE DATABASE [{_database}]");
        await Execute(Connection, "CREATE SCHEMA sales");
        await Execute(Connection, "CREATE TABLE sales.Orders (Id int NOT NULL PRIMARY KEY, Total int NOT NULL CONSTRAINT DF_Total DEFAULT 0, CONSTRAINT CK_Total CHECK (Total >= 0))");
        await Execute(Connection, "CREATE TABLE dbo.Orders (Id int NOT NULL PRIMARY KEY)");
        await Execute(Connection, "CREATE TABLE dbo.Audit (Id int IDENTITY PRIMARY KEY, Note nvarchar(100) NOT NULL)");
        await Execute(Connection, "CREATE TABLE dbo.Kinds (Id int NOT NULL PRIMARY KEY, Name nvarchar(50) NOT NULL); INSERT INTO dbo.Kinds VALUES (1, N'city'), (2, N'village')");
        await Execute(Connection, "CREATE TABLE dbo.Other (Id int NOT NULL PRIMARY KEY, Name nvarchar(50) NOT NULL); INSERT INTO dbo.Other VALUES (1, N'one')");
        await Execute(Connection, "CREATE TRIGGER sales.Orders_Audit ON sales.Orders AFTER INSERT AS INSERT INTO dbo.Audit (Note) SELECT N'order' FROM inserted");
        await Execute(Connection, "CREATE PROCEDURE sales.Total AS SELECT SUM(Total) FROM Orders");
        await Execute(Connection, "CREATE PROCEDURE dbo.Search @name nvarchar(50) AS BEGIN DECLARE @sql nvarchar(max) = N'SELECT Id FROM dbo.Kinds WHERE Name = @name'; EXEC sp_executesql @sql, N'@name nvarchar(50)', @name; END");
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }

        if (new SqlServerFactAttribute().Skip is not null)
        {
            return;
        }

        SqlConnection.ClearAllPools();
        await Execute(SqlServerTests.Master, $"ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_database}]");
    }

    [SqlServerFact]
    public async Task Trigger_IsPartOfItsTable_ItsHashAndWhatItWrites()
    {
        var before = await Read();

        await Execute(Connection, "ALTER TRIGGER sales.Orders_Audit ON sales.Orders AFTER INSERT AS INSERT INTO dbo.Audit (Note) SELECT N'new order' FROM inserted");
        var after = await Read();

        Assert.Contains("dbo.audit", before.Objects["sales.orders"].References);
        Assert.NotEqual(before.Objects["sales.orders"].Hash, after.Objects["sales.orders"].Hash);
        Assert.Equal(before.Objects["dbo.orders"].Hash, after.Objects["dbo.orders"].Hash);
        Assert.DoesNotContain(after.Objects, pair => pair.Key.Contains("orders_audit", StringComparison.Ordinal));
    }

    [SqlServerFact]
    public async Task DefaultAndCheck_ArePartOfTheirTable()
    {
        var before = await Read();

        await Execute(Connection, "ALTER TABLE sales.Orders DROP CONSTRAINT DF_Total; ALTER TABLE sales.Orders ADD CONSTRAINT DF_Total DEFAULT 1 FOR Total");

        Assert.NotEqual(before.Objects["sales.orders"].Hash, (await Read()).Objects["sales.orders"].Hash);
    }

    [SqlServerFact]
    public async Task UnqualifiedName_InABody_IsTheOneInItsSchema()
    {
        var snapshot = await Read();

        Assert.Equal(["sales.orders"], snapshot.Objects["sales.total"].References);
    }

    [SqlServerFact]
    public async Task DynamicSql_IsMarked_AndItsReferencesListedByHand()
    {
        var snapshot = await Read();
        Directory.CreateDirectory(_folder);
        var file = Path.Combine(_folder, "references.json");
        File.WriteAllText(file, """{ "dbo.search": ["dbo.Kinds"], "dbo.gone": ["dbo.kinds"] }""");

        Assert.True(snapshot.Objects["dbo.search"].Dynamic);
        Assert.False(snapshot.Objects["sales.total"].Dynamic);
        Assert.Equal(["dbo.gone"], snapshot.MergeReferences(file));
        Assert.True(snapshot.Objects["dbo.search"].Listed);
        Assert.Contains("dbo.kinds", snapshot.Expand(["dbo.search"]));
    }

    [SqlServerFact]
    public async Task ReferenceData_ChangedRows_ChangeOnlyTheListedTablesHash()
    {
        var before = await Read("dbo.Kinds");

        await Execute(Connection, "UPDATE dbo.Kinds SET Name = N'town' WHERE Id = 2; UPDATE dbo.Other SET Name = N'two'");
        var after = await Read("dbo.Kinds");

        Assert.NotEqual(before.Objects["dbo.kinds"].Hash, after.Objects["dbo.kinds"].Hash);
        Assert.Equal(before.Objects["dbo.other"].Hash, after.Objects["dbo.other"].Hash);
    }

    private async Task<SchemaSnapshot> Read(params string[] data)
    {
        await using var connection = new SqlConnection(Connection);
        await connection.OpenAsync();
        return await SchemaSnapshot.ReadAsync(connection, new SnapshotOptions { Data = data });
    }

    private static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>The default schema (S5) and the stale check, on schemas made by hand.</summary>
public sealed class StampTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bruinell", "StampTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Named_UnqualifiedName_IsTakenInTheDefaultSchema_ThenDbo()
    {
        var schema = Schema(("sales.orders", "1"), ("dbo.orders", "2"), ("dbo.audit", "3"));

        Assert.Equal(["sales.orders", "dbo.audit", "dbo.orders"], schema.Named("SELECT * FROM orders JOIN audit ON 1 = 1 JOIN dbo.orders ON 1 = 1"));
        Assert.Equal(["dbo.orders"], (schema with { DefaultSchema = "dbo" }).Named("SELECT * FROM orders"));
    }

    [Fact]
    public void Recording_IsStampedInTheDefaultSchema()
    {
        var path = Path.Combine(_folder, "bunit", "Orders.json");
        var session = new ReplaySession(ReplayMode.Record, path, Schema(("sales.orders", "1"), ("dbo.orders", "2")));
        session.Record(new Exchange { Kind = "reader", Text = "SELECT Id FROM orders" });

        session.Finish();

        Assert.Equal(["sales.orders"], Recording.Load(path)!.Stamp!.Objects.Keys);
    }

    [Fact]
    public void Check_ListsOnlyTheRecordingsAChangeReaches_AndSkipsOtherFiles()
    {
        var before = Schema(("sales.orders", "1"), ("dbo.kinds", "2"));
        Record("bunit/Orders.json", before, "SELECT Id FROM orders");
        Record("bunit/Kinds.json", before, "SELECT Id FROM dbo.kinds");
        File.WriteAllText(Path.Combine(_folder, "schema.json"), """{ "Objects": {} }""");

        var states = RecordingCheck.Check(_folder, Schema(("sales.orders", "1"), ("dbo.kinds", "changed")));

        Assert.Equal(2, states.Count);
        var stale = Assert.Single(states, state => state.IsStale);
        Assert.EndsWith("Kinds.json", stale.Path);
        Assert.Contains("dbo.kinds", Assert.Single(stale.Differences));
    }

    private void Record(string name, Schema schema, string text)
    {
        var session = new ReplaySession(ReplayMode.Record, Path.Combine(_folder, name), schema);
        session.Record(new Exchange { Kind = "reader", Text = text });
        session.Finish();
    }

    private static Schema Schema(params (string Name, string Hash)[] objects) => new(
        new Dictionary<string, string>(),
        objects.ToDictionary(item => item.Name, item => item.Hash, StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, IReadOnlyList<string>>())
    {
        DefaultSchema = "sales",
    };
}
