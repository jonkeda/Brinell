using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using AppiumPointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for scrollable collection controls in MAUI.
/// Extends ItemsControlBase with scrolling capabilities.
/// </summary>
public abstract class ScrollableItemsControlBase : ItemsControlBase, IScrollableItemsControlObject
{
    /// <summary>
    /// Creates a new scrollable items control.
    /// </summary>
    protected ScrollableItemsControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new scrollable items control using AutomationId.
    /// </summary>
    protected ScrollableItemsControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Maximum scroll attempts before giving up.
    /// </summary>
    protected virtual int MaxScrollAttempts => 10;

    /// <summary>
    /// Default scroll distance in pixels.
    /// </summary>
    protected virtual int DefaultScrollDistance => 300;

    #region Scroll To Item

    /// <inheritdoc/>
    public virtual void ScrollToItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"ScrollToItem({index})");

        int attempts = 0;
        while (attempts < MaxScrollAttempts && !IsItemVisible(index.Value, timeoutMs))
        {
            ScrollDown(DefaultScrollDistance, timeoutMs);
            attempts++;
        }
    }

    /// <inheritdoc/>
    public virtual void ScrollToItem(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"ScrollToItem(\"{text}\")");

        int attempts = 0;
        while (attempts < MaxScrollAttempts)
        {
            var index = GetItemIndex(text, timeoutMs);
            if (index >= 0 && IsItemVisible(index, timeoutMs))
                return;

            ScrollDown(DefaultScrollDistance, timeoutMs);
            attempts++;
        }
    }

    /// <inheritdoc/>
    public virtual void ScrollToTop(int? timeoutMs = null)
    {
        Log("ScrollToTop");
        for (int i = 0; i < MaxScrollAttempts; i++)
        {
            ScrollUp(DefaultScrollDistance, timeoutMs);
        }
    }

    /// <inheritdoc/>
    public virtual void ScrollToBottom(int? timeoutMs = null)
    {
        Log("ScrollToBottom");
        for (int i = 0; i < MaxScrollAttempts; i++)
        {
            ScrollDown(DefaultScrollDistance, timeoutMs);
        }
    }

    #endregion

    #region Item Visibility

    /// <inheritdoc/>
    public virtual bool IsItemVisible(int index, int? timeoutMs = null)
    {
        try
        {
            var item = GetItemElement(index, timeoutMs);
            return item.Displayed;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual bool WaitItemVisible(int index, bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (IsItemVisible(index, timeoutMs) == expected.Value)
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertItemVisible(int index, bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsItemVisible(index, timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected item[{index}] to be {(expected.Value ? "visible" : "not visible")} but was {(actual ? "visible" : "not visible")}";
            throw new AssertionException(msg, Locator.Value, "AssertItemVisible");
        }
    }

    #endregion

    #region Scroll Helpers

    /// <summary>
    /// Scrolls down by the specified distance.
    /// </summary>
    protected virtual void ScrollDown(int? distance = null, int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startX = location.X + size.Width / 2;
        var startY = location.Y + size.Height * 3 / 4;
        var endY = startY - (distance ?? DefaultScrollDistance);

        PerformSwipe(startX, startY, startX, endY);
    }

    /// <summary>
    /// Scrolls up by the specified distance.
    /// </summary>
    protected virtual void ScrollUp(int? distance = null, int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startX = location.X + size.Width / 2;
        var startY = location.Y + size.Height / 4;
        var endY = startY + (distance ?? DefaultScrollDistance);

        PerformSwipe(startX, startY, startX, endY);
    }

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
