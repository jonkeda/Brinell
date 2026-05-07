namespace Brinell.Scraper.Services;

public interface ICopilotService
{
    bool IsAuthenticated { get; }
    long CurrentSiteId { get; set; }
    Task InitializeAsync(CancellationToken ct = default);
    Task<string> AnalyzeAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
}
