namespace Brinell.Maui.Controls.Range;

/// <summary>
/// MAUI Stepper control with +/- buttons for discrete value changes.
/// Inherits GetValue, SetValue, GetMinimum, GetMaximum, Increment, Decrement from MauiRangeControlBase.
/// Provides additional stepper-specific methods like IncrementBy and DecrementBy.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiStepperControl<TScope> : MauiRangeControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new stepper control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the stepper element.</param>
    public MauiStepperControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new stepper control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiStepperControl(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Stepper-Specific Methods

    /// <summary>
    /// Increments the stepper value multiple times.
    /// </summary>
    /// <param name="times">Number of times to increment. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout for each increment.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope IncrementBy(int? times, int? timeoutMs = null)
    {
        if (times == null || times <= 0)
            return ContainingScope;

        return RunWithElement(nameof(IncrementBy), times, timeoutMs, element =>
        {
            for (int i = 0; i < times; i++)
            {
                IncrementCore(element);
            }
        });
    }

    /// <summary>
    /// Decrements the stepper value multiple times.
    /// </summary>
    /// <param name="times">Number of times to decrement. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout for each decrement.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope DecrementBy(int? times, int? timeoutMs = null)
    {
        if (times == null || times <= 0)
            return ContainingScope;

        return RunWithElement(nameof(DecrementBy), times, timeoutMs, element =>
        {
            for (int i = 0; i < times; i++)
            {
                DecrementCore(element);
            }
        });
    }

    /// <summary>
    /// Sets the stepper to its minimum value.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SetToMinimum(int? timeoutMs = null)
    {
        return RunWithElement(nameof(SetToMinimum), timeoutMs, element =>
        {
            var min = GetMinimumCore(element) ?? 0;
            SetValueCore(element, min);
        });
    }

    /// <summary>
    /// Sets the stepper to its maximum value.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SetToMaximum(int? timeoutMs = null)
    {
        return RunWithElement(nameof(SetToMaximum), timeoutMs, element =>
        {
            var max = GetMaximumCore(element) ?? 100;
            SetValueCore(element, max);
        });
    }

    /// <summary>
    /// Checks if the stepper can be incremented (not at maximum).
    /// </summary>
    /// <returns>True if increment is possible, false otherwise.</returns>
    public bool? CanIncrement()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var current = GetValueCore(element);
        var max = GetMaximumCore(element);

        if (current == null || max == null) return true;
        return current.Value < max.Value;
    }

    /// <summary>
    /// Checks if the stepper can be decremented (not at minimum).
    /// </summary>
    /// <returns>True if decrement is possible, false otherwise.</returns>
    public bool? CanDecrement()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var current = GetValueCore(element);
        var min = GetMinimumCore(element);

        if (current == null || min == null) return true;
        return current.Value > min.Value;
    }

    #endregion
}
