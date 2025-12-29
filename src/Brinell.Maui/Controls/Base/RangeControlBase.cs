using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI base class for controls with numeric range values (Slider, ProgressBar).
/// </summary>
public abstract class RangeControlBase : ControlBase, IRangeControl
{
    protected RangeControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected RangeControlBase(AppiumTestContext context, IPageObject? page, AppiumElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected RangeControlBase(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current value.
    /// </summary>
    public abstract double GetValue();

    /// <summary>
    /// Get the minimum value.
    /// </summary>
    public abstract double GetMinimum();

    /// <summary>
    /// Get the maximum value.
    /// </summary>
    public abstract double GetMaximum();

    /// <summary>
    /// Set the value (for controls that support it).
    /// </summary>
    public virtual void SetValue(double value)
    {
        LogAction("SetValue", value.ToString());
        // Base implementation - can be overridden in derived classes
        throw new NotSupportedException($"SetValue not supported for {GetType().Name}");
    }

    /// <summary>
    /// Increment the value.
    /// </summary>
    public virtual void Increment()
    {
        LogAction("Increment");
        throw new NotSupportedException($"Increment not supported for {GetType().Name}");
    }

    /// <summary>
    /// Decrement the value.
    /// </summary>
    public virtual void Decrement()
    {
        LogAction("Decrement");
        throw new NotSupportedException($"Decrement not supported for {GetType().Name}");
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
