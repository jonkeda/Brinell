using System.Collections.Generic;
using System.Linq;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms ListBox control wrapper.
/// Inherits from SelectorControlBase which provides GetSelectedItem, GetSelectedIndex, GetItems, GetItemCount, SelectByText, SelectByIndex, WaitSelected, AssertSelectedItem.
/// </summary>
public class ListBoxControl : SelectorControlBase
{
    public ListBoxControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ListBoxControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ListBoxControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }
}
