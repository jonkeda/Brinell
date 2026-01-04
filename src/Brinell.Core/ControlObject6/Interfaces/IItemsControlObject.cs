namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for collection controls that display a list of items.
/// Provides methods for item count, item text, and item interaction.
/// </summary>
public interface IItemsControlObject : IControlObject
{
    /// <summary>
    /// Gets the number of items in the collection.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The item count.</returns>
    int GetItemCount(int? timeoutMs = null);

    /// <summary>
    /// Waits for the item count to reach the expected value.
    /// </summary>
    /// <param name="expected">Expected item count.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if count matches within timeout.</returns>
    bool WaitItemCount(int? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the item count equals the expected value.
    /// </summary>
    /// <param name="expected">Expected item count.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the text of an item at the specified index.
    /// </summary>
    /// <param name="index">The item index (0-based).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The item text.</returns>
    string GetItemText(int index, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the item text at the specified index equals the expected value.
    /// </summary>
    /// <param name="index">The item index (0-based).</param>
    /// <param name="expected">Expected text value.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertItemText(int index, string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Checks if an item with the specified text exists.
    /// </summary>
    /// <param name="text">The text to search for.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if item exists.</returns>
    bool HasItem(string text, int? timeoutMs = null);

    /// <summary>
    /// Gets the index of the item with the specified text.
    /// </summary>
    /// <param name="text">The text to search for.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The item index, or -1 if not found.</returns>
    int GetItemIndex(string text, int? timeoutMs = null);

    /// <summary>
    /// Gets all item texts as a list.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>List of item texts.</returns>
    IReadOnlyList<string> GetAllItemTexts(int? timeoutMs = null);

    /// <summary>
    /// Clicks the item at the specified index.
    /// </summary>
    /// <param name="index">The item index (0-based). If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void ClickItem(int? index, int? timeoutMs = null);

    /// <summary>
    /// Clicks the item with the specified text.
    /// </summary>
    /// <param name="text">The item text. If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void ClickItem(string? text, int? timeoutMs = null);
}
