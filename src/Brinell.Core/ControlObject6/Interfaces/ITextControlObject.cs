namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for text input controls (Entry, TextBox, etc.).
/// Provides text entry, clearing, and read-only state verification.
/// </summary>
public interface ITextControlObject : IFocusableControlObject
{
    /// <summary>
    /// Enters text into the control.
    /// Clears existing text first, then types the new text.
    /// If text is null, does nothing (skip operation).
    /// </summary>
    /// <param name="text">Text to enter, or null to skip.</param>
    /// <param name="timeoutMs">Timeout for element to be ready.</param>
    void Enter(string? text, int? timeoutMs = null);

    /// <summary>
    /// Clears all text from the control.
    /// </summary>
    /// <param name="timeoutMs">Timeout for element to be ready.</param>
    void Clear(int? timeoutMs = null);

    /// <summary>
    /// Clears existing text and enters new text.
    /// If text is null, only clears the field.
    /// </summary>
    /// <param name="text">Text to enter, or null to just clear.</param>
    /// <param name="timeoutMs">Timeout for element to be ready.</param>
    void ClearAndEnter(string? text, int? timeoutMs = null);

    /// <summary>
    /// Appends text to existing content without clearing.
    /// If text is null, does nothing (skip operation).
    /// </summary>
    /// <param name="text">Text to append, or null to skip.</param>
    /// <param name="timeoutMs">Timeout for element to be ready.</param>
    void Append(string? text, int? timeoutMs = null);

    /// <summary>
    /// Immediately checks if the control is read-only.
    /// </summary>
    bool IsReadOnly();

    /// <summary>
    /// Waits for the control to become read-only or editable.
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    bool WaitReadOnly(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts the control's read-only state.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Gets the length of the text content.
    /// </summary>
    /// <param name="timeoutMs">Timeout for element to exist.</param>
    int GetTextLength(int? timeoutMs = null);

    /// <summary>
    /// Asserts the text length equals expected.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertTextLength(int? expected, string? message = null, int? timeoutMs = null);
}
