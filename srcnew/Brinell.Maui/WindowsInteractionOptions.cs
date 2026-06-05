namespace Brinell.Maui;

/// <summary>
/// Controls whether Windows desktop automation may use foreground activation and global input fallbacks.
/// </summary>
public sealed class WindowsInteractionOptions
{
    public const string InteractionModeEnvironmentVariable = "BRINELL_WINDOWS_INTERACTION_MODE";
    public const string AllowForegroundActivationEnvironmentVariable = "BRINELL_ALLOW_FOREGROUND_ACTIVATION";
    public const string AllowPointerInputEnvironmentVariable = "BRINELL_ALLOW_POINTER_INPUT";
    public const string AllowGlobalKeyboardInputEnvironmentVariable = "BRINELL_ALLOW_GLOBAL_KEYBOARD_INPUT";
    public const string AllowClipboardInputEnvironmentVariable = "BRINELL_ALLOW_CLIPBOARD_INPUT";

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
    /// Parses Windows interaction policy from environment variables.
    /// </summary>
    public static WindowsInteractionOptions FromEnvironment()
        => FromEnvironment(Environment.GetEnvironmentVariable);

    /// <summary>
    /// Parses Windows interaction policy using a supplied environment lookup, useful for tests.
    /// </summary>
    public static WindowsInteractionOptions FromEnvironment(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var mode = ParseMode(getEnvironmentVariable(InteractionModeEnvironmentVariable));
        var options = mode switch
        {
            WindowsInteractionMode.Interactive => Interactive.Clone(),
            _ => Semantic.Clone()
        };

        return new WindowsInteractionOptions
        {
            AllowForegroundActivation = ParseBool(
                getEnvironmentVariable(AllowForegroundActivationEnvironmentVariable),
                options.AllowForegroundActivation,
                AllowForegroundActivationEnvironmentVariable),
            AllowPointerInput = ParseBool(
                getEnvironmentVariable(AllowPointerInputEnvironmentVariable),
                options.AllowPointerInput,
                AllowPointerInputEnvironmentVariable),
            AllowGlobalKeyboardInput = ParseBool(
                getEnvironmentVariable(AllowGlobalKeyboardInputEnvironmentVariable),
                options.AllowGlobalKeyboardInput,
                AllowGlobalKeyboardInputEnvironmentVariable),
            AllowClipboardInput = ParseBool(
                getEnvironmentVariable(AllowClipboardInputEnvironmentVariable),
                options.AllowClipboardInput,
                AllowClipboardInputEnvironmentVariable)
        };
    }

    private static WindowsInteractionMode ParseMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return WindowsInteractionMode.Semantic;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "semantic" => WindowsInteractionMode.Semantic,
            "interactive" => WindowsInteractionMode.Interactive,
            _ => throw new InvalidOperationException(
                $"{InteractionModeEnvironmentVariable} must be 'semantic' or 'interactive'. Actual value: '{value}'.")
        };
    }

    private static bool ParseBool(string? value, bool defaultValue, string variableName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => throw new InvalidOperationException(
                $"{variableName} must be a boolean value such as true/false or 1/0. Actual value: '{value}'.")
        };
    }
}
