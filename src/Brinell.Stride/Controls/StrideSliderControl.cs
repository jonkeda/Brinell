using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI slider controls.
/// </summary>
public class StrideSliderControl : StrideRangeControlBase
{
    /// <summary>
    /// Create a new slider control.
    /// </summary>
    public StrideSliderControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <inheritdoc />
    public override void SetValue(double value)
    {
        CheckEnabled();

        var min = GetMinimum();
        var max = GetMaximum();

        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Value {value} is outside range [{min}, {max}]");
        }

        // Calculate position on the track
        var bounds = GetBounds();
        var percentage = (value - min) / (max - min);
        var targetX = bounds.X + (int)(bounds.Width * percentage);
        var targetY = bounds.CenterY;

        // Click on the track at the calculated position
        Context.Input.Click(targetX, targetY);
        LogAction("SetValue", value.ToString());
    }

    /// <inheritdoc />
    public override void Increment()
    {
        // Stride sliders don't respond to keyboard keys reliably
        // Instead, calculate a position increment based on slider step
        var current = GetValue();
        var min = GetMinimum();
        var max = GetMaximum();
        var step = (max - min) / 10.0; // 10% increments

        var newValue = Math.Min(current + step, max);
        if (newValue != current)
        {
            SetValue(newValue);
        }
        LogAction("Increment");
    }

    /// <inheritdoc />
    public override void Decrement()
    {
        // Stride sliders don't respond to keyboard keys reliably
        // Instead, calculate a position decrement based on slider step
        var current = GetValue();
        var min = GetMinimum();
        var max = GetMaximum();
        var step = (max - min) / 10.0; // 10% decrements

        var newValue = Math.Max(current - step, min);
        if (newValue != current)
        {
            SetValue(newValue);
        }
        LogAction("Decrement");
    }

    /// <summary>
    /// Increment slider by multiple steps.
    /// </summary>
    public void IncrementBy(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            Increment();
        }
        LogAction("IncrementBy", steps.ToString());
    }

    /// <summary>
    /// Decrement slider by multiple steps.
    /// </summary>
    public void DecrementBy(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            Decrement();
        }
        LogAction("DecrementBy", steps.ToString());
    }

    /// <summary>
    /// Focus the slider control.
    /// </summary>
    public void Focus()
    {
        CheckVisible();
        Context.ClickElement(_automationId);
    }
}
