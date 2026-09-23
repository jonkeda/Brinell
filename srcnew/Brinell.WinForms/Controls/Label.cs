namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms Label control.
/// </summary>
public sealed class Label<TScope> : ControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public Label(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public Label(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
