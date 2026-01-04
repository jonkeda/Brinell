namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for toolbar controls.
/// </summary>
public interface IToolbarControlObject : IControlObject
{
    /// <summary>
    /// Gets the number of toolbar items.
    /// </summary>
    int GetToolbarItemCount(int? timeoutMs = null);

    /// <summary>
    /// Gets all toolbar item names.
    /// </summary>
    IReadOnlyList<string> GetToolbarItemNames(int? timeoutMs = null);

    /// <summary>
    /// Checks if a toolbar item exists by name.
    /// </summary>
    bool HasToolbarItem(string name, int? timeoutMs = null);

    /// <summary>
    /// Clicks a toolbar item by name.
    /// </summary>
    void ClickToolbarItem(string? name, int? timeoutMs = null);

    /// <summary>
    /// Clicks a toolbar item by index.
    /// </summary>
    void ClickToolbarItem(int? index, int? timeoutMs = null);

    /// <summary>
    /// Checks if a toolbar item is enabled.
    /// </summary>
    bool IsToolbarItemEnabled(string name, int? timeoutMs = null);

    /// <summary>
    /// Asserts the toolbar item enabled state matches the expected value.
    /// </summary>
    void AssertToolbarItemEnabled(string name, bool? expected, string? message = null, int? timeoutMs = null);
}
