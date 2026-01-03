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
    /// Handles different attribute names for Windows/Android/iOS platforms.
    /// </summary>
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element == null) return false;

        // Windows UIA uses Toggle.ToggleState ("1" = checked, "0" = unchecked)
        var toggleState = element.GetAttribute("Toggle.ToggleState");
        if (toggleState != null)
        {
            return toggleState == "1" || toggleState.Equals("On", StringComparison.OrdinalIgnoreCase);
        }

        // Try standard checked attribute (Android/iOS)
        var checkedAttr = element.GetAttribute("checked");
        if (checkedAttr != null)
        {
            return checkedAttr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // Try IsChecked for MAUI CheckBox
        var isChecked = element.GetAttribute("IsChecked");
        if (isChecked != null)
        {
            return isChecked.Equals("true", StringComparison.OrdinalIgnoreCase) || isChecked == "1";
        }

        return false;
    }
}
