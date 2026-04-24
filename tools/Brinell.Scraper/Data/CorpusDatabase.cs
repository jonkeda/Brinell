using System.IO;
using Brinell.Scraper.Models;
using Microsoft.Data.Sqlite;

namespace Brinell.Scraper.Data;

public sealed class CorpusDatabase
{
    private readonly string _dbPath;

    public CorpusDatabase()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Brinell.Scraper", "scraper.db"))
    {
    }

    public CorpusDatabase(string dbPath)
    {
        _dbPath = dbPath;
        var dir = Path.GetDirectoryName(_dbPath);
        if (dir is not null) Directory.CreateDirectory(dir);
        EnsureCreated();
    }

    private string ConnectionString => $"Data Source={_dbPath}";

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Sites (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                StartUrl TEXT NOT NULL,
                Namespace TEXT NOT NULL DEFAULT '',
                OutputPath TEXT NOT NULL DEFAULT '',
                UrlAliases TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                LastOpenedAt TEXT NOT NULL DEFAULT (datetime('now')),
                PageCount INTEGER NOT NULL DEFAULT 0,
                ControlCount INTEGER NOT NULL DEFAULT 0
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public List<SiteInfo> GetAllSites()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Sites ORDER BY LastOpenedAt DESC";

        using var reader = cmd.ExecuteReader();
        var sites = new List<SiteInfo>();
        while (reader.Read())
        {
            sites.Add(ReadSite(reader));
        }
        return sites;
    }

    public SiteInfo CreateSite(string name, string startUrl, string ns, string outputPath, List<string> aliases)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Sites (Name, StartUrl, Namespace, OutputPath, UrlAliases)
            VALUES (@name, @startUrl, @ns, @outputPath, @aliases);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@startUrl", startUrl);
        cmd.Parameters.AddWithValue("@ns", ns);
        cmd.Parameters.AddWithValue("@outputPath", outputPath);
        cmd.Parameters.AddWithValue("@aliases", string.Join("|", aliases));

        var id = (long)cmd.ExecuteScalar()!;
        return new SiteInfo
        {
            Id = id,
            Name = name,
            StartUrl = startUrl,
            Namespace = ns,
            OutputPath = outputPath,
            UrlAliases = aliases
        };
    }

    public void UpdateSite(long siteId, string name, string startUrl, string ns, string outputPath, List<string> aliases)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Sites SET Name=@name, StartUrl=@url, Namespace=@ns, OutputPath=@path, UrlAliases=@aliases WHERE Id=@id";
        cmd.Parameters.AddWithValue("@id", siteId);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@url", startUrl);
        cmd.Parameters.AddWithValue("@ns", ns);
        cmd.Parameters.AddWithValue("@path", outputPath);
        cmd.Parameters.AddWithValue("@aliases", string.Join("|", aliases));
        cmd.ExecuteNonQuery();
    }

    public void TouchSite(long siteId)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Sites SET LastOpenedAt = datetime('now') WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", siteId);
        cmd.ExecuteNonQuery();
    }

    private static SiteInfo ReadSite(SqliteDataReader reader)
    {
        var aliasStr = reader.GetString(reader.GetOrdinal("UrlAliases"));
        return new SiteInfo
        {
            Id = reader.GetInt64(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            StartUrl = reader.GetString(reader.GetOrdinal("StartUrl")),
            Namespace = reader.GetString(reader.GetOrdinal("Namespace")),
            OutputPath = reader.GetString(reader.GetOrdinal("OutputPath")),
            UrlAliases = string.IsNullOrEmpty(aliasStr) ? [] : [.. aliasStr.Split('|')],
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
            LastOpenedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("LastOpenedAt"))),
            PageCount = reader.GetInt32(reader.GetOrdinal("PageCount")),
            ControlCount = reader.GetInt32(reader.GetOrdinal("ControlCount")),
        };
    }
}
