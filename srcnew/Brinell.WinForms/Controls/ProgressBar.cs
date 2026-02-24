namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms ProgressBar control (read-only range).
/// </summary>
public sealed class ProgressBar<TScope> : RangeControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public ProgressBar(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public ProgressBar(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
