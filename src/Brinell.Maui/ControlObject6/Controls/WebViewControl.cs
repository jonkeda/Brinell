using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// WebView control for MAUI.
/// </summary>
public class WebViewControl : WebViewControlBase
{
    /// <summary>
    /// Creates a new WebView control.
    /// </summary>
    public WebViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new WebView control using AutomationId.
    /// </summary>
    public WebViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
