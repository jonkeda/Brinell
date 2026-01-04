using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Button control implementation for MAUI.
/// Inherits virtual click capabilities from ClickableControlBase.
/// </summary>
public class ButtonControl : ClickableControlBase
{
    /// <summary>
    /// Creates a new button control.
    /// </summary>
    public ButtonControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new button control using AutomationId.
    /// </summary>
    public ButtonControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    // All click methods are inherited from ClickableControlBase as virtual methods
    // Override if MAUI-specific behavior is needed
}
