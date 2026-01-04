namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for controls that can be interacted with (enabled/disabled state).
/// Extends IControlObject with enabled state verification.
/// </summary>
public interface IInteractiveControlObject : IControlObject
{
    /// <summary>
    /// Immediately checks if the element is enabled.
    /// Does not wait or retry.
    /// </summary>
    bool IsEnabled();

    /// <summary>
    /// Waits for the element to become enabled or disabled.
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    /// <param name="expected">Expected enabled state, or null to skip.</param>
    /// <param name="timeoutMs">Timeout in milliseconds, or null for default.</param>
    /// <returns>True if expected state was reached, false if timed out.</returns>
    bool WaitEnabled(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Checks that element is enabled/disabled, throwing on failure.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void CheckEnabled(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that element is enabled/disabled for test verification.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
}
