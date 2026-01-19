namespace Brinell.Maui.Controls.Range;

/// <summary>
/// MAUI Slider control for continuous value selection.
/// Inherits GetValue, SetValue, GetMinimum, GetMaximum, Increment, Decrement from MauiRangeControlBase.
/// Provides additional slider-specific methods like SlideToPercentage.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiSliderControl<TScope> : MauiRangeControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new slider control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the slider element.</param>
    public MauiSliderControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new slider control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiSliderControl(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Slider-Specific Methods

    /// <summary>
    /// Slides to the specified percentage of the slider range.
    /// </summary>
    /// <param name="percentage">Percentage (0-100) to slide to. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SlideToPercentage(double? percentage, int? timeoutMs = null)
    {
        if (percentage == null)
            return ContainingScope;

        return RunWithElement(nameof(SlideToPercentage), percentage, timeoutMs, element =>
        {
            var min = GetMinimumCore(element) ?? 0;
            var max = GetMaximumCore(element) ?? 100;
            var value = min + ((max - min) * (percentage.Value / 100.0));
            SetValueCore(element, value);
        });
    }

    /// <summary>
    /// Gets the current value as a percentage of the range.
    /// </summary>
    /// <returns>The percentage (0-100), or null if element not found.</returns>
    public double? GetPercentage()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var current = GetValueCore(element);
        if (current == null) return null;

        var min = GetMinimumCore(element) ?? 0;
        var max = GetMaximumCore(element) ?? 100;

        if (Math.Abs(max - min) < 0.0001) return 0;

        return ((current.Value - min) / (max - min)) * 100.0;
    }

    /// <summary>
    /// Slides to the minimum value.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SlideToMinimum(int? timeoutMs = null)
    {
        return RunWithElement(nameof(SlideToMinimum), timeoutMs, element =>
        {
            var min = GetMinimumCore(element) ?? 0;
            SetValueCore(element, min);
        });
    }

    /// <summary>
    /// Slides to the maximum value.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SlideToMaximum(int? timeoutMs = null)
    {
        return RunWithElement(nameof(SlideToMaximum), timeoutMs, element =>
        {
            var max = GetMaximumCore(element) ?? 100;
            SetValueCore(element, max);
        });
    }

    #endregion
}
