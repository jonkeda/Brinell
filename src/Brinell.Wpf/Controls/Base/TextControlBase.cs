using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls.Base;

/// <summary>
/// WPF base class for text input controls.
/// </summary>
public abstract class TextControlBase : ControlBase, IEditableTextControl
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
    /// Get element text.
    /// </summary>
    public override string GetText()
    {
        var textBox = GetTextBox();
        return textBox?.Text ?? string.Empty;
    }

    /// <summary>
    /// Focus the control.
    /// </summary>
    public virtual void Focus()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Focus", $"Element '{AutomationId}' not visible for focus.");
        }
        element?.Focus();
        LogAction("Focus");
    }

    /// <summary>
    /// Select all text in the control.
    /// </summary>
    public virtual void SelectAll()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("SelectAll", $"Element '{AutomationId}' not visible for select all.");
        }
        
        element?.Focus();
        System.Windows.Forms.SendKeys.SendWait("^a");
        LogAction("SelectAll");
    }

    /// <summary>
    /// Copy selected text to clipboard.
    /// </summary>
    public virtual void Copy()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Copy", $"Element '{AutomationId}' not visible for copy.");
        }
        
        SelectAll();
        System.Windows.Forms.SendKeys.SendWait("^c");
        LogAction("Copy");
    }

    /// <summary>
    /// Cut selected text to clipboard.
    /// </summary>
    public virtual void Cut()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Cut", $"Element '{AutomationId}' not visible for cut.");
        }
        
        SelectAll();
        System.Windows.Forms.SendKeys.SendWait("^x");
        LogAction("Cut");
    }

    /// <summary>
    /// Paste from clipboard.
    /// </summary>
    public virtual void Paste()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Paste", $"Element '{AutomationId}' not visible for paste.");
        }
        
        System.Windows.Forms.SendKeys.SendWait("^v");
        LogAction("Paste");
    }}