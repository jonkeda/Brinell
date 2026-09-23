namespace Brinell.Scraper.Models;

/// <summary>
/// Metadata for a page (distinct page name) with info from its latest snapshot.
/// Used for page-level CRUD operations and visibility reconciliation.
/// </summary>
public sealed class PageMetadata
{
    public long LatestSnapshotId { get; init; }
    public string PageName { get; init; } = "";
    public string PageUrl { get; init; } = "";
    public DateTimeOffset LatestCapturedAt { get; init; }
}
