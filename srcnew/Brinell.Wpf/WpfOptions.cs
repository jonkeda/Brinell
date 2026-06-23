namespace Brinell.Wpf;

/// <summary>
/// WPF platform-specific configuration options.
/// Controls FlaUI driver behavior and target WPF application settings.
/// </summary>
public class WpfOptions
{
    /// <summary>
    /// Full path to the WPF application binary/executable to test.
    /// Null = attach to running application.
    /// </summary>
    public string? AppPath { get; set; }

    /// <summary>
    /// Process name of running WPF application.
    /// Used when AppPath is null to locate running app.
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// Window handle (HWND) for targeting specific WPF window.
    /// </summary>
    public string? WindowHandle { get; set; }

    /// <summary>
    /// Attach to already-running application instead of launching new one.
    /// Default: false
    /// </summary>
    public bool AttachToRunning { get; set; }
}
