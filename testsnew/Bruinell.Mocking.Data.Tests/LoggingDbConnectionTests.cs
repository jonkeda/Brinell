using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Bruinell.Mocking.Data.Tests;

/// <summary>The ADO.NET wrapper over SQLite: every command logged with its kind, time and shape, and nothing changed.</summary>
public sealed class LoggingDbConnectionTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Bruinell", "SqlLogTests", Guid.NewGuid().ToString("N"));
    private readonly SqlLog _log;
    private readonly LoggingDbConnection _connection;

    public LoggingDbConnectionTests()
    {
        _log = new SqlLog(_folder, keep: true);
        _connection = new LoggingDbConnection(new SqliteConnection("Data Source=:memory:"), _log);
        _connection.Open();
        Execute("CREATE TABLE Places (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL UNIQUE)");
        _log.Clear();
    }

    [Fact]
    public void Open_IsLogged()
    {
        using var other = new LoggingDbConnection(new SqliteConnection("Data Source=:memory:"), _log);

        other.Open();

        Assert.Equal("open", Assert.Single(_log.Entries).Kind);
    }

    [Fact]
    public void NonQuery_LogsItsTextObjectsParameterNamesAndRowsAffected_NotTheValues()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "INSERT INTO Places (Id, Name) VALUES ($id, $name)";
        Add(command, "$id", 1);
        Add(command, "$name", "Hoorn");

        Assert.Equal(1, command.ExecuteNonQuery());

        var entry = Assert.Single(_log.Entries);
        Assert.Equal("non-query", entry.Kind);
        Assert.Equal("INSERT INTO Places (Id, Name) VALUES ($id, $name)", entry.Text);
        Assert.Equal(["$id", "$name"], entry.Parameters!);
        Assert.Equal(["dbo.places"], entry.Objects!);
        Assert.Equal(1, entry.Affected);
        Assert.DoesNotContain(SqlLog.Read(_folder), read => read.Text?.Contains("Hoorn", StringComparison.Ordinal) == true);
        using var file = new StreamReader(new FileStream(Directory.GetFiles(_folder, "*.jsonl").Single(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        Assert.DoesNotContain("Hoorn", file.ReadToEnd(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reader_IsLoggedOnceWhenDisposed_WithTheRowsRead()
    {
        Execute("INSERT INTO Places (Id, Name) VALUES (1, 'Hoorn'), (2, 'Enkhuizen')");
        _log.Clear();
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT Name FROM Places ORDER BY Name";

        var names = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }

            Assert.Empty(_log.Entries);
            reader.Close();
        }

        Assert.Equal(["Enkhuizen", "Hoorn"], names);
        var entry = Assert.Single(_log.Entries);
        Assert.Equal("reader", entry.Kind);
        Assert.Equal(2, entry.Rows);
    }

    [Fact]
    public void Scalar_IsLogged_AndItsValueReturned()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Places";

        Assert.Equal(0L, command.ExecuteScalar());
        Assert.Equal("scalar", Assert.Single(_log.Entries).Kind);
    }

    [Fact]
    public void Error_IsLoggedWithTheProvidersNumber_AndRethrown()
    {
        Execute("INSERT INTO Places (Id, Name) VALUES (1, 'Hoorn')");
        _log.Clear();

        var error = Assert.Throws<SqliteException>(() => Execute("INSERT INTO Places (Id, Name) VALUES (2, 'Hoorn')"));

        var entry = Assert.Single(_log.Entries);
        Assert.Equal("error", entry.Kind);
        Assert.Equal(error.SqliteErrorCode, entry.Error);
    }

    [Fact]
    public void Transaction_ReachesTheRealCommand()
    {
        using (var transaction = _connection.BeginTransaction())
        {
            using var command = _connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Places (Id, Name) VALUES (1, 'Hoorn')";
            command.ExecuteNonQuery();
            Assert.Same(transaction, command.Transaction);
            transaction.Rollback();
        }

        using var count = _connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM Places";
        Assert.Equal(0L, count.ExecuteScalar());
    }

    [Fact]
    public void StoredProcedure_IsLoggedAsItsExec()
    {
        // SQLite has no stored procedures: a SqlClient command, never run.
        using var command = new LoggingDbConnection(new Microsoft.Data.SqlClient.SqlConnection(), _log).CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "billing.Close";

        var entry = ((LoggingDbCommand)command).Entry("non-query", DateTime.UtcNow, TimeSpan.Zero);

        Assert.Equal("EXEC billing.Close", entry.Text);
        Assert.Equal(["billing.close"], entry.Objects!);
    }

    [Fact]
    public void Scope_NamesTheTest_AndTheFileReadsBack()
    {
        var before = SqlLogScope.Current;
        SqlLogScope.Current = () => "Tests.One";
        try
        {
            Execute("SELECT 1");
        }
        finally
        {
            SqlLogScope.Current = before;
        }

        var entry = Assert.Single(SqlLog.Read(_folder), read => read.Text == "SELECT ?");
        Assert.Equal("Tests.One", entry.Test);
        Assert.Equal(SqlLog.Run, entry.Run);
    }

    [Fact]
    public void Factory_MakesLoggingConnections()
    {
        var factory = new LoggingDbProviderFactory(SqliteFactory.Instance, _log);

        using var connection = factory.CreateConnection();

        Assert.IsType<LoggingDbConnection>(connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
    }

    private void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void Add(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
