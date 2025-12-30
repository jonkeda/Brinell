using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright base class for clickable content controls (buttons, links, labels).
/// </summary>
public abstract class ContentControlBase : ControlBase, IContentControl
{
    protected ContentControlBase(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ContentControlBase(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ContentControlBase(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Double-click the control.
    /// </summary>
    public virtual void DoubleClick()
    {
        LogAction("DoubleClick");
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for double-click.");
        element.DblClickAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Double-click the control asynchronously.
    /// </summary>
    public virtual async Task DoubleClickAsync()
    {
        LogAction("DoubleClick");
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for double-click.");
        await element.DblClickAsync();
    }

    /// <summary>
    /// Right-click the control.
    /// </summary>
    public virtual void RightClick()
    {
        LogAction("RightClick");
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for right-click.");
        element.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right })
            .GetAwaiter().GetResult();
    }

    /// <summary>
    /// Right-click the control asynchronously.
    /// </summary>
    public virtual async Task RightClickAsync()
    {
        LogAction("RightClick");
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for right-click.");
        await element.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
    }

    /// <summary>
    /// Hover over the control.
    /// </summary>
    public virtual void Hover()
    {
        LogAction("Hover");
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for hover.");
        element.HoverAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Hover over the control asynchronously.
    /// </summary>
    public virtual async Task HoverAsync()
    {
        LogAction("Hover");
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not visible for hover.");
        await element.HoverAsync();
    }
}
