using System.Data;
using Microsoft.Data.SqlClient;

namespace Bruinell.Mocking.Data.Tests;

/// <summary>A test that needs the local SQL Server (<c>BRUINELL_SQL_SERVER</c>, else <c>.</c>); skipped when none answers.</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    private static readonly Lazy<string?> Missing = new(() =>
    {
        try
        {
            using var connection = new SqlConnection(SqlServerTests.Master);
            connection.Open();
            return null;
        }
        catch (Exception error)
        {
            return $"No SQL Server at {SqlServerTests.Server}: {error.Message}";
        }
    });

    public SqlServerFactAttribute()
    {
        if (Missing.Value is { } reason)
        {
            Skip = reason;
        }
    }
}

/// <summary>The schema snapshot and an Extended Events capture, on a database of their own on the local SQL Server.</summary>
[Trait("Category", "SqlServer")]
public sealed class SqlServerTests : IAsyncLifetime
{
    public static string Server => Environment.GetEnvironmentVariable("BRUINELL_SQL_SERVER") is { Length: > 0 } server ? server : ".";

    public static string Master => $"Server={Server};Database=master;Integrated Security=True;TrustServerCertificate=True";

    private readonly string _database = "BruinellSqlLogTests_" + Guid.NewGuid().ToString("N")[..8];

    private string Connection => $"Server={Server};Database={_database};Integrated Security=True;TrustServerCertificate=True";

    public async Task InitializeAsync()
    {
        if (new SqlServerFactAttribute().Skip is not null)
        {
            return;
        }

        await Execute(Master, $"CREATE DATABASE [{_database}]");
        await Execute(Connection, "CREATE TABLE dbo.Places (Id int NOT NULL PRIMARY KEY, Name nvarchar(100) NOT NULL)");
        await Execute(Connection, "CREATE VIEW dbo.Names AS SELECT Name FROM dbo.Places");
        await Execute(Connection, "CREATE PROCEDURE dbo.CountPlaces @min int AS SELECT COUNT(*) FROM dbo.Names WHERE LEN(Name) >= @min");
    }

    public async Task DisposeAsync()
    {
        if (new SqlServerFactAttribute().Skip is not null)
        {
            return;
        }

        SqlConnection.ClearAllPools();
        await Execute(Master, $"ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_database}]");
    }

    [SqlServerFact]
    public async Task Snapshot_HasEveryObject_AndFollowsAProcedureThroughItsViewToItsTable()
    {
        await using var connection = new SqlConnection(Connection);
        await connection.OpenAsync();

        var snapshot = await SchemaSnapshot.ReadAsync(connection);

        Assert.Equal("user_table", snapshot.Objects["dbo.places"].Kind);
        Assert.Equal(["dbo.places"], snapshot.Objects["dbo.names"].References);
        Assert.Equal(["dbo.countplaces", "dbo.names", "dbo.places"], snapshot.Expand(["dbo.countplaces"]).Order(StringComparer.Ordinal).ToArray());
    }

    [SqlServerFact]
    public async Task ExtendedEvents_LogTheAppsCommands_WithoutItsValues_OnThisMachinesClock()
    {
        await using var connection = new SqlConnection(Connection);
        await connection.OpenAsync();
        var folder = (string)(await Scalar(connection, "SELECT CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(4000))"))!;
        var state = await ExtendedEvents.StartAsync(connection, folder);
        var before = DateTime.UtcNow;
        try
        {
            await using var app = new SqlConnection(Connection);
            await app.OpenAsync();
            await using (var insert = new SqlCommand("INSERT INTO dbo.Places (Id, Name) VALUES (@id, @name)", app))
            {
                insert.Parameters.AddWithValue("@id", 1);
                insert.Parameters.AddWithValue("@name", "Hoorn");
                await insert.ExecuteNonQueryAsync();
            }

            await using (var procedure = new SqlCommand("dbo.CountPlaces", app) { CommandType = CommandType.StoredProcedure })
            {
                procedure.Parameters.AddWithValue("@min", 3);
                Assert.Equal(1, await procedure.ExecuteScalarAsync());
            }

            await using (var batch = new SqlCommand("SELECT Name FROM dbo.Places WHERE Name = N'Hoorn'", app))
            {
                await batch.ExecuteScalarAsync();
            }
        }
        finally
        {
            await ExtendedEvents.StopAsync(connection, _database);
        }

        var after = DateTime.UtcNow;
        var entries = await ExtendedEvents.ReadAsync(connection, state);

        Assert.DoesNotContain(entries, entry => entry.Text?.Contains("bruinell-marker", StringComparison.Ordinal) == true);
        var insertEntry = Assert.Single(entries, entry => entry.Text?.StartsWith("INSERT INTO dbo.Places", StringComparison.Ordinal) == true);
        Assert.Equal(["@id", "@name"], insertEntry.Parameters!);
        Assert.Equal(1, insertEntry.Affected);
        Assert.Contains(entries, entry => entry.Text == "EXEC CountPlaces" || entry.Text == "EXEC dbo.CountPlaces");
        var batchEntry = Assert.Single(entries, entry => entry.Text == "SELECT Name FROM dbo.Places WHERE Name = ?");
        Assert.InRange(batchEntry.End, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.All(entries, entry => Assert.DoesNotContain("Hoorn", entry.Text ?? string.Empty, StringComparison.Ordinal));
    }

    private static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> Scalar(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
}
