namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms ComboBox control.
/// </summary>
public sealed class ComboBox<TScope> : SelectorControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public ComboBox(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ComboBox(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
