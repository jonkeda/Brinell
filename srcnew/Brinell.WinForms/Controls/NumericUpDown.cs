namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms NumericUpDown (Spinner) control.
/// </summary>
public sealed class NumericUpDown<TScope> : RangeControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public NumericUpDown(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public NumericUpDown(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
