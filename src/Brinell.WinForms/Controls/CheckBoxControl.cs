using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms CheckBox control wrapper.
/// Inherits from ToggleControlBase which provides IsChecked, SetChecked, Check, Uncheck, WaitChecked, AssertChecked, AssertUnchecked.
/// </summary>
public class CheckBoxControl : ToggleControlBase, ICheckBox
{
    public CheckBoxControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public CheckBoxControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public CheckBoxControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get checkbox text/label.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        return element?.Name ?? string.Empty;
    }

    /// <summary>
    /// Toggle the checkbox (convenience method for SetChecked(!IsChecked())).
    /// </summary>
    public void Toggle()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Toggle", $"Element '{AutomationId}' not visible.");
        }
        
        var checkbox = element!.AsCheckBox();
        checkbox.Toggle();
        LogAction("Toggle");
    }

    /// <summary>
    /// Wait for checkbox to be checked (convenience method for WaitChecked(true)).
    /// </summary>
    public bool WaitForChecked(int? timeoutMs = null)
    {
        return WaitChecked(true, timeoutMs);
    }
}
