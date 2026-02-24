namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF PasswordBox control (masked text entry).
/// </summary>
public sealed class PasswordBox<TScope> : EditableTextControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public PasswordBox(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public PasswordBox(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
