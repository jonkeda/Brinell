using System.Globalization;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms NumericUpDown control wrapper.
/// Inherits from InputControlBase which provides Clear, AppendText, IsReadOnly, GetTextLength.
/// Provides numeric-specific operations for setting and getting numeric values.
/// </summary>
public class NumericUpDownControl : InputControlBase
{
    public NumericUpDownControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public NumericUpDownControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public NumericUpDownControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Set the numeric value by entering text.
    /// </summary>
    public void SetValue(decimal value)
    {
        SetText(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Get the current numeric value.
    /// </summary>
    public decimal GetValue()
    {
        var text = GetText();
        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        ThrowCheckFailed("GetValue", $"Element '{AutomationId}' contains non-numeric value: '{text}'");
        return 0; // Never reached
    }

    /// <summary>
    /// Get the minimum value of the numeric up down.
    /// Note: This requires UI automation support for Minimum property, which may not always be available.
    /// </summary>
    public decimal GetMinimum()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetMinimum", $"Element '{AutomationId}' not found.");
        }

        // Try to get the RangeValue pattern from the element
        try
        {
            var rangePattern = element!.Patterns.RangeValue.PatternOrDefault;
            if (rangePattern != null)
            {
                return (decimal)(double)rangePattern.Minimum;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Could not retrieve RangeValue pattern for element '{AutomationId}': {ex.Message}");
        }

        return 0m; // Default minimum
    }

    /// <summary>
    /// Get the maximum value of the numeric up down.
    /// </summary>
    public decimal GetMaximum()
    {
        var element = FindElement();
        if (element == null)
        {
            ThrowCheckFailed("GetMaximum", $"Element '{AutomationId}' not found.");
        }

        // Try to get the RangeValue pattern from the element
        try
        {
            var rangePattern = element!.Patterns.RangeValue.PatternOrDefault;
            if (rangePattern != null)
            {
                return (decimal)(double)rangePattern.Maximum;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Could not retrieve RangeValue pattern for element '{AutomationId}': {ex.Message}");
        }

        return 100m; // Default maximum
    }

    /// <summary>
    /// Increment the value by clicking the up button.
    /// </summary>
    public void Increment()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Increment", $"Element '{AutomationId}' not visible.");
        }

        // Find and click the up button (typically an increase button in the NumericUpDown)
        var upButton = element!.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button).And(cf.ByName("Increase")));
        if (upButton != null)
        {
            upButton.Click();
            System.Threading.Thread.Sleep(50);
            LogAction("Increment");
        }
        else
        {
            // Alternative: use keyboard to increment
            element.Focus();
            System.Windows.Forms.SendKeys.SendWait("{UP}");
            System.Threading.Thread.Sleep(50);
            LogAction("Increment");
        }
    }

    /// <summary>
    /// Decrement the value by clicking the down button.
    /// </summary>
    public void Decrement()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Decrement", $"Element '{AutomationId}' not visible.");
        }

        // Find and click the down button (typically a decrease button in the NumericUpDown)
        var downButton = element!.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Button).And(cf.ByName("Decrease")));
        if (downButton != null)
        {
            downButton.Click();
            System.Threading.Thread.Sleep(50);
            LogAction("Decrement");
        }
        else
        {
            // Alternative: use keyboard to decrement
            element.Focus();
            System.Windows.Forms.SendKeys.SendWait("{DOWN}");
            System.Threading.Thread.Sleep(50);
            LogAction("Decrement");
        }
    }

    /// <summary>
    /// Assert that the numeric value equals expected.
    /// </summary>
    public void AssertValueEquals(decimal expected)
    {
        var actual = GetValue();
        if (actual != expected)
        {
            ThrowAssertionFailed("ValueEquals", actual.ToString(CultureInfo.InvariantCulture), expected.ToString(CultureInfo.InvariantCulture),
                $"NumericUpDown '{AutomationId}' value is {actual}, expected {expected}.");
        }
        LogAssertPass("ValueEquals", actual.ToString(CultureInfo.InvariantCulture), expected.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Wait and assert that the numeric value equals expected.
    /// </summary>
    public void AssertValueEqualsWait(decimal expected, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var result = _context.WaitFor(() => GetValue() == expected, timeout, $"value equals {expected}");
        
        if (!result)
        {
            var actual = GetValue();
            ThrowAssertionFailed("ValueEqualsWait", actual.ToString(CultureInfo.InvariantCulture), expected.ToString(CultureInfo.InvariantCulture),
                $"NumericUpDown '{AutomationId}' value is {actual}, expected {expected}.");
        }
        LogAssertPass("ValueEqualsWait", expected.ToString(CultureInfo.InvariantCulture), expected.ToString(CultureInfo.InvariantCulture));
    }
}
