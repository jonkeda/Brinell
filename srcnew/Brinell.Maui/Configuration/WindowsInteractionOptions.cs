namespace Brinell.Maui.Configuration;

using Brinell.Core.Configuration;

/// <summary>
/// Controls whether Windows desktop automation may use foreground activation and global input fallbacks.
/// </summary>
public sealed class WindowsInteractionOptions
{
    /// <summary>
    /// Creates background-safe defaults that only allow semantic UI Automation operations.
    /// </summary>
    public static WindowsInteractionOptions Semantic { get; } = new();

    /// <summary>
    /// Creates compatibility defaults that allow physical desktop input.
    /// </summary>
    public static WindowsInteractionOptions Interactive { get; } = new()
    {
        AllowForegroundActivation = true,
        AllowPointerInput = true,
        AllowGlobalKeyboardInput = true,
        AllowClipboardInput = true
    };

    public bool AllowForegroundActivation { get; init; }
    public bool AllowPointerInput { get; init; }
    public bool AllowGlobalKeyboardInput { get; init; }
    public bool AllowClipboardInput { get; init; }

    /// <summary>
    /// Creates a detached copy so callers cannot accidentally mutate shared defaults.
    /// </summary>
    public WindowsInteractionOptions Clone()
        => new()
        {
            AllowForegroundActivation = AllowForegroundActivation,
            AllowPointerInput = AllowPointerInput,
            AllowGlobalKeyboardInput = AllowGlobalKeyboardInput,
            AllowClipboardInput = AllowClipboardInput
        };

    /// <summary>
    /// Creates Windows interaction policy from configuration object.
    /// Configuration is required; no fallback to environment variables.
    /// </summary>
    public static WindowsInteractionOptions FromConfiguration(WindowsInteractionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var mode = config.Mode?.ToLowerInvariant() ?? "semantic";
        var baseOptions = mode switch
        {
            "interactive" => Interactive.Clone(),
            _ => Semantic.Clone()
        };

        return new WindowsInteractionOptions
        {
            AllowForegroundActivation = config.AllowForegroundActivation || baseOptions.AllowForegroundActivation,
            AllowPointerInput = config.AllowPointerInput || baseOptions.AllowPointerInput,
            AllowGlobalKeyboardInput = config.AllowGlobalKeyboardInput || baseOptions.AllowGlobalKeyboardInput,
            AllowClipboardInput = config.AllowClipboardInput || baseOptions.AllowClipboardInput
        };
    }
}
