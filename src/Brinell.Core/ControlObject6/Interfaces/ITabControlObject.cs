namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for tab controls (tabbed pages, tab bars).
/// </summary>
public interface ITabControlObject : IControlObject
{
    /// <summary>
    /// Gets the number of tabs.
    /// </summary>
    int GetTabCount(int? timeoutMs = null);

    /// <summary>
    /// Asserts the tab count matches the expected value.
    /// </summary>
    void AssertTabCount(int? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets all tab names.
    /// </summary>
    IReadOnlyList<string> GetTabNames(int? timeoutMs = null);

    /// <summary>
    /// Gets the index of the currently selected tab.
    /// </summary>
    int GetSelectedTabIndex(int? timeoutMs = null);

    /// <summary>
    /// Asserts the selected tab index matches the expected value.
    /// </summary>
    void AssertSelectedTabIndex(int? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the name of the currently selected tab.
    /// </summary>
    string? GetSelectedTabName(int? timeoutMs = null);

    /// <summary>
    /// Asserts the selected tab name matches the expected value.
    /// </summary>
    void AssertSelectedTabName(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Selects a tab by index.
    /// </summary>
    void SelectTab(int? index, int? timeoutMs = null);

    /// <summary>
    /// Selects a tab by name.
    /// </summary>
    void SelectTab(string? name, int? timeoutMs = null);

    /// <summary>
    /// Waits for a tab to be selected.
    /// </summary>
    bool WaitTabSelected(int index, int? timeoutMs = null);
}
