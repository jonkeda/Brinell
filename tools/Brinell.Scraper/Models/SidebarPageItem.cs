namespace Brinell.Scraper.Models;

public sealed class SidebarPageItem
{
    public long PageId { get; init; }
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public string StatusIcon { get; init; } = "";
}
