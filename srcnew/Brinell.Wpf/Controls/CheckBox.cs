namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF CheckBox control.
/// </summary>
public sealed class CheckBox<TScope> : ToggleControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public CheckBox(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public CheckBox(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
