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

    #region Assert Methods

    /// <summary>
    /// Assert progress is complete (at 100%).
    /// Captures screenshot on failure.
    /// </summary>
    public void AssertComplete(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsComplete())
        {
            var actual = GetPercentage();
            ThrowAssertionFailed("Complete", $"{actual:F1}%", "100%",
                message ?? $"Expected progress to be complete but got {actual:F1}% for element '{AutomationId}'.");
        }
        LogAssertPass("Complete", "100%", "100%");
    }

    /// <summary>
    /// Assert progress is not complete (less than 100%).
    /// Captures screenshot on failure.
    /// </summary>
    public void AssertNotComplete(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsComplete())
        {
            ThrowAssertionFailed("NotComplete", "100%", "<100%",
                message ?? $"Expected progress to not be complete but it is at 100% for element '{AutomationId}'.");
        }
        var actual = GetPercentage();
        LogAssertPass("NotComplete", $"{actual:F1}%", "<100%");
    }

    /// <summary>
    /// Assert progress percentage is at least the expected value.
    /// Captures screenshot on failure.
    /// </summary>
    public void AssertProgressAtLeast(double expectedPercentage, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetPercentage();
        if (actual < expectedPercentage)
        {
            ThrowAssertionFailed("ProgressAtLeast", $"{actual:F1}%", $">= {expectedPercentage}%",
                message ?? $"Expected progress at least {expectedPercentage}% but got {actual:F1}% for element '{AutomationId}'.");
        }
        LogAssertPass("ProgressAtLeast", $"{actual:F1}%", $">= {expectedPercentage}%");
    }

    #endregion
}
