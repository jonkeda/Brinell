namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms CheckBox control.
/// </summary>
public sealed class CheckBox<TScope> : ToggleControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public CheckBox(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public CheckBox(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
