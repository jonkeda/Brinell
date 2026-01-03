using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms NumericUpDown control wrapper.
/// Combines a text box with increment/decrement buttons.
/// </summary>
public class NumericUpDownControl : RangeControlBase, IRangeControl
{
    public NumericUpDownControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public NumericUpDownControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get current numeric value from the spinner.
    /// </summary>
    public override double GetValue()
    {
        var spinner = GetSpinner();
        return spinner?.Value ?? 0;
    }

    /// <summary>
    /// Set numeric value using the RangeValue pattern.
    /// </summary>
    public override void SetValue(double value)
    {
        CheckVisible();
        
        var spinner = GetSpinner();
        if (spinner != null)
        {
            spinner.Value = value;
            LogAction("SetValue", value.ToString());
        }
    }

    /// <summary>
    /// Get minimum value.
    /// </summary>
    public override double GetMinimum()
    {
        var spinner = GetSpinner();
        return spinner?.Minimum ?? 0;
    }

    /// <summary>
    /// Get maximum value.
    /// </summary>
    public override double GetMaximum()
    {
        var spinner = GetSpinner();
        return spinner?.Maximum ?? 100;
    }

    /// <summary>
    /// Increment the value.
    /// </summary>
    public override void Increment()
    {
        CheckVisible();
        
        var spinner = GetSpinner();
        spinner?.Increment();
        LogAction("Increment");
    }

    /// <summary>
    /// Decrement the value.
    /// </summary>
    public override void Decrement()
    {
        CheckVisible();
        
        var spinner = GetSpinner();
        spinner?.Decrement();
        LogAction("Decrement");
    }

    /// <summary>
    /// Get the value as text.
    /// </summary>
    public override string GetText()
    {
        return GetValue().ToString();
    }

    /// <summary>
    /// Wait for value to match expected.
    /// </summary>
    public bool WaitForValue(double expected, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => Math.Abs(GetValue() - expected) < 0.001,
            timeoutMs,
            $"value = {expected}");
        LogWait($"Value={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }
}
