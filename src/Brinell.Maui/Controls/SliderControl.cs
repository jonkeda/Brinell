using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Slider control wrapper.
/// Provides value manipulation for slider controls.
/// </summary>
public class SliderControl : RangeControlBase
{
    public SliderControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public SliderControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get current value.
    /// </summary>
    public override double GetValue()
    {
        var element = FindElement();
        if (element != null)
        {
            // Try different attribute names used by different platforms
            var value = element.GetAttribute("value");
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
    /// Get minimum value.
    /// Note: MAUI Slider may not expose min via automation - defaults to 0.
    /// </summary>
    public override double GetMinimum()
    {
        var element = FindElement();
        if (element != null)
        {
            var min = element.GetAttribute("minimum") ?? element.GetAttribute("min");
            if (double.TryParse(min, out var result))
                return result;
        }
        return 0;
    }

    /// <summary>
    /// Get maximum value.
    /// Note: MAUI Slider may not expose max via automation - defaults to 1.
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
        return 1;
    }

    /// <summary>
    /// Set the slider value.
    /// </summary>
    public override void SetValue(double value)
    {
        LogAction("SetValue", value.ToString());
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Slider '{AutomationId}' not visible for value change.");
        
        // Clamp value to valid range
        var min = GetMinimum();
        var max = GetMaximum();
        value = Math.Max(min, Math.Min(max, value));
        
        // For now, log what we're trying to do
        // Actual implementation would use touch/drag gestures
        Log($"SetValue({value}) - would require platform-specific touch gestures");
    }

    /// <summary>
    /// Increment the slider.
    /// </summary>
    public override void Increment()
    {
        LogAction("Increment");
        var current = GetValue();
        var max = GetMaximum();
        var min = GetMinimum();
        var step = (max - min) * 0.1; // 10% increment
        SetValue(Math.Min(current + step, max));
    }

    /// <summary>
    /// Decrement the slider.
    /// </summary>
    public override void Decrement()
    {
        LogAction("Decrement");
        var current = GetValue();
        var max = GetMaximum();
        var min = GetMinimum();
        var step = (max - min) * 0.1; // 10% decrement
        SetValue(Math.Max(current - step, min));
    }

    /// <summary>
    /// Set value as percentage (0-100).
    /// </summary>
    public void SetPercentage(double percentage)
    {
        percentage = Math.Max(0, Math.Min(100, percentage));
        var min = GetMinimum();
        var max = GetMaximum();
        var value = min + (percentage / 100) * (max - min);
        SetValue(value);
    }
}
