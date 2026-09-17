namespace Brinell.Maui.Controls.Range;

using Brinell.Core.Utilities;

/// <summary>
/// MAUI Slider control for continuous value selection.
/// Inherits GetValue, SetValue, GetMinimum, GetMaximum, Increment, Decrement from RangeControlBase.
/// Provides additional slider-specific methods like SlideToPercentage.
/// Overrides SetValueCore to clamp the value to the slider's range before the element sets it -
/// through RangeValue on Windows, with arrow keys on Android and iOS.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Slider<TScope> : Base.RangeControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new slider control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the slider element.</param>
    public Slider(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new slider control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Slider(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Sets slider value, clamped to the slider's range.
    /// The element picks the route: the RangeValue pattern on Windows, arrow keys on Android and iOS.
    /// </summary>
    /// <param name="element">The slider element.</param>
    /// <param name="value">The target value. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected override void SetValueCore(IMauiElement element, double? value, int? timeoutMs = null)
    {
        if (value == null) return;

        EnsureSettableCore(element);

        var min = GetMinimumCore(element) ?? 0;
        var max = GetMaximumCore(element) ?? 100;
        var range = max - min;

        if (range <= 0)
        {
            throw new InvalidOperationException($"Invalid slider range: min={min}, max={max}");
        }

        // Clamp value to valid range
        var target = Math.Clamp(value.Value, min, max);

        element.SetRangeValue(target);
    }

    /// <summary>
    /// Gets the current value as a percentage of the range from a pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The percentage (0-100), or null if not available.</returns>
    protected virtual double? GetPercentageCore(IMauiElement? element)
    {
        if (element == null) return null;

        var current = GetValueCore(element);
        if (current == null) return null;

        var min = GetMinimumCore(element) ?? 0;
        var max = GetMaximumCore(element) ?? 100;

        if (Math.Abs(max - min) < 0.0001) return 0;

        return ((current.Value - min) / (max - min)) * 100.0;
    }

    #endregion

    #region Slider-Specific Core Methods

    /// <summary>
    /// Slides to the specified percentage of the slider range.
    /// </summary>
    /// <param name="element">The pre-found slider element.</param>
    /// <param name="percentage">Percentage (0-100) to slide to. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SlideToPercentageCore(IMauiElement element, double? percentage,
        int? timeoutMs = null)
    {
        if (percentage == null) return;

        var min = GetMinimumCore(element) ?? 0;
        var max = GetMaximumCore(element) ?? 100;
        var value = min + ((max - min) * (percentage.Value / 100.0));
        SetValueCore(element, value, timeoutMs);
    }

    /// <summary>
    /// Slides to the minimum value.
    /// </summary>
    /// <param name="element">The pre-found slider element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SlideToMinimumCore(IMauiElement element, int? timeoutMs = null)
    {
        var min = GetMinimumCore(element) ?? 0;
        SetValueCore(element, min, timeoutMs);
    }

    /// <summary>
    /// Slides to the maximum value.
    /// </summary>
    /// <param name="element">The pre-found slider element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SlideToMaximumCore(IMauiElement element, int? timeoutMs = null)
    {
        var max = GetMaximumCore(element) ?? 100;
        SetValueCore(element, max, timeoutMs);
    }

    #endregion
}
