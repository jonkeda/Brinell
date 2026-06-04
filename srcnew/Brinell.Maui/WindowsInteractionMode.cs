namespace Brinell.Maui;

/// <summary>
/// Defines how Windows desktop automation may interact with the active user desktop.
/// </summary>
public enum WindowsInteractionMode
{
    /// <summary>
    /// Prefer UI Automation patterns and block foreground, pointer, keyboard, and clipboard fallbacks.
    /// </summary>
    Semantic,

    /// <summary>
    /// Allow compatibility fallbacks that use the foreground window, pointer, keyboard, and clipboard.
    /// </summary>
    Interactive
}
