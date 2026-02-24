namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF ListBox control with item selection.
/// </summary>
public sealed class ListBox<TScope> : SelectorControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public ListBox(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ListBox(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
