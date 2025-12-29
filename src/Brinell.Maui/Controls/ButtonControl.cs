using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Button control wrapper.
/// Inherits from ContentControlBase for click support.
/// </summary>
public class ButtonControl : ContentControlBase
{
    public ButtonControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ButtonControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }
}
