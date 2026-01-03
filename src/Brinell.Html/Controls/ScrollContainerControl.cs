using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML/Selenium control for scrollable container elements.
/// Provides scroll functionality for divs or other containers with overflow.
/// </summary>
public class ScrollContainerControl : ScrollableControlBase
{
    public ScrollContainerControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ScrollContainerControl(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ScrollContainerControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the visible height of the container (clientHeight).
    /// </summary>
    public virtual double GetVisibleHeight()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var result = _context.ExecuteScript("return arguments[0].clientHeight;", element);
        return Convert.ToDouble(result);
    }

    /// <summary>
    /// Get the visible width of the container (clientWidth).
    /// </summary>
    public virtual double GetVisibleWidth()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var result = _context.ExecuteScript("return arguments[0].clientWidth;", element);
        return Convert.ToDouble(result);
    }

    /// <summary>
    /// Check if the container has vertical scroll capability.
    /// </summary>
    public virtual bool CanScrollVertically()
    {
        return GetScrollHeight() > GetVisibleHeight();
    }

    /// <summary>
    /// Check if the container has horizontal scroll capability.
    /// </summary>
    public virtual bool CanScrollHorizontally()
    {
        return GetScrollWidth() > GetVisibleWidth();
    }

    /// <summary>
    /// Scroll to a specific vertical position.
    /// </summary>
    public virtual void ScrollToPosition(double top)
    {
        LogAction("ScrollToPosition", top.ToString());
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript($"arguments[0].scrollTop = {top};", element);
    }

    /// <summary>
    /// Scroll to a specific horizontal position.
    /// </summary>
    public virtual void ScrollToHorizontalPosition(double left)
    {
        LogAction("ScrollToHorizontalPosition", left.ToString());
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript($"arguments[0].scrollLeft = {left};", element);
    }

    /// <summary>
    /// Scroll by page (one viewport height) down.
    /// </summary>
    public virtual void PageDown()
    {
        LogAction("PageDown");
        ScrollDown((int)GetVisibleHeight());
    }

    /// <summary>
    /// Scroll by page (one viewport height) up.
    /// </summary>
    public virtual void PageUp()
    {
        LogAction("PageUp");
        ScrollUp((int)GetVisibleHeight());
    }

    /// <summary>
    /// Scroll smoothly to a position using CSS smooth scroll behavior.
    /// </summary>
    public virtual void SmoothScrollTo(double top)
    {
        LogAction("SmoothScrollTo", top.ToString());
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript(
            $"arguments[0].scrollTo({{ top: {top}, behavior: 'smooth' }});",
            element);
    }

    /// <summary>
    /// Wait for scroll to complete (no more position changes).
    /// </summary>
    public virtual bool WaitForScrollComplete(int timeoutMs = 2000)
    {
        double lastPosition = GetScrollTop();
        return _context.WaitFor(() =>
        {
            System.Threading.Thread.Sleep(100);
            double currentPosition = GetScrollTop();
            bool stopped = Math.Abs(currentPosition - lastPosition) < 1;
            lastPosition = currentPosition;
            return stopped;
        }, timeoutMs, "scroll complete");
    }
}
