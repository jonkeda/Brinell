namespace Brinell.Maui.Controls.Range;

using Brinell.Core.Utilities;

/// <summary>
/// MAUI Slider control for continuous value selection.
/// Inherits GetValue, SetValue, GetMinimum, GetMaximum, Increment, Decrement from RangeControlBase.
/// Provides additional slider-specific methods like SlideToPercentage.
/// Overrides SetValueCore to use keyboard-based approach since Windows Appium driver 
/// doesn't support mouse Actions API (only pen/touch pointer input supported).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Slider<TScope> : RangeControlBase<TScope>
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
    
    #region SetValue Override
    
    /// <summary>
    /// Sets slider value using the best available approach.
    /// Priority: 1) RangeValue pattern (FlaUI), 2) windows: click, 3) keyboard navigation.
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
        
        // Try RangeValue pattern first (Windows/FlaUI) - most reliable
        if (element is IRangePatternElement rangeElement && rangeElement.SupportsRangeValue)
        {
            if (rangeElement.SetRangeValue(value))
                return;
        }
        
        // Try using windows: click extension (bypasses W3C Actions)
        if (TrySetValueWithWindowsClick(element, value, min, max))
        {
            return;
        }
        
        // Fallback: Use keyboard-based approach
        SetValueWithKeyboard(element, value, min, max);
    }
    
    /// <summary>
    /// Attempts to set slider value using the windows: click extension.
    /// This bypasses the W3C Actions API that doesn't work on Windows.
    /// </summary>
    private bool TrySetValueWithWindowsClick(IMauiElement element, double value, double min, double max)
    {
        try
        {
            var range = max - min;
            var percentage = (value - min) / range;
            
            // Get element bounds
            var location = element.Location;
            var size = element.Size;
            
            // Calculate click position with padding
            var padding = (int)(size.Width * 0.05);
            var usableWidth = size.Width - (2 * padding);
            var targetX = location.X + padding + (int)(usableWidth * percentage);
            var centerY = location.Y + (size.Height / 2);
            
            // Execute windows: click extension
            Context.Driver.ExecuteScript("windows: click", new Dictionary<string, object>
            {
                { "x", targetX },
                { "y", centerY }
            });
            
            // Brief pause for value to update
            WaitHelper.Pause(50);
            return true;
        }
        catch (Exception)
        {
            // windows: click not supported or failed
            return false;
        }
    }
    
    /// <summary>
    /// Sets slider value using keyboard arrow keys.
    /// This is a reliable fallback when mouse-based approaches don't work.
    /// </summary>
    private void SetValueWithKeyboard(IMauiElement element, double value, double min, double max)
    {
        var step = GetStepCore(element) ?? 1;
        var range = max - min;
        
        // Calculate how many steps from min to target
        var stepsToTarget = (int)Math.Round((value - min) / step);
        var totalSteps = (int)Math.Round(range / step);
        
        // Click on the element to focus it
        element.Click();
        WaitHelper.Pause(50);
        
        // Get current value
        var currentValue = GetValueCore(element) ?? min;
        
        // If we need to go to minimum first (more reliable)
        if (Math.Abs(value - min) < Math.Abs(value - currentValue))
        {
            // Send Home key to go to minimum
            element.SendKeys(OpenQA.Selenium.Keys.Home);
            WaitHelper.Pause(50);
            
            // Send right arrow keys to reach target
            for (int i = 0; i < stepsToTarget; i++)
            {
                element.SendKeys(OpenQA.Selenium.Keys.ArrowRight);
                WaitHelper.Pause(10);
            }
        }
        else if (Math.Abs(value - max) < Math.Abs(value - currentValue))
        {
            // Send End key to go to maximum
            element.SendKeys(OpenQA.Selenium.Keys.End);
            WaitHelper.Pause(50);
            
            // Calculate steps back from max
            var stepsFromMax = (int)Math.Round((max - value) / step);
            for (int i = 0; i < stepsFromMax; i++)
            {
                element.SendKeys(OpenQA.Selenium.Keys.ArrowLeft);
                WaitHelper.Pause(10);
            }
        }
        else
        {
            // Move from current position
            var stepsNeeded = (int)Math.Round((value - currentValue) / step);
            var key = stepsNeeded > 0 ? OpenQA.Selenium.Keys.ArrowRight : OpenQA.Selenium.Keys.ArrowLeft;
            
            for (int i = 0; i < Math.Abs(stepsNeeded); i++)
            {
                element.SendKeys(key);
                WaitHelper.Pause(10);
            }
        }
        
        // Poll until the value is near target or timeout
        _ = PollWithElement(
            element,
            e =>
            {
                var current = GetValueCore(e);
                if (current == null) return false;
                return Math.Abs(current.Value - value) <= Math.Max(step, 0.5);
            },
            1000);
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

        return RunSetWithElement(percentage, element =>
        {
            var min = GetMinimumCore(element) ?? 0;
            var max = GetMaximumCore(element) ?? 100;
            var value = min + ((max - min) * (percentage!.Value / 100.0));
            SetValueCore(element, value);
        }, timeoutMs);
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
        return RunDoWithElement( element =>
        {
            var min = GetMinimumCore(element) ?? 0;
            SetValueCore(element, min);
        }, timeoutMs);
    }

    /// <summary>
    /// Slides to the maximum value.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SlideToMaximum(int? timeoutMs = null)
    {
        return RunDoWithElement( element =>
        {
            var max = GetMaximumCore(element) ?? 100;
            SetValueCore(element, max);
        }, timeoutMs);
    }

    #endregion
}
