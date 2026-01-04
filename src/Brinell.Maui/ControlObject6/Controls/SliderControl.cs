using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Interactions;
using AppiumPointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Control object for MAUI Slider elements.
/// </summary>
public class SliderControl : RangeControlBase
{
    /// <summary>
    /// Creates a new SliderControl.
    /// </summary>
    public SliderControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new SliderControl using AutomationId.
    /// </summary>
    public SliderControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    protected override void PerformSetValue(double value, int? timeoutMs = null)
    {
        Log($"PerformSetValue({value})");
        
        var element = FindElementRequired(timeoutMs);
        var (min, max) = GetRange(timeoutMs);
        
        // Calculate target position as percentage
        var targetPercent = (value - min) / (max - min);
        
        // Get element size and location
        var size = element.Size;
        var location = element.Location;
        
        // Calculate target X position (for horizontal slider)
        var targetX = (int)(size.Width * targetPercent);
        var centerY = size.Height / 2;
        
        // Get current value position
        var currentValue = GetValue(timeoutMs);
        var currentPercent = (currentValue - min) / (max - min);
        var currentX = (int)(size.Width * currentPercent);
        
        // Use touch actions to slide to target
        try
        {
            var actions = new Actions(Driver);
            actions.MoveToElement(element, currentX - size.Width / 2, 0)
                   .ClickAndHold()
                   .MoveToElement(element, targetX - size.Width / 2, 0)
                   .Release()
                   .Perform();
        }
        catch
        {
            // Fallback: click at target position
            var clickX = location.X + targetX;
            var clickY = location.Y + centerY;
            
            var touchAction = new AppiumPointerInputDevice(PointerKind.Touch, "finger");
            var sequence = new ActionSequence(touchAction);
            sequence.AddAction(touchAction.CreatePointerMove(CoordinateOrigin.Viewport, clickX, clickY, TimeSpan.Zero));
            sequence.AddAction(touchAction.CreatePointerDown(MouseButton.Left));
            sequence.AddAction(touchAction.CreatePointerUp(MouseButton.Left));
            
            Driver.PerformActions(new List<ActionSequence> { sequence });
        }
    }

    /// <inheritdoc />
    protected override void PerformIncrease(int? timeoutMs = null)
    {
        // Slider: move right by 10%
        var element = FindElementRequired(timeoutMs);
        element.SendKeys(OpenQA.Selenium.Keys.Right);
    }

    /// <inheritdoc />
    protected override void PerformDecrease(int? timeoutMs = null)
    {
        // Slider: move left by 10%
        var element = FindElementRequired(timeoutMs);
        element.SendKeys(OpenQA.Selenium.Keys.Left);
    }

    /// <summary>
    /// Slides to a specific percentage (0-100).
    /// </summary>
    public void SlideToPercent(double percent, int? timeoutMs = null)
    {
        Log($"SlideToPercent({percent})");
        SetValuePercent(percent / 100.0, timeoutMs);
    }

    /// <summary>
    /// Slides left (decreases value).
    /// </summary>
    public void SlideLeft(int? timeoutMs = null)
    {
        Log("SlideLeft()");
        Decrease(timeoutMs);
    }

    /// <summary>
    /// Slides right (increases value).
    /// </summary>
    public void SlideRight(int? timeoutMs = null)
    {
        Log("SlideRight()");
        Increase(timeoutMs);
    }
}
