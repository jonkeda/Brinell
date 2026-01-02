using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms ComboBox control wrapper.
/// Inherits from SelectorControlBase which provides GetSelectedItem, GetSelectedIndex, GetItems, GetItemCount, SelectByText, SelectByIndex, WaitSelected, AssertSelectedItem.
/// </summary>
public class ComboBoxControl : SelectorControlBase, ISelectorControl
{
    public ComboBoxControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ComboBoxControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ComboBoxControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get selected item text (convenience alias for GetSelectedItem).
    /// </summary>
    public string GetSelectedText()
    {
        return GetSelectedItem();
    }

    /// <summary>
    /// Assert selected text equals expected (convenience alias for AssertSelectedItem).
    /// </summary>
    public void AssertSelectedText(string expected, string? message = null)
    {
        AssertSelectedItem(expected);
    }
}
