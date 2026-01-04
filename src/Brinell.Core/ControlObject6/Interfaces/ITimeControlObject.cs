using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for time picker controls.
/// Provides methods for getting/setting times and interacting with time pickers.
/// </summary>
public interface ITimeControlObject : IControlObject
{
    /// <summary>
    /// Gets the currently selected time.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The selected time, or TimeSpan.Zero if not set.</returns>
    TimeSpan GetTime(int? timeoutMs = null);

    /// <summary>
    /// Sets the time value.
    /// </summary>
    /// <param name="time">The time to set. If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void SetTime(TimeSpan? time, int? timeoutMs = null);

    /// <summary>
    /// Waits for the time to match expected value.
    /// </summary>
    /// <param name="expected">Expected time value.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if time matches within timeout.</returns>
    bool WaitTime(TimeSpan? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the time equals the expected value.
    /// </summary>
    /// <param name="expected">Expected time value.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the time is within the specified range.
    /// </summary>
    /// <param name="min">Minimum time (inclusive).</param>
    /// <param name="max">Maximum time (inclusive).</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertTimeInRange(TimeSpan? min, TimeSpan? max, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the minimum selectable time.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The minimum time.</returns>
    TimeSpan GetMinTime(int? timeoutMs = null);

    /// <summary>
    /// Gets the maximum selectable time.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The maximum time.</returns>
    TimeSpan GetMaxTime(int? timeoutMs = null);

    /// <summary>
    /// Checks if the time picker is currently open.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if picker is open.</returns>
    bool IsPickerOpen(int? timeoutMs = null);

    /// <summary>
    /// Opens the time picker popup.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void OpenPicker(int? timeoutMs = null);

    /// <summary>
    /// Closes the time picker popup.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void ClosePicker(int? timeoutMs = null);
}
