using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Stepper control wrapper.
/// Provides increment/decrement functionality for numeric values.
/// </summary>
public class StepperControl : RangeControlBase
{
    public StepperControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public StepperControl(AppiumTestContext context, string automationId)
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
            var value = element.GetAttribute("value");
            if (double.TryParse(value, out var result))
                return result;
        }
        return 0;
    }

    /// <summary>
    /// Get minimum value.
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
        return 100;
    }

    /// <summary>
    /// Increment the stepper value.
    /// </summary>
    public override void Increment()
    {
        LogAction("Increment");
        var incrementButton = FindIncrementButton();
        if (incrementButton != null)
        {
            incrementButton.Click();
        }
        else
        {
            Log("Increment button not found, using tap on right side");
            var element = WaitForElementVisible();
            if (element != null)
            {
                var size = element.Size;
                var location = element.Location;
                // Tap on the right side of the stepper (increment)
                _context.Driver.TapAtCoordinates(location.X + (int)(size.Width * 0.8), location.Y + size.Height / 2);
            }
        }
    }

    /// <summary>
    /// Decrement the stepper value.
    /// </summary>
    public override void Decrement()
    {
        LogAction("Decrement");
        var decrementButton = FindDecrementButton();
        if (decrementButton != null)
        {
            decrementButton.Click();
        }
        else
        {
            Log("Decrement button not found, using tap on left side");
            var element = WaitForElementVisible();
            if (element != null)
            {
                var size = element.Size;
                var location = element.Location;
                // Tap on the left side of the stepper (decrement)
                _context.Driver.TapAtCoordinates(location.X + (int)(size.Width * 0.2), location.Y + size.Height / 2);
            }
        }
    }

    /// <summary>
    /// Set value by incrementing/decrementing.
    /// </summary>
    public override void SetValue(double value)
    {
        LogAction("SetValue", value.ToString());
        var current = GetValue();
        var step = GetStep();
        
        while (current < value && current < GetMaximum())
        {
            Increment();
            current = GetValue();
        }
        while (current > value && current > GetMinimum())
        {
            Decrement();
            current = GetValue();
        }
    }

    /// <summary>
    /// Get the step increment value.
    /// </summary>
    public double GetStep()
    {
        var element = FindElement();
        if (element != null)
        {
            var step = element.GetAttribute("increment") ?? element.GetAttribute("step");
            if (double.TryParse(step, out var result))
                return result;
        }
        return 1;
    }

    private OpenQA.Selenium.Appium.AppiumElement? FindIncrementButton()
    {
        return _context.Driver.FindElementDirect($"{AutomationId}_Increment") 
            ?? _context.Driver.FindElementDirect($"{AutomationId}Increment");
    }

    private OpenQA.Selenium.Appium.AppiumElement? FindDecrementButton()
    {
        return _context.Driver.FindElementDirect($"{AutomationId}_Decrement")
            ?? _context.Driver.FindElementDirect($"{AutomationId}Decrement");
    }
}
