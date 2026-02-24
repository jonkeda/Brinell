namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms ListBox control.
/// </summary>
public sealed class ListBox<TScope> : SelectorControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public ListBox(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ListBox(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
