namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF TabItem control (clickable tab header).
/// </summary>
public sealed class TabItem<TScope> : ClickableControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public TabItem(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TabItem(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
