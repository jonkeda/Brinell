using Brinell.Maui.Enums;
using Brinell.Core.Utilities;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with scrolling capability.
/// Implements IScrollableControlObject with ScrollToTop, ScrollToEnd, ScrollBy.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract class ScrollableControlBase<TScope> : ControlBase<TScope>, IScrollableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new scrollable control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    public ScrollableControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new scrollable control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public ScrollableControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
    
    #region IScrollableControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope ScrollToTop(int? timeoutMs = null)
    {
        return RunWithElement(nameof(ScrollToTop), timeoutMs, element =>
        {
            ScrollToTopCore(element);
        });
    }
    
    /// <inheritdoc />
    public TScope ScrollToEnd(int? timeoutMs = null)
    {
        return RunWithElement(nameof(ScrollToEnd), timeoutMs, element =>
        {
            ScrollToEndCore(element);
        });
    }
    
    /// <inheritdoc />
    public TScope ScrollBy(int deltaX, int deltaY, int? timeoutMs = null)
    {
        return RunWithElement(nameof(ScrollBy), timeoutMs, element =>
        {
            ScrollByCore(element, deltaX, deltaY);
        });
    }
    
    /// <inheritdoc />
    public TScope ScrollTo(Locator locator, int? timeoutMs = null)
    {
        return RunWithElement(nameof(ScrollTo), timeoutMs, element =>
        {
            ScrollToCore(element, locator, timeoutMs ?? DefaultTimeoutMs);
        });
    }
    
    /// <inheritdoc />
    public double? GetScrollPosition(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return GetScrollPositionCore(TryFindElement());
    }
    
    /// <inheritdoc />
    public TScope SetScrollPosition(double percent, int? timeoutMs = null)
    {
        return RunWithElement(nameof(SetScrollPosition), percent, timeoutMs, element =>
        {
            SetScrollPositionCore(element, percent);
        });
    }
    
    /// <inheritdoc />
    public bool? CanScrollDown(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return CanScrollDownCore(TryFindElement());
    }
    
    /// <inheritdoc />
    public bool? CanScrollUp(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        return CanScrollUpCore(TryFindElement());
    }
    
    #endregion
    
    #region Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Scrolls to top of the scrollable area.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void ScrollToTopCore(IMauiElement element)
    {
        // Swipe down repeatedly until at top
        var maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            var canScroll = CanScrollUpCore(element);
            if (canScroll != true) break;
            
            SwipeDownCore(element);
        }
    }
    
    /// <summary>
    /// Scrolls to end of the scrollable area.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void ScrollToEndCore(IMauiElement element)
    {
        // Swipe up repeatedly until at bottom
        var maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            var canScroll = CanScrollDownCore(element);
            if (canScroll != true) break;
            
            SwipeUpCore(element);
        }
    }
    
    /// <summary>
    /// Scrolls by specified delta amounts.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="deltaX">Horizontal scroll amount (positive = right).</param>
    /// <param name="deltaY">Vertical scroll amount (positive = down).</param>
    protected virtual void ScrollByCore(IMauiElement element, int deltaX, int deltaY)
    {
        var rect = element.Rect;
        var centerX = rect.X + rect.Width / 2;
        var centerY = rect.Y + rect.Height / 2;
        
        // Swipe in opposite direction to scroll (negative because scroll direction is opposite to swipe)
        element.Swipe(centerX, centerY, centerX - deltaX, centerY - deltaY);
    }
    
    /// <summary>
    /// Scrolls to make element at locator visible.
    /// </summary>
    /// <param name="element">The scrollable container element.</param>
    /// <param name="locator">The locator of element to scroll to.</param>
    /// <param name="timeoutMs">Timeout for finding element.</param>
    protected virtual void ScrollToCore(IMauiElement element, Locator locator, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();
        var maxAttempts = 20;
        
        for (int i = 0; i < maxAttempts && stopwatch.ElapsedMilliseconds < timeoutMs; i++)
        {
            // Try to find the target element
            var target = TryFindElementInContainer(locator);
            if (target != null && target.Visible)
            {
                return; // Found and visible
            }
            
            // Scroll down to reveal more content
            if (CanScrollDownCore(element) == true)
            {
                SwipeUpCore(element);
            }
            else
            {
                // Can't scroll down anymore - try from top
                ScrollToTopCore(element);
                break;
            }
            
            WaitHelper.Pause(100); // Brief pause for scroll to settle
        }
        
        // Final check
        var finalTarget = TryFindElementInContainer(locator);
        if (finalTarget == null || !finalTarget.Visible)
        {
            throw new ElementNotFoundException(
                $"Could not scroll to element with locator: {locator}");
        }
    }
    
    /// <summary>
    /// Gets scroll position as 0-100 percentage.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>Scroll position percentage, or null if not available.</returns>
    protected virtual double? GetScrollPositionCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        // Try scroll pattern attributes
        var scrollPercent = element.GetAttribute("Scroll.VerticalScrollPercent");
        if (!string.IsNullOrEmpty(scrollPercent) && double.TryParse(scrollPercent, out var percent))
        {
            return percent;
        }
        
        // Default to 0 if at top
        return 0;
    }
    
    /// <summary>
    /// Sets scroll position using percentage (0-100).
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="percent">Target scroll percentage (0-100).</param>
    protected virtual void SetScrollPositionCore(IMauiElement element, double percent)
    {
        var current = GetScrollPositionCore(element) ?? 0;
        var diff = percent - current;
        
        if (Math.Abs(diff) < 1) return; // Already at target
        
        // Estimate scroll amount needed
        var rect = element.Rect;
        var scrollAmount = (int)(rect.Height * (diff / 100.0));
        
        ScrollByCore(element, 0, scrollAmount);
    }
    
    /// <summary>
    /// Checks if can scroll down.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if can scroll down, false otherwise.</returns>
    protected virtual bool? CanScrollDownCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        var scrollPercent = GetScrollPositionCore(element);
        if (scrollPercent.HasValue)
        {
            return scrollPercent.Value < 100;
        }
        
        // Check scroll pattern
        var canScroll = element.GetAttribute("Scroll.VerticallyScrollable");
        if (!string.IsNullOrEmpty(canScroll))
        {
            return canScroll.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        return true; // Assume scrollable by default
    }
    
    /// <summary>
    /// Checks if can scroll up.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if can scroll up, false otherwise.</returns>
    protected virtual bool? CanScrollUpCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        var scrollPercent = GetScrollPositionCore(element);
        if (scrollPercent.HasValue)
        {
            return scrollPercent.Value > 0;
        }
        
        return true; // Assume scrollable by default
    }
    
    /// <summary>
    /// Performs swipe up gesture (scrolls content down).
    /// </summary>
    /// <param name="element">The element to swipe on.</param>
    protected virtual void SwipeUpCore(IMauiElement element)
    {
        var rect = element.Rect;
        var centerX = rect.X + rect.Width / 2;
        var startY = rect.Y + (int)(rect.Height * 0.8);
        var endY = rect.Y + (int)(rect.Height * 0.2);
        
        element.Swipe(centerX, startY, centerX, endY);
    }
    
    /// <summary>
    /// Performs swipe down gesture (scrolls content up).
    /// </summary>
    /// <param name="element">The element to swipe on.</param>
    protected virtual void SwipeDownCore(IMauiElement element)
    {
        var rect = element.Rect;
        var centerX = rect.X + rect.Width / 2;
        var startY = rect.Y + (int)(rect.Height * 0.2);
        var endY = rect.Y + (int)(rect.Height * 0.8);
        
        element.Swipe(centerX, startY, centerX, endY);
    }
    
    /// <summary>
    /// Tries to find element within the scrollable container.
    /// Override for specific container logic.
    /// </summary>
    /// <param name="locator">The locator to find.</param>
    /// <returns>The element if found, null otherwise.</returns>
    protected virtual IMauiElement? TryFindElementInContainer(Locator locator)
    {
        // Default: use scope to find element
        try
        {
            return MauiScope.TryFindElement(locator);
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region WaitScrollPosition
    
    /// <inheritdoc />
    public bool WaitScrollPosition(double? expected, double tolerance = 1.0, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        return Poll(
            () =>
            {
                var actual = GetScrollPosition();
                if (actual == null) return false;
                return Math.Abs(actual.Value - expected.Value) <= tolerance;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
    
    #region AssertScrollPosition
    
    /// <inheritdoc />
    public TScope AssertScrollPosition(double? expected, double tolerance = 1.0, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertScrollPosition), expected, () =>
        {
            WaitScrollPosition(expected, tolerance, timeoutMs);
            return GetScrollPosition();
        },
        (actual, exp) =>
        {
            if (actual == null || exp == null) return actual == exp;
            return Math.Abs(actual.Value - exp.Value) <= tolerance;
        },
        message ?? $"Expected scroll position '{expected}' (±{tolerance}). Locator: {Locator}");
    }
    
    #endregion
}
