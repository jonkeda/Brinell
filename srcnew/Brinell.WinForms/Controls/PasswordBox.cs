namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms PasswordBox (masked TextBox) control.
/// </summary>
public sealed class PasswordBox<TScope> : EditableTextControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public PasswordBox(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public PasswordBox(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
