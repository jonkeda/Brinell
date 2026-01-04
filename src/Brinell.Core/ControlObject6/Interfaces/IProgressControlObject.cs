namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for progress display controls (progress bars).
/// Provides methods for checking progress value and completion state.
/// </summary>
public interface IProgressControlObject : IControlObject
{
    /// <summary>
    /// Gets the current progress value.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The progress value (typically 0 to 1 or 0 to 100).</returns>
    double GetProgress(int? timeoutMs = null);

    /// <summary>
    /// Waits for progress to reach the expected value.
    /// </summary>
    /// <param name="expected">Expected progress value.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if progress matches within timeout.</returns>
    bool WaitProgress(double? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that progress equals the expected value.
    /// </summary>
    /// <param name="expected">Expected progress value.</param>
    /// <param name="tolerance">Allowed tolerance for comparison.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertProgress(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the minimum and maximum progress values.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>Tuple of (min, max).</returns>
    (double min, double max) GetMinMax(int? timeoutMs = null);

    /// <summary>
    /// Gets the progress as a percentage (0-100).
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>Progress percentage.</returns>
    double GetProgressPercent(int? timeoutMs = null);

    /// <summary>
    /// Checks if progress is complete (at maximum value).
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if complete.</returns>
    bool IsComplete(int? timeoutMs = null);

    /// <summary>
    /// Waits for progress to complete.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if completed within timeout.</returns>
    bool WaitComplete(int? timeoutMs = null);

    /// <summary>
    /// Asserts that progress is complete.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    void AssertComplete(string? message = null, int? timeoutMs = null);
}
