namespace Brinell.Scraper.Models;

public sealed class SnapshotSummary
{
    public long Id { get; init; }
    public long SiteId { get; init; }
    public string PageName { get; init; } = "";
    public string PageUrl { get; init; } = "";
    public string? PageTitle { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
    public int ElementCount { get; init; }
    public long SnapshotSizeBytes { get; init; }
    public bool IsLatest { get; init; }
}
