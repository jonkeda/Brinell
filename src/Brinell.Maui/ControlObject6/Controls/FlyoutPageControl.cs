using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// FlyoutPage control for MAUI.
/// </summary>
public class FlyoutPageControl : FlyoutControlBase
{
    /// <summary>
    /// Creates a new FlyoutPage control.
    /// </summary>
    public FlyoutPageControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new FlyoutPage control using AutomationId.
    /// </summary>
    public FlyoutPageControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
