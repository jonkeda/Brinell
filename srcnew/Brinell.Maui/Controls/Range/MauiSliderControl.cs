namespace Brinell.Maui.Controls.Range;

/// <summary>
/// MAUI Slider control for continuous value selection.
/// Inherits GetValue, SetValue, GetMinimum, GetMaximum, Increment, Decrement from MauiRangeControlBase.
/// Provides additional slider-specific methods like SlideToPercentage.
/// Overrides SetValueCore to use click-based positioning instead of SendKeys.
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
    
    #region SetValue Override
    
    /// <summary>
    /// Sets slider value by clicking at the calculated position on the slider track.
    /// Overrides base SendKeys approach which doesn't work for native sliders.
    /// </summary>
    /// <param name="element">The slider element.</param>
    /// <param name="value">The target value.</param>
    protected override void SetValueCore(IMauiElement element, double value)
    {
        var min = GetMinimumCore(element) ?? 0;
        var max = GetMaximumCore(element) ?? 100;
        var range = max - min;
        
        if (range <= 0)
        {
            throw new InvalidOperationException($"Invalid slider range: min={min}, max={max}");
        }
        
        // Clamp value to valid range
        value = Math.Clamp(value, min, max);
        
        // Calculate target position as percentage of range
        var percentage = (value - min) / range;
        
        // Get element bounds
        var location = element.Location;
        var size = element.Size;
        
        // Calculate click position
        // Use 5% padding on each side to avoid edge issues
        var padding = (int)(size.Width * 0.05);
        var usableWidth = size.Width - (2 * padding);
        var targetX = location.X + padding + (int)(usableWidth * percentage);
        var centerY = location.Y + (size.Height / 2);
        
        // Perform click at target position using Selenium Actions
        var driver = Context.Driver.UnwrapDriver();
        var actions = new OpenQA.Selenium.Interactions.Actions(driver);
        actions.MoveToLocation(targetX, centerY);
        actions.Click();
        actions.Perform();
        
        // Brief pause for value to update
        Thread.Sleep(50);
    }
    
    #endregion

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
