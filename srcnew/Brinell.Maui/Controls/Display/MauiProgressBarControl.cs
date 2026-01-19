namespace Brinell.Maui.Controls.Display;

/// <summary>
/// MAUI ProgressBar control for displaying progress values (0-1).
/// Provides GetProgress(), IsIndeterminate(), and progress assertions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiProgressBarControl<TScope> : MauiControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new progress bar control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the progress bar element.</param>
    public MauiProgressBarControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new progress bar control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiProgressBarControl(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Progress - Core Methods

    /// <summary>
    /// Gets the progress value from pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>Progress value between 0 and 1, or null if not found.</returns>
    protected double? GetProgressCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try Value attribute first (Windows/MAUI)
        var valueAttr = element.GetAttribute("Value") 
            ?? element.GetAttribute("Progress")
            ?? element.GetAttribute("RangeValue.Value");
        
        if (!string.IsNullOrEmpty(valueAttr) && double.TryParse(valueAttr, out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Gets the current progress value (0-1).
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>The progress value, or null if element not found.</returns>
    public double? GetProgress(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        return GetProgressCore(TryFindElement());
    }

    #endregion

    #region Indeterminate - Core Methods

    /// <summary>
    /// Checks if progress bar is in indeterminate mode using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if indeterminate, false otherwise, null if not found.</returns>
    protected bool? IsIndeterminateCore(IMauiElement? element)
    {
        if (element == null) return null;

        var isIndeterminate = element.GetAttribute("IsIndeterminate")
            ?? element.GetAttribute("isIndeterminate");
        
        if (!string.IsNullOrEmpty(isIndeterminate))
        {
            return isIndeterminate.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Checks if the progress bar is in indeterminate (loading) mode.
    /// </summary>
    /// <returns>True if indeterminate, false if showing progress, null if not found.</returns>
    public bool? IsIndeterminate()
    {
        return IsIndeterminateCore(TryFindElement());
    }

    #endregion

    #region Wait Progress

    /// <summary>
    /// Waits for progress value using pre-found element.
    /// </summary>
    protected bool WaitProgressCore(IMauiElement element, double expected, double tolerance, int timeoutMs)
    {
        return PollWithElement(
            element,
            e =>
            {
                var actual = GetProgressCore(e);
                return actual.HasValue && Math.Abs(actual.Value - expected) <= tolerance;
            },
            timeoutMs);
    }

    /// <summary>
    /// Waits for progress to reach expected value.
    /// </summary>
    /// <param name="expected">The expected progress value (0-1).</param>
    /// <param name="tolerance">Allowed tolerance for comparison (default 0.01).</param>
    /// <param name="timeoutMs">Maximum time to wait.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    public bool WaitProgress(double? expected, double tolerance = 0.01, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null) return false;

        return WaitProgressCore(element, expected.Value, tolerance, timeoutMs ?? DefaultTimeoutMs);
    }

    #endregion

    #region Assert Progress

    /// <summary>
    /// Asserts progress has reached expected value.
    /// </summary>
    /// <param name="expected">The expected progress value (0-1).</param>
    /// <param name="tolerance">Allowed tolerance for comparison (default 0.01).</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertProgress(double? expected, double tolerance = 0.01, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(nameof(AssertProgress), expected, () =>
        {
            WaitProgress(expected, tolerance, timeoutMs);
            return GetProgress();
        }, (actual, exp) => actual.HasValue && exp.HasValue && Math.Abs(actual.Value - exp.Value) <= tolerance,
            message ?? $"Expected progress {expected} (±{tolerance}). Locator: {Locator}");
    }

    /// <summary>
    /// Asserts progress bar is indeterminate.
    /// </summary>
    public TScope AssertIndeterminate(string? message = null, int? timeoutMs = null)
        => AssertIndeterminate(true, message, timeoutMs);

    /// <summary>
    /// Asserts progress bar indeterminate state.
    /// </summary>
    public TScope AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        var passed = Poll(() => IsIndeterminate() == expected.Value, timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            var actual = IsIndeterminate();
            throw new AssertionException(
                message ?? $"Expected indeterminate={expected} but was {actual}. Locator: {Locator}");
        }

        return ContainingScope;
    }

    #endregion
}
