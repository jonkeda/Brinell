using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Shell control for MAUI (includes flyout and tab bar).
/// </summary>
public class ShellControl : FlyoutControlBase
{
    /// <summary>
    /// Creates a new Shell control.
    /// </summary>
    public ShellControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Shell control using AutomationId.
    /// </summary>
    public ShellControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc/>
    protected override string FlyoutItemXPath => ".//*[@ClassName='ShellFlyoutItem' or @ClassName='FlyoutItem' or contains(@ClassName,'Shell')]";
}
