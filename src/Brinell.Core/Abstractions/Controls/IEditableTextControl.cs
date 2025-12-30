namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for editable text controls (text box, text area).
/// </summary>
public interface IEditableTextControl : ITextControl
{
    /// <summary>
    /// Focus the control.
    /// </summary>
    void Focus();
    
    /// <summary>
    /// Select all text in the control.
    /// </summary>
    void SelectAll();
    
    /// <summary>
    /// Copy selected text to clipboard.
    /// </summary>
    void Copy();
    
    /// <summary>
    /// Cut selected text to clipboard.
    /// </summary>
    void Cut();
    
    /// <summary>
    /// Paste from clipboard.
    /// </summary>
    void Paste();
}
