using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI progress bar controls.
/// </summary>
public class StrideProgressBarControl : StrideRangeControlBase
{
    /// <summary>
    /// Create a new progress bar control.
    /// </summary>
    public StrideProgressBarControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
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
        return Context.WaitFor(
            () => IsComplete(),
            timeoutMs,
            $"progress '{AutomationId}' complete");
    }

    /// <summary>
    /// Assert completion.
    /// </summary>
    public void AssertComplete(string? message = null)
    {
        if (!IsComplete())
        {
            throw new Brinell.Core.Exceptions.AssertionException(
                message ?? $"Progress bar '{AutomationId}' should be complete but is at {GetPercentage():F1}%");
        }
    }
}
