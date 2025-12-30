using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Label control wrapper.
/// Corresponds to MAUI Label and Span controls.
/// Inherits from ContentControlBase for tap/click support (gesture recognizers).
/// Tap(), Click(), DoubleTap(), and LongPress() methods are inherited from ControlBase.
/// </summary>
public class LabelControl : ContentControlBase
{
    public LabelControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public LabelControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }
}
