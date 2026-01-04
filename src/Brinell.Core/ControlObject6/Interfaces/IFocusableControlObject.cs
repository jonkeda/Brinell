namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for controls that can receive focus.
/// Extends IInteractiveControlObject with focus state and actions.
/// </summary>
public interface IFocusableControlObject : IInteractiveControlObject
{
    /// <summary>
    /// Immediately checks if the element has focus.
    /// Does not wait or retry.
    /// </summary>
    bool IsFocused();

    /// <summary>
    /// Waits for the element to gain or lose focus.
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    bool WaitFocused(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Checks that element has/doesn't have focus, throwing on failure.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void CheckFocused(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that element has/doesn't have focus for test verification.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertFocused(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Sets focus to this element.
    /// </summary>
    /// <param name="timeoutMs">Timeout for element to exist and be enabled.</param>
    void Focus(int? timeoutMs = null);

    /// <summary>
    /// Removes focus from this element.
    /// </summary>
    /// <param name="timeoutMs">Timeout for operation.</param>
    void Blur(int? timeoutMs = null);
}
