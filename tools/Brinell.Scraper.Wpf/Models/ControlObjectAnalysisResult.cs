namespace Brinell.Scraper.Models;

public sealed class ControlObjectAnalysisResult
{
    public List<ControlProposal> Proposals { get; init; } = [];
    public LocatorReport? LocatorReport { get; init; }
    public int LocalGroupCount { get; init; }
    public int SnapshotsAnalyzed { get; init; }
    public DateTimeOffset AnalyzedAt { get; init; }
}
