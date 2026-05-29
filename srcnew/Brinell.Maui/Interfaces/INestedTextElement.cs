namespace Brinell.Maui.Interfaces;

/// <summary>
/// Interface for elements with nested text structure (Windows UI Automation).
/// Used for SearchBar and Editor controls on Windows where the
/// actual text is in a nested TextBox element.
/// Implemented by platform-specific drivers (e.g., FlaUIMauiElement).
/// </summary>
public interface INestedTextElement
{
    /// <summary>
    /// Finds the nested TextBox (Edit) element within this control.
    /// Used for controls like SearchBar/AutoSuggestBox where text is in a child element.
    /// </summary>
    /// <returns>The nested TextBox element, or null if not found.</returns>
    IMauiElement? FindNestedTextBox();
    
    /// <summary>
    /// Gets text from the nested TextBox if direct text access fails.
    /// First tries direct Value pattern, then falls back to nested TextBox.
    /// </summary>
    /// <returns>The text content, or null if not available.</returns>
    string? GetNestedText();
    
    /// <summary>
    /// Clears text with fallback mechanisms for complex controls.
    /// Tries Value.SetValue("") first, then Ctrl+A Delete.
    /// </summary>
    /// <returns>True if successful, false if operation failed.</returns>
    bool ClearWithFallback();

    /// <summary>
    /// Sets text through direct or nested UI Automation value patterns for complex controls.
    /// </summary>
    /// <param name="text">The text to set.</param>
    /// <returns>True if successful, false if operation failed.</returns>
    bool SetTextWithFallback(string text);
}
