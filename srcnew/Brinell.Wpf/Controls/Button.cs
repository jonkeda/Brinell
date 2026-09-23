namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF Button control.
/// </summary>
public sealed class Button<TScope> : ClickableControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public Button(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public Button(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
