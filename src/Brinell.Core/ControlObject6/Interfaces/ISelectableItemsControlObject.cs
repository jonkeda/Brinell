namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for collection controls that support item selection.
/// Extends IItemsControlObject with selection capabilities.
/// </summary>
public interface ISelectableItemsControlObject : IItemsControlObject
{
    /// <summary>
    /// Selects the item at the specified index.
    /// </summary>
    /// <param name="index">The item index (0-based). If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void SelectItem(int? index, int? timeoutMs = null);

    /// <summary>
    /// Selects the item with the specified text.
    /// </summary>
    /// <param name="text">The item text. If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void SelectItem(string? text, int? timeoutMs = null);

    /// <summary>
    /// Gets the index of the currently selected item.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The selected item index, or -1 if none selected.</returns>
    int GetSelectedItemIndex(int? timeoutMs = null);

    /// <summary>
    /// Asserts that the selected item index equals the expected value.
    /// </summary>
    /// <param name="expected">Expected selected index.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertSelectedItemIndex(int? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the text of the currently selected item.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The selected item text, or null if none selected.</returns>
    string? GetSelectedItemText(int? timeoutMs = null);

    /// <summary>
    /// Asserts that the selected item text equals the expected value.
    /// </summary>
    /// <param name="expected">Expected selected text.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertSelectedItemText(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Checks if the item at the specified index is selected.
    /// </summary>
    /// <param name="index">The item index (0-based).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if selected.</returns>
    bool IsItemSelected(int index, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the item at the specified index has the expected selection state.
    /// </summary>
    /// <param name="index">The item index (0-based).</param>
    /// <param name="expected">Expected selection state.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertItemSelected(int index, bool? expected, string? message = null, int? timeoutMs = null);
}
