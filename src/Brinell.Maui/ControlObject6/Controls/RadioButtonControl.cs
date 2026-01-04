using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Control object for MAUI RadioButton elements.
/// Note: RadioButton can only be checked, not unchecked directly.
/// </summary>
public class RadioButtonControl : ToggleControlBase
{
    /// <summary>
    /// Creates a new RadioButtonControl.
    /// </summary>
    public RadioButtonControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new RadioButtonControl using AutomationId.
    /// </summary>
    public RadioButtonControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element is null) return false;

        // RadioButton uses IsChecked or SelectionItem.IsSelected
        var isChecked = element.GetAttribute("IsChecked");
        if (isChecked is not null)
            return isChecked.Equals("True", StringComparison.OrdinalIgnoreCase);

        var isSelected = element.GetAttribute("SelectionItem.IsSelected");
        return isSelected?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Selects this radio button. Alias for Check.
    /// </summary>
    public void Select(int? timeoutMs = null)
    {
        Log("Select()");
        Check(timeoutMs);
    }

    /// <inheritdoc />
    /// <remarks>
    /// RadioButtons cannot be unchecked directly - select another RadioButton in the group instead.
    /// This method is a no-op for RadioButton.
    /// </remarks>
    public override void Uncheck(int? timeoutMs = null)
    {
        Log("Uncheck() - RadioButton cannot be unchecked directly");
        // RadioButtons cannot be unchecked directly
        // User must select another radio button in the group
    }
}
