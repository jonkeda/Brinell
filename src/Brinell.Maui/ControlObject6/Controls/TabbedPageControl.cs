using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// TabbedPage control for MAUI.
/// </summary>
public class TabbedPageControl : TabControlBase
{
    /// <summary>
    /// Creates a new TabbedPage control.
    /// </summary>
    public TabbedPageControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new TabbedPage control using AutomationId.
    /// </summary>
    public TabbedPageControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
