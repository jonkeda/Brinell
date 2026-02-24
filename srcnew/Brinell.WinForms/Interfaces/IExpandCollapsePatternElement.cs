namespace Brinell.WinForms.Interfaces;

/// <summary>
/// Expand/collapse pattern support for WinForms elements (ComboBox, TreeNode, etc.).
/// </summary>
public interface IExpandCollapsePatternElement
{
    /// <summary>
    /// Whether this element supports expand/collapse operations.
    /// </summary>
    bool SupportsExpandCollapse { get; }

    /// <summary>
    /// Whether the element is currently expanded.
    /// </summary>
    bool IsExpanded { get; }

    /// <summary>
    /// Expand the element.
    /// </summary>
    void Expand();

    /// <summary>
    /// Collapse the element.
    /// </summary>
    void Collapse();

    /// <summary>
    /// Gets the expanded child items as elements.
    /// </summary>
    IReadOnlyList<IWinFormsElement>? GetExpandedItems();

    /// <summary>
    /// Selects a child item by its text.
    /// </summary>
    void SelectItemByText(string text);

    /// <summary>
    /// Selects a child item by its index.
    /// </summary>
    void SelectItemByIndex(int index);

    /// <summary>
    /// Gets the text of the currently selected item.
    /// </summary>
    string? GetSelectedItemText();
}
