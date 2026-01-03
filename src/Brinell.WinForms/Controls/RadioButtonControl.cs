using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms RadioButton control wrapper.
/// Uses shared ToggleControlBase for FlaUI integration.
/// </summary>
public class RadioButtonControl : ToggleControlBase, ICheckBox
{
    public RadioButtonControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a radio button control that searches within a container element.
    /// Use this for radio buttons inside group boxes or panels.
    /// </summary>
    public RadioButtonControl(FlaUITestContext context, PageBase? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public RadioButtonControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Check if radio button is selected (immediate, no wait).
    /// </summary>
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element != null)
        {
            var radioButton = element.AsRadioButton();
            return radioButton?.IsChecked == true;
        }
        return false;
    }

    /// <summary>
    /// Select this radio button.
    /// Note: Unlike checkbox, radio button only supports selection, not toggle.
    /// </summary>
    public override void Toggle()
    {
        Check();
    }

    /// <summary>
    /// Select this radio button (check it).
    /// </summary>
    public override void Check()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Check", $"Element '{AutomationId}' not visible for selection.");
            return;
        }
        
        var radioButton = element.AsRadioButton();
        if (radioButton != null)
        {
            radioButton.Click();
        }
        else
        {
            element.Click();
        }
        LogAction("Check");
    }

    /// <summary>
    /// Uncheck is not supported for radio buttons.
    /// Radio buttons can only be deselected by selecting another in the same group.
    /// </summary>
    public override void Uncheck()
    {
        // Radio buttons cannot be unchecked directly
        LogDebug("RadioButton.Uncheck - not supported, select another radio button in the group instead");
    }

    /// <summary>
    /// Get radio button label text.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element != null)
        {
            var radioButton = element.AsRadioButton();
            return radioButton?.Name ?? string.Empty;
        }
        return string.Empty;
    }
}
