namespace Brinell.Scraper.Models;

public sealed class SiteInfo
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string StartUrl { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public List<string> UrlAliases { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastOpenedAt { get; set; } = DateTime.UtcNow;
    public int PageCount { get; set; }
    public int ControlCount { get; set; }
}
