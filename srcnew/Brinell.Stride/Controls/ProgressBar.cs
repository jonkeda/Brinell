using Brinell.Core.Exceptions;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Progress bar control for Stride UI (read-only range).
/// </summary>
public class ProgressBar<TScope> : RangeControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public ProgressBar(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    /// <summary>
    /// Get progress percentage (0-100).
    /// </summary>
    public double GetPercentage()
    {
        var min = GetMinimum();
        var max = GetMaximum();
        var value = GetValue();

        if (Math.Abs(max - min) < double.Epsilon)
            return 0;

        return (value - min) / (max - min) * 100;
    }

    /// <summary>
    /// Check if progress is complete.
    /// </summary>
    public bool IsComplete() => Math.Abs(GetValue() - GetMaximum()) < 0.01;

    /// <summary>
    /// Wait for completion.
    /// </summary>
    public bool WaitComplete(int? timeoutMs = null)
    {
        return Poll(() => IsComplete(), timeoutMs ?? Context.Timeouts.DefaultWait);
    }

    /// <summary>
    /// Assert completion.
    /// </summary>
    public TScope AssertComplete(string? message = null)
    {
        if (!IsComplete())
        {
            throw new AssertionException(
                message ?? $"ProgressBar '{AutomationId}' is not complete. Value: {GetValue()}, Max: {GetMaximum()}");
        }
        return ContainingScope;
    }
}
