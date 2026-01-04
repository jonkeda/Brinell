using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// SwipeView control for MAUI.
/// </summary>
public class SwipeViewControl : SwipeViewControlBase
{
    /// <summary>
    /// Creates a new SwipeView control.
    /// </summary>
    public SwipeViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new SwipeView control using AutomationId.
    /// </summary>
    public SwipeViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
