using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF CheckBox control wrapper.
/// Uses WPF-specific ToggleControlBase for FlaUI integration.
/// </summary>
public class CheckBoxControl : ToggleControlBase, ICheckBox
{
    public CheckBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a checkbox control that searches within a container element.
    /// Use this for checkboxes inside list items or repeated templates.
    /// </summary>
    public CheckBoxControl(FlaUITestContext context, PageBase? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public CheckBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Check if checkbox is checked (immediate, no wait).
    /// </summary>
    public override bool IsChecked()
    {
        var element = FindElement();
        if (element != null)
        {
            var checkBox = element.AsCheckBox();
            return checkBox?.IsChecked == true;
        }
        return false;
    }

    /// <summary>
    /// Toggle the checkbox using FlaUI's native toggle pattern.
    /// This is more reliable than Click() for WPF data-bound checkboxes.
    /// </summary>
    public override void Toggle()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Toggle", $"Element '{AutomationId}' not visible for toggle.");
            return; // Never reached, but satisfies null analysis
        }
        
        var checkBox = element.AsCheckBox();
        if (checkBox != null)
        {
            // Use FlaUI's native toggle which works better with WPF bindings
            checkBox.Toggle();
        }
        else
        {
            // Fallback to click
            element.Click();
        }
        LogAction("Toggle");
    }

    /// <summary>
    /// Check if checkbox is in indeterminate state (three-state checkbox).
    /// </summary>
    public bool IsIndeterminate()
    {
        var element = FindElement();
        if (element != null)
        {
            var checkBox = element.AsCheckBox();
            return checkBox?.IsChecked == null;
        }
        return false;
    }

    /// <summary>
    /// Get checkbox label text.
    /// </summary>
    public override string GetText()
    {
        var element = FindElement();
        if (element != null)
        {
            var checkBox = element.AsCheckBox();
            return checkBox?.Name ?? string.Empty;
        }
        return string.Empty;
    }
}
