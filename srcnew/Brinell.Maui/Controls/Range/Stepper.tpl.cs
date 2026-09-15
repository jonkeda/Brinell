using Brinell.Core.Locators;
using Brinell.Core.Utilities;
using System.Globalization;

namespace Brinell.Maui.Controls.Range;

/// <summary>
/// MAUI Stepper control with +/- buttons for discrete value changes.
/// Inherits GetValue, SetValue, GetMinimum, GetMaximum, Increment, Decrement from RangeControlBase.
/// Provides additional stepper-specific methods like IncrementBy and DecrementBy.
/// Overrides Increment/Decrement to invoke the stepper's own buttons instead of sending keys.
///
/// Windows MAUI Note: On Windows, MAUI Stepper doesn't expose a single element with the AutomationId.
/// Instead, it exposes separate button elements with "{AutomationId}Minus" and "{AutomationId}Plus".
/// This control resolves through that pair, presses them through the Invoke pattern, and reads
/// Value, Minimum, Maximum and Increment from the app with the bridge's GetState verb, which the
/// Stepper must declare.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Stepper<TScope> : Base.RangeControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private readonly string? _baseAutomationId;

    private sealed record StepperParts(
        IMauiElement Proxy,
        IMauiElement? Native,
        IMauiElement? Minus,
        IMauiElement? Plus)
    {
        public bool IsButtonMode => Native is null;
    }
    
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
    /// Resolves the native Stepper or its two Windows buttons.
    /// </summary>
    private StepperParts? ResolveParts()
    {
        var element = base.TryFindElement();
        if (element != null)
        {
            return new StepperParts(element, element, null, null);
        }

        if (string.IsNullOrEmpty(_baseAutomationId))
        {
            return null;
        }

        var minus = MauiScope.TryFindElement(
            new Locator(LocatorStrategy.AutomationId, $"{_baseAutomationId}Minus"));
        var plus = MauiScope.TryFindElement(
            new Locator(LocatorStrategy.AutomationId, $"{_baseAutomationId}Plus"));

        return minus is not null && plus is not null
            ? new StepperParts(minus, null, minus, plus)
            : null;
    }

    /// <summary>
    /// Tries to find the stepper element. On Windows, the minus button is its proxy.
    /// </summary>
    protected override IMauiElement? TryFindElement()
    {
        return ResolveParts()?.Proxy;
    }

    /// <summary>
    /// Finds the stepper element through the same native-or-buttons resolution as optional reads.
    /// </summary>
    protected override IMauiElement FindElement()
    {
        var parts = ResolveParts();
        if (parts is not null)
        {
            return parts.Proxy;
        }

        throw new ElementNotFoundException(
            $"Stepper was not found by '{Locator}', '{_baseAutomationId}Minus', or "
            + $"'{_baseAutomationId}Plus'.");
    }
    
    private bool SupportsStateReads()
    {
        return !string.IsNullOrEmpty(_baseAutomationId)
            && Context.Driver.SupportsStateReads(_baseAutomationId);
    }

    private double? ReadNumericState(string property)
    {
        if (!SupportsStateReads())
        {
            return null;
        }

        var value = Context.Driver.ReadState(_baseAutomationId!, property);
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : throw new FormatException(
                $"Stepper '{_baseAutomationId}' returned '{value}' for {property}.");
    }
    
    #endregion
    
    #region Core Method Overrides
    
    /// <summary>
    /// Increments the stepper by invoking its increment button.
    /// Uses button mode on Windows where buttons are exposed separately.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected override void IncrementCore(IMauiElement element, int? timeoutMs = null)
    {
        var parts = ResolveParts();
        if (parts?.IsButtonMode == true)
        {
            parts.Plus!.Invoke();
            return;
        }
        
        // Try to find increment button as child
        var incrementButton = FindChildButton(element, isIncrement: true);
        if (incrementButton != null)
        {
            incrementButton.Invoke();
            return;
        }
        
        base.IncrementCore(element, timeoutMs);
    }
    
    /// <summary>
    /// Decrements the stepper by invoking its decrement button.
    /// Uses button mode on Windows where buttons are exposed separately.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected override void DecrementCore(IMauiElement element, int? timeoutMs = null)
    {
        var parts = ResolveParts();
        if (parts?.IsButtonMode == true)
        {
            parts.Minus!.Invoke();
            return;
        }
        
        // Try to find decrement button as child
        var decrementButton = FindChildButton(element, isIncrement: false);
        if (decrementButton != null)
        {
            decrementButton.Invoke();
            return;
        }
        
        base.DecrementCore(element, timeoutMs);
    }
    
    /// <summary>
    /// Gets the current value: from the app's GetState when the Stepper declares it, otherwise
    /// from the RangeValue pattern.
    /// </summary>
    /// <param name="element">The stepper element (or proxy button in button mode).</param>
    /// <returns>The current value, or null if not available.</returns>
    protected override double? GetValueCore(IMauiElement? element)
    {
        if (SupportsStateReads())
        {
            return ReadNumericState("Value");
        }

        return base.GetValueCore(element);
    }

    /// <inheritdoc />
    protected override double? GetMinimumCore(IMauiElement? element)
    {
        if (SupportsStateReads())
        {
            return ReadNumericState("Minimum");
        }

        return base.GetMinimumCore(element);
    }

    /// <inheritdoc />
    protected override double? GetMaximumCore(IMauiElement? element)
    {
        if (SupportsStateReads())
        {
            return ReadNumericState("Maximum");
        }

        return base.GetMaximumCore(element);
    }

    /// <inheritdoc />
    protected override double? GetStepCore(IMauiElement? element)
    {
        if (SupportsStateReads())
        {
            return ReadNumericState("Increment");
        }

        return base.GetStepCore(element);
    }
    
    /// <summary>
    /// Sets value by clamping it to the range, invoking increment or decrement the needed number
    /// of times, and waiting until the read value reaches the target.
    /// </summary>
    /// <param name="element">The pre-found stepper element.</param>
    /// <param name="value">The target value. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected override void SetValueCore(IMauiElement element, double? value, int? timeoutMs = null)
    {
        if (value == null) return;

        EnsureSettableCore(element);

        var minimum = GetMinimumCore(element) ?? double.NegativeInfinity;
        var maximum = GetMaximumCore(element) ?? double.PositiveInfinity;
        var target = Math.Clamp(value.Value, minimum, maximum);
        var current = GetValueCore(element)
            ?? throw new NotSupportedException(
                $"Stepper '{_baseAutomationId ?? Locator.Value}' does not expose its current value.");
        var step = GetStepCore(element)
            ?? throw new NotSupportedException(
                $"Stepper '{_baseAutomationId ?? Locator.Value}' does not expose its increment.");
        if (step <= 0)
        {
            throw new InvalidOperationException(
                $"Stepper '{_baseAutomationId ?? Locator.Value}' reported a non-positive increment: {step}.");
        }

        var diff = target - current;
        var presses = (int)Math.Round(Math.Abs(diff / step), MidpointRounding.AwayFromZero);
        
        var increment = diff > 0;
        for (int i = 0; i < presses; i++)
        {
            if (increment)
                IncrementCore(element, timeoutMs);
            else
                DecrementCore(element, timeoutMs);
        }

        if (!WaitHelper.WaitFor(
                () => GetValueCore(element),
                actual => actual.HasValue && Math.Abs(actual.Value - target) < 0.01,
                timeoutMs: timeoutMs ?? DefaultTimeoutMs,
                pollingIntervalMs: PollingIntervalMs))
        {
            throw new TimeoutException(
                $"Stepper '{_baseAutomationId ?? Locator.Value}' did not reach {target}.");
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

        var current = GetValueCore(element);
        var max = GetMaximumCore(element);

        if (current == null || max == null) return null;
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

        var current = GetValueCore(element);
        var min = GetMinimumCore(element);

        if (current == null || min == null) return null;
        return current.Value > min.Value;
    }

    #endregion
}
