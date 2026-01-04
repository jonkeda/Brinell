using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// MediaElement control for MAUI.
/// </summary>
public class MediaElementControl : MediaElementControlBase
{
    /// <summary>
    /// Creates a new MediaElement control.
    /// </summary>
    public MediaElementControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new MediaElement control using AutomationId.
    /// </summary>
    public MediaElementControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
