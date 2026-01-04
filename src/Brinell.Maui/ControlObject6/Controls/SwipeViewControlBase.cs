using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Interactions;
using AppiumPointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for swipeable controls in MAUI.
/// </summary>
public abstract class SwipeViewControlBase : ContainerControlBase, ISwipeableControlObject
{
    /// <summary>
    /// Creates a new swipeable control.
    /// </summary>
    protected SwipeViewControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new swipeable control using AutomationId.
    /// </summary>
    protected SwipeViewControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Swipe distance as percentage of element size.
    /// </summary>
    protected virtual double SwipePercentage => 0.6;

    #region Swipe Actions

    /// <inheritdoc/>
    public virtual void SwipeLeft(int? timeoutMs = null)
    {
        Log("SwipeLeft()");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startX = location.X + (int)(size.Width * 0.8);
        var endX = location.X + (int)(size.Width * 0.2);
        var y = location.Y + size.Height / 2;

        PerformSwipe(startX, y, endX, y);
    }

    /// <inheritdoc/>
    public virtual void SwipeRight(int? timeoutMs = null)
    {
        Log("SwipeRight()");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startX = location.X + (int)(size.Width * 0.2);
        var endX = location.X + (int)(size.Width * 0.8);
        var y = location.Y + size.Height / 2;

        PerformSwipe(startX, y, endX, y);
    }

    /// <inheritdoc/>
    public virtual void SwipeUp(int? timeoutMs = null)
    {
        Log("SwipeUp()");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var x = location.X + size.Width / 2;
        var startY = location.Y + (int)(size.Height * 0.8);
        var endY = location.Y + (int)(size.Height * 0.2);

        PerformSwipe(x, startY, x, endY);
    }

    /// <inheritdoc/>
    public virtual void SwipeDown(int? timeoutMs = null)
    {
        Log("SwipeDown()");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var x = location.X + size.Width / 2;
        var startY = location.Y + (int)(size.Height * 0.2);
        var endY = location.Y + (int)(size.Height * 0.8);

        PerformSwipe(x, startY, x, endY);
    }

    #endregion

    #region Swipe State

    /// <inheritdoc/>
    public virtual bool IsLeftSwipeRevealed(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var leftItems = element.FindElements(OpenQA.Selenium.By.XPath(".//*[@AutomationId='LeftSwipeItems' or contains(@ClassName,'LeftSwipe')]"));
        var result = leftItems.Count > 0 && leftItems[0].Displayed;
        Log($"IsLeftSwipeRevealed: {result}");
        return result;
    }

    /// <inheritdoc/>
    public virtual bool IsRightSwipeRevealed(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var rightItems = element.FindElements(OpenQA.Selenium.By.XPath(".//*[@AutomationId='RightSwipeItems' or contains(@ClassName,'RightSwipe')]"));
        var result = rightItems.Count > 0 && rightItems[0].Displayed;
        Log($"IsRightSwipeRevealed: {result}");
        return result;
    }

    /// <inheritdoc/>
    public virtual void CloseSwipe(int? timeoutMs = null)
    {
        Log("CloseSwipe()");
        var element = FindElementRequired(timeoutMs);
        element.Click();
    }

    #endregion

    #region Swipe Helper

    /// <summary>
    /// Performs a swipe gesture from start to end coordinates.
    /// </summary>
    protected virtual void PerformSwipe(int startX, int startY, int endX, int endY)
    {
        var pointer = new AppiumPointerInputDevice(PointerKind.Touch, "finger");
        var sequence = new ActionSequence(pointer, 0);

        sequence.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
        sequence.AddAction(pointer.CreatePointerDown(MouseButton.Left));
        sequence.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(300)));
        sequence.AddAction(pointer.CreatePointerUp(MouseButton.Left));

        Driver.PerformActions(new[] { sequence });
    }

    #endregion
}
