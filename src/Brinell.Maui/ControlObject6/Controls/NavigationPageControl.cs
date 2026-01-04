using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// NavigationPage control for MAUI.
/// </summary>
public class NavigationPageControl : NavigationPageControlBase
{
    /// <summary>
    /// Creates a new NavigationPage control.
    /// </summary>
    public NavigationPageControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new NavigationPage control using AutomationId.
    /// </summary>
    public NavigationPageControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
