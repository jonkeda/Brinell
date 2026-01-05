namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for date picker/input controls.
/// </summary>
public interface IDateControl : IControlObject
{
    /// <summary>
    /// Get the current date value.
    /// </summary>
    /// <returns>The selected date.</returns>
    DateTime GetDate();

    /// <summary>
    /// Set the date value.
    /// </summary>
    /// <param name="date">The date to set.</param>
    void SetDate(DateTime date);

    /// <summary>
    /// Set the date value using year, month, and day components.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="day">The day (1-31).</param>
    void SetDate(int year, int month, int day);

    /// <summary>
    /// Assert the date equals expected value.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="expected">The expected date.</param>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertDate(DateTime expected, string? message = null);

    /// <summary>
    /// Assert the date is within an expected range.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="minDate">Minimum date (inclusive).</param>
    /// <param name="maxDate">Maximum date (inclusive).</param>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertDateInRange(DateTime minDate, DateTime maxDate, string? message = null);
}
