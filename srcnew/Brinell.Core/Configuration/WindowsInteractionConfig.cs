namespace Brinell.Core.Configuration;

/// <summary>
/// Windows desktop automation interaction policy configuration.
/// Controls whether automation may use foreground activation and global input fallbacks.
/// </summary>
public class WindowsInteractionConfig
{
    /// <summary>
    /// Interaction mode: "semantic" (safe for CI/CD) or "interactive" (allows physical input).
    /// Default: "semantic"
    /// </summary>
    public string Mode { get; set; } = "semantic";

    /// <summary>
    /// Allow foreground window activation.
    /// Default: false (semantic mode)
    /// </summary>
    public bool AllowForegroundActivation { get; set; }

    /// <summary>
    /// Allow pointer (mouse) input operations.
    /// Default: false (semantic mode)
    /// </summary>
    public bool AllowPointerInput { get; set; }

    /// <summary>
    /// Allow global keyboard input operations.
    /// Default: false (semantic mode)
    /// </summary>
    public bool AllowGlobalKeyboardInput { get; set; }

    /// <summary>
    /// Allow clipboard input operations.
    /// Default: false (semantic mode)
    /// </summary>
    public bool AllowClipboardInput { get; set; }
}
