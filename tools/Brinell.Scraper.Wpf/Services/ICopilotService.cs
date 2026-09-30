namespace Brinell.Scraper.Services;

public interface ICopilotService
{
    bool IsAuthenticated { get; }
    string? LastInitError { get; }
    Task InitializeAsync(long siteId, string siteSlug, CancellationToken ct = default);
    Task DisposeSessionAsync();
    Task<string> AnalyzeAsync(string prompt, CancellationToken ct = default);
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default);
    string? GetCliPath();
}
