using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF ComboBox control wrapper.
/// Uses WPF-specific SelectorControlBase for FlaUI integration.
/// </summary>
public class ComboBoxControl : SelectorControlBase, ISelectorControl
{
    public ComboBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ComboBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get selected item text (from base class).
    /// </summary>
    public override string GetText()
    {
        return GetSelectedText() ?? string.Empty;
    }

    /// <summary>
    /// Expand/Open the dropdown.
    /// </summary>
    public virtual void Open()
    {
        CheckVisible();
        var comboBox = GetComboBox();
        comboBox?.Expand();
        LogAction("Open");
    }

    /// <summary>
    /// Collapse/Close the dropdown.
    /// </summary>
    public virtual void Close()
    {
        var comboBox = GetComboBox();
        comboBox?.Collapse();
        LogAction("Close");
    }
}
