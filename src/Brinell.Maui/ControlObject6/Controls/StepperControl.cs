using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Control object for MAUI Stepper elements.
/// </summary>
public class StepperControl : RangeControlBase
{
    /// <summary>
    /// Creates a new StepperControl.
    /// </summary>
    public StepperControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new StepperControl using AutomationId.
    /// </summary>
    public StepperControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public override double GetValue(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        // Try Value attribute first
        var value = element.GetAttribute("Value");
        if (double.TryParse(value, out var v))
            return v;

        // Try RangeValue.Value
        value = element.GetAttribute("RangeValue.Value");
        if (double.TryParse(value, out v))
            return v;

        return 0;
    }

    /// <summary>
    /// Gets the increment/step size.
    /// </summary>
    public virtual double GetIncrement(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        var increment = element.GetAttribute("Increment");
        if (double.TryParse(increment, out var v))
            return v;

        // Default step
        return 1;
    }

    /// <inheritdoc />
    protected override void PerformSetValue(double value, int? timeoutMs = null)
    {
        Log($"PerformSetValue({value})");
        
        var current = GetValue(timeoutMs);
        var increment = GetIncrement(timeoutMs);
        
        // Calculate number of steps needed
        var diff = value - current;
        var steps = (int)Math.Round(diff / increment);
        
        if (steps > 0)
        {
            for (int i = 0; i < steps; i++)
                PerformIncrease(timeoutMs);
        }
        else if (steps < 0)
        {
            for (int i = 0; i < Math.Abs(steps); i++)
                PerformDecrease(timeoutMs);
        }
    }

    /// <inheritdoc />
    protected override void PerformIncrease(int? timeoutMs = null)
    {
        Log("PerformIncrease()");
        
        // Find and click the increment button
        var incrementButton = FindIncrementButton(timeoutMs);
        if (incrementButton is not null)
        {
            incrementButton.Click();
        }
        else
        {
            // Fallback: use keyboard
            var element = FindElementRequired(timeoutMs);
            element.SendKeys(OpenQA.Selenium.Keys.Up);
        }
    }

    /// <inheritdoc />
    protected override void PerformDecrease(int? timeoutMs = null)
    {
        Log("PerformDecrease()");
        
        // Find and click the decrement button
        var decrementButton = FindDecrementButton(timeoutMs);
        if (decrementButton is not null)
        {
            decrementButton.Click();
        }
        else
        {
            // Fallback: use keyboard
            var element = FindElementRequired(timeoutMs);
            element.SendKeys(OpenQA.Selenium.Keys.Down);
        }
    }

    /// <summary>
    /// Finds the increment (+) button within the stepper.
    /// </summary>
    protected virtual AppiumElement? FindIncrementButton(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        try
        {
            // Common patterns for increment button
            var button = element.FindElement(MobileBy.XPath(
                ".//*[contains(@AutomationId,'Increment') or contains(@AutomationId,'Plus') or " +
                "contains(@Name,'+') or contains(@AutomationId,'increase')]"));
            return (AppiumElement)button;
        }
        catch
        {
            // Try finding by position (right button for horizontal stepper)
            try
            {
                var buttons = element.FindElements(MobileBy.XPath(".//Button"));
                if (buttons.Count >= 2)
                    return (AppiumElement)buttons[1]; // Second button is usually increment
            }
            catch { }
        }
        
        return null;
    }

    /// <summary>
    /// Finds the decrement (-) button within the stepper.
    /// </summary>
    protected virtual AppiumElement? FindDecrementButton(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        
        try
        {
            // Common patterns for decrement button
            var button = element.FindElement(MobileBy.XPath(
                ".//*[contains(@AutomationId,'Decrement') or contains(@AutomationId,'Minus') or " +
                "contains(@Name,'-') or contains(@AutomationId,'decrease')]"));
            return (AppiumElement)button;
        }
        catch
        {
            // Try finding by position (left button for horizontal stepper)
            try
            {
                var buttons = element.FindElements(MobileBy.XPath(".//Button"));
                if (buttons.Count >= 1)
                    return (AppiumElement)buttons[0]; // First button is usually decrement
            }
            catch { }
        }
        
        return null;
    }

    /// <summary>
    /// Clicks the increment button once.
    /// </summary>
    public void Increment(int? timeoutMs = null)
    {
        Log("Increment()");
        Increase(timeoutMs);
    }

    /// <summary>
    /// Clicks the decrement button once.
    /// </summary>
    public void Decrement(int? timeoutMs = null)
    {
        Log("Decrement()");
        Decrease(timeoutMs);
    }

    /// <summary>
    /// Clicks the increment button multiple times.
    /// </summary>
    public void IncrementBy(int steps, int? timeoutMs = null)
    {
        Log($"IncrementBy({steps})");
        for (int i = 0; i < steps; i++)
            Increase(timeoutMs);
    }

    /// <summary>
    /// Clicks the decrement button multiple times.
    /// </summary>
    public void DecrementBy(int steps, int? timeoutMs = null)
    {
        Log($"DecrementBy({steps})");
        for (int i = 0; i < steps; i++)
            Decrease(timeoutMs);
    }
}
