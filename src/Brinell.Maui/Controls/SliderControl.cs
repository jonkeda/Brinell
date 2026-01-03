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
    /// Windows UIA uses RangeValue.Value pattern.
    /// </summary>
    public override double GetValue()
    {
        var element = FindElement();
        if (element != null)
        {
            // Windows UIA uses RangeValue pattern
            var value = element.GetAttribute("RangeValue.Value");
            if (double.TryParse(value, out var result))
                return result;
            
            // Try standard value attribute
            value = element.GetAttribute("value") ?? element.GetAttribute("Value");
            if (double.TryParse(value, out result))
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
    /// Windows UIA uses RangeValue.Minimum, MAUI uses Minimum or minimum.
    /// </summary>
    public override double GetMinimum()
    {
        var element = FindElement();
        if (element != null)
        {
            // Windows UIA uses RangeValue pattern
            var min = element.GetAttribute("RangeValue.Minimum");
            if (double.TryParse(min, out var result))
                return result;
            
            // Try MAUI-specific attributes
            min = element.GetAttribute("Minimum") ?? element.GetAttribute("minimum") ?? element.GetAttribute("min");
            if (double.TryParse(min, out result))
                return result;
        }
        return 0;
    }

    /// <summary>
    /// Get maximum value.
    /// Windows UIA uses RangeValue.Maximum, MAUI uses Maximum or maximum.
    /// </summary>
    public override double GetMaximum()
    {
        var element = FindElement();
        if (element != null)
        {
            // Windows UIA uses RangeValue pattern
            var max = element.GetAttribute("RangeValue.Maximum");
            if (double.TryParse(max, out var result))
                return result;
            
            // Try MAUI-specific attributes
            max = element.GetAttribute("Maximum") ?? element.GetAttribute("maximum") ?? element.GetAttribute("max");
            if (double.TryParse(max, out result))
                return result;
        }
        return 100; // Default to 100 for percentage-based sliders
    }

    /// <summary>
    /// Set the slider value by clicking at the appropriate position on the slider track.
    /// Uses touch gestures for Windows compatibility.
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
        
        // Calculate relative position (0.0 to 1.0)
        var range = max - min;
        if (range <= 0) range = 1;
        var relativePosition = (value - min) / range;
        
        // Get element bounds
        var location = element.Location;
        var size = element.Size;
        
        // Calculate click position (horizontal slider assumed)
        // Use minimal padding (2px) for thumb at edges - most sliders handle this well
        var padding = 2;
        var effectiveWidth = size.Width - (2 * padding);
        var clickX = (int)(location.X + padding + (effectiveWidth * relativePosition));
        var clickY = location.Y + (size.Height / 2);
        
        // Use the driver's tap at coordinates
        _context.Driver.TapAtCoordinates(clickX, clickY);
        
        // Small delay for UI to update
        Thread.Sleep(100);
        
        Log($"SetValue({value}) - tapped at position ({clickX}, {clickY})");
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
