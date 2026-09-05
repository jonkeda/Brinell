using Brinell.Core.Locators;
using Brinell.Core.Utilities;

namespace Brinell.Maui.Controls.Range;

/// <summary>
/// MAUI Stepper control with +/- buttons for discrete value changes.
/// Inherits GetValue, SetValue, GetMinimum, GetMaximum, Increment, Decrement from RangeControlBase.
/// Provides additional stepper-specific methods like IncrementBy and DecrementBy.
/// Overrides Increment/Decrement to use child button clicks instead of SendKeys.
/// 
/// Windows MAUI Note: On Windows, MAUI Stepper doesn't expose a single element with the AutomationId.
/// Instead, it exposes separate button elements with "{AutomationId}Minus" and "{AutomationId}Plus".
/// This control automatically handles this by falling back to the button-based approach.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Stepper<TScope> : Base.RangeControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string? _baseAutomationId;
    private IMauiElement? _minusButton;
    private IMauiElement? _plusButton;
    private IMauiElement? _valueLabelElement;
    private bool _usingButtonMode;
    
    /// <summary>
    /// Creates a new stepper control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the stepper element.</param>
    public Stepper(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
        // Extract base automation ID for button mode fallback
        if (locator.Strategy == LocatorStrategy.AutomationId)
        {
            _baseAutomationId = locator.Value;
        }
    }

    /// <summary>
    /// Creates a new stepper control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public Stepper(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
        // Store base automation ID for button mode fallback
        _baseAutomationId = locatorValue;
    }
    
    #region Button Mode for Windows
    
    /// <summary>
    /// Tries to find the stepper element. On Windows, falls back to button-based mode
    /// where the minus button serves as the proxy element.
    /// </summary>
    protected override IMauiElement? TryFindElement()
    {
        // First try the standard approach (works on Android/iOS)
        var element = base.TryFindElement();
        if (element != null)
        {
            _usingButtonMode = false;
            return element;
        }
        
        // Fall back to Windows button mode
        if (!string.IsNullOrEmpty(_baseAutomationId))
        {
            var minusLocator = new Locator(LocatorStrategy.AutomationId, $"{_baseAutomationId}Minus");
            _minusButton = MauiScope.TryFindElement(minusLocator);
            
            if (_minusButton != null)
            {
                var plusLocator = new Locator(LocatorStrategy.AutomationId, $"{_baseAutomationId}Plus");
                _plusButton = MauiScope.TryFindElement(plusLocator);
                
                _usingButtonMode = true;
                // Return minus button as proxy element (for existence checks)
                return _minusButton;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Tries to find the value label element (used on Windows for reading the value).
    /// </summary>
    private IMauiElement? TryFindValueLabel()
    {
        if (_valueLabelElement != null)
            return _valueLabelElement;
            
        // On Windows, the value is typically in a label like "{BaseId}Label" or near the stepper
        // Common patterns: "QuantityLabel", "Quantity: 1"
        if (!string.IsNullOrEmpty(_baseAutomationId))
        {
            // Try common label naming patterns
            var labelPatterns = new[]
            {
                $"{_baseAutomationId.Replace("Stepper", "")}Label",  // QuantityStepper -> QuantityLabel
                $"{_baseAutomationId}Value",
                $"{_baseAutomationId}Label"
            };
            
            foreach (var pattern in labelPatterns)
            {
                var locator = new Locator(LocatorStrategy.AutomationId, pattern);
                _valueLabelElement = MauiScope.TryFindElement(locator);
                if (_valueLabelElement != null)
                    return _valueLabelElement;
            }
        }
        
        return null;
    }
    
    #endregion
    
    #region Core Method Overrides
    
    /// <summary>
    /// Increments the stepper by clicking the increment button.
    /// Uses button mode on Windows where buttons are exposed separately.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected override void IncrementCore(IMauiElement element, int? timeoutMs = null)
    {
        // In Windows button mode, use the cached plus button
        if (_usingButtonMode && _plusButton != null)
        {
            _plusButton.Click();
            return;
        }
        
        // Try to find increment button as child
        var incrementButton = FindChildButton(element, isIncrement: true);
        if (incrementButton != null)
        {
            incrementButton.Click();
            return;
        }
        
        base.IncrementCore(element, timeoutMs);
    }
    
    /// <summary>
    /// Decrements the stepper by clicking the decrement button.
    /// Uses button mode on Windows where buttons are exposed separately.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected override void DecrementCore(IMauiElement element, int? timeoutMs = null)
    {
        // In Windows button mode, use the cached minus button
        if (_usingButtonMode && _minusButton != null)
        {
            _minusButton.Click();
            return;
        }
        
        // Try to find decrement button as child
        var decrementButton = FindChildButton(element, isIncrement: false);
        if (decrementButton != null)
        {
            decrementButton.Click();
            return;
        }
        
        base.DecrementCore(element, timeoutMs);
    }
    
    /// <summary>
    /// Gets the current value. In button mode, reads from the value label.
    /// </summary>
    /// <param name="element">The stepper element (or proxy button in button mode).</param>
    /// <returns>The current value, or null if not available.</returns>
    protected override double? GetValueCore(IMauiElement? element)
    {
        // In button mode, read from the value label
        if (_usingButtonMode)
        {
            var label = TryFindValueLabel();
            if (label != null)
            {
                var text = label.Text ?? label.GetAttribute("Name");
                if (!string.IsNullOrEmpty(text))
                {
                    // Parse value from text like "Quantity: 5" or just "5"
                    var numericPart = ExtractNumericValue(text);
                    if (numericPart.HasValue)
                        return numericPart.Value;
                }
            }
            // If no label found, we can't determine the value
            return null;
        }
        
        // Standard RangeValue pattern
        return base.GetValueCore(element);
    }
    
    /// <summary>
    /// Extracts numeric value from text like "Quantity: 5" or "5".
    /// </summary>
    private static double? ExtractNumericValue(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;
            
        // Try to find a number in the text
        var matches = System.Text.RegularExpressions.Regex.Matches(text, @"-?\d+\.?\d*");
        if (matches.Count > 0)
        {
            // Take the last number (typically the value)
            var lastMatch = matches[^1];
            if (double.TryParse(lastMatch.Value, out var value))
                return value;
        }
        
        return null;
    }
    
    /// <summary>
    /// Sets value by repeatedly clicking increment/decrement buttons.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="value">The target value. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected override void SetValueCore(IMauiElement element, double? value, int? timeoutMs = null)
    {
        if (value == null) return;

        EnsureSettableCore(element);

        var target = value.Value;
        var current = GetValueCore(element) ?? 0;
        var step = GetStepCore(element) ?? 1;
        var diff = target - current;
        var clicks = (int)Math.Abs(diff / step);
        
        // Limit to prevent infinite loops while still allowing full stepper ranges
        clicks = Math.Min(clicks, 200);
        
        var increment = diff > 0;
        for (int i = 0; i < clicks; i++)
        {
            if (increment)
                IncrementCore(element, timeoutMs);
            else
                DecrementCore(element, timeoutMs);
        }
        
        // In button mode, poll until the label reflects the target value
        if (_usingButtonMode)
        {
            WaitHelper.WaitFor(
                () => GetValueCore(element),
                v => v.HasValue && Math.Abs(v.Value - target) < 0.01,
                timeoutMs: 1000,
                pollingIntervalMs: 50);
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
        
        var buttons = parent.FindElements(
            Locator.ByClassName("RepeatButton"));
        
        if (buttons.Count >= 2)
        {
            // First is decrement, last is increment
            return isIncrement ? buttons[^1] : buttons[0];
        }
        
        // Try Button class name as alternative
        var altButtons = parent.FindElements(
            Locator.ByClassName("Button"));
        
        if (altButtons.Count >= 2)
        {
            return isIncrement ? altButtons[^1] : altButtons[0];
        }
        
        return null;
    }
    
    #endregion

    #region Stepper-Specific Core Methods

    /// <summary>
    /// Reads the value and polls until two consecutive reads return the same result,
    /// ensuring data binding has propagated after recent button clicks.
    /// </summary>
    private double? GetStableValue(IMauiElement element)
    {
        var value = GetValueCore(element);
        if (value == null) return null;
        
        // Poll until value stabilizes (two consecutive reads match)
        WaitHelper.WaitFor(
            () =>
            {
                var next = GetValueCore(element);
                if (next.HasValue && Math.Abs(next.Value - value.Value) < 0.01)
                    return true;
                value = next;
                return false;
            },
            timeoutMs: 500,
            pollingIntervalMs: 50);
        
        return value;
    }

    /// <summary>
    /// Increments the stepper value multiple times.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="times">Number of times to increment. Null or non-positive skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout for each increment.</param>
    protected virtual void IncrementByCore(IMauiElement element, int? times, int? timeoutMs = null)
    {
        if (times == null || times <= 0) return;

        for (int i = 0; i < times; i++)
        {
            IncrementCore(element, timeoutMs);
        }
    }

    /// <summary>
    /// Decrements the stepper value multiple times.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="times">Number of times to decrement. Null or non-positive skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout for each decrement.</param>
    protected virtual void DecrementByCore(IMauiElement element, int? times, int? timeoutMs = null)
    {
        if (times == null || times <= 0) return;

        for (int i = 0; i < times; i++)
        {
            DecrementCore(element, timeoutMs);
        }
    }

    /// <summary>
    /// Sets the stepper to its minimum value.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetToMinimumCore(IMauiElement element, int? timeoutMs = null)
    {
        var min = GetMinimumCore(element) ?? 0;
        SetValueCore(element, min, timeoutMs);
    }

    /// <summary>
    /// Sets the stepper to its maximum value.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetToMaximumCore(IMauiElement element, int? timeoutMs = null)
    {
        var max = GetMaximumCore(element) ?? 100;
        SetValueCore(element, max, timeoutMs);
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Checks if the stepper can be incremented (not at maximum).
    /// Hand-written: the generator always names its state trio <c>Is{Name}</c>, so it
    /// cannot emit a member called <c>CanIncrement</c>.
    /// </summary>
    /// <returns>True if increment is possible, false otherwise.</returns>
    public bool? CanIncrement()
    {
        var element = TryFindElement();
        if (element == null) return null;

        // In button mode, try incrementing and check if value actually changed
        if (_usingButtonMode && _plusButton != null)
        {
            // Read current stable value
            var before = GetStableValue(element);
            if (before == null) return null;

            _plusButton.Click();

            // Poll to see if value changed
            var changed = WaitHelper.WaitFor(
                () => GetValueCore(element),
                v => v.HasValue && Math.Abs(v.Value - before.Value) > 0.01,
                timeoutMs: 500,
                pollingIntervalMs: 50);

            if (changed)
            {
                // Value increased — undo the increment
                _minusButton?.Click();
                // Wait for undo to apply
                WaitHelper.WaitFor(
                    () => GetValueCore(element),
                    v => v.HasValue && Math.Abs(v.Value - before.Value) < 0.01,
                    timeoutMs: 500,
                    pollingIntervalMs: 50);
                return true;
            }

            return false; // Value didn't change — at maximum
        }

        var current = GetValueCore(element);
        var max = GetMaximumCore(element);

        if (current == null || max == null) return true;
        return current.Value < max.Value;
    }

    /// <summary>
    /// Checks if the stepper can be decremented (not at minimum).
    /// Hand-written: the generator always names its state trio <c>Is{Name}</c>, so it
    /// cannot emit a member called <c>CanDecrement</c>.
    /// </summary>
    /// <returns>True if decrement is possible, false otherwise.</returns>
    public bool? CanDecrement()
    {
        var element = TryFindElement();
        if (element == null) return null;

        // In button mode, try decrementing and check if value actually changed
        if (_usingButtonMode && _minusButton != null)
        {
            // Read current stable value
            var before = GetStableValue(element);
            if (before == null) return null;

            _minusButton.Click();

            // Poll to see if value changed
            var changed = WaitHelper.WaitFor(
                () => GetValueCore(element),
                v => v.HasValue && Math.Abs(v.Value - before.Value) > 0.01,
                timeoutMs: 500,
                pollingIntervalMs: 50);

            if (changed)
            {
                // Value decreased — undo the decrement
                _plusButton?.Click();
                // Wait for undo to apply
                WaitHelper.WaitFor(
                    () => GetValueCore(element),
                    v => v.HasValue && Math.Abs(v.Value - before.Value) < 0.01,
                    timeoutMs: 500,
                    pollingIntervalMs: 50);
                return true;
            }

            return false; // Value didn't change — at minimum
        }

        var current = GetValueCore(element);
        var min = GetMinimumCore(element);

        if (current == null || min == null) return true;
        return current.Value > min.Value;
    }

    #endregion
}
