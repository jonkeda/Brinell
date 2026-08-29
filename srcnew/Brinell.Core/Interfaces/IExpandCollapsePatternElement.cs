namespace Brinell.Core.Interfaces;

/// <summary>
/// Interface for elements supporting ExpandCollapse pattern (Windows UI Automation).
/// Used for ComboBox/Picker controls on Windows.
/// Implemented by platform-specific drivers (e.g., FlaUIMauiElement).
/// </summary>
/// <typeparam name="TElement">The platform element type returned by <see cref="GetExpandedItems"/>.</typeparam>
public interface IExpandCollapsePatternElement<TElement>
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
    IReadOnlyList<TElement>? GetExpandedItems();
    
    /// <summary>
    /// Selects an item by text. Expands, finds the item, selects it using the appropriate 
    /// UIA pattern (SelectionItem), then collapses.
    /// </summary>
    /// <param name="text">The text of the item to select.</param>
    /// <returns>True if item was found and selected, false otherwise.</returns>
    bool SelectItemByText(string text);
    
    /// <summary>
    /// Selects an item by index. Expands, finds the item at the given index, selects it 
    /// using the appropriate UIA pattern (SelectionItem), then collapses.
    /// </summary>
    /// <param name="index">The 0-based index of the item to select.</param>
    /// <returns>True if item was found and selected, false otherwise.</returns>
    bool SelectItemByIndex(int index);
    
    /// <summary>
    /// Gets the text of the currently selected item using the SelectionPattern.
    /// For ComboBox controls, this returns the Name of the selected item (not the ComboBox header/title).
    /// </summary>
    /// <returns>The selected item text, or null if nothing is selected or pattern not supported.</returns>
    string? GetSelectedItemText();
}
