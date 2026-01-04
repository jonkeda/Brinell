using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Interactions;
using AppiumPointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for scrollable container controls in MAUI.
/// </summary>
public abstract class ScrollViewControlBase : ContainerControlBase, IScrollableControlObject
{
    /// <summary>
    /// Creates a new scrollable container control.
    /// </summary>
    protected ScrollViewControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new scrollable container control using AutomationId.
    /// </summary>
    protected ScrollViewControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Default scroll distance in pixels.
    /// </summary>
    protected virtual int DefaultScrollDistance => 300;

    #region Scroll Position

    /// <inheritdoc/>
    public virtual (double horizontal, double vertical) GetScrollPosition(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var h = double.TryParse(element.GetAttribute("Scroll.HorizontalScrollPercent"), out var hVal) ? hVal : 0;
        var v = double.TryParse(element.GetAttribute("Scroll.VerticalScrollPercent"), out var vVal) ? vVal : 0;
        Log($"GetScrollPosition: ({h}, {v})");
        return (h, v);
    }

    /// <inheritdoc/>
    public virtual bool CanScrollHorizontally(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var canScroll = element.GetAttribute("Scroll.HorizontallyScrollable");
        return canScroll == "True" || canScroll == "true";
    }

    /// <inheritdoc/>
    public virtual bool CanScrollVertically(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var canScroll = element.GetAttribute("Scroll.VerticallyScrollable");
        return canScroll == "True" || canScroll == "true";
    }

    #endregion

    #region Scroll Actions

    /// <inheritdoc/>
    public virtual void ScrollTo(double? horizontalPercent, double? verticalPercent, int? timeoutMs = null)
    {
        if (horizontalPercent is null && verticalPercent is null) return;
        Log($"ScrollTo({horizontalPercent}, {verticalPercent})");
        
        // Calculate scroll amounts based on current position
        var currentPosition = GetScrollPosition(timeoutMs);
        
        if (verticalPercent.HasValue)
        {
            var diff = verticalPercent.Value - currentPosition.vertical;
            if (Math.Abs(diff) > 1)
            {
                if (diff > 0)
                    ScrollDown(diff * 3, timeoutMs); // Approximate scroll
                else
                    ScrollUp(Math.Abs(diff) * 3, timeoutMs);
            }
        }
        
        if (horizontalPercent.HasValue)
        {
            var diff = horizontalPercent.Value - currentPosition.horizontal;
            if (Math.Abs(diff) > 1)
            {
                if (diff > 0)
                    ScrollRight(diff * 3, timeoutMs);
                else
                    ScrollLeft(Math.Abs(diff) * 3, timeoutMs);
            }
        }
    }

    /// <inheritdoc/>
    public virtual void ScrollToTop(int? timeoutMs = null)
    {
        Log("ScrollToTop");
        ScrollTo(null, 0, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual void ScrollToBottom(int? timeoutMs = null)
    {
        Log("ScrollToBottom");
        ScrollTo(null, 100, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual void ScrollToLeft(int? timeoutMs = null)
    {
        Log("ScrollToLeft");
        ScrollTo(0, null, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual void ScrollToRight(int? timeoutMs = null)
    {
        Log("ScrollToRight");
        ScrollTo(100, null, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual void ScrollUp(double? amount = null, int? timeoutMs = null)
    {
        Log($"ScrollUp({amount})");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startX = location.X + size.Width / 2;
        var startY = location.Y + size.Height / 4;
        var endY = startY + (int)(amount ?? DefaultScrollDistance);

        PerformSwipe(startX, startY, startX, endY);
    }

    /// <inheritdoc/>
    public virtual void ScrollDown(double? amount = null, int? timeoutMs = null)
    {
        Log($"ScrollDown({amount})");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startX = location.X + size.Width / 2;
        var startY = location.Y + size.Height * 3 / 4;
        var endY = startY - (int)(amount ?? DefaultScrollDistance);

        PerformSwipe(startX, startY, startX, endY);
    }

    /// <inheritdoc/>
    public virtual void ScrollLeft(double? amount = null, int? timeoutMs = null)
    {
        Log($"ScrollLeft({amount})");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startY = location.Y + size.Height / 2;
        var startX = location.X + size.Width / 4;
        var endX = startX + (int)(amount ?? DefaultScrollDistance);

        PerformSwipe(startX, startY, endX, startY);
    }

    /// <inheritdoc/>
    public virtual void ScrollRight(double? amount = null, int? timeoutMs = null)
    {
        Log($"ScrollRight({amount})");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startY = location.Y + size.Height / 2;
        var startX = location.X + size.Width * 3 / 4;
        var endX = startX - (int)(amount ?? DefaultScrollDistance);

        PerformSwipe(startX, startY, endX, startY);
    }

    #endregion

    #region Scroll To Element

    /// <inheritdoc/>
    public virtual void ScrollToElement(IControlObject? control, int? timeoutMs = null)
    {
        if (control is null) return;
        Log($"ScrollToElement({control})");

        int attempts = 0;
        while (attempts < 10)
        {
            if (control.IsExists())
            {
                if (control.IsVisible())
                    return;
            }
            
            ScrollDown(DefaultScrollDistance, timeoutMs);
            attempts++;
        }
    }

    /// <inheritdoc/>
    public virtual bool WaitScrollComplete(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);
        var lastPosition = GetScrollPosition(timeoutMs);

        Thread.Sleep(100);

        while (DateTime.Now < deadline)
        {
            var currentPosition = GetScrollPosition(timeoutMs);
            if (Math.Abs(currentPosition.horizontal - lastPosition.horizontal) < 0.01 &&
                Math.Abs(currentPosition.vertical - lastPosition.vertical) < 0.01)
            {
                return true;
            }
            lastPosition = currentPosition;
            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
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
