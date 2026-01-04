using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Control object for MAUI CheckBox elements.
/// </summary>
public class CheckBoxControl : ToggleControlBase
{
    /// <summary>
    /// Creates a new CheckBoxControl.
    /// </summary>
    public CheckBoxControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new CheckBoxControl using AutomationId.
    /// </summary>
    public CheckBoxControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element is null) return false;

        // CheckBox uses IsChecked attribute
        var isChecked = element.GetAttribute("IsChecked");
        return isChecked?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
