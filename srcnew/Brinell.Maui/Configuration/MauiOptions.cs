namespace Brinell.Maui.Configuration;

/// <summary>
/// MAUI platform-specific configuration options.
/// Controls Appium/FlaUI driver behavior and target application settings.
/// </summary>
public class MauiOptions
{
    /// <summary>
    /// Appium server URI (for remote device testing).
    /// Default: http://127.0.0.1:4723
    /// </summary>
    public string ServerUri { get; set; } = "http://127.0.0.1:4723";

    /// <summary>
    /// Target platform: "windows", "ios", or "android".
    /// Default: "windows"
    /// </summary>
    public MauiPlatform Platform { get; set; } = MauiPlatform.Windows;

    /// <summary>
    /// Full path to the application binary/executable to test.
    /// Null = attach to running application or use platform defaults.
    /// </summary>
    public string? AppPath { get; set; }

    /// <summary>
    /// Process name of running application (for desktop: windows, wpf, winforms).
    /// Used when AppPath is null to locate running app.
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// Window handle (HWND on Windows) for targeting specific window.
    /// Platform-specific format depending on OS.
    /// </summary>
    public string? WindowHandle { get; set; }

    /// <summary>
    /// Device name for remote testing (iOS/Android device identifier).
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Platform version for remote testing (iOS/Android version).
    /// </summary>
    public string? PlatformVersion { get; set; }

    /// <summary>
    /// Attach to already-running application instead of launching new one.
    /// Default: false
    /// </summary>
    public bool AttachToRunning { get; set; }
}
