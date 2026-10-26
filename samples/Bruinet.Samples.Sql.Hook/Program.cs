using System.Data.Common;
using Bruinet.Mocking.Data.AspNetCore;
using Bruinet.Mocking.Data.Replay;
using Microsoft.Data.Sqlite;

// An app as a legacy one reaches its database: ADO.NET, its connections from a provider factory. Wrapped in
// ReplayingDbProviderFactory, a connection made in a request that names a test records or replays in that test's
// session; any other is the real one. A `new SqliteConnection` in the app's code would not be reached.
var builder = WebApplication.CreateBuilder(args);
var database = builder.Configuration["Hook:Database"] ?? Path.Combine(AppContext.BaseDirectory, "places.db");
var connectionString = $"Data Source={database}";
DbProviderFactory factory = new ReplayingDbProviderFactory(SqliteFactory.Instance);

var app = builder.Build();
app.UseSqlReplayHook();

// Replay has no database: the table is made only when there is one.
if (!string.Equals(Environment.GetEnvironmentVariable(ReplayRegistry.ModeVariable), "Replay", StringComparison.OrdinalIgnoreCase))
{
    using var setup = new SqliteConnection(connectionString);
    setup.Open();
    using var create = setup.CreateCommand();
    create.CommandText = "CREATE TABLE IF NOT EXISTS Places (Name TEXT NOT NULL PRIMARY KEY, Visits INTEGER NOT NULL)";
    create.ExecuteNonQuery();
}

app.MapPost("/places/{name}", async (string name) =>
{
    try
    {
        await ExecuteAsync("INSERT INTO Places (Name, Visits) VALUES ($name, 0)", name);
        return Results.Created($"/places/{name}", name);
    }
    catch (SqliteException error) when (error.SqliteErrorCode == 19)
    {
        return Results.Conflict($"{name} exists");
    }
});

app.MapPost("/places/{name}/visit", async (string name)
    => await ExecuteAsync("UPDATE Places SET Visits = Visits + 1 WHERE Name = $name", name) == 1 ? Results.NoContent() : Results.NotFound());

app.MapGet("/places/{name}", async (string name) =>
{
    await using var connection = await OpenAsync();
    await using var command = Command(connection, "SELECT Name, Visits FROM Places WHERE Name = $name", name);
    await using var reader = await command.ExecuteReaderAsync();
    return await reader.ReadAsync() ? Results.Text($"{reader.GetString(0)}:{reader.GetInt64(1)}") : Results.NotFound();
});

app.Run();

async Task<DbConnection> OpenAsync()
{
    var connection = factory.CreateConnection()!;
    connection.ConnectionString = connectionString;
    await connection.OpenAsync();
    return connection;
}

async Task<int> ExecuteAsync(string sql, string name)
{
    await using var connection = await OpenAsync();
    await using var command = Command(connection, sql, name);
    return await command.ExecuteNonQueryAsync();
}

static DbCommand Command(DbConnection connection, string sql, string name)
{
    var command = connection.CreateCommand();
    command.CommandText = sql;
    var parameter = command.CreateParameter();
    parameter.ParameterName = "$name";
    parameter.Value = name;
    command.Parameters.Add(parameter);
    return command;
}

public partial class Program;
