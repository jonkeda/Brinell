namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF Slider control (range value input).
/// </summary>
public sealed class Slider<TScope> : RangeControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public Slider(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public Slider(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
