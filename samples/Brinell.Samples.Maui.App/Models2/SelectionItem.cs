namespace Brinell.Samples.Maui.App.Models2;

/// <summary>
/// Represents a selectable item for list demonstrations.
/// </summary>
public class SelectionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
