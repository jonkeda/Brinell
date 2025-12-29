using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI ProgressBar control wrapper.
/// Provides read-only progress tracking for progress bar controls.
/// </summary>
public class ProgressBarControl : RangeControlBase
{
    public ProgressBarControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ProgressBarControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get current progress value.
    /// </summary>
    public override double GetValue()
    {
        var element = FindElement();
        if (element != null)
        {
            // Try different attribute names used by different platforms
            var value = element.GetAttribute("value") ?? element.GetAttribute("progress");
            if (double.TryParse(value, out var result))
                return result;
            
            // Try text as fallback
            var text = element.Text;
            if (double.TryParse(text, out result))
                return result;
        }
        return 0;
    }

    /// <summary>
    /// Get minimum value (always 0 for MAUI ProgressBar).
    /// </summary>
    public override double GetMinimum()
    {
        return 0;
    }

    /// <summary>
    /// Get maximum value (typically 1 for MAUI ProgressBar).
    /// </summary>
    public override double GetMaximum()
    {
        var element = FindElement();
        if (element != null)
        {
            var max = element.GetAttribute("maximum") ?? element.GetAttribute("max");
            if (double.TryParse(max, out var result))
                return result;
        }
        return 1; // MAUI ProgressBar default max is 1
    }

    /// <summary>
    /// Check if progress is complete (at maximum).
    /// </summary>
    public bool IsComplete()
    {
        var value = GetValue();
        var max = GetMaximum();
        return Math.Abs(value - max) < 0.001;
    }

    /// <summary>
    /// Wait for progress to reach a specific percentage.
    /// </summary>
    public bool WaitForProgress(double targetPercentage, int? timeoutMs = null)
    {
        Log($"WaitForProgress({targetPercentage}%)");
        return _context.WaitFor(
            () => GetPercentage() >= targetPercentage,
            timeoutMs,
            $"progress reaches {targetPercentage}%");
    }

    /// <summary>
    /// Wait for progress to complete (100%).
    /// </summary>
    public bool WaitForComplete(int? timeoutMs = null)
    {
        return WaitForProgress(100, timeoutMs);
    }
}
