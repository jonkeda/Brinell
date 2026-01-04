using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Expander control for MAUI.
/// </summary>
public class ExpanderControl : ExpanderControlBase
{
    /// <summary>
    /// Creates a new Expander control.
    /// </summary>
    public ExpanderControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Expander control using AutomationId.
    /// </summary>
    public ExpanderControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
