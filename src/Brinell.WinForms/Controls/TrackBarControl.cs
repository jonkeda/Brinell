using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TrackBar control wrapper.
/// Provides numeric range operations for trackbar/slider controls.
/// </summary>
public class TrackBarControl : ControlBase
{
    public TrackBarControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TrackBarControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TrackBarControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Set the trackbar position to a specific value.
    /// Uses the RangeValue pattern if available.
    /// </summary>
    public void SetValue(int value)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SetValue", $"Element '{AutomationId}' not visible.");
        }

        try
        {
            var rangePattern = element!.Patterns.RangeValue.PatternOrDefault;
            if (rangePattern != null)
            {
                rangePattern.SetValue(value);
                System.Threading.Thread.Sleep(50);
                LogAction("SetValue", value.ToString());
                return;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"SetValue via RangeValue pattern failed: {ex.Message}");
        }

        ThrowCheckFailed("SetValue", $"Could not set trackbar value: RangeValue pattern not available.");
    }

    /// <summary>
    /// Get the current trackbar position.
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
            ThrowCheckFailed("GetValue", $"Could not get trackbar value: {ex.Message}");
        }

        return 0;
    }

    /// <summary>
    /// Get the minimum allowed value.
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
    /// Get the maximum allowed value.
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
    /// Increment the value by 1.
    /// Uses arrow keys for navigation.
    /// </summary>
    public void Increment()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Increment", $"Element '{AutomationId}' not visible.");
        }

        element!.Focus();
        System.Windows.Forms.SendKeys.SendWait("{RIGHT}");
        System.Threading.Thread.Sleep(50);
        LogAction("Increment");
    }

    /// <summary>
    /// Decrement the value by 1.
    /// Uses arrow keys for navigation.
    /// </summary>
    public void Decrement()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Decrement", $"Element '{AutomationId}' not visible.");
        }

        element!.Focus();
        System.Windows.Forms.SendKeys.SendWait("{LEFT}");
        System.Threading.Thread.Sleep(50);
        LogAction("Decrement");
    }

    /// <summary>
    /// Assert that the value equals expected.
    /// </summary>
    public void AssertValueEquals(int expected)
    {
        var actual = GetValue();
        if (actual != expected)
        {
            ThrowAssertionFailed("ValueEquals", actual.ToString(), expected.ToString(),
                $"TrackBar '{AutomationId}' value is {actual}, expected {expected}.");
        }
        LogAssertPass("ValueEquals", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert that the value equals expected, with optional timeout.
    /// </summary>
    public void AssertValueEqualsWait(int expected, int? timeoutMs = null)
    {
        WaitForElement(timeoutMs);
        AssertValueEquals(expected);
    }

    /// <summary>
    /// Assert that the value is in range.
    /// </summary>
    public void AssertValueInRange(int minValue, int maxValue)
    {
        var actual = GetValue();
        if (actual < minValue || actual > maxValue)
        {
            ThrowAssertionFailed("ValueInRange", actual.ToString(), $"{minValue}-{maxValue}",
                $"TrackBar '{AutomationId}' value {actual} is not in range {minValue}-{maxValue}.");
        }
        LogAssertPass("ValueInRange", actual.ToString(), $"{minValue}-{maxValue}");
    }
}
