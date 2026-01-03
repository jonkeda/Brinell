using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms RichTextBox control wrapper.
/// Uses shared TextControlBase for FlaUI integration.
/// </summary>
public class RichTextBoxControl : TextControlBase, ITextBox
{
    public RichTextBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a rich textbox control that searches within a container element.
    /// </summary>
    public RichTextBoxControl(FlaUITestContext context, PageBase? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public RichTextBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Get the text, trimming trailing newlines that WinForms RichTextBox adds.
    /// </summary>
    public override string GetText()
    {
        return base.GetText().TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Get the rich text content (alias for GetText with newline trimming).
    /// </summary>
    public virtual string GetContent()
    {
        return GetText();
    }

    /// <summary>
    /// Set the rich text content.
    /// </summary>
    public virtual void SetContent(string content)
    {
        SetText(content);
    }

    /// <summary>
    /// Append text to the existing content.
    /// </summary>
    public override void Append(string text)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Append", $"Element '{AutomationId}' not visible for append.");
        }
        
        element!.Focus();
        
        // Move to end and type
        var textBox = element.AsTextBox();
        if (textBox != null)
        {
            var existingText = textBox.Text ?? "";
            textBox.Text = existingText + text;
        }
        LogAction("Append", text);
    }
}
