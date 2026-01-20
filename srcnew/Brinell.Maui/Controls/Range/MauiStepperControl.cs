namespace Brinell.Maui.Controls.Range;

/// <summary>
/// MAUI Stepper control with +/- buttons for discrete value changes.
/// Inherits GetValue, SetValue, GetMinimum, GetMaximum, Increment, Decrement from MauiRangeControlBase.
/// Provides additional stepper-specific methods like IncrementBy and DecrementBy.
/// Overrides Increment/Decrement to use child button clicks instead of SendKeys.
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
    
    #region Core Method Overrides
    
    /// <summary>
    /// Increments the stepper by clicking the increment button.
    /// Falls back to base implementation if buttons not found.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    protected override void IncrementCore(IMauiElement element)
    {
        // Try to find increment button child
        // MAUI Stepper on Windows has two RepeatButton children
        try
        {
            var incrementButton = FindChildButton(element, isIncrement: true);
            if (incrementButton != null)
            {
                incrementButton.Click();
                Thread.Sleep(20); // Brief pause between clicks
                return;
            }
        }
        catch
        {
            // Fall through to base implementation
        }
        
        base.IncrementCore(element);
    }
    
    /// <summary>
    /// Decrements the stepper by clicking the decrement button.
    /// Falls back to base implementation if buttons not found.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    protected override void DecrementCore(IMauiElement element)
    {
        try
        {
            var decrementButton = FindChildButton(element, isIncrement: false);
            if (decrementButton != null)
            {
                decrementButton.Click();
                Thread.Sleep(20); // Brief pause between clicks
                return;
            }
        }
        catch
        {
            // Fall through to base implementation
        }
        
        base.DecrementCore(element);
    }
    
    /// <summary>
    /// Sets value by repeatedly clicking increment/decrement buttons.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="value">The target value.</param>
    protected override void SetValueCore(IMauiElement element, double value)
    {
        var current = GetValueCore(element) ?? 0;
        var step = GetStepCore(element) ?? 1;
        var diff = value - current;
        var clicks = (int)Math.Abs(diff / step);
        
        // Limit to reasonable number of clicks
        clicks = Math.Min(clicks, 100);
        
        var increment = diff > 0;
        for (int i = 0; i < clicks; i++)
        {
            if (increment)
                IncrementCore(element);
            else
                DecrementCore(element);
        }
    }
    
    /// <summary>
    /// Finds increment or decrement button child element.
    /// </summary>
    /// <param name="parent">The parent stepper element.</param>
    /// <param name="isIncrement">True for increment button, false for decrement.</param>
    /// <returns>The button element, or null if not found.</returns>
    private IMauiElement? FindChildButton(IMauiElement parent, bool isIncrement)
    {
        // MAUI Stepper structure:
        // - RepeatButton (decrement, typically first or has "-" text)
        // - TextBlock (value display)
        // - RepeatButton (increment, typically last or has "+" text)
        
        try
        {
            // Use element's FindElements to search within the stepper
            var buttons = parent.FindElements(
                OpenQA.Selenium.By.ClassName("RepeatButton"));
            
            if (buttons.Count >= 2)
            {
                // Assume first is decrement, last is increment
                return isIncrement ? buttons[^1] : buttons[0];
            }
            
            // Try Button class name as alternative
            var altButtons = parent.FindElements(
                OpenQA.Selenium.By.ClassName("Button"));
            
            if (altButtons.Count >= 2)
            {
                return isIncrement ? altButtons[^1] : altButtons[0];
            }
        }
        catch
        {
            // Swallow - will fall back to base implementation
        }
        
        return null;
    }
    
    #endregion

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
