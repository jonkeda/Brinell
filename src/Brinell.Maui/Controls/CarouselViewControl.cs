using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Gestures;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI CarouselView control wrapper.
/// Provides horizontal swipeable carousel functionality.
/// </summary>
public class CarouselViewControl : ItemsControlBase
{
    public CarouselViewControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public CarouselViewControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current position (zero-based index).
    /// </summary>
    public int GetCurrentPosition()
    {
        var element = FindElement();
        if (element == null) return -1;
        
        var position = element.GetAttribute("position") ?? element.GetAttribute("currentItem");
        if (int.TryParse(position, out var result))
            return result;
        
        return 0;
    }

    /// <summary>
    /// Swipe to the next item.
    /// </summary>
    public void SwipeNext()
    {
        LogAction("SwipeNext");
        SwipeLeft(250);
    }

    /// <summary>
    /// Swipe to the previous item.
    /// </summary>
    public void SwipePrevious()
    {
        LogAction("SwipePrevious");
        SwipeRight(250);
    }

    /// <summary>
    /// Navigate to a specific position.
    /// </summary>
    /// <param name="position">Zero-based position index.</param>
    public void GoToPosition(int position)
    {
        LogAction("GoToPosition", position.ToString());
        
        var current = GetCurrentPosition();
        var count = GetItemCount();
        
        if (position < 0 || position >= count)
            throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is out of range [0, {count - 1}].");
        
        while (current < position)
        {
            SwipeNext();
            Thread.Sleep(300); // Wait for animation
            current = GetCurrentPosition();
        }
        
        while (current > position)
        {
            SwipePrevious();
            Thread.Sleep(300); // Wait for animation
            current = GetCurrentPosition();
        }
    }

    /// <summary>
    /// Check if we're at the first item.
    /// </summary>
    public bool IsAtStart() => GetCurrentPosition() == 0;

    /// <summary>
    /// Check if we're at the last item.
    /// </summary>
    public bool IsAtEnd() => GetCurrentPosition() == GetItemCount() - 1;

    #region Assert Methods

    /// <summary>
    /// Assert the current position.
    /// </summary>
    public void AssertPosition(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetCurrentPosition();
        if (actual != expected)
        {
            ThrowAssertionFailed("Position", actual.ToString(), expected.ToString(),
                message ?? $"Expected position {expected} but got {actual}.");
        }
        LogAssertPass("Position", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert we're at the first item.
    /// </summary>
    public void AssertAtStart(string? message = null)
    {
        if (!IsAtStart())
        {
            var actual = GetCurrentPosition();
            ThrowAssertionFailed("AtStart", actual.ToString(), "0",
                message ?? $"Expected to be at start (position 0) but at position {actual}.");
        }
        LogAssertPass("AtStart", "0", "0");
    }

    /// <summary>
    /// Assert we're at the last item.
    /// </summary>
    public void AssertAtEnd(string? message = null)
    {
        if (!IsAtEnd())
        {
            var actual = GetCurrentPosition();
            var lastIndex = GetItemCount() - 1;
            ThrowAssertionFailed("AtEnd", actual.ToString(), lastIndex.ToString(),
                message ?? $"Expected to be at end (position {lastIndex}) but at position {actual}.");
        }
        LogAssertPass("AtEnd", GetCurrentPosition().ToString(), (GetItemCount() - 1).ToString());
    }

    #endregion
}
