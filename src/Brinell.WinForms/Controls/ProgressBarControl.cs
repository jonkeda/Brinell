using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms ProgressBar control wrapper.
/// Provides read-only access to progress value for tracking progress.
/// </summary>
public class ProgressBarControl : ControlBase
{
    public ProgressBarControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ProgressBarControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ProgressBarControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current progress value.
    /// Returns a value between 0 and the maximum.
    /// </summary>
    public int GetValue()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetValue", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var rangePattern = element!.Patterns.RangeValue.PatternOrDefault;
            if (rangePattern != null)
            {
                var value = (int)(double)rangePattern.Value;
                LogAction("GetValue", value.ToString());
                return value;
            }
        }
        catch (Exception ex)
        {
            ThrowCheckFailed("GetValue", $"Could not get progress value: {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Get the minimum progress value (typically 0).
    /// </summary>
    public int GetMinimum()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetMinimum", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var rangePattern = element!.Patterns.RangeValue.PatternOrDefault;
            if (rangePattern != null)
            {
                var value = (int)(double)rangePattern.Minimum;
                LogAction("GetMinimum", value.ToString());
                return value;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Could not retrieve minimum: {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Get the maximum progress value (typically 100).
    /// </summary>
    public int GetMaximum()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetMaximum", $"Element '{AutomationId}' not found.");
        }

        try
        {
            var rangePattern = element!.Patterns.RangeValue.PatternOrDefault;
            if (rangePattern != null)
            {
                var value = (int)(double)rangePattern.Maximum;
                LogAction("GetMaximum", value.ToString());
                return value;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Could not retrieve maximum: {ex.Message}");
        }

        return 100;
    }

    /// <summary>
    /// Get the progress as a percentage (0-100).
    /// </summary>
    public int GetPercentage()
    {
        var current = GetValue();
        var minimum = GetMinimum();
        var maximum = GetMaximum();

        if (maximum == minimum) return 0;

        var percentage = (int)((current - minimum) * 100 / (maximum - minimum));
        LogAction("GetPercentage", percentage.ToString());
        return percentage;
    }

    /// <summary>
    /// Assert that the progress value equals expected.
    /// </summary>
    public void AssertValueEquals(int expected)
    {
        var actual = GetValue();
        if (actual != expected)
        {
            ThrowAssertionFailed("ValueEquals", actual.ToString(), expected.ToString(),
                $"ProgressBar '{AutomationId}' value is {actual}, expected {expected}.");
        }
        LogAssertPass("ValueEquals", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert that the progress value equals expected, with optional timeout.
    /// </summary>
    public void AssertValueEqualsWait(int expected, int? timeoutMs = null)
    {
        WaitForElement(timeoutMs);
        AssertValueEquals(expected);
    }

    /// <summary>
    /// Assert that the progress is at least the specified value.
    /// </summary>
    public void AssertValueAtLeast(int minValue)
    {
        var actual = GetValue();
        if (actual < minValue)
        {
            ThrowAssertionFailed("ValueAtLeast", actual.ToString(), minValue.ToString(),
                $"ProgressBar '{AutomationId}' value {actual} is less than {minValue}.");
        }
        LogAssertPass("ValueAtLeast", actual.ToString(), $">= {minValue}");
    }

    /// <summary>
    /// Assert that the progress is complete (at maximum).
    /// </summary>
    public void AssertComplete()
    {
        var actual = GetValue();
        var maximum = GetMaximum();
        if (actual != maximum)
        {
            ThrowAssertionFailed("Complete", actual.ToString(), maximum.ToString(),
                $"ProgressBar '{AutomationId}' value {actual} is not at maximum {maximum}.");
        }
        LogAssertPass("Complete", actual.ToString(), maximum.ToString());
    }

    /// <summary>
    /// Assert that the progress percentage equals expected.
    /// </summary>
    public void AssertPercentageEquals(int expectedPercentage)
    {
        var actual = GetPercentage();
        if (actual != expectedPercentage)
        {
            ThrowAssertionFailed("PercentageEquals", actual.ToString(), expectedPercentage.ToString(),
                $"ProgressBar '{AutomationId}' percentage is {actual}%, expected {expectedPercentage}%.");
        }
        LogAssertPass("PercentageEquals", actual.ToString(), expectedPercentage.ToString());
    }

    /// <summary>
    /// Wait for progress to reach expected value.
    /// </summary>
    public void WaitForValue(int expectedValue, int timeoutMs = 30000)
    {
        WaitForElement(timeoutMs);
        AssertValueEquals(expectedValue);
    }

    /// <summary>
    /// Wait for progress to complete (reach maximum).
    /// </summary>
    public void WaitForComplete(int timeoutMs = 30000)
    {
        var maximum = GetMaximum();
        WaitForValue(maximum, timeoutMs);
    }
}
