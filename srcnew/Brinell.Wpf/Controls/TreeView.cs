namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF TreeView control (hierarchical item display).
/// </summary>
public sealed class TreeView<TScope> : ControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public TreeView(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TreeView(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
