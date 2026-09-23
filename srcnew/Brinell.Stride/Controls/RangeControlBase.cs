using Brinell.Core.Exceptions;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Base class for range controls (Slider, ProgressBar).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract class RangeControlBase<TScope> : ControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    protected RangeControlBase(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    public double GetValue() => GetState().Value ?? 0;
    public double GetMinimum() => GetState().Minimum ?? 0;
    public double GetMaximum() => GetState().Maximum ?? 100;

    public virtual TScope SetValue(double value)
    {
        throw new NotSupportedException($"Control '{AutomationId}' does not support SetValue.");
    }

    public virtual TScope Increment()
    {
        throw new NotSupportedException($"Control '{AutomationId}' does not support Increment.");
    }

    public virtual TScope Decrement()
    {
        throw new NotSupportedException($"Control '{AutomationId}' does not support Decrement.");
    }

    public bool WaitValue(double expected, double tolerance = 0.01, int? timeoutMs = null)
    {
        return Poll(
            () => Math.Abs(GetValue() - expected) <= tolerance,
            timeoutMs ?? Context.Timeouts.DefaultWait);
    }

    public TScope AssertValue(double expected, double tolerance = 0.001, string? message = null)
    {
        var actual = GetValue();
        if (Math.Abs(actual - expected) > tolerance)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' value mismatch. Expected: {expected}±{tolerance}, Actual: {actual}");
        }
        return ContainingScope;
    }

    public TScope AssertValueLessThan(double expected, string? message = null)
    {
        var actual = GetValue();
        if (actual >= expected)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' value should be less than {expected}, but was {actual}");
        }
        return ContainingScope;
    }

    public TScope AssertValueGreaterThan(double expected, string? message = null)
    {
        var actual = GetValue();
        if (actual <= expected)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' value should be greater than {expected}, but was {actual}");
        }
        return ContainingScope;
    }
}
