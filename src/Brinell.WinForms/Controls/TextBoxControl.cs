using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TextBox control wrapper.
/// Inherits from InputControlBase which provides Clear, AppendText, IsReadOnly, GetTextLength, and WaitForTextEquals.
/// </summary>
public class TextBoxControl : InputControlBase, ITextBox
{
    public TextBoxControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a textbox control that searches within a container element.
    /// Use this for textboxes inside list items or repeated templates.
    /// </summary>
    public TextBoxControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TextBoxControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Enter text into the textbox (appends to existing text).
    /// </summary>
    public override void Enter(string text)
    {
        AppendText(text);
    }

    /// <summary>
    /// Clear the textbox and enter text.
    /// </summary>
    public override void ClearAndEnter(string text)
    {
        Clear();
        Enter(text);
    }

    /// <summary>
    /// Append text to the textbox (convenience method for AppendText from InputControlBase).
    /// </summary>
    public override void Append(string text)
    {
        AppendText(text);
    }
}
