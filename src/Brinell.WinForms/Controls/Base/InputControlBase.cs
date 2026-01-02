using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls.Base;

/// <summary>
/// Abstract base class for input controls (TextBox, PasswordBox, NumericUpDown, RichTextBox).
/// Extends ControlBase with text input-specific operations.
/// </summary>
public abstract class InputControlBase : ControlBase, IEditableTextControl
{
    /// <summary>
    /// Create an input control with page context and AutomationId.
    /// </summary>
    protected InputControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create an input control that searches within a container element.
    /// </summary>
    protected InputControlBase(FlaUITestContext context, IPageObject? page, AutomationElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    /// <summary>
    /// Create an input control without page context (for global controls).
    /// </summary>
    protected InputControlBase(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Clear all text from the control.
    /// </summary>
    public virtual void Clear()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Clear", $"Element '{AutomationId}' not visible for clearing.");
        }

        var textBox = element!.AsTextBox();
        if (textBox != null)
        {
            textBox.Text = string.Empty;
            System.Threading.Thread.Sleep(50); // Brief delay to allow UI to process
            LogAction("Clear");
        }
        else
        {
            ThrowCheckFailed("Clear", $"Element '{AutomationId}' is not a TextBox.");
        }
    }

    /// <summary>
    /// Append text to the control without clearing first.
    /// </summary>
    public virtual void AppendText(string text)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("AppendText", $"Element '{AutomationId}' not visible for text input.");
        }

        var textBox = element!.AsTextBox();
        if (textBox != null)
        {
            var currentText = textBox.Text ?? string.Empty;
            textBox.Text = currentText + text;
            System.Threading.Thread.Sleep(50); // Brief delay to allow UI to process
            LogAction("AppendText", text);
        }
        else
        {
            ThrowCheckFailed("AppendText", $"Element '{AutomationId}' is not a TextBox.");
        }
    }

    /// <summary>
    /// Enter text into the control (appends to existing text).
    /// </summary>
    public virtual void Enter(string text)
    {
        AppendText(text);
    }

    /// <summary>
    /// Clear and enter new text.
    /// </summary>
    public virtual void ClearAndEnter(string text)
    {
        Clear();
        Enter(text);
    }

    /// <summary>
    /// Set text (alias for ClearAndEnter for backward compatibility).
    /// </summary>
    public override void SetText(string text)
    {
        ClearAndEnter(text);
    }

    /// <summary>
    /// Append text to existing text.
    /// </summary>
    public virtual void Append(string text)
    {
        AppendText(text);
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
    }

    /// <summary>
    /// Check if the control is read-only.
    /// </summary>
    public virtual bool IsReadOnly()
    {
        var element = FindElement();
        if (element == null) return false;

        var textBox = element.AsTextBox();
        if (textBox != null)
        {
            return textBox.IsReadOnly;
        }

        return false;
    }

    /// <summary>
    /// Get the length of text in the control.
    /// </summary>
    public override int GetTextLength()
    {
        var text = GetText();
        return text.Length;
    }

    /// <summary>
    /// Wait for the text to equal the expected value.
    /// Useful for verifying async text updates.
    /// </summary>
    public virtual bool WaitForTextEquals(string expected, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        var result = _context.WaitFor(() => GetText() == expected, timeout, $"text equals '{expected}'");
        LogWait($"TextEquals={expected}", result, 0);
        return result;
    }

    /// <summary>
    /// Assert that text equals expected value, waiting up to timeout.
    /// </summary>
    public virtual void AssertTextEqualsWait(string expected, int? timeoutMs = null)
    {
        if (!WaitForTextEquals(expected, timeoutMs))
        {
            var actual = GetText();
            ThrowAssertionFailed("TextEqualsWait", actual, expected,
                $"Expected text '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("TextEqualsWait", expected, expected);
    }
}
