namespace Brinell.Maui.Enums;

/// <summary>
/// Controls whether the Windows FlaUI driver may use foreground physical input fallbacks.
/// </summary>
public sealed class WindowsInteractionOptions
{
    public const string InteractionModeEnvironmentVariable = "BRINELL_WINDOWS_INTERACTION_MODE";
    public const string AllowPointerInputEnvironmentVariable = "BRINELL_WINDOWS_ALLOW_POINTER_INPUT";
    public const string AllowGlobalKeyboardInputEnvironmentVariable = "BRINELL_WINDOWS_ALLOW_GLOBAL_KEYBOARD_INPUT";
    public const string AllowClipboardInputEnvironmentVariable = "BRINELL_WINDOWS_ALLOW_CLIPBOARD_INPUT";
    public const string AllowForegroundActivationEnvironmentVariable = "BRINELL_WINDOWS_ALLOW_FOREGROUND_ACTIVATION";

    public bool AllowPointerInput { get; set; }

    public bool AllowGlobalKeyboardInput { get; set; }

    public bool AllowClipboardInput { get; set; }

    public bool AllowForegroundActivation { get; set; }

    public static WindowsInteractionOptions Semantic => new();

    public static WindowsInteractionOptions Interactive => new()
    {
        AllowPointerInput = true,
        AllowGlobalKeyboardInput = true,
        AllowClipboardInput = true,
        AllowForegroundActivation = true
    };

    public WindowsInteractionOptions Clone()
        => new()
        {
            AllowPointerInput = AllowPointerInput,
            AllowGlobalKeyboardInput = AllowGlobalKeyboardInput,
            AllowClipboardInput = AllowClipboardInput,
            AllowForegroundActivation = AllowForegroundActivation
        };

    public static WindowsInteractionOptions FromEnvironment()
    {
        var options = string.Equals(
                Environment.GetEnvironmentVariable(InteractionModeEnvironmentVariable),
                "interactive",
                StringComparison.OrdinalIgnoreCase)
            ? Interactive.Clone()
            : Semantic.Clone();

        options.AllowPointerInput = ReadBoolean(
            AllowPointerInputEnvironmentVariable,
            options.AllowPointerInput);
        options.AllowGlobalKeyboardInput = ReadBoolean(
            AllowGlobalKeyboardInputEnvironmentVariable,
            options.AllowGlobalKeyboardInput);
        options.AllowClipboardInput = ReadBoolean(
            AllowClipboardInputEnvironmentVariable,
            options.AllowClipboardInput);
        options.AllowForegroundActivation = ReadBoolean(
            AllowForegroundActivationEnvironmentVariable,
            options.AllowForegroundActivation);

        return options;
    }

    private static bool ReadBoolean(string variable, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class WindowsInteractionPolicyException : InvalidOperationException
{
    public WindowsInteractionPolicyException(string message)
        : base(message)
    {
    }
}
