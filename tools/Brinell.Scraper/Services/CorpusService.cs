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

            CREATE TABLE IF NOT EXISTS AnalysisResults (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                SiteId      INTEGER NOT NULL,
                AnalyzedAt  TEXT    NOT NULL,
                IsCurrent   INTEGER NOT NULL DEFAULT 1,
                Snapshots   INTEGER NOT NULL,
                LocalGroups INTEGER NOT NULL,
                ProposalsJson TEXT  NOT NULL,
                LocatorReportJson TEXT,
                FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_AnalysisResults_Site ON AnalysisResults(SiteId, IsCurrent);

            CREATE TABLE IF NOT EXISTS PageObjects (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                SiteId        INTEGER NOT NULL,
                SnapshotId    INTEGER NOT NULL,
                ClassName     TEXT    NOT NULL,
                Namespace     TEXT    NOT NULL,
                MainCode      TEXT    NOT NULL,
                ContainerCodesJson TEXT NOT NULL DEFAULT '[]',
                UsedControlsJson   TEXT NOT NULL DEFAULT '[]',
                Status        TEXT    NOT NULL,
                ValidationJson TEXT,
                GeneratedAt   TEXT    NOT NULL,
                FOREIGN KEY (SiteId) REFERENCES Sites(Id) ON DELETE CASCADE,
                FOREIGN KEY (SnapshotId) REFERENCES Snapshots(Id) ON DELETE CASCADE,
                UNIQUE(SnapshotId)
            );
            CREATE INDEX IF NOT EXISTS IX_PageObjects_Site ON PageObjects(SiteId);
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

    public DomSnapshot? GetSnapshotById(long snapshotId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Snapshots WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", snapshotId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var snapshot = ReadSnapshot(reader);
        snapshot.PageName = reader.GetString(reader.GetOrdinal("PageName"));
        return snapshot;
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

    public void DeleteSnapshot(long snapshotId)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM Elements WHERE SnapshotId = @id";
            cmd.Parameters.AddWithValue("@id", snapshotId);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM Snapshots WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", snapshotId);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        _logger.LogInformation("Deleted snapshot {SnapshotId}", snapshotId);
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

    // --- AnalysisResults ---------------------------------------------------

    public long StoreAnalysisResult(long siteId, ControlObjectAnalysisResult result)
    {
        var proposalsJson = JsonSerializer.Serialize(result.Proposals, JsonOptions);
        var locatorJson = result.LocatorReport is null
            ? null
            : JsonSerializer.Serialize(result.LocatorReport, JsonOptions);

        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using (var markCmd = conn.CreateCommand())
        {
            markCmd.CommandText = "UPDATE AnalysisResults SET IsCurrent = 0 WHERE SiteId = @siteId";
            markCmd.Parameters.AddWithValue("@siteId", siteId);
            markCmd.ExecuteNonQuery();
        }

        long id;
        using (var insertCmd = conn.CreateCommand())
        {
            insertCmd.CommandText = """
                INSERT INTO AnalysisResults (SiteId, AnalyzedAt, IsCurrent, Snapshots, LocalGroups, ProposalsJson, LocatorReportJson)
                VALUES (@siteId, @analyzedAt, 1, @snapshots, @localGroups, @proposalsJson, @locatorJson);
                SELECT last_insert_rowid();
                """;
            insertCmd.Parameters.AddWithValue("@siteId", siteId);
            insertCmd.Parameters.AddWithValue("@analyzedAt", result.AnalyzedAt.ToString("o"));
            insertCmd.Parameters.AddWithValue("@snapshots", result.SnapshotsAnalyzed);
            insertCmd.Parameters.AddWithValue("@localGroups", result.LocalGroupCount);
            insertCmd.Parameters.AddWithValue("@proposalsJson", proposalsJson);
            insertCmd.Parameters.AddWithValue("@locatorJson", (object?)locatorJson ?? DBNull.Value);
            id = (long)insertCmd.ExecuteScalar()!;
        }

        tx.Commit();

        _logger.LogInformation(
            "AnalysisResult stored — Site: {SiteId}, Id: {Id}, Proposals: {Count}",
            siteId, id, result.Proposals.Count);

        return id;
    }

    public ControlObjectAnalysisResult? GetCurrentAnalysisResult(long siteId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT AnalyzedAt, Snapshots, LocalGroups, ProposalsJson, LocatorReportJson
            FROM AnalysisResults
            WHERE SiteId = @siteId AND IsCurrent = 1
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@siteId", siteId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var proposalsJson = reader.GetString(3);
        var locatorJson = reader.IsDBNull(4) ? null : reader.GetString(4);

        return new ControlObjectAnalysisResult
        {
            AnalyzedAt = DateTimeOffset.Parse(reader.GetString(0)),
            SnapshotsAnalyzed = reader.GetInt32(1),
            LocalGroupCount = reader.GetInt32(2),
            Proposals = JsonSerializer.Deserialize<List<ControlProposal>>(proposalsJson, JsonOptions) ?? [],
            LocatorReport = locatorJson is null
                ? null
                : JsonSerializer.Deserialize<LocatorReport>(locatorJson, JsonOptions)
        };
    }

    public void UpdateProposalApproval(long siteId, string proposalName, ControlObjectStatus status)
    {
        var current = GetCurrentAnalysisResult(siteId);
        if (current is null)
        {
            _logger.LogWarning("UpdateProposalApproval — no current AnalysisResult for site {SiteId}", siteId);
            return;
        }

        var proposal = current.Proposals.FirstOrDefault(p => p.Name == proposalName);
        if (proposal is null)
        {
            _logger.LogWarning(
                "UpdateProposalApproval — proposal {Name} not found for site {SiteId}",
                proposalName, siteId);
            return;
        }

        proposal.Status = status;
        var proposalsJson = JsonSerializer.Serialize(current.Proposals, JsonOptions);

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE AnalysisResults
            SET ProposalsJson = @proposalsJson
            WHERE SiteId = @siteId AND IsCurrent = 1
            """;
        cmd.Parameters.AddWithValue("@siteId", siteId);
        cmd.Parameters.AddWithValue("@proposalsJson", proposalsJson);
        cmd.ExecuteNonQuery();

        _logger.LogInformation(
            "Proposal status updated — Site: {SiteId}, Name: {Name}, Status: {Status}",
            siteId, proposalName, status);
    }

    // --- PageObjects -------------------------------------------------------

    public void StorePageObject(PageGenerationResult result)
    {
        var containerCodesJson = JsonSerializer.Serialize(result.ContainerCodes, JsonOptions);
        var usedControlsJson = JsonSerializer.Serialize(result.CustomControlsUsed, JsonOptions);
        var validationJson = JsonSerializer.Serialize(result.Validation, JsonOptions);

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO PageObjects
                (SiteId, SnapshotId, ClassName, Namespace, MainCode, ContainerCodesJson, UsedControlsJson, Status, ValidationJson, GeneratedAt)
            VALUES
                (@siteId, @snapshotId, @className, @namespace, @mainCode, @containerCodesJson, @usedControlsJson, @status, @validationJson, @generatedAt)
            """;
        cmd.Parameters.AddWithValue("@siteId", result.SiteId);
        cmd.Parameters.AddWithValue("@snapshotId", result.SnapshotId);
        cmd.Parameters.AddWithValue("@className", result.ClassName);
        cmd.Parameters.AddWithValue("@namespace", result.Namespace);
        cmd.Parameters.AddWithValue("@mainCode", result.MainCode);
        cmd.Parameters.AddWithValue("@containerCodesJson", containerCodesJson);
        cmd.Parameters.AddWithValue("@usedControlsJson", usedControlsJson);
        cmd.Parameters.AddWithValue("@status", result.Status.ToString());
        cmd.Parameters.AddWithValue("@validationJson", (object?)validationJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@generatedAt", result.GeneratedAt.ToString("o"));
        cmd.ExecuteNonQuery();

        _logger.LogInformation(
            "PageObject stored — Site: {SiteId}, Snapshot: {SnapshotId}, Class: {ClassName}",
            result.SiteId, result.SnapshotId, result.ClassName);
    }

    public List<PageGenerationResult> GetPageObjects(long siteId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT SiteId, SnapshotId, ClassName, Namespace, MainCode, ContainerCodesJson, UsedControlsJson, Status, ValidationJson, GeneratedAt
            FROM PageObjects
            WHERE SiteId = @siteId
            ORDER BY GeneratedAt DESC
            """;
        cmd.Parameters.AddWithValue("@siteId", siteId);

        using var reader = cmd.ExecuteReader();
        var results = new List<PageGenerationResult>();
        while (reader.Read())
        {
            results.Add(ReadPageObject(reader));
        }
        return results;
    }

    public PageGenerationResult? GetPageObjectBySnapshot(long snapshotId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT SiteId, SnapshotId, ClassName, Namespace, MainCode, ContainerCodesJson, UsedControlsJson, Status, ValidationJson, GeneratedAt
            FROM PageObjects
            WHERE SnapshotId = @snapshotId
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("@snapshotId", snapshotId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return ReadPageObject(reader);
    }

    public void DeletePageObject(long snapshotId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM PageObjects WHERE SnapshotId = @snapshotId";
        cmd.Parameters.AddWithValue("@snapshotId", snapshotId);
        cmd.ExecuteNonQuery();

        _logger.LogInformation("PageObject deleted — Snapshot: {SnapshotId}", snapshotId);
    }

    private static PageGenerationResult ReadPageObject(SqliteDataReader reader)
    {
        var containerCodesJson = reader.GetString(5);
        var usedControlsJson = reader.GetString(6);
        var statusText = reader.GetString(7);
        var validationJson = reader.IsDBNull(8) ? null : reader.GetString(8);

        return new PageGenerationResult
        {
            SiteId = reader.GetInt64(0),
            SnapshotId = reader.GetInt64(1),
            ClassName = reader.GetString(2),
            Namespace = reader.GetString(3),
            MainCode = reader.GetString(4),
            ContainerCodes = JsonSerializer.Deserialize<List<string>>(containerCodesJson, JsonOptions) ?? [],
            CustomControlsUsed = JsonSerializer.Deserialize<List<string>>(usedControlsJson, JsonOptions) ?? [],
            Status = Enum.TryParse<PageObjectStatus>(statusText, out var s) ? s : PageObjectStatus.NotGenerated,
            Validation = validationJson is null
                ? new ValidationResult()
                : JsonSerializer.Deserialize<ValidationResult>(validationJson, JsonOptions) ?? new ValidationResult(),
            GeneratedAt = DateTimeOffset.Parse(reader.GetString(9))
        };
    }
}
