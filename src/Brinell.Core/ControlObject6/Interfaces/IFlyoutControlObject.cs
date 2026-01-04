namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for flyout/drawer controls.
/// </summary>
public interface IFlyoutControlObject : IControlObject
{
    /// <summary>
    /// Gets whether the flyout is currently open.
    /// </summary>
    bool IsOpen(int? timeoutMs = null);

    /// <summary>
    /// Waits for the flyout open state to match the expected value.
    /// </summary>
    bool WaitOpen(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts the flyout open state matches the expected value.
    /// </summary>
    void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Opens the flyout.
    /// </summary>
    void Open(int? timeoutMs = null);

    /// <summary>
    /// Closes the flyout.
    /// </summary>
    void Close(int? timeoutMs = null);

    /// <summary>
    /// Toggles the flyout open state.
    /// </summary>
    void Toggle(int? timeoutMs = null);

    /// <summary>
    /// Clicks a flyout item by name.
    /// </summary>
    void ClickFlyoutItem(string? name, int? timeoutMs = null);

    /// <summary>
    /// Gets all flyout item names.
    /// </summary>
    IReadOnlyList<string> GetFlyoutItemNames(int? timeoutMs = null);
}
