using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Gestures;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI ScrollView control wrapper.
/// Provides scrollable container functionality.
/// Implements IScrollableControl per FR-002.7.
/// </summary>
public class ScrollViewControl : ControlBase, IScrollableControl
{
    public ScrollViewControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ScrollViewControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current vertical scroll position.
    /// </summary>
    public double GetVerticalScrollPosition()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var scrollY = element.GetAttribute("scrollY") ?? element.GetAttribute("verticalOffset");
        if (double.TryParse(scrollY, out var result))
            return result;
        
        return 0;
    }

    /// <summary>
    /// Get the current horizontal scroll position.
    /// </summary>
    public double GetHorizontalScrollPosition()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var scrollX = element.GetAttribute("scrollX") ?? element.GetAttribute("horizontalOffset");
        if (double.TryParse(scrollX, out var result))
            return result;
        
        return 0;
    }

    /// <summary>
    /// Scroll down by swiping up.
    /// </summary>
    /// <param name="distance">Distance to scroll in pixels.</param>
    public void ScrollDown(int distance = 300)
    {
        LogAction("ScrollDown", distance.ToString());
        SwipeUp(distance);
    }

    /// <summary>
    /// Scroll up by swiping down.
    /// </summary>
    /// <param name="distance">Distance to scroll in pixels.</param>
    public void ScrollUp(int distance = 300)
    {
        LogAction("ScrollUp", distance.ToString());
        SwipeDown(distance);
    }

    /// <summary>
    /// Scroll right by swiping left.
    /// </summary>
    /// <param name="distance">Distance to scroll in pixels.</param>
    public void ScrollRight(int distance = 300)
    {
        LogAction("ScrollRight", distance.ToString());
        SwipeLeft(distance);
    }

    /// <summary>
    /// Scroll left by swiping right.
    /// </summary>
    /// <param name="distance">Distance to scroll in pixels.</param>
    public void ScrollLeft(int distance = 300)
    {
        LogAction("ScrollLeft", distance.ToString());
        SwipeRight(distance);
    }

    /// <summary>
    /// Scroll to top.
    /// </summary>
    public void ScrollToTop()
    {
        LogAction("ScrollToTop");
        // Perform multiple up swipes until at top or no change
        var lastPosition = GetVerticalScrollPosition();
        for (int i = 0; i < 10; i++)
        {
            ScrollUp(500);
            Thread.Sleep(200);
            var newPosition = GetVerticalScrollPosition();
            if (Math.Abs(newPosition - lastPosition) < 1 || newPosition <= 0)
                break;
            lastPosition = newPosition;
        }
    }

    /// <summary>
    /// Scroll to bottom.
    /// </summary>
    public void ScrollToBottom()
    {
        LogAction("ScrollToBottom");
        var lastPosition = GetVerticalScrollPosition();
        for (int i = 0; i < 20; i++)
        {
            ScrollDown(500);
            Thread.Sleep(200);
            var newPosition = GetVerticalScrollPosition();
            if (Math.Abs(newPosition - lastPosition) < 1)
                break;
            lastPosition = newPosition;
        }
        
        // Final wait for UI to stabilize after scroll completes
        Thread.Sleep(300);
    }

    /// <summary>
    /// Scroll to element with automation ID.
    /// Throws if element not found after max attempts.
    /// </summary>
    /// <param name="automationId">The automation ID of the element to scroll to.</param>
    /// <param name="maxAttempts">Maximum scroll attempts.</param>
    public void ScrollToElement(string automationId, int maxAttempts = 10)
    {
        LogAction("ScrollToElement", automationId);
        
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                var element = _context.Driver.FindElementDirect(automationId);
                if (element?.Displayed == true)
                {
                    Log($"ScrollToElement: Found '{automationId}' after {i} scrolls.");
                    return;
                }
            }
            catch (Exception ex)
            {
                // Log and continue trying - element may become visible after scroll
                Log($"ScrollToElement: Exception on attempt {i}: {ex.GetType().Name}");
            }
            
            ScrollDown(300);
            Thread.Sleep(300); // Slightly longer wait for scroll to settle
        }
        
        throw new InvalidOperationException($"ScrollToElement: Element '{automationId}' not found after {maxAttempts} attempts.");
    }

    /// <summary>
    /// Scroll to element with automation ID (IScrollableControl interface).
    /// </summary>
    void IScrollableControl.ScrollToElement(string automationId)
    {
        ScrollToElement(automationId);
    }
}
