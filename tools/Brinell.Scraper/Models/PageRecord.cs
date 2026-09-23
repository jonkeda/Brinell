namespace Brinell.Scraper.Models;

public sealed class PageRecord
{
    public long Id { get; init; }
    public long SiteId { get; init; }
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime CapturedAt { get; init; }
    public int ElementCount { get; init; }
}
