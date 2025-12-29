using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI CheckBox control wrapper.
/// Inherits from ToggleControlBase for standard toggle behavior.
/// </summary>
public class CheckBoxControl : ToggleControlBase
{
    public CheckBoxControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public CheckBoxControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the checkbox is checked (immediate, no wait).
    /// </summary>
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element != null)
        {
            var checkedAttr = element.GetAttribute("checked");
            return checkedAttr?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        }
        return false;
    }
}
