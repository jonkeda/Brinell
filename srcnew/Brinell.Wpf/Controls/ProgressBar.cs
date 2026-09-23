namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF ProgressBar control (read-only range display).
/// </summary>
public sealed class ProgressBar<TScope> : RangeControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public ProgressBar(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ProgressBar(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
