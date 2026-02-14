namespace Brinell.Maui.Controls.Display;

/// <summary>
/// MAUI ActivityIndicator control for displaying loading/busy state.
/// Provides IsRunning(), WaitRunning(), and running state assertions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class ActivityIndicator<TScope> : ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new activity indicator control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the activity indicator element.</param>
    public ActivityIndicator(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new activity indicator control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public ActivityIndicator(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region IsRunning - Core Methods

    /// <summary>
    /// Checks if activity indicator is running using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if running, false otherwise, null if not found.</returns>
    protected bool? IsRunningCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try IsRunning attribute (MAUI property)
        var isRunning = element.GetAttribute("IsRunning")
            ?? element.GetAttribute("isRunning");
        
        if (!string.IsNullOrEmpty(isRunning))
        {
            return isRunning.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // Fallback: check if visible (running indicators are typically visible)
        return element.Visible;
    }

    /// <summary>
    /// Checks if the activity indicator is currently running (spinning).
    /// </summary>
    /// <returns>True if running, false if stopped, null if not found.</returns>
    public bool? IsRunning()
    {
        return IsRunningCore(TryFindElement());
    }

    #endregion

    #region WaitRunning

    /// <summary>
    /// Waits for running state using pre-found element.
    /// </summary>
    protected bool WaitRunningCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsRunningCore(e) == expected,
            timeoutMs);
    }

    /// <summary>
    /// Waits for the activity indicator to reach expected running state.
    /// </summary>
    /// <param name="expected">The expected running state.</param>
    /// <param name="timeoutMs">Maximum time to wait.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    public bool WaitRunning(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null)
        {
            // If not found and expecting not running, that's a match
            return expected.Value == false;
        }

        return WaitRunningCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    #endregion

    #region AssertRunning

    /// <summary>
    /// Asserts the activity indicator is running.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertRunning(string? message = null, int? timeoutMs = null)
        => AssertRunning(true, message, timeoutMs);

    /// <summary>
    /// Asserts the activity indicator running state.
    /// </summary>
    /// <param name="expected">The expected running state.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertRunning(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(nameof(AssertRunning), expected, () =>
        {
            WaitRunning(expected, timeoutMs);
            return IsRunning();
        }, message ?? $"Expected activity indicator {(expected.Value ? "to be running" : "to be stopped")}. Locator: {Locator}");
    }

    #endregion
}
