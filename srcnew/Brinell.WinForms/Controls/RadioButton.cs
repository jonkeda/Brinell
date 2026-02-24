namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms RadioButton control.
/// Note: RadioButtons cannot be unchecked directly — only another radio button
/// in the same group can be selected instead.
/// </summary>
public sealed class RadioButton<TScope> : ToggleControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public RadioButton(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public RadioButton(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }

    /// <summary>
    /// Uncheck is a no-op for radio buttons.
    /// Radio buttons can only be deselected by selecting another in the same group.
    /// </summary>
    public override TScope Uncheck(int? timeoutMs = null)
    {
        // RadioButtons cannot be unchecked directly
        return ContainingScope;
    }
}
