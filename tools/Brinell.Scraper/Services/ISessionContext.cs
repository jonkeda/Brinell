namespace Brinell.Scraper.Services;

public interface ISessionContext
{
    long? CurrentSiteId { get; set; }
    string? CurrentSiteSlug { get; set; }
}

public sealed class SessionContext : ISessionContext
{
    public long? CurrentSiteId { get; set; }
    public string? CurrentSiteSlug { get; set; }
}
