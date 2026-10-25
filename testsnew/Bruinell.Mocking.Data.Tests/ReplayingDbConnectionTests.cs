using System.Data.Common;
using Bruinell.Mocking.Data.Replay;
using Microsoft.Data.Sqlite;

namespace Bruinell.Mocking.Data.Tests;

/// <summary>
/// The ADO.NET adapter over SQLite (R1 and R4 of .my/sql-replay/plan.md): recorded against a database file, replayed with
/// the file gone; and sessions in parallel through one factory, each answered from its own recording.
/// </summary>
public sealed class ReplayingDbConnectionTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bruinell", "ReplayTests", Guid.NewGuid().ToString("N"));

    public ReplayingDbConnectionTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task Replay_AnswersReadsWritesScalarsAndErrors_WithTheDatabaseGone()
    {
        var database = Path.Combine(_folder, "places.db");
        var recording = Path.Combine(_folder, "places.json");
        var recorded = await Run(new ReplaySession(ReplayMode.Record, recording), database);
        SqliteConnection.ClearAllPools();
        File.Delete(database);

        var session = new ReplaySession(ReplayMode.Replay, recording);
        var replayed = await Run(session, database);

        Assert.Equal(recorded, replayed);
        Assert.Equal("1|Hoorn,2|Enkhuizen|2|1|19", replayed);
        Assert.Empty(session.Misses);
        Assert.False(File.Exists(database));
    }

    [Fact]
    public async Task Replay_ACommandNotRecorded_IsAMiss()
    {
        var database = Path.Combine(_folder, "places.db");
        var recording = Path.Combine(_folder, "places.json");
        await Run(new ReplaySession(ReplayMode.Record, recording), database);
        var session = new ReplaySession(ReplayMode.Replay, recording);
        using var connection = new ReplayingDbConnection(new SqliteConnection($"Data Source={database}"), session);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Places";

        Assert.Throws<ReplayMissException>(() => command.ExecuteNonQuery());
        Assert.Single(session.Misses);
    }

    [Fact]
    public async Task Factory_SessionsInParallel_EachReplaysItsOwn()
    {
        var factory = new ReplayingDbProviderFactory(SqliteFactory.Instance);
        var workers = Enumerable.Range(1, 8).ToList();

        // Recorded in parallel, a database per worker; replayed in parallel with every database gone, 5 times over.
        await Task.WhenAll(workers.Select(worker => Task.Run(() => Worker(factory, ReplayMode.Record, worker))));
        SqliteConnection.ClearAllPools();
        foreach (var worker in workers)
        {
            File.Delete(Database(worker));
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 5).SelectMany(_ => workers).Select(worker => Task.Run(() => Worker(factory, ReplayMode.Replay, worker))));

        Assert.Equal(Enumerable.Range(0, 5).SelectMany(_ => workers).Select(worker => $"Town {worker}|{worker}"), results);
    }

    [Fact]
    public void Factory_OutsideAScope_GivesTheRealConnection()
    {
        var factory = new ReplayingDbProviderFactory(SqliteFactory.Instance);

        Assert.IsType<SqliteConnection>(factory.CreateConnection());
    }

    private string Database(int worker) => Path.Combine(_folder, $"worker{worker}.db");

    private async Task<string> Worker(DbProviderFactory factory, ReplayMode mode, int worker)
    {
        var session = new ReplaySession(mode, Path.Combine(_folder, $"worker{worker}.json"));
        using (ReplayScope.Enter(session))
        {
            using var connection = factory.CreateConnection()!;
            connection.ConnectionString = $"Data Source={Database(worker)}";
            await connection.OpenAsync();
            await Execute(connection, "CREATE TABLE IF NOT EXISTS Places (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL)");
            await Execute(connection, "INSERT INTO Places (Id, Name) VALUES ($id, $name)", ("$id", worker), ("$name", $"Town {worker}"));

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name, Id FROM Places";
            using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            var result = $"{reader.GetString(0)}|{reader.GetInt64(1)}";
            session.Finish();
            Assert.Empty(session.Misses);
            return result;
        }
    }

    // A table, two inserts in a transaction, a read, a scalar, an update and a unique key broken: the same answer both ways.
    private static async Task<string> Run(ReplaySession session, string database)
    {
        using var connection = new ReplayingDbConnection(new SqliteConnection($"Data Source={database};Mode=ReadWriteCreate"), session);
        await connection.OpenAsync();
        await Execute(connection, "CREATE TABLE Places (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL UNIQUE)");
        using (var transaction = connection.BeginTransaction())
        {
            await Execute(connection, transaction, "INSERT INTO Places (Id, Name) VALUES ($id, $name)", ("$id", 1), ("$name", "Hoorn"));
            await Execute(connection, transaction, "INSERT INTO Places (Id, Name) VALUES ($id, $name)", ("$id", 2), ("$name", "Enkhuizen"));
            transaction.Commit();
        }

        var rows = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Id, Name FROM Places ORDER BY Id";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add($"{reader.GetInt64(0)}|{reader.GetString(1)}");
            }
        }

        object? count;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT COUNT(*) FROM Places";
            count = await command.ExecuteScalarAsync();
        }

        var updated = await Execute(connection, "UPDATE Places SET Name = $name WHERE Id = $id", ("$id", 2), ("$name", "Medemblik"));
        var error = await Assert.ThrowsAsync<SqliteException>(() => Execute(connection, "INSERT INTO Places (Id, Name) VALUES ($id, $name)", ("$id", 3), ("$name", "Hoorn")));
        session.Finish();
        return $"{string.Join(",", rows)}|{count}|{updated}|{error.SqliteErrorCode}";
    }

    private static Task<int> Execute(DbConnection connection, string sql, params (string Name, object Value)[] parameters)
        => Execute(connection, null, sql, parameters);

    private static async Task<int> Execute(DbConnection connection, DbTransaction? transaction, string sql, params (string Name, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return await command.ExecuteNonQueryAsync();
    }
}
