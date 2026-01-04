namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for expandable controls (accordions, expanders, etc.).
/// </summary>
public interface IExpandableControlObject : IControlObject
{
    /// <summary>
    /// Gets whether the control is currently expanded.
    /// </summary>
    bool IsExpanded(int? timeoutMs = null);

    /// <summary>
    /// Waits for the expanded state to match the expected value.
    /// </summary>
    bool WaitExpanded(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts the expanded state matches the expected value.
    /// </summary>
    void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Expands the control.
    /// </summary>
    void Expand(int? timeoutMs = null);

    /// <summary>
    /// Collapses the control.
    /// </summary>
    void Collapse(int? timeoutMs = null);

    /// <summary>
    /// Toggles the expanded state.
    /// </summary>
    void Toggle(int? timeoutMs = null);

    /// <summary>
    /// Gets the header text.
    /// </summary>
    string GetHeaderText(int? timeoutMs = null);
}
