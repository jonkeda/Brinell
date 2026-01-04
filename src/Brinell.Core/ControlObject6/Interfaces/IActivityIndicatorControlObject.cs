namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for activity indicator controls (loading spinners).
/// Provides methods for checking running state.
/// </summary>
public interface IActivityIndicatorControlObject : IControlObject
{
    /// <summary>
    /// Checks if the activity indicator is currently running/animating.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if running.</returns>
    bool IsRunning(int? timeoutMs = null);

    /// <summary>
    /// Waits for the activity indicator to reach expected running state.
    /// </summary>
    /// <param name="expected">Expected running state.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if state matches within timeout.</returns>
    bool WaitRunning(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the activity indicator is in the expected running state.
    /// </summary>
    /// <param name="expected">Expected running state.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Waits until the activity indicator stops running.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void WaitUntilStopped(int? timeoutMs = null);

    /// <summary>
    /// Waits until the activity indicator starts running.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void WaitUntilStarted(int? timeoutMs = null);
}
