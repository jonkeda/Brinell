using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright base class for scrollable container controls.
/// Uses JavaScript scrolling for web platform compatibility.
/// </summary>
public abstract class ScrollableControlBase : ControlBase, IScrollableControl
{
    protected ScrollableControlBase(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ScrollableControlBase(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ScrollableControlBase(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Scroll until the element with the specified automation ID is visible.
    /// </summary>
    public virtual void ScrollToElement(string automationId)
    {
        ScrollToElementAsync(automationId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll until the element with the specified automation ID is visible asynchronously.
    /// </summary>
    public virtual async Task ScrollToElementAsync(string automationId)
    {
        LogAction("ScrollToElement", automationId);
        
        // Build selector for target element
        string targetSelector;
        if (automationId.StartsWith('#') || automationId.StartsWith('.') || automationId.StartsWith('['))
        {
            targetSelector = automationId;
        }
        else
        {
            targetSelector = $"[data-automation-id='{automationId}'], [id='{automationId}']";
        }
        
        var target = _context.Page.Locator(targetSelector).First;
        await target.ScrollIntoViewIfNeededAsync();
    }

    /// <summary>
    /// Scroll to the top of the content.
    /// </summary>
    public virtual void ScrollToTop()
    {
        ScrollToTopAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll to the top of the content asynchronously.
    /// </summary>
    public virtual async Task ScrollToTopAsync()
    {
        LogAction("ScrollToTop");
        
        var element = GetLocator();
        await element.EvaluateAsync("el => el.scrollTop = 0");
    }

    /// <summary>
    /// Scroll to the bottom of the content.
    /// </summary>
    public virtual void ScrollToBottom()
    {
        ScrollToBottomAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll to the bottom of the content asynchronously.
    /// </summary>
    public virtual async Task ScrollToBottomAsync()
    {
        LogAction("ScrollToBottom");
        
        var element = GetLocator();
        await element.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
    }

    /// <summary>
    /// Scroll up by the specified distance.
    /// </summary>
    public virtual void ScrollUp(int distance = 100)
    {
        ScrollUpAsync(distance).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll up by the specified distance asynchronously.
    /// </summary>
    public virtual async Task ScrollUpAsync(int distance = 100)
    {
        LogAction("ScrollUp", distance.ToString());
        
        var element = GetLocator();
        await element.EvaluateAsync($"el => el.scrollTop -= {distance}");
    }

    /// <summary>
    /// Scroll down by the specified distance.
    /// </summary>
    public virtual void ScrollDown(int distance = 100)
    {
        ScrollDownAsync(distance).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll down by the specified distance asynchronously.
    /// </summary>
    public virtual async Task ScrollDownAsync(int distance = 100)
    {
        LogAction("ScrollDown", distance.ToString());
        
        var element = GetLocator();
        await element.EvaluateAsync($"el => el.scrollTop += {distance}");
    }

    /// <summary>
    /// Scroll left by the specified distance.
    /// </summary>
    public virtual void ScrollLeft(int distance = 100)
    {
        ScrollLeftAsync(distance).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll left by the specified distance asynchronously.
    /// </summary>
    public virtual async Task ScrollLeftAsync(int distance = 100)
    {
        LogAction("ScrollLeft", distance.ToString());
        
        var element = GetLocator();
        await element.EvaluateAsync($"el => el.scrollLeft -= {distance}");
    }

    /// <summary>
    /// Scroll right by the specified distance.
    /// </summary>
    public virtual void ScrollRight(int distance = 100)
    {
        ScrollRightAsync(distance).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Scroll right by the specified distance asynchronously.
    /// </summary>
    public virtual async Task ScrollRightAsync(int distance = 100)
    {
        LogAction("ScrollRight", distance.ToString());
        
        var element = GetLocator();
        await element.EvaluateAsync($"el => el.scrollLeft += {distance}");
    }

    /// <summary>
    /// Get the current vertical scroll position.
    /// </summary>
    public virtual double GetScrollTop()
    {
        return GetScrollTopAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the current vertical scroll position asynchronously.
    /// </summary>
    public virtual async Task<double> GetScrollTopAsync()
    {
        var element = GetLocator();
        var count = await element.CountAsync();
        if (count == 0) return 0;
        
        return await element.EvaluateAsync<double>("el => el.scrollTop");
    }

    /// <summary>
    /// Get the current horizontal scroll position.
    /// </summary>
    public virtual double GetScrollLeft()
    {
        return GetScrollLeftAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the current horizontal scroll position asynchronously.
    /// </summary>
    public virtual async Task<double> GetScrollLeftAsync()
    {
        var element = GetLocator();
        var count = await element.CountAsync();
        if (count == 0) return 0;
        
        return await element.EvaluateAsync<double>("el => el.scrollLeft");
    }

    /// <summary>
    /// Get the total scrollable height.
    /// </summary>
    public virtual double GetScrollHeight()
    {
        return GetScrollHeightAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the total scrollable height asynchronously.
    /// </summary>
    public virtual async Task<double> GetScrollHeightAsync()
    {
        var element = GetLocator();
        var count = await element.CountAsync();
        if (count == 0) return 0;
        
        return await element.EvaluateAsync<double>("el => el.scrollHeight");
    }

    /// <summary>
    /// Get the total scrollable width.
    /// </summary>
    public virtual double GetScrollWidth()
    {
        return GetScrollWidthAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the total scrollable width asynchronously.
    /// </summary>
    public virtual async Task<double> GetScrollWidthAsync()
    {
        var element = GetLocator();
        var count = await element.CountAsync();
        if (count == 0) return 0;
        
        return await element.EvaluateAsync<double>("el => el.scrollWidth");
    }

    /// <summary>
    /// Check if the container is at the top.
    /// </summary>
    public virtual bool IsAtTop()
    {
        return GetScrollTop() <= 0;
    }

    /// <summary>
    /// Check if the container is at the top asynchronously.
    /// </summary>
    public virtual async Task<bool> IsAtTopAsync()
    {
        return await GetScrollTopAsync() <= 0;
    }

    /// <summary>
    /// Check if the container is at the bottom.
    /// </summary>
    public virtual bool IsAtBottom()
    {
        return IsAtBottomAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if the container is at the bottom asynchronously.
    /// </summary>
    public virtual async Task<bool> IsAtBottomAsync()
    {
        var element = GetLocator();
        var count = await element.CountAsync();
        if (count == 0) return false;
        
        return await element.EvaluateAsync<bool>(
            "el => el.scrollTop + el.clientHeight >= el.scrollHeight - 1");
    }
}
