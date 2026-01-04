using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for range controls (Slider, Stepper).
/// Provides value and range operations.
/// </summary>
public abstract class RangeControlBase : ControlObjectBase, IRangeControlObject
{
    /// <summary>
    /// Creates a new range control.
    /// </summary>
    protected RangeControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new range control using AutomationId.
    /// </summary>
    protected RangeControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    #region Value

    /// <inheritdoc />
    public virtual double GetValue(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        // Try RangeValue.Value first (UIA pattern)
        var value = element.GetAttribute("RangeValue.Value");
        if (double.TryParse(value, out var v))
            return v;

        // Try Value attribute
        value = element.GetAttribute("Value");
        if (double.TryParse(value, out v))
            return v;

        return 0;
    }

    /// <inheritdoc />
    public virtual void SetValue(double? value, int? timeoutMs = null)
    {
        if (value is null) return;

        Log($"SetValue({value})");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        PerformSetValue(value.Value, timeoutMs);
    }

    /// <summary>
    /// Performs the set value action. Override for control-specific behavior.
    /// </summary>
    protected virtual void PerformSetValue(double value, int? timeoutMs = null)
    {
        // Validate value is in range
        var (min, max) = GetRange(timeoutMs);
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} out of range ({min}-{max})");

        // Default implementation using percentage-based interaction
        var percent = (value - min) / (max - min);
        SetValuePercent(percent, timeoutMs);
    }

    /// <inheritdoc />
    public virtual bool WaitValue(double? expected, double tolerance = 0.01, int? timeoutMs = null)
    {
        if (expected is null) return true;

        return WaitForBool(
            () => Math.Abs(GetValue() - expected.Value) <= tolerance,
            true,
            timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public virtual void AssertValue(double? expected, double tolerance = 0.01, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetValue(timeoutMs);
        if (Math.Abs(actual - expected.Value) > tolerance)
        {
            throw new AssertionException(
                message ?? $"Expected value {expected} (±{tolerance}), but was {actual}",
                Locator.Value,
                "AssertValue");
        }
    }

    #endregion

    #region Range

    /// <inheritdoc />
    public virtual double GetMinimum(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        var min = element.GetAttribute("RangeValue.Minimum");
        if (double.TryParse(min, out var v))
            return v;

        min = element.GetAttribute("Minimum");
        if (double.TryParse(min, out v))
            return v;

        return 0;
    }

    /// <inheritdoc />
    public virtual double GetMaximum(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        var max = element.GetAttribute("RangeValue.Maximum");
        if (double.TryParse(max, out var v))
            return v;

        max = element.GetAttribute("Maximum");
        if (double.TryParse(max, out v))
            return v;

        return 100;
    }

    /// <inheritdoc />
    public virtual (double minimum, double maximum) GetRange(int? timeoutMs = null)
    {
        return (GetMinimum(timeoutMs), GetMaximum(timeoutMs));
    }

    /// <inheritdoc />
    public virtual double GetValuePercent(int? timeoutMs = null)
    {
        var value = GetValue(timeoutMs);
        var (min, max) = GetRange(timeoutMs);
        
        if (Math.Abs(max - min) < 0.0001)
            return 0;

        return (value - min) / (max - min);
    }

    /// <inheritdoc />
    public virtual void SetValuePercent(double? percent, int? timeoutMs = null)
    {
        if (percent is null) return;

        Log($"SetValuePercent({percent})");

        var p = Math.Clamp(percent.Value, 0, 1);
        var (min, max) = GetRange(timeoutMs);
        var value = min + (max - min) * p;

        PerformSetValue(value, timeoutMs);
    }

    #endregion

    #region Step Actions

    /// <inheritdoc />
    public virtual void Increase(int? timeoutMs = null)
    {
        Log("Increase()");
        PerformIncrease(timeoutMs);
    }

    /// <summary>
    /// Performs the increase action. Override for control-specific behavior.
    /// </summary>
    protected virtual void PerformIncrease(int? timeoutMs = null)
    {
        // Default: increment by small amount
        var current = GetValue(timeoutMs);
        var (min, max) = GetRange(timeoutMs);
        var step = (max - min) / 10;
        SetValue(Math.Min(current + step, max), timeoutMs);
    }

    /// <inheritdoc />
    public virtual void Decrease(int? timeoutMs = null)
    {
        Log("Decrease()");
        PerformDecrease(timeoutMs);
    }

    /// <summary>
    /// Performs the decrease action. Override for control-specific behavior.
    /// </summary>
    protected virtual void PerformDecrease(int? timeoutMs = null)
    {
        // Default: decrement by small amount
        var current = GetValue(timeoutMs);
        var (min, max) = GetRange(timeoutMs);
        var step = (max - min) / 10;
        SetValue(Math.Max(current - step, min), timeoutMs);
    }

    /// <inheritdoc />
    public virtual void SetToMinimum(int? timeoutMs = null)
    {
        Log("SetToMinimum()");
        var min = GetMinimum(timeoutMs);
        SetValue(min, timeoutMs);
    }

    /// <inheritdoc />
    public virtual void SetToMaximum(int? timeoutMs = null)
    {
        Log("SetToMaximum()");
        var max = GetMaximum(timeoutMs);
        SetValue(max, timeoutMs);
    }

    #endregion
}
