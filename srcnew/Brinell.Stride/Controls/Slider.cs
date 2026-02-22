using Brinell.Core.Utilities;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Slider control for Stride UI.
/// </summary>
public class Slider<TScope> : RangeControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public Slider(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    public override TScope SetValue(double value)
    {
        var min = GetMinimum();
        var max = GetMaximum();

        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} is outside range [{min}, {max}]");

        var success = Context.SetSliderValue(AutomationId, value);

        if (!success)
            throw new InvalidOperationException($"Server-side SetSliderValue failed for '{AutomationId}'");

        // Wait for the value to actually change
        WaitValue(value, tolerance: 0.5, timeoutMs: 500);

        return ContainingScope;
    }

    public override TScope Increment()
    {
        var current = GetValue();
        var min = GetMinimum();
        var max = GetMaximum();
        var step = (max - min) / 10.0;

        var newValue = Math.Min(current + step, max);
        if (Math.Abs(newValue - current) > double.Epsilon)
            SetValue(newValue);

        return ContainingScope;
    }

    public override TScope Decrement()
    {
        var current = GetValue();
        var min = GetMinimum();
        var max = GetMaximum();
        var step = (max - min) / 10.0;

        var newValue = Math.Max(current - step, min);
        if (Math.Abs(newValue - current) > double.Epsilon)
            SetValue(newValue);

        return ContainingScope;
    }

    /// <summary>
    /// Focus the slider control by clicking on it.
    /// </summary>
    public TScope Focus()
    {
        Context.ClickElement(AutomationId);
        return ContainingScope;
    }
}
