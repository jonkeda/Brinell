using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls.Base;

/// <summary>
/// WPF base class for text input controls.
/// </summary>
public abstract class TextControlBase : ControlBase, ITextControl
{
    protected TextControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// </summary>
    protected TextControlBase(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected TextControlBase(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the TextBox pattern from the element.
    /// </summary>
    protected TextBox? GetTextBox()
    {
        var element = FindElement();
        return element?.AsTextBox();
    }

    /// <summary>
    /// Enter text into the control (appends to existing text).
    /// </summary>
    public virtual void Enter(string text)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Enter", $"Element '{AutomationId}' not visible for text entry.");
        }
        
        var textBox = element!.AsTextBox();
        textBox?.Enter(text);
        LogAction("Enter", text);
    }

    /// <summary>
    /// Clear the control's text.
    /// </summary>
    public virtual void Clear()
    {
        var textBox = GetTextBox();
        if (textBox != null)
        {
            textBox.Text = string.Empty;
        }
        LogAction("Clear");
    }

    /// <summary>
    /// Clear and enter new text.
    /// </summary>
    public virtual void ClearAndEnter(string text)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("ClearAndEnter", $"Element '{AutomationId}' not visible for text entry.");
        }
        
        var textBox = element!.AsTextBox();
        if (textBox != null)
        {
            textBox.Text = string.Empty;
            textBox.Enter(text);
        }
        LogAction("ClearAndEnter", text);
    }

    /// <summary>
    /// Set text (alias for ClearAndEnter for backward compatibility).
    /// </summary>
    public virtual void SetText(string text)
    {
        ClearAndEnter(text);
    }

    /// <summary>
    /// Append text to existing text.
    /// </summary>
    public virtual void Append(string text)
    {
        Enter(text);
    }

    /// <summary>
    /// Check if control is read-only.
    /// </summary>
    public virtual bool IsReadOnly()
    {
        var textBox = GetTextBox();
        return textBox?.IsReadOnly ?? true;
    }

    /// <summary>
    /// Get the length of the text.
    /// </summary>
    public virtual int GetTextLength()
    {
        return GetText().Length;
    }

    /// <summary>
    /// Get element text.
    /// </summary>
    public override string GetText()
    {
        var textBox = GetTextBox();
        return textBox?.Text ?? string.Empty;
    }
}
