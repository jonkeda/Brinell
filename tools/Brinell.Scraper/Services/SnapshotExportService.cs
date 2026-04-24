using System.IO;
using System.Text.Json;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.Services;

public sealed class SnapshotExportService
{
    private static readonly JsonSerializerOptions ExportOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions ImportOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public string Export(DomSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, ExportOptions);
    }

    public DomSnapshot Import(string json)
    {
        return JsonSerializer.Deserialize<DomSnapshot>(json, ImportOptions)
            ?? throw new JsonException("Failed to deserialize DomSnapshot: result was null.");
    }

    public string GenerateFilename(DomSnapshot snapshot)
    {
        var site = SanitizeFilename(snapshot.SiteName);
        var page = SanitizeFilename(snapshot.PageName);
        var timestamp = snapshot.CapturedAt.ToString("yyyyMMdd-HHmmss");
        return $"{site}-{page}-{timestamp}.json";
    }

    private static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }
}
