namespace Brinell.Wpf.Controls;

/// <summary>
/// WPF TextBox control (editable text input).
/// </summary>
public sealed class TextBox<TScope> : EditableTextControlBase<TScope>
    where TScope : IWpfScope<TScope>
{
    public TextBox(IWpfScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public TextBox(IWpfScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }
}
