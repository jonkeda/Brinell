using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Frame control for MAUI (deprecated, use Border instead).
/// </summary>
public class FrameControl : ContainerControlBase
{
    /// <summary>
    /// Creates a new Frame control.
    /// </summary>
    public FrameControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Frame control using AutomationId.
    /// </summary>
    public FrameControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
}
