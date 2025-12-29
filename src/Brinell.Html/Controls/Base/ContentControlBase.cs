using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// HTML/Selenium base class for clickable content controls (buttons, links, labels).
/// </summary>
public abstract class ContentControlBase : ControlBase, IContentControl
{
    protected ContentControlBase(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ContentControlBase(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Click the control.
    /// </summary>
    public virtual void Click()
    {
        LogAction("Click");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for click.");
        element.Click();
    }

    /// <summary>
    /// Double-click the control.
    /// </summary>
    public virtual void DoubleClick()
    {
        LogAction("DoubleClick");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for double-click.");
        
        var actions = new OpenQA.Selenium.Interactions.Actions(_context.Driver.Driver);
        actions.DoubleClick(element).Perform();
    }

    /// <summary>
    /// Right-click the control.
    /// </summary>
    public virtual void RightClick()
    {
        LogAction("RightClick");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for right-click.");
        
        var actions = new OpenQA.Selenium.Interactions.Actions(_context.Driver.Driver);
        actions.ContextClick(element).Perform();
    }
    
    /// <summary>
    /// Hover over the control.
    /// </summary>
    public virtual void Hover()
    {
        LogAction("Hover");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for hover.");
        
        var actions = new OpenQA.Selenium.Interactions.Actions(_context.Driver.Driver);
        actions.MoveToElement(element).Perform();
    }
}
