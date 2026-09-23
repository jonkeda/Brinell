namespace Brinell.WinForms;

/// <summary>
/// WinForms platform-specific configuration options.
/// Controls FlaUI driver behavior and target WinForms application settings.
/// </summary>
public class WinFormsOptions
{
    /// <summary>
    /// Full path to the WinForms application binary/executable to test.
    /// Null = attach to running application.
    /// </summary>
    public string? AppPath { get; set; }

    /// <summary>
    /// Process name of running WinForms application.
    /// Used when AppPath is null to locate running app.
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// Window handle (HWND) for targeting specific WinForms window.
    /// </summary>
    public string? WindowHandle { get; set; }

    /// <summary>
    /// Attach to already-running application instead of launching new one.
    /// Default: false
    /// </summary>
    public bool AttachToRunning { get; set; }
}
