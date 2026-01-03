using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms PasswordBox/MaskedTextBox control wrapper.
/// Handles password input since it doesn't expose Text property through UI Automation.
/// Uses keyboard input to set password value.
/// </summary>
public class PasswordBoxControl : ControlBase, IEditableTextControl
{
    public PasswordBoxControl(FlaUITestContext context, PageBase? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a password control that searches within a container element.
    /// </summary>
    public PasswordBoxControl(FlaUITestContext context, PageBase? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public PasswordBoxControl(FlaUITestContext context, string automationId)
        : base(context, null, automationId)
    {
    }

    /// <summary>
    /// Enter text into the password box using keyboard simulation.
    /// </summary>
    public virtual void Enter(string text)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Enter", $"Element '{AutomationId}' not visible for text entry.");
        }
        
        element!.Focus();
        Keyboard.Type(text);
        LogAction("Enter", "***");
    }

    /// <summary>
    /// Clear the password box using keyboard shortcuts.
    /// </summary>
    public virtual void Clear()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Clear", $"Element '{AutomationId}' not visible for clear.");
        }
        
        element!.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Press(VirtualKeyShort.DELETE);
        LogAction("Clear");
    }

    /// <summary>
    /// Clear and enter new password text.
    /// </summary>
    public virtual void ClearAndEnter(string text)
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("ClearAndEnter", $"Element '{AutomationId}' not visible for text entry.");
        }
        
        element!.Focus();
        Thread.Sleep(50);
        
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Thread.Sleep(50);
        Keyboard.Type(text);
        Thread.Sleep(50);
        LogAction("ClearAndEnter", "***");
    }

    /// <summary>
    /// Set password text (alias for ClearAndEnter).
    /// </summary>
    public virtual void SetText(string text)
    {
        ClearAndEnter(text);
    }

    /// <summary>
    /// Append text to existing password.
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
        var element = FindElement();
        return element == null || !element.IsEnabled;
    }

    /// <summary>
    /// Get password text.
    /// Note: For security, password fields don't expose their value through UI Automation.
    /// This returns empty string by design.
    /// </summary>
    public override string GetText()
    {
        return string.Empty;
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
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        LogAction("SelectAll");
    }

    /// <summary>
    /// Copy selected text to clipboard.
    /// </summary>
    public virtual void Copy()
    {
        SelectAll();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_C);
        LogAction("Copy");
    }

    /// <summary>
    /// Cut selected text to clipboard.
    /// </summary>
    public virtual void Cut()
    {
        SelectAll();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_X);
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
        
        element?.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
        LogAction("Paste");
    }
}
