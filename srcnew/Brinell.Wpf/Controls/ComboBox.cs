namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF ComboBox control with dropdown selection.
/// </summary>
public sealed class ComboBox<TScope> : SelectorControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public ComboBox(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ComboBox(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
