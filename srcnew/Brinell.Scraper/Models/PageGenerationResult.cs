namespace Brinell.Scraper.Models;

public sealed class PageGenerationResult
{
    public long SiteId { get; set; }
    public long SnapshotId { get; set; }
    public string ClassName { get; init; } = "";
    public string Namespace { get; init; } = "";
    public string MainCode { get; init; } = "";
    public List<string> ContainerCodes { get; init; } = [];
    public ValidationResult Validation { get; init; } = new();
    public List<string> CustomControlsUsed { get; init; } = [];
    public List<ControlObjectReference> UsedControlObjects { get; init; } = [];
    public PageObjectStatus Status { get; init; } = PageObjectStatus.Generated;
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}
