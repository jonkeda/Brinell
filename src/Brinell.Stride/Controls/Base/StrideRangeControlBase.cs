using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Core.Logging;
using Brinell.Stride.Communication;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls.Base;

/// <summary>
/// Base class for range controls (Slider, ProgressBar).
/// </summary>
public abstract class StrideRangeControlBase : StrideControlBase, IRangeControl
{
    /// <summary>
    /// Create a new range control.
    /// </summary>
    protected StrideRangeControlBase(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <inheritdoc />
    public double GetValue() => GetState().Value ?? 0;

    /// <inheritdoc />
    public double GetMinimum() => GetState().Minimum ?? 0;

    /// <inheritdoc />
    public double GetMaximum() => GetState().Maximum ?? 100;

    /// <inheritdoc />
    public virtual void SetValue(double value)
    {
        throw new NotSupportedException($"Control '{AutomationId}' does not support SetValue.");
    }

    /// <inheritdoc />
    public virtual void Increment()
    {
        throw new NotSupportedException($"Control '{AutomationId}' does not support Increment.");
    }

    /// <inheritdoc />
    public virtual void Decrement()
    {
        throw new NotSupportedException($"Control '{AutomationId}' does not support Decrement.");
    }

    /// <summary>
    /// Wait for specific value.
    /// </summary>
    public bool WaitValue(double expected, double tolerance = 0.01, int? timeoutMs = null)
    {
        return Context.WaitFor(
            () => Math.Abs(GetValue() - expected) <= tolerance,
            timeoutMs,
            $"element '{AutomationId}' value={expected}±{tolerance}");
    }

    /// <inheritdoc />
    public void AssertValue(double expected, double tolerance = 0.001, string? message = null)
    {
        var actual = GetValue();
        var inRange = Math.Abs(actual - expected) <= tolerance;
        LogAssertion("AssertValue", expected, actual);

        if (!inRange)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' value mismatch. Expected: {expected}±{tolerance}, Actual: {actual}");
        }
    }

    /// <summary>
    /// Assert value is less than.
    /// </summary>
    public void AssertValueLessThan(double expected, string? message = null)
    {
        var actual = GetValue();
        LogAssertion("AssertValueLessThan", $"< {expected}", actual);

        if (actual >= expected)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' value should be less than {expected}, but was {actual}");
        }
    }

    /// <summary>
    /// Assert value is greater than.
    /// </summary>
    public void AssertValueGreaterThan(double expected, string? message = null)
    {
        var actual = GetValue();
        LogAssertion("AssertValueGreaterThan", $"> {expected}", actual);

        if (actual <= expected)
        {
            throw new AssertionException(
                message ?? $"Control '{AutomationId}' value should be greater than {expected}, but was {actual}");
        }
    }
}
