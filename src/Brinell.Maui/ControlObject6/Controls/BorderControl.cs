using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Border control for MAUI.
/// </summary>
public class BorderControl : ContainerControlBase
{
    /// <summary>
    /// Creates a new Border control.
    /// </summary>
    public BorderControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Border control using AutomationId.
    /// </summary>
    public BorderControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
