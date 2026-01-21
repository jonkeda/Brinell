namespace Brinell.Maui.Interfaces;

/// <summary>
/// Interface for elements supporting ExpandCollapse pattern (Windows UI Automation).
/// Used for ComboBox/Picker controls on Windows.
/// Implemented by platform-specific drivers (e.g., FlaUIMauiElement).
/// </summary>
public interface IExpandCollapsePatternElement
{
    /// <summary>
    /// Gets whether the element supports the ExpandCollapse UI Automation pattern.
    /// </summary>
    bool SupportsExpandCollapse { get; }
    
    /// <summary>
    /// Gets whether the element is currently expanded.
    /// </summary>
    bool IsExpanded { get; }
    
    /// <summary>
    /// Expands the ComboBox dropdown to reveal items.
    /// </summary>
    /// <returns>True if successful, false if pattern not supported or operation failed.</returns>
    bool Expand();
    
    /// <summary>
    /// Collapses the ComboBox dropdown.
    /// </summary>
    /// <returns>True if successful, false if pattern not supported or operation failed.</returns>
    bool Collapse();
    
    /// <summary>
    /// Gets the list items after expanding the ComboBox.
    /// Automatically expands if needed and restores original state.
    /// </summary>
    /// <returns>List of item elements, or null if pattern not supported.</returns>
    IReadOnlyList<IMauiElement>? GetExpandedItems();
}
