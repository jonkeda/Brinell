namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for text input controls.
/// </summary>
public interface ITextControl : IControlObject
{
    /// <summary>
    /// Enter text into the control.
    /// </summary>
    void Enter(string text);
    
    /// <summary>
    /// Clear the control's text.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Clear and enter new text.
    /// </summary>
    void ClearAndEnter(string text);
    
    /// <summary>
    /// Set text (alias for ClearAndEnter for backward compatibility).
    /// </summary>
    void SetText(string text);
    
    /// <summary>
    /// Append text to existing text.
    /// </summary>
    void Append(string text);
    
    /// <summary>
    /// Check if control is read-only.
    /// </summary>
    bool IsReadOnly();
    
    /// <summary>
    /// Get the length of the text.
    /// </summary>
    int GetTextLength();
}
