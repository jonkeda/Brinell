using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// HTML/Selenium base class for scrollable container controls.
/// Uses JavaScript scrolling for web platform compatibility.
/// </summary>
public abstract class ScrollableControlBase : ControlBase, IScrollableControl
{
    protected ScrollableControlBase(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ScrollableControlBase(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ScrollableControlBase(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Scroll until the element with the specified automation ID is visible.
    /// </summary>
    public virtual void ScrollToElement(string automationId)
    {
        LogAction("ScrollToElement", automationId);
        
        var container = FindElement();
        if (container == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        // Find the target element within the container
        var target = _context.Driver.FindElementDirect(automationId);
        if (target == null)
            throw new InvalidOperationException($"Element '{automationId}' not found for scroll.");
        
        // Use JavaScript to scroll the element into view
        _context.ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", target);
        
        // Wait for element to be visible
        _context.WaitFor(() => target.Displayed, 2000, $"element '{automationId}' visible after scroll");
    }

    /// <summary>
    /// Scroll to the top of the content.
    /// </summary>
    public virtual void ScrollToTop()
    {
        LogAction("ScrollToTop");
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript("arguments[0].scrollTop = 0;", element);
    }

    /// <summary>
    /// Scroll to the bottom of the content.
    /// </summary>
    public virtual void ScrollToBottom()
    {
        LogAction("ScrollToBottom");
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript("arguments[0].scrollTop = arguments[0].scrollHeight;", element);
    }

    /// <summary>
    /// Scroll up by the specified distance.
    /// </summary>
    public virtual void ScrollUp(int distance = 100)
    {
        LogAction("ScrollUp", distance.ToString());
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript($"arguments[0].scrollTop -= {distance};", element);
    }

    /// <summary>
    /// Scroll down by the specified distance.
    /// </summary>
    public virtual void ScrollDown(int distance = 100)
    {
        LogAction("ScrollDown", distance.ToString());
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript($"arguments[0].scrollTop += {distance};", element);
    }

    /// <summary>
    /// Scroll left by the specified distance.
    /// </summary>
    public virtual void ScrollLeft(int distance = 100)
    {
        LogAction("ScrollLeft", distance.ToString());
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript($"arguments[0].scrollLeft -= {distance};", element);
    }

    /// <summary>
    /// Scroll right by the specified distance.
    /// </summary>
    public virtual void ScrollRight(int distance = 100)
    {
        LogAction("ScrollRight", distance.ToString());
        
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Container '{AutomationId}' not found.");
        
        _context.ExecuteScript($"arguments[0].scrollLeft += {distance};", element);
    }

    /// <summary>
    /// Get the current vertical scroll position.
    /// </summary>
    public virtual double GetScrollTop()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var result = _context.ExecuteScript("return arguments[0].scrollTop;", element);
        return Convert.ToDouble(result);
    }

    /// <summary>
    /// Get the current horizontal scroll position.
    /// </summary>
    public virtual double GetScrollLeft()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var result = _context.ExecuteScript("return arguments[0].scrollLeft;", element);
        return Convert.ToDouble(result);
    }

    /// <summary>
    /// Get the total scrollable height.
    /// </summary>
    public virtual double GetScrollHeight()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var result = _context.ExecuteScript("return arguments[0].scrollHeight;", element);
        return Convert.ToDouble(result);
    }

    /// <summary>
    /// Get the total scrollable width.
    /// </summary>
    public virtual double GetScrollWidth()
    {
        var element = FindElement();
        if (element == null) return 0;
        
        var result = _context.ExecuteScript("return arguments[0].scrollWidth;", element);
        return Convert.ToDouble(result);
    }

    /// <summary>
    /// Check if the container is at the top.
    /// </summary>
    public virtual bool IsAtTop()
    {
        return GetScrollTop() <= 0;
    }

    /// <summary>
    /// Check if the container is at the bottom.
    /// </summary>
    public virtual bool IsAtBottom()
    {
        var element = FindElement();
        if (element == null) return false;
        
        var result = _context.ExecuteScript(
            "return arguments[0].scrollTop + arguments[0].clientHeight >= arguments[0].scrollHeight - 1;",
            element);
        return Convert.ToBoolean(result);
    }
}
