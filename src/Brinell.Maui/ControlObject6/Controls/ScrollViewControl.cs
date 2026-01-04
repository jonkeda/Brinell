using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// ScrollView control for MAUI.
/// </summary>
public class ScrollViewControl : ScrollViewControlBase
{
    /// <summary>
    /// Creates a new ScrollView control.
    /// </summary>
    public ScrollViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new ScrollView control using AutomationId.
    /// </summary>
    public ScrollViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
