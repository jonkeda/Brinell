using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// RefreshView control for MAUI (pull-to-refresh pattern).
/// </summary>
public class RefreshViewControl : RefreshViewControlBase
{
    /// <summary>
    /// Creates a new RefreshView control.
    /// </summary>
    public RefreshViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new RefreshView control using AutomationId.
    /// </summary>
    public RefreshViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
