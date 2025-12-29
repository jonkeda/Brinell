using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Label control wrapper.
/// Corresponds to MAUI Label and Span controls.
/// Inherits from ContentControlBase for tap/click support (gesture recognizers).
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

    /// <summary>
    /// Tap the label (alias for Click - for labels with gesture recognizers).
    /// </summary>
    public void Tap() => Click();
}
