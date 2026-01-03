using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms ProgressBar control wrapper.
/// Uses shared RangeControlBase for FlaUI integration.
/// Note: ProgressBar is read-only, so SetValue is not available.
/// </summary>
public class ProgressBarControl : RangeControlBase, IRangeControl
{
    public ProgressBarControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ProgressBarControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get current progress value - overrides base to use ProgressBar pattern specifically.
    /// </summary>
    public override double GetValue()
    {
        var progressBar = GetProgressBar();
        return progressBar?.Value ?? 0;
    }

    /// <summary>
    /// Get minimum value.
    /// </summary>
    public override double GetMinimum()
    {
        var progressBar = GetProgressBar();
        return progressBar?.Minimum ?? 0;
    }

    /// <summary>
    /// Get maximum value.
    /// </summary>
    public override double GetMaximum()
    {
        var progressBar = GetProgressBar();
        return progressBar?.Maximum ?? 100;
    }

    /// <summary>
    /// Get progress as percentage (0-100).
    /// </summary>
    public double GetPercentage()
    {
        var value = GetValue();
        var min = GetMinimum();
        var max = GetMaximum();
        
        if (max - min == 0) return 0;
        return (value - min) / (max - min) * 100;
    }

    /// <summary>
    /// Check if progress bar is indeterminate (no specific value, shows animation).
    /// </summary>
    public bool IsIndeterminate()
    {
        var progressBar = GetProgressBar();
        return progressBar?.Minimum == 0 && progressBar?.Maximum == 0;
    }

    /// <summary>
    /// Wait for progress to reach 100% (or maximum).
    /// </summary>
    public bool WaitForComplete(int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => Math.Abs(GetValue() - GetMaximum()) < 0.01,
            timeoutMs,
            "progress complete");
        LogWait("Complete", result, (int)sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Get progress value as text (percentage).
    /// </summary>
    public override string GetText()
    {
        return $"{GetPercentage():F0}%";
    }
}
