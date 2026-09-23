namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms RichTextBox control.
/// Handles trailing \r\n that WinForms RichTextBox appends to text.
/// </summary>
public sealed class RichTextBox<TScope> : EditableTextControlBase<TScope>
    where TScope : IWinFormsScope<TScope>
{
    public RichTextBox(IWinFormsScope<TScope> scope, Locator locator) : base(scope, locator) { }
    public RichTextBox(IWinFormsScope<TScope> scope, string locatorValue) : base(scope, locatorValue) { }

    /// <summary>
    /// Gets text with trailing \r\n trimmed (WinForms RichTextBox artifact).
    /// </summary>
    public override string? GetText(int? timeoutMs = null)
    {
        var text = base.GetText(timeoutMs);
        return text?.TrimEnd('\r', '\n');
    }
}
