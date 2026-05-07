namespace Brinell.Scraper.Models;

public sealed class PageGenerationResult
{
    public string ClassName { get; init; } = "";
    public string Namespace { get; init; } = "";
    public string MainCode { get; init; } = "";
    public List<string> ContainerCodes { get; init; } = [];
    public ValidationResult Validation { get; init; } = new();
    public List<string> CustomControlsUsed { get; init; } = [];
}
