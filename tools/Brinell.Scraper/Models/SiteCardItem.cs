namespace Brinell.Scraper.Models;

public sealed class SiteCardItem
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string StartUrl { get; set; } = "";
    public string DomainShort { get; set; } = "";
    public int PageCount { get; set; }
    public int ControlCount { get; set; }
    public DateTime? LastOpenedAt { get; set; }
    public string LastOpenedRelative { get; set; } = "never";
}
