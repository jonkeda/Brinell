namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms TextBox control.
/// </summary>
public sealed class TextBox<TScope> : EditableTextControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public TextBox(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TextBox(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
