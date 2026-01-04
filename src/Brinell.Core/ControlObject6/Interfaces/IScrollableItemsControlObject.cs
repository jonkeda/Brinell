namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for scrollable collection controls.
/// Extends IItemsControlObject with scrolling capabilities.
/// </summary>
public interface IScrollableItemsControlObject : IItemsControlObject
{
    /// <summary>
    /// Scrolls to the item at the specified index.
    /// </summary>
    /// <param name="index">The item index (0-based). If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void ScrollToItem(int? index, int? timeoutMs = null);

    /// <summary>
    /// Scrolls to the item with the specified text.
    /// </summary>
    /// <param name="text">The item text. If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void ScrollToItem(string? text, int? timeoutMs = null);

    /// <summary>
    /// Scrolls to the top of the collection.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void ScrollToTop(int? timeoutMs = null);

    /// <summary>
    /// Scrolls to the bottom of the collection.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void ScrollToBottom(int? timeoutMs = null);

    /// <summary>
    /// Checks if the item at the specified index is currently visible.
    /// </summary>
    /// <param name="index">The item index (0-based).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if visible.</returns>
    bool IsItemVisible(int index, int? timeoutMs = null);

    /// <summary>
    /// Waits for the item visibility to reach expected state.
    /// </summary>
    /// <param name="index">The item index (0-based).</param>
    /// <param name="expected">Expected visibility state.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if state matches within timeout.</returns>
    bool WaitItemVisible(int index, bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the item at the specified index has the expected visibility.
    /// </summary>
    /// <param name="index">The item index (0-based).</param>
    /// <param name="expected">Expected visibility state.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertItemVisible(int index, bool? expected, string? message = null, int? timeoutMs = null);
}
