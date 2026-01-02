using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TextBox used as password input control wrapper.
/// Inherits from InputControlBase which provides Clear, AppendText, IsReadOnly, GetTextLength, and WaitForTextEquals.
/// Note: Password box in WinForms is typically a TextBox with UseSystemPasswordChar = true.
/// </summary>
public class PasswordBoxControl : InputControlBase
{
    public PasswordBoxControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public PasswordBoxControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public PasswordBoxControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Enter password into the password box.
    /// </summary>
    public void Enter(string password)
    {
        SetText(password);
    }

    /// <summary>
    /// Clear the password box and enter new password.
    /// </summary>
    public void ClearAndEnter(string password)
    {
        Clear();
        Enter(password);
    }

    /// <summary>
    /// Get the current password value.
    /// Note: In real WinForms with password masking, the value will appear masked in automation.
    /// This method returns the actual text content.
    /// </summary>
    public string GetPassword()
    {
        return GetText();
    }

    /// <summary>
    /// Assert that password equals expected value.
    /// </summary>
    public void AssertPasswordEquals(string expected)
    {
        AssertTextEquals(expected);
    }

    /// <summary>
    /// Wait and assert that password equals expected value.
    /// </summary>
    public void AssertPasswordEqualsWait(string expected, int? timeoutMs = null)
    {
        AssertTextEqualsWait(expected, timeoutMs);
    }
}
