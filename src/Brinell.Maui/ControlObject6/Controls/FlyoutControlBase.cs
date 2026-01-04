using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using AppiumPointerInputDevice = OpenQA.Selenium.Appium.Interactions.PointerInputDevice;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for flyout controls in MAUI.
/// </summary>
public abstract class FlyoutControlBase : ControlObjectBase, IFlyoutControlObject
{
    /// <summary>
    /// Creates a new flyout control.
    /// </summary>
    protected FlyoutControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new flyout control using AutomationId.
    /// </summary>
    protected FlyoutControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// XPath pattern for finding flyout items.
    /// </summary>
    protected virtual string FlyoutItemXPath => ".//*[@ClassName='FlyoutItem' or @ClassName='MenuItem' or contains(@ClassName,'Item')]";

    #region Flyout State

    /// <inheritdoc/>
    public virtual bool IsOpen(int? timeoutMs = null)
    {
        var element = FindElement();
        var result = element?.Displayed ?? false;
        Log($"IsOpen: {result}");
        return result;
    }

    /// <inheritdoc/>
    public virtual bool WaitOpen(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (IsOpen(timeoutMs) == expected.Value)
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsOpen(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected flyout to be {(expected.Value ? "open" : "closed")} but was {(actual ? "open" : "closed")}";
            throw new AssertionException(msg, Locator.Value, "AssertOpen");
        }
    }

    #endregion

    #region Open/Close

    /// <inheritdoc/>
    public virtual void Open(int? timeoutMs = null)
    {
        Log("Open()");
        if (!IsOpen(timeoutMs))
        {
            SwipeFromLeftEdge();
            WaitOpen(true, timeoutMs);
        }
    }

    /// <inheritdoc/>
    public virtual void Close(int? timeoutMs = null)
    {
        Log("Close()");
        if (IsOpen(timeoutMs))
        {
            SwipeToLeftEdge();
            WaitOpen(false, timeoutMs);
        }
    }

    /// <inheritdoc/>
    public virtual void Toggle(int? timeoutMs = null)
    {
        Log("Toggle()");
        if (IsOpen(timeoutMs))
        {
            Close(timeoutMs);
        }
        else
        {
            Open(timeoutMs);
        }
    }

    #endregion

    #region Flyout Items

    /// <inheritdoc/>
    public virtual void ClickFlyoutItem(string? name, int? timeoutMs = null)
    {
        if (name is null) return;
        Log($"ClickFlyoutItem(\"{name}\")");

        Open(timeoutMs);
        var element = FindElementRequired(timeoutMs);
        var item = element.FindElement(OpenQA.Selenium.By.XPath($".//*[@Name='{name}' or @AutomationId='{name}']"));
        item.Click();
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> GetFlyoutItemNames(int? timeoutMs = null)
    {
        Open(timeoutMs);
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(FlyoutItemXPath));
        var names = items.Select(i => i.Text ?? ((AppiumElement)i).GetAttribute("Name") ?? string.Empty).ToList();
        Log($"GetFlyoutItemNames: [{string.Join(", ", names)}]");
        return names.AsReadOnly();
    }

    #endregion

    #region Swipe Helpers

    /// <summary>
    /// Swipes from the left edge to open the flyout.
    /// </summary>
    protected virtual void SwipeFromLeftEdge()
    {
        var pointer = new AppiumPointerInputDevice(PointerKind.Touch, "finger");
        var sequence = new ActionSequence(pointer, 0);

        sequence.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, 0, 300, TimeSpan.Zero));
        sequence.AddAction(pointer.CreatePointerDown(MouseButton.Left));
        sequence.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, 200, 300, TimeSpan.FromMilliseconds(300)));
        sequence.AddAction(pointer.CreatePointerUp(MouseButton.Left));

        Driver.PerformActions(new[] { sequence });
    }

    /// <summary>
    /// Swipes to the left edge to close the flyout.
    /// </summary>
    protected virtual void SwipeToLeftEdge()
    {
        var pointer = new AppiumPointerInputDevice(PointerKind.Touch, "finger");
        var sequence = new ActionSequence(pointer, 0);

        sequence.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, 200, 300, TimeSpan.Zero));
        sequence.AddAction(pointer.CreatePointerDown(MouseButton.Left));
        sequence.AddAction(pointer.CreatePointerMove(CoordinateOrigin.Viewport, 0, 300, TimeSpan.FromMilliseconds(300)));
        sequence.AddAction(pointer.CreatePointerUp(MouseButton.Left));

        Driver.PerformActions(new[] { sequence });
    }

    #endregion
}
