namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for tab container controls.
/// </summary>
public interface ITabControl : IControlObject
{
    /// <summary>
    /// Get the number of tabs.
    /// </summary>
    /// <returns>The total tab count.</returns>
    int GetTabCount();

    /// <summary>
    /// Get the index of the currently selected tab.
    /// </summary>
    /// <returns>Zero-based index of the selected tab.</returns>
    int GetSelectedTabIndex();

    /// <summary>
    /// Get the name/text of the currently selected tab.
    /// </summary>
    /// <returns>The tab name or text.</returns>
    string GetSelectedTabName();

    /// <summary>
    /// Select a tab by its zero-based index.
    /// </summary>
    /// <param name="index">The tab index to select.</param>
    void SelectTab(int index);

    /// <summary>
    /// Select a tab by its name/text.
    /// </summary>
    /// <param name="name">The tab name or text.</param>
    void SelectTab(string name);

    /// <summary>
    /// Assert the selected tab has the expected name.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="name">Expected tab name.</param>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertSelectedTab(string name, string? message = null);

    /// <summary>
    /// Assert the selected tab has the expected index.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="index">Expected tab index.</param>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertSelectedTabIndex(int index, string? message = null);

    /// <summary>
    /// Assert the tab count equals expected value.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="expected">Expected tab count.</param>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertTabCount(int expected, string? message = null);
}
