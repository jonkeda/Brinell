namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF Label (read-only text display).
/// </summary>
public sealed class Label<TScope> : ControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public Label(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public Label(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
