using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Gestures;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI SwipeView control wrapper.
/// Provides swipeable item functionality with left/right actions.
/// </summary>
public class SwipeViewControl : ContentControlBase
{
    public SwipeViewControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public SwipeViewControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Swipe left to reveal right actions.
    /// </summary>
    /// <param name="distance">Distance to swipe in pixels.</param>
    public new void SwipeLeft(int distance = 200)
    {
        LogAction("SwipeLeft", distance.ToString());
        base.SwipeLeft(distance);
    }

    /// <summary>
    /// Swipe right to reveal left actions.
    /// </summary>
    /// <param name="distance">Distance to swipe in pixels.</param>
    public new void SwipeRight(int distance = 200)
    {
        LogAction("SwipeRight", distance.ToString());
        base.SwipeRight(distance);
    }

    /// <summary>
    /// Close any open swipe actions.
    /// </summary>
    public void CloseSwipe()
    {
        LogAction("CloseSwipe");
        // Tap the control to close swipe actions
        Tap();
    }

    /// <summary>
    /// Check if left swipe items are visible.
    /// </summary>
    public bool IsLeftSwipeOpen()
    {
        var leftItems = _context.Driver.FindElementDirect($"{AutomationId}_LeftItems");
        return leftItems?.Displayed ?? false;
    }

    /// <summary>
    /// Check if right swipe items are visible.
    /// </summary>
    public bool IsRightSwipeOpen()
    {
        var rightItems = _context.Driver.FindElementDirect($"{AutomationId}_RightItems");
        return rightItems?.Displayed ?? false;
    }

    #region Assert Methods

    /// <summary>
    /// Assert left swipe items are visible.
    /// </summary>
    public void AssertLeftSwipeOpen(string? message = null)
    {
        if (!IsLeftSwipeOpen())
        {
            ThrowAssertionFailed("LeftSwipeOpen", "false", "true",
                message ?? "Expected left swipe items to be visible.");
        }
        LogAssertPass("LeftSwipeOpen", "true", "true");
    }

    /// <summary>
    /// Assert right swipe items are visible.
    /// </summary>
    public void AssertRightSwipeOpen(string? message = null)
    {
        if (!IsRightSwipeOpen())
        {
            ThrowAssertionFailed("RightSwipeOpen", "false", "true",
                message ?? "Expected right swipe items to be visible.");
        }
        LogAssertPass("RightSwipeOpen", "true", "true");
    }

    #endregion
}
