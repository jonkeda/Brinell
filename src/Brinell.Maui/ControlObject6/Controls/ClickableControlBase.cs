using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Interactions;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for clickable controls like buttons and links.
/// Provides virtual click methods that can be overridden.
/// </summary>
public abstract class ClickableControlBase : ControlObjectBase, IClickableControlObject
{
    /// <summary>
    /// Creates a new clickable control.
    /// </summary>
    protected ClickableControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new clickable control using AutomationId.
    /// </summary>
    protected ClickableControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public virtual void Click(int? timeoutMs = null)
    {
        Log("Click()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);
        element.Click();
    }

    /// <inheritdoc />
    public virtual void DoubleClick(int? timeoutMs = null)
    {
        Log("DoubleClick()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);
        var actions = new Actions(Driver);
        actions.DoubleClick(element).Perform();
    }

    /// <inheritdoc />
    public virtual void RightClick(int? timeoutMs = null)
    {
        Log("RightClick()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);
        var actions = new Actions(Driver);
        actions.ContextClick(element).Perform();
    }

    /// <inheritdoc />
    public virtual void Hover(int? timeoutMs = null)
    {
        Log("Hover()");
        CheckVisible(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);
        var actions = new Actions(Driver);
        actions.MoveToElement(element).Perform();
    }

    /// <inheritdoc />
    public virtual void LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        var duration = durationMs ?? 1000;
        Log($"LongPress(duration={duration}ms)");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);

        var element = FindElementRequired(timeoutMs);

        // Use W3C Actions for long press
        var finger = new PointerInputDevice(PointerKind.Touch, "finger");
        var sequence = new ActionSequence(finger, 0);

        var location = element.Location;
        var size = element.Size;
        var centerX = location.X + size.Width / 2;
        var centerY = location.Y + size.Height / 2;

        sequence.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, centerX, centerY, TimeSpan.Zero));
        sequence.AddAction(finger.CreatePointerDown(MouseButton.Left));
        sequence.AddAction(finger.CreatePause(TimeSpan.FromMilliseconds(duration)));
        sequence.AddAction(finger.CreatePointerUp(MouseButton.Left));

        Driver.PerformActions(new List<ActionSequence> { sequence });
    }
}
