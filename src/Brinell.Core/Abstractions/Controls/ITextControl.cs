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
    
    /// <summary>
    /// Assert text is empty or null.
    /// </summary>
    void AssertTextEmpty(string? message = null);
    
    /// <summary>
    /// Assert text is not empty.
    /// </summary>
    void AssertTextNotEmpty(string? message = null);
    
    /// <summary>
    /// Assert text starts with expected prefix.
    /// </summary>
    void AssertTextStartsWith(string prefix, string? message = null);
    
    /// <summary>
    /// Assert text ends with expected suffix.
    /// </summary>
    void AssertTextEndsWith(string suffix, string? message = null);
    
    /// <summary>
    /// Assert text matches the specified regex pattern.
    /// </summary>
    void AssertTextMatches(string pattern, string? message = null);
}
