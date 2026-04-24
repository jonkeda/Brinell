namespace Brinell.Scraper.Models;

public sealed class ControlGroupSuggestion
{
    public required string ContainerType { get; init; }
    public required string DisplayName { get; init; }
    public required DomElement Element { get; init; }
    public List<DomElement> ChildElements { get; init; } = [];
}
