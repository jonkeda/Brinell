namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms Button control.
/// </summary>
public sealed class Button<TScope> : ClickableControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public Button(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public Button(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
