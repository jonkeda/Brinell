using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI base class for clickable content controls (buttons, labels).
/// </summary>
public abstract class ContentControlBase : ControlBase, IContentControl
{
    protected ContentControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ContentControlBase(AppiumTestContext context, string automationId)
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
    /// Double-click (tap twice on mobile).
    /// </summary>
    public virtual void DoubleClick()
    {
        LogAction("DoubleClick");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for double-click.");
        element.Click();
        Thread.Sleep(100);
        element.Click();
    }

    /// <summary>
    /// Right-click (long press on mobile).
    /// </summary>
    public virtual void RightClick()
    {
        LogAction("RightClick");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for right-click.");
        // On mobile, right-click is typically a long press
        // For now, just click as a fallback
        element.Click();
    }

    /// <summary>
    /// Long press (right-click equivalent on mobile).
    /// </summary>
    public virtual void LongPress()
    {
        LogAction("LongPress");
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for long press.");
        // Long press implementation would use touch actions
        // For now, just click as a fallback
        element.Click();
    }
}
