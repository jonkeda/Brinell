namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for MAUI controls with range/numeric capability.
/// Implements IRangeControlObject with GetValue, SetValue, Increment, Decrement.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class RangeControlBase<TScope> : FocusableControlBase<TScope>,
    IRangeControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new range control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    protected RangeControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new range control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    protected RangeControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Gets value from pre-found element.
    /// Uses RangeValue pattern when available, otherwise reads from attributes.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The current value, or null if element is null.</returns>
    protected virtual double? GetValueCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try RangeValue pattern first (Windows/FlaUI)
        if (element is IRangePatternElement rangeElement && rangeElement.SupportsRangeValue)
        {
            var value = rangeElement.GetRangeValue();
            if (value.HasValue)
                return value.Value;
        }

        // Try RangeValue.Value attribute first (Windows/MAUI)
        var rangeValue = element.GetAttribute("RangeValue.Value");
        if (!string.IsNullOrEmpty(rangeValue) && double.TryParse(rangeValue, out var rv))
        {
            return rv;
        }

        // Try Value attribute
        var valueAttr = element.GetAttribute("Value");
        if (!string.IsNullOrEmpty(valueAttr) && double.TryParse(valueAttr, out var v))
        {
            return v;
        }

        // Try text content as fallback
        var text = element.Text;
        if (!string.IsNullOrEmpty(text) && double.TryParse(text, out var t))
        {
            return t;
        }

        return null;
    }

    /// <summary>
    /// Sets value on pre-found element.
    /// Uses FlaUI RangeValue.SetValue pattern when available, otherwise falls back to SendKeys.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="value">The value to set. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetValueCore(IMauiElement element, double? value, int? timeoutMs = null)
    {
        if (value == null) return;

        EnsureSettableCore(element);

        // Try RangeValue pattern first (Windows/FlaUI)
        if (element is IRangePatternElement rangeElement && rangeElement.SupportsRangeValue)
        {
            if (rangeElement.SetRangeValue(value.Value))
                return;
        }

        // Default implementation: try to use SendKeys
        // Override in derived classes for slider-specific drag behavior
        element.Clear();
        element.SendKeys(value.Value.ToString());
    }

    /// <summary>
    /// Gets minimum value from pre-found element.
    /// Uses FlaUI RangeValue pattern when available.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The minimum value, or null if not available.</returns>
    protected virtual double? GetMinimumCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try RangeValue pattern first (Windows/FlaUI)
        if (element is IRangePatternElement rangeElement && rangeElement.SupportsRangeValue)
        {
            var min = rangeElement.GetRangeMinimum();
            if (min.HasValue)
                return min.Value;
        }

        var minAttr = element.GetAttribute("RangeValue.Minimum");
        if (!string.IsNullOrEmpty(minAttr) && double.TryParse(minAttr, out var min2))
        {
            return min2;
        }

        var minAttr2 = element.GetAttribute("Minimum");
        if (!string.IsNullOrEmpty(minAttr2) && double.TryParse(minAttr2, out var min3))
        {
            return min3;
        }

        return null;
    }

    /// <summary>
    /// Gets maximum value from pre-found element.
    /// Uses FlaUI RangeValue pattern when available.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The maximum value, or null if not available.</returns>
    protected virtual double? GetMaximumCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try RangeValue pattern first (Windows/FlaUI)
        if (element is IRangePatternElement rangeElement && rangeElement.SupportsRangeValue)
        {
            var max = rangeElement.GetRangeMaximum();
            if (max.HasValue)
                return max.Value;
        }

        var maxAttr = element.GetAttribute("RangeValue.Maximum");
        if (!string.IsNullOrEmpty(maxAttr) && double.TryParse(maxAttr, out var max2))
        {
            return max2;
        }

        var maxAttr2 = element.GetAttribute("Maximum");
        if (!string.IsNullOrEmpty(maxAttr2) && double.TryParse(maxAttr2, out var max3))
        {
            return max3;
        }

        return null;
    }

    /// <summary>
    /// Gets step value from pre-found element.
    /// Uses RangeValue pattern when available.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>The step value, or null if not available.</returns>
    protected virtual double? GetStepCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try RangeValue pattern first (Windows/FlaUI)
        if (element is IRangePatternElement rangeElement && rangeElement.SupportsRangeValue)
        {
            var step = rangeElement.GetRangeSmallChange();
            if (step.HasValue)
                return step.Value;
        }

        var stepAttr = element.GetAttribute("RangeValue.SmallChange");
        if (!string.IsNullOrEmpty(stepAttr) && double.TryParse(stepAttr, out var step2))
        {
            return step2;
        }

        var stepAttr2 = element.GetAttribute("Step");
        if (!string.IsNullOrEmpty(stepAttr2) && double.TryParse(stepAttr2, out var step3))
        {
            return step3;
        }

        return 1.0; // Default step
    }

    /// <summary>
    /// Increments value by step amount.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void IncrementCore(IMauiElement element, int? timeoutMs = null)
    {
        var current = GetValueCore(element) ?? 0;
        var step = GetStepCore(element) ?? 1;
        var max = GetMaximumCore(element);

        var newValue = current + step;
        if (max.HasValue && newValue > max.Value)
        {
            newValue = max.Value;
        }

        SetValueCore(element, newValue, timeoutMs);
    }

    /// <summary>
    /// Decrements value by step amount.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void DecrementCore(IMauiElement element, int? timeoutMs = null)
    {
        var current = GetValueCore(element) ?? 0;
        var step = GetStepCore(element) ?? 1;
        var min = GetMinimumCore(element);

        var newValue = current - step;
        if (min.HasValue && newValue < min.Value)
        {
            newValue = min.Value;
        }

        SetValueCore(element, newValue, timeoutMs);
    }

    #endregion

    #region Guards

    /// <summary>
    /// Throws when the element cannot accept a new value.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void EnsureSettableCore(IMauiElement element)
    {
        if (IsEnabledCore(element) != true)
        {
            throw new TimeoutException(
                $"Element was not enabled. Locator: {Locator}");
        }
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Waits until the value matches expected within the given tolerance.
    /// Hand-written: the generated WaitValue compares with equality only.
    /// </summary>
    /// <param name="expected">The expected value. Null skips the wait.</param>
    /// <param name="tolerance">The allowed absolute difference.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True when the value matched within the timeout.</returns>
    public bool WaitValueWithin(double? expected, double tolerance, int? timeoutMs = null)
    {
        return RunWaitWithElement(expected,
            element =>
            {
                var actual = GetValueCore(element);
                return actual != null && Math.Abs(actual.Value - expected!.Value) <= tolerance;
            },
            timeoutMs);
    }

    /// <summary>
    /// Asserts the value matches expected within the given tolerance.
    /// Hand-written: the generated AssertValue compares with equality only.
    /// </summary>
    /// <param name="expected">The expected value. Null skips the assertion.</param>
    /// <param name="tolerance">The allowed absolute difference.</param>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertValueWithin(double? expected, double tolerance, string? message = null,
        int? timeoutMs = null)
    {
        return RunAssertWithElement(expected,
            GetValueCore,
            (actual, expected1) =>
            {
                if (actual == null || expected1 == null)
                    return actual == expected1;
                return Math.Abs(actual.Value - expected1.Value) <= tolerance;
            },
            message ?? $"Expected Value to be '{expected}' (±{tolerance}). Locator: {Locator}",
            timeoutMs);
    }

    #endregion
}
