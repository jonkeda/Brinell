using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright control for scrollable container elements.
/// Provides scroll functionality for divs or other containers with overflow.
/// </summary>
public class ScrollContainerControl : ScrollableControlBase
{
    public ScrollContainerControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ScrollContainerControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ScrollContainerControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the visible height of the container (clientHeight).
    /// </summary>
    public virtual double GetVisibleHeight()
    {
        return GetVisibleHeightAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the visible height of the container asynchronously.
    /// </summary>
    public virtual async Task<double> GetVisibleHeightAsync()
    {
        var element = GetLocator();
        var count = await element.CountAsync();
        if (count == 0) return 0;
        
        return await element.EvaluateAsync<double>("el => el.clientHeight");
    }

    /// <summary>
    /// Get the visible width of the container (clientWidth).
    /// </summary>
    public virtual double GetVisibleWidth()
    {
        return GetVisibleWidthAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the visible width of the container asynchronously.
    /// </summary>
    public virtual async Task<double> GetVisibleWidthAsync()
    {
        var element = GetLocator();
        var count = await element.CountAsync();
        if (count == 0) return 0;
        
        return await element.EvaluateAsync<double>("el => el.clientWidth");
    }

    /// <summary>
    /// Check if the container has vertical scroll capability.
    /// </summary>
    public virtual bool CanScrollVertically()
    {
        return GetScrollHeight() > GetVisibleHeight();
    }

    /// <summary>
    /// Check if the container has vertical scroll capability asynchronously.
    /// </summary>
    public virtual async Task<bool> CanScrollVerticallyAsync()
    {
        var scrollHeight = await GetScrollHeightAsync();
        var visibleHeight = await GetVisibleHeightAsync();
        return scrollHeight > visibleHeight;
    }

    /// <summary>
    /// Check if the container has horizontal scroll capability.
    /// </summary>
    public virtual bool CanScrollHorizontally()
    {
        return GetScrollWidth() > GetVisibleWidth();
    }

    /// <summary>
    /// Check if the container has horizontal scroll capability asynchronously.
    /// </summary>
    public virtual async Task<bool> CanScrollHorizontallyAsync()
    {
        var scrollWidth = await GetScrollWidthAsync();
        var visibleWidth = await GetVisibleWidthAsync();
        return scrollWidth > visibleWidth;
    }

    /// <summary>
    /// Scroll to a specific vertical position.
    /// </summary>
    public virtual void ScrollToPosition(double top)
    {
        ScrollToPositionAsync(top).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll to a specific vertical position asynchronously.
    /// </summary>
    public virtual async Task ScrollToPositionAsync(double top)
    {
        LogAction("ScrollToPosition", top.ToString());
        
        var element = GetLocator();
        await element.EvaluateAsync($"el => el.scrollTop = {top}");
    }

    /// <summary>
    /// Scroll to a specific horizontal position.
    /// </summary>
    public virtual void ScrollToHorizontalPosition(double left)
    {
        ScrollToHorizontalPositionAsync(left).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll to a specific horizontal position asynchronously.
    /// </summary>
    public virtual async Task ScrollToHorizontalPositionAsync(double left)
    {
        LogAction("ScrollToHorizontalPosition", left.ToString());
        
        var element = GetLocator();
        await element.EvaluateAsync($"el => el.scrollLeft = {left}");
    }

    /// <summary>
    /// Scroll by page (one viewport height) down.
    /// </summary>
    public virtual void PageDown()
    {
        PageDownAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll by page (one viewport height) down asynchronously.
    /// </summary>
    public virtual async Task PageDownAsync()
    {
        LogAction("PageDown");
        var height = await GetVisibleHeightAsync();
        await ScrollDownAsync((int)height);
    }

    /// <summary>
    /// Scroll by page (one viewport height) up.
    /// </summary>
    public virtual void PageUp()
    {
        PageUpAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll by page (one viewport height) up asynchronously.
    /// </summary>
    public virtual async Task PageUpAsync()
    {
        LogAction("PageUp");
        var height = await GetVisibleHeightAsync();
        await ScrollUpAsync((int)height);
    }

    /// <summary>
    /// Scroll smoothly to a position using CSS smooth scroll behavior.
    /// </summary>
    public virtual void SmoothScrollTo(double top)
    {
        SmoothScrollToAsync(top).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll smoothly to a position asynchronously.
    /// </summary>
    public virtual async Task SmoothScrollToAsync(double top)
    {
        LogAction("SmoothScrollTo", top.ToString());
        
        var element = GetLocator();
        await element.EvaluateAsync($"el => el.scrollTo({{ top: {top}, behavior: 'smooth' }})");
    }

    /// <summary>
    /// Wait for scroll to complete (no more position changes).
    /// </summary>
    public virtual bool WaitForScrollComplete(int timeoutMs = 2000)
    {
        return WaitForScrollCompleteAsync(timeoutMs).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Wait for scroll to complete asynchronously.
    /// </summary>
    public virtual async Task<bool> WaitForScrollCompleteAsync(int timeoutMs = 2000)
    {
        double lastPosition = await GetScrollTopAsync();
        return await _context.WaitForAsync(async () =>
        {
            await Task.Delay(100);
            double currentPosition = await GetScrollTopAsync();
            bool stopped = Math.Abs(currentPosition - lastPosition) < 1;
            lastPosition = currentPosition;
            return stopped;
        }, timeoutMs, "scroll complete");
    }
}
