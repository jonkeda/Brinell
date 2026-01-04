using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for date picker controls.
/// Provides methods for getting/setting dates and interacting with date pickers.
/// </summary>
public interface IDateControlObject : IControlObject
{
    /// <summary>
    /// Gets the currently selected date.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The selected date, or DateTime.MinValue if not set.</returns>
    DateTime GetDate(int? timeoutMs = null);

    /// <summary>
    /// Sets the date value.
    /// </summary>
    /// <param name="date">The date to set. If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void SetDate(DateTime? date, int? timeoutMs = null);

    /// <summary>
    /// Waits for the date to match expected value.
    /// </summary>
    /// <param name="expected">Expected date value.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if date matches within timeout.</returns>
    bool WaitDate(DateTime? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the date equals the expected value.
    /// </summary>
    /// <param name="expected">Expected date value.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts that the date is within the specified range.
    /// </summary>
    /// <param name="min">Minimum date (inclusive).</param>
    /// <param name="max">Maximum date (inclusive).</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the minimum selectable date.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The minimum date.</returns>
    DateTime GetMinDate(int? timeoutMs = null);

    /// <summary>
    /// Gets the maximum selectable date.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The maximum date.</returns>
    DateTime GetMaxDate(int? timeoutMs = null);

    /// <summary>
    /// Checks if the date picker is currently open.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if picker is open.</returns>
    bool IsPickerOpen(int? timeoutMs = null);

    /// <summary>
    /// Opens the date picker popup.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void OpenPicker(int? timeoutMs = null);

    /// <summary>
    /// Closes the date picker popup.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void ClosePicker(int? timeoutMs = null);
}
