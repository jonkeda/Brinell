using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// HTML/Selenium base class for range controls (input type="range", progress).
/// </summary>
public abstract class RangeControlBase : ControlBase, IRangeControl
{
    protected RangeControlBase(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected RangeControlBase(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected RangeControlBase(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current value.
    /// </summary>
    public virtual double GetValue()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var value = element.GetAttribute("value");
        return double.TryParse(value, out var result) ? result : 0;
    }

    /// <summary>
    /// Get the minimum value.
    /// </summary>
    public virtual double GetMinimum()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var min = element.GetAttribute("min");
        return double.TryParse(min, out var result) ? result : 0;
    }

    /// <summary>
    /// Get the maximum value.
    /// </summary>
    public virtual double GetMaximum()
    {
        var element = FindElement();
        if (element == null) return 100;
        
        var max = element.GetAttribute("max");
        return double.TryParse(max, out var result) ? result : 100;
    }

    /// <summary>
    /// Set the value.
    /// </summary>
    public virtual void SetValue(double value)
    {
        LogAction("SetValue", value.ToString());
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for value change.");
        
        // Use JavaScript to set the value
        _context.ExecuteScript($"arguments[0].value = {value}; arguments[0].dispatchEvent(new Event('input'));", element);
    }

    /// <summary>
    /// Increment the value.
    /// </summary>
    public virtual void Increment()
    {
        LogAction("Increment");
        var step = GetStep();
        var current = GetValue();
        var max = GetMaximum();
        SetValue(Math.Min(current + step, max));
    }

    /// <summary>
    /// Decrement the value.
    /// </summary>
    public virtual void Decrement()
    {
        LogAction("Decrement");
        var step = GetStep();
        var current = GetValue();
        var min = GetMinimum();
        SetValue(Math.Max(current - step, min));
    }

    /// <summary>
    /// Get the step value.
    /// </summary>
    public virtual double GetStep()
    {
        var element = FindElement();
        if (element == null) return 1;
        
        var step = element.GetAttribute("step");
        return double.TryParse(step, out var result) ? result : 1;
    }

    /// <summary>
    /// Get value as percentage (0-100).
    /// </summary>
    public virtual double GetPercentage()
    {
        var value = GetValue();
        var min = GetMinimum();
        var max = GetMaximum();
        
        if (max - min == 0) return 0;
        return (value - min) / (max - min) * 100;
    }

    /// <summary>
    /// Assert value equals expected (within tolerance).
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertValue(double expected, double tolerance = 0.001, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetValue();
        if (Math.Abs(actual - expected) > tolerance)
        {
            ThrowAssertionFailed("Value", actual.ToString(), expected.ToString(),
                message ?? $"Expected value '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("Value", actual.ToString(), expected.ToString());
    }
}
