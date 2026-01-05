namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for time picker/input controls.
/// </summary>
public interface ITimeControl : IControlObject
{
    /// <summary>
    /// Get the current time value.
    /// </summary>
    /// <returns>The selected time as TimeSpan.</returns>
    TimeSpan GetTime();

    /// <summary>
    /// Set the time value.
    /// </summary>
    /// <param name="time">The time to set.</param>
    void SetTime(TimeSpan time);

    /// <summary>
    /// Set the time value using hour and minute components.
    /// </summary>
    /// <param name="hour">The hour (0-23).</param>
    /// <param name="minute">The minute (0-59).</param>
    void SetTime(int hour, int minute);

    /// <summary>
    /// Assert the time equals expected value.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="expected">The expected time.</param>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertTime(TimeSpan expected, string? message = null);
}
