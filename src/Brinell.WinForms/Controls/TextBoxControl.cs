using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TextBox control wrapper.
/// Uses shared TextControlBase for FlaUI integration.
/// </summary>
public class TextBoxControl : TextControlBase, ITextBox
{
    public TextBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a textbox control that searches within a container element.
    /// Use this for textboxes inside list items or repeated templates.
    /// </summary>
    public TextBoxControl(FlaUITestContext context, PageBase? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TextBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }
}
