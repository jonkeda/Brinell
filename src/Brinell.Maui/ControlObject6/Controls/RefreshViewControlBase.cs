using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Interactions;
using AppiumPointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for refreshable controls in MAUI (pull-to-refresh pattern).
/// </summary>
public abstract class RefreshViewControlBase : ContainerControlBase, IRefreshableControlObject
{
    /// <summary>
    /// Creates a new refreshable control.
    /// </summary>
    protected RefreshViewControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new refreshable control using AutomationId.
    /// </summary>
    protected RefreshViewControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Refreshing State

    /// <inheritdoc/>
    public virtual bool IsRefreshing(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var refreshing = element.GetAttribute("IsRefreshing");
        var result = refreshing == "True" || refreshing == "true";
        Log($"IsRefreshing: {result}");
        return result;
    }

    /// <inheritdoc/>
    public virtual bool WaitRefreshing(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (IsRefreshing(timeoutMs) == expected.Value)
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsRefreshing(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected IsRefreshing to be {expected} but was {actual}";
            throw new AssertionException(msg, Locator.Value, "AssertRefreshing");
        }
    }

    #endregion

    #region Refresh Action

    /// <inheritdoc/>
    public virtual void Refresh(int? timeoutMs = null)
    {
        Log("Refresh()");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        var location = element.Location;

        var startX = location.X + size.Width / 2;
        var startY = location.Y + 50;
        var endY = startY + size.Height / 2;

        PerformSwipe(startX, startY, startX, endY);
    }

    /// <inheritdoc/>
    public virtual void WaitRefreshComplete(int? timeoutMs = null)
    {
        WaitRefreshing(false, timeoutMs);
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
        sequence.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(500)));
        sequence.AddAction(pointer.CreatePointerUp(MouseButton.Left));

        Driver.PerformActions(new[] { sequence });
    }

    #endregion
}
