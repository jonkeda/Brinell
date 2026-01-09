namespace Brinell.Core.Interfaces;

/// <summary>
/// Text input capability for entries, editors, and other editable text controls.
/// </summary>
public interface IEditableTextControlObject : ITextControlObject
{
    /// <summary>
    /// Enter text into the control (appends to existing).
    /// If text is null, returns immediately (skip).
    /// </summary>
    void Enter(string? text, int? timeoutMs = null);
    
    /// <summary>
    /// Clear the control's text content.
    /// </summary>
    void Clear(int? timeoutMs = null);
    
    /// <summary>
    /// Set the control's text (clears first, then enters).
    /// If text is null, returns immediately (skip).
    /// </summary>
    void SetText(string? text, int? timeoutMs = null);
    
    /// <summary>
    /// Get the placeholder/hint text.
    /// Returns null if not available.
    /// </summary>
    string? GetPlaceholder();
    
    /// <summary>
    /// Wait until placeholder text matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitPlaceholder(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert placeholder text matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Check if the control is read-only.
    /// Returns null if element not found.
    /// </summary>
    bool? IsReadOnly();
    
    /// <summary>
    /// Wait until read-only state matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitReadOnly(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert read-only state matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null);
}
