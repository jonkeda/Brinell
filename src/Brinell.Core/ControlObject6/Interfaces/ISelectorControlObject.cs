namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for selector controls (Picker, ComboBox, Select).
/// Provides item selection operations.
/// </summary>
public interface ISelectorControlObject : IInteractiveControlObject
{
    #region Selected Item

    /// <summary>
    /// Gets the index of the currently selected item, or -1 if none selected.
    /// </summary>
    int GetSelectedIndex(int? timeoutMs = null);

    /// <summary>
    /// Gets the text of the currently selected item.
    /// </summary>
    string GetSelectedText(int? timeoutMs = null);

    /// <summary>
    /// Asserts the selected index matches the expected value.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts the selected text matches the expected value.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Item Count

    /// <summary>
    /// Gets the number of items in the selector.
    /// </summary>
    int GetItemCount(int? timeoutMs = null);

    /// <summary>
    /// Asserts the item count matches the expected value.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Select Actions

    /// <summary>
    /// Selects an item by its index.
    /// If index is null, does nothing (skip operation).
    /// </summary>
    void SelectByIndex(int? index, int? timeoutMs = null);

    /// <summary>
    /// Selects an item by its text.
    /// If text is null, does nothing (skip operation).
    /// </summary>
    void SelectByText(string? text, int? timeoutMs = null);

    #endregion

    #region Items

    /// <summary>
    /// Gets all item texts in the selector.
    /// </summary>
    IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);

    /// <summary>
    /// Checks if the selector contains an item with the specified text.
    /// </summary>
    bool HasItem(string text, int? timeoutMs = null);

    #endregion
}
