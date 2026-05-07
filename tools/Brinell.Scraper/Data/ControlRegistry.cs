using Brinell.Scraper.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Data;

public sealed class ControlRegistry : Services.IControlRegistry
{
    private readonly string _connectionString;
    private readonly ILogger<ControlRegistry> _logger;

    public ControlRegistry(string connectionString, ILogger<ControlRegistry> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
        EnsureCreated();
    }

    private void EnsureCreated()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS GeneratedControls (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Namespace TEXT NOT NULL,
                Code TEXT NOT NULL,
                DomSignature TEXT NOT NULL,
                Confidence REAL NOT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<GeneratedControl> GetAllControls()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Namespace, Code, DomSignature, Confidence, CreatedAt FROM GeneratedControls ORDER BY Name";

        using var reader = cmd.ExecuteReader();
        var results = new List<GeneratedControl>();
        while (reader.Read())
        {
            results.Add(ReadControl(reader));
        }
        return results;
    }

    public GeneratedControl? GetControl(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Namespace, Code, DomSignature, Confidence, CreatedAt FROM GeneratedControls WHERE Name = @name";
        cmd.Parameters.AddWithValue("@name", name);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadControl(reader) : null;
    }

    public void StoreControl(GeneratedControl control)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO GeneratedControls (Name, Namespace, Code, DomSignature, Confidence, CreatedAt)
            VALUES (@name, @namespace, @code, @domSignature, @confidence, @createdAt);
            """;
        cmd.Parameters.AddWithValue("@name", control.Name);
        cmd.Parameters.AddWithValue("@namespace", control.Namespace);
        cmd.Parameters.AddWithValue("@code", control.Code);
        cmd.Parameters.AddWithValue("@domSignature", control.DomSignature);
        cmd.Parameters.AddWithValue("@confidence", control.Confidence);
        cmd.Parameters.AddWithValue("@createdAt", control.CreatedAt.ToString("o"));
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Control stored — Name: {ControlName}", control.Name);
    }

    public void DeleteControl(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM GeneratedControls WHERE Name = @name";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Control deleted — Name: {ControlName}", name);
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static GeneratedControl ReadControl(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Namespace = reader.GetString(2),
        Code = reader.GetString(3),
        DomSignature = reader.GetString(4),
        Confidence = reader.GetDouble(5),
        CreatedAt = DateTimeOffset.Parse(reader.GetString(6))
    };
}
