using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Toolbar control for MAUI.
/// </summary>
public class ToolbarControl : ToolbarControlBase
{
    /// <summary>
    /// Creates a new Toolbar control.
    /// </summary>
    public ToolbarControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Toolbar control using AutomationId.
    /// </summary>
    public ToolbarControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
