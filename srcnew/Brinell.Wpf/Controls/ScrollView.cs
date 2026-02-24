namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF ScrollViewer control.
/// </summary>
public sealed class ScrollView<TScope> : ControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public ScrollView(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ScrollView(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
