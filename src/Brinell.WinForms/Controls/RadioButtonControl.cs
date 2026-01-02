using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms RadioButton control wrapper.
/// Inherits from ToggleControlBase which provides IsChecked, SetChecked, Check, Uncheck, WaitChecked, AssertChecked, AssertUnchecked.
/// </summary>
public class RadioButtonControl : ToggleControlBase
{
    public RadioButtonControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public RadioButtonControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public RadioButtonControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if radio button is selected (alias for IsChecked from ToggleControlBase).
    /// </summary>
    public bool IsSelected()
    {
        return IsChecked();
    }

    /// <summary>
    /// Select the radio button (alias for Check from ToggleControlBase).
    /// </summary>
    public void Select()
    {
        Check();
    }
}
