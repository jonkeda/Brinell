namespace Brinell.Maui.Controls.Display;

/// <summary>
/// MAUI ProgressBar control for displaying progress values (0-1).
/// Provides GetProgress(), IsIndeterminate(), and progress assertions.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class ProgressBar<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new progress bar control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the progress bar element.</param>
    public ProgressBar(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new progress bar control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public ProgressBar(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Progress - Core Methods

    /// <summary>
    /// Gets the progress value from pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>Progress value between 0 and 1, or null if not found.</returns>
    protected virtual double? GetProgressCore(IMauiElement? element)
    {
        if (element == null) return null;

        // A progress bar reports its value through the range pattern, and reports it in its own
        // units: WinUI uses 0-100 where MAUI's Progress is 0-1. Normalising against the reported
        // minimum and maximum is what makes the returned value mean the same thing everywhere.
        if (element is IRangePatternElement range && range.SupportsRangeValue)
        {
            var current = range.GetRangeValue();
            if (current.HasValue)
            {
                var min = range.GetRangeMinimum() ?? 0d;
                var max = range.GetRangeMaximum() ?? 1d;
                return max > min ? (current.Value - min) / (max - min) : current.Value;
            }
        }

        var valueAttr = element.GetAttribute("Value")
            ?? element.GetAttribute("Progress")
            ?? element.GetAttribute("RangeValue.Value");

        if (!string.IsNullOrEmpty(valueAttr) && double.TryParse(valueAttr, out var value))
        {
            return value;
        }

        return null;
    }

    #endregion

    #region Indeterminate - Core Methods

    /// <summary>
    /// Checks if progress bar is in indeterminate mode using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if indeterminate, false otherwise, null if not found.</returns>
    protected virtual bool? IsIndeterminateCore(IMauiElement? element)
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

    #endregion

    #region Hand-written Convenience Members

    // Tolerance-based comparison is beyond the generated Assert/Wait variants, so the
    // progress members that take a tolerance stay hand-written. They overload the
    // generated exact-equality AssertProgress/WaitProgress rather than replacing them.

    /// <summary>
    /// Waits for the progress value to reach the expected value within a tolerance.
    /// </summary>
    /// <param name="expected">The expected progress value (0-1). Null skips the wait.</param>
    /// <param name="tolerance">Allowed tolerance for comparison.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>True if the condition was met, false if the timeout was reached.</returns>
    public bool WaitProgress(double? expected, double tolerance, int? timeoutMs = null)
    {
        if (expected == null)
            return true;

        return RunWaitWithElement(expected,
            e =>
            {
                var actual = GetProgressCore(e);
                return actual.HasValue && Math.Abs(actual.Value - expected.Value) <= tolerance;
            },
            timeoutMs);
    }

    /// <summary>
    /// Asserts progress has reached expected value within a tolerance.
    /// </summary>
    /// <param name="expected">The expected progress value (0-1). Null skips the assertion.</param>
    /// <param name="tolerance">Allowed tolerance for comparison.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertProgress(double? expected, double tolerance, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssertWithElement(expected,
            GetProgressCore,
            (actual, exp) => actual.HasValue && exp.HasValue && Math.Abs(actual.Value - exp.Value) <= tolerance,
            message ?? $"Expected progress {expected} (±{tolerance}). Locator: {Locator}", timeoutMs);
    }

    /// <summary>
    /// Asserts progress bar is indeterminate.
    /// </summary>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertIndeterminate(string? message, int? timeoutMs = null)
        => AssertIndeterminate(true, message, timeoutMs);

    #endregion
}
