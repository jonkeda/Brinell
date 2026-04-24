using System.Text.Json;
using Brinell.Scraper.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.Services;

public sealed class CorpusService
{
    private readonly string _connectionString;
    private readonly ILogger<CorpusService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public CorpusService(string connectionString, ILogger<CorpusService> logger)
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
            CREATE TABLE IF NOT EXISTS Snapshots (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SiteId INTEGER NOT NULL,
                PageName TEXT NOT NULL,
                PageUrl TEXT NOT NULL,
                PageTitle TEXT,
                CapturedAt TEXT NOT NULL,
                DomJson TEXT NOT NULL,
                ElementCount INTEGER NOT NULL,
                SnapshotSizeBytes INTEGER NOT NULL,
                IsLatest INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Elements (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SnapshotId INTEGER NOT NULL REFERENCES Snapshots(Id),
                Tag TEXT NOT NULL,
                ElementId TEXT,
                ClassName TEXT,
                DataTestId TEXT,
                AriaLabel TEXT,
                Role TEXT,
                TextContent TEXT,
                ParentPath TEXT,
                AttributesJson TEXT
            );

            CREATE INDEX IF NOT EXISTS IX_Elements_Tag ON Elements(Tag);
            CREATE INDEX IF NOT EXISTS IX_Elements_DataTestId ON Elements(DataTestId);
            CREATE INDEX IF NOT EXISTS IX_Snapshots_SiteId ON Snapshots(SiteId);
            CREATE INDEX IF NOT EXISTS IX_Snapshots_PageName ON Snapshots(SiteId, PageName);
            """;
        cmd.ExecuteNonQuery();
    }

    public void StoreSnapshot(long siteId, DomSnapshot snapshot)
    {
        var domJson = JsonSerializer.Serialize(snapshot.RootElement, JsonOptions);
        var elementCount = CountElements(snapshot.RootElement);

        using var conn = Open();
        using var tx = conn.BeginTransaction();

        // Mark any existing latest snapshot for this page as historical
        using (var markCmd = conn.CreateCommand())
        {
            markCmd.CommandText = "UPDATE Snapshots SET IsLatest = 0 WHERE SiteId = @siteId AND PageName = @pageName AND IsLatest = 1";
            markCmd.Parameters.AddWithValue("@siteId", siteId);
            markCmd.Parameters.AddWithValue("@pageName", snapshot.PageName);
            markCmd.ExecuteNonQuery();
        }

        // Insert new snapshot
        long snapshotId;
        using (var insertCmd = conn.CreateCommand())
        {
            insertCmd.CommandText = """
                INSERT INTO Snapshots (SiteId, PageName, PageUrl, PageTitle, CapturedAt, DomJson, ElementCount, SnapshotSizeBytes, IsLatest)
                VALUES (@siteId, @pageName, @pageUrl, @pageTitle, @capturedAt, @domJson, @elementCount, @sizeBytes, 1);
                SELECT last_insert_rowid();
                """;
            insertCmd.Parameters.AddWithValue("@siteId", siteId);
            insertCmd.Parameters.AddWithValue("@pageName", snapshot.PageName);
            insertCmd.Parameters.AddWithValue("@pageUrl", snapshot.PageUrl);
            insertCmd.Parameters.AddWithValue("@pageTitle", (object?)snapshot.PageTitle ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@capturedAt", snapshot.CapturedAt.ToString("o"));
            insertCmd.Parameters.AddWithValue("@domJson", domJson);
            insertCmd.Parameters.AddWithValue("@elementCount", elementCount);
            insertCmd.Parameters.AddWithValue("@sizeBytes", domJson.Length);
            snapshotId = (long)insertCmd.ExecuteScalar()!;
        }

        // Index individual elements
        IndexElements(conn, snapshotId, snapshot.RootElement, "");

        tx.Commit();

        _logger.LogInformation(
            "Corpus store — Site: {SiteId}, Page: {PageName}, Elements: {ElementCount}, Size: {SizeBytes} bytes",
            siteId, snapshot.PageName, elementCount, domJson.Length);
    }

    public DomSnapshot? GetLatestSnapshot(long siteId, string pageName)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Snapshots WHERE SiteId = @siteId AND PageName = @pageName AND IsLatest = 1";
        cmd.Parameters.AddWithValue("@siteId", siteId);
        cmd.Parameters.AddWithValue("@pageName", pageName);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return ReadSnapshot(reader);
    }

    public List<SnapshotSummary> ListSnapshots(long siteId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, SiteId, PageName, PageUrl, PageTitle, CapturedAt, ElementCount, SnapshotSizeBytes, IsLatest FROM Snapshots WHERE SiteId = @siteId ORDER BY CapturedAt DESC";
        cmd.Parameters.AddWithValue("@siteId", siteId);

        using var reader = cmd.ExecuteReader();
        var results = new List<SnapshotSummary>();
        while (reader.Read())
        {
            results.Add(new SnapshotSummary
            {
                Id = reader.GetInt64(0),
                SiteId = reader.GetInt64(1),
                PageName = reader.GetString(2),
                PageUrl = reader.GetString(3),
                PageTitle = reader.IsDBNull(4) ? null : reader.GetString(4),
                CapturedAt = DateTimeOffset.Parse(reader.GetString(5)),
                ElementCount = reader.GetInt32(6),
                SnapshotSizeBytes = reader.GetInt64(7),
                IsLatest = reader.GetInt32(8) == 1
            });
        }
        return results;
    }

    public List<DomElement> SearchElements(long siteId, string query)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT e.Tag, e.ElementId, e.ClassName, e.DataTestId, e.AriaLabel, e.Role, e.TextContent
            FROM Elements e
            INNER JOIN Snapshots s ON e.SnapshotId = s.Id
            WHERE s.SiteId = @siteId AND s.IsLatest = 1
              AND (e.Tag LIKE @q OR e.ElementId LIKE @q OR e.DataTestId LIKE @q OR e.AriaLabel LIKE @q OR e.TextContent LIKE @q)
            """;
        cmd.Parameters.AddWithValue("@siteId", siteId);
        cmd.Parameters.AddWithValue("@q", $"%{query}%");

        using var reader = cmd.ExecuteReader();
        var results = new List<DomElement>();
        while (reader.Read())
        {
            results.Add(new DomElement
            {
                Tag = reader.GetString(0),
                Id = reader.IsDBNull(1) ? null : reader.GetString(1),
                ClassName = reader.IsDBNull(2) ? null : reader.GetString(2),
                DataTestId = reader.IsDBNull(3) ? null : reader.GetString(3),
                AriaLabel = reader.IsDBNull(4) ? null : reader.GetString(4),
                Role = reader.IsDBNull(5) ? null : reader.GetString(5),
                TextContent = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }
        return results;
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private DomSnapshot ReadSnapshot(SqliteDataReader reader)
    {
        var domJson = reader.GetString(reader.GetOrdinal("DomJson"));
        var root = JsonSerializer.Deserialize<DomElement>(domJson, JsonOptions) ?? new DomElement();
        return new DomSnapshot
        {
            PageUrl = reader.GetString(reader.GetOrdinal("PageUrl")),
            PageTitle = reader.IsDBNull(reader.GetOrdinal("PageTitle")) ? "" : reader.GetString(reader.GetOrdinal("PageTitle")),
            CapturedAt = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("CapturedAt"))),
            RootElement = root
        };
    }

    private void IndexElements(SqliteConnection conn, long snapshotId, DomElement element, string parentPath)
    {
        var currentPath = string.IsNullOrEmpty(parentPath) ? element.Tag : $"{parentPath}/{element.Tag}";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Elements (SnapshotId, Tag, ElementId, ClassName, DataTestId, AriaLabel, Role, TextContent, ParentPath)
            VALUES (@sid, @tag, @eid, @cls, @dtid, @aria, @role, @text, @path)
            """;
        cmd.Parameters.AddWithValue("@sid", snapshotId);
        cmd.Parameters.AddWithValue("@tag", element.Tag);
        cmd.Parameters.AddWithValue("@eid", (object?)element.Id ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cls", (object?)element.ClassName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dtid", (object?)element.DataTestId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@aria", (object?)element.AriaLabel ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@role", (object?)element.Role ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@text", (object?)element.TextContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@path", currentPath);
        cmd.ExecuteNonQuery();

        foreach (var child in element.Children)
            IndexElements(conn, snapshotId, child, currentPath);
    }

    private static int CountElements(DomElement element)
    {
        var count = 1;
        foreach (var child in element.Children)
            count += CountElements(child);
        return count;
    }
}
