namespace Brinell.Scraper.Models;

public sealed class DomElement
{
    public string Tag { get; init; } = "";
    public string? Id { get; init; }
    public string? ClassName { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public string? DataTestId { get; init; }
    public string? Role { get; init; }
    public string? AriaLabel { get; init; }
    public string? Placeholder { get; init; }
    public string? TextContent { get; init; }
    public string? FrameSource { get; init; }
    public BoundingBox? BoundingBox { get; init; }
    public List<DomElement> Children { get; init; } = [];
}

public sealed record BoundingBox(double X, double Y, double Width, double Height);
