namespace Brinell.Wpf.Interfaces;

/// <summary>
/// Interface for elements supporting ExpandCollapse pattern (Windows UI Automation).
/// Used for ComboBox controls on WPF.
/// Implemented by <see cref="FlaUI.FlaUIWpfElement"/> when the underlying UIA element supports ExpandCollapse.
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
    /// Expands the dropdown to reveal items.
    /// </summary>
    /// <returns>True if successful, false if pattern not supported or operation failed.</returns>
    bool Expand();

    /// <summary>
    /// Collapses the dropdown.
    /// </summary>
    /// <returns>True if successful, false if pattern not supported or operation failed.</returns>
    bool Collapse();

    /// <summary>
    /// Gets the list items after expanding.
    /// Automatically expands if needed and restores original state.
    /// </summary>
    /// <returns>List of item elements, or null if pattern not supported.</returns>
    IReadOnlyList<IWpfElement>? GetExpandedItems();

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
    /// </summary>
    /// <returns>The selected item text, or null if nothing is selected or pattern not supported.</returns>
    string? GetSelectedItemText();
}
