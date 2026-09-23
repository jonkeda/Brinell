namespace Brinell.WinForms.Context;

/// <summary>
/// Configuration options for creating a WinForms test context.
/// </summary>
public class WinFormsTestContextOptions
{
    /// <summary>
    /// Pre-created driver instance (for testing or custom drivers).
    /// If set, factory properties are ignored.
    /// </summary>
    public IWinFormsDriver? Driver { get; init; }

    /// <summary>
    /// Path to the WinForms application executable.
    /// Used to launch the application if Driver is not provided.
    /// </summary>
    public string? ExecutablePath { get; init; }

    /// <summary>
    /// Command line arguments for the application.
    /// </summary>
    public string? Arguments { get; init; }

    /// <summary>
    /// Process ID to attach to an existing running application.
    /// </summary>
    public int? ProcessId { get; init; }

    /// <summary>
    /// Window handle to attach to an existing window.
    /// </summary>
    public IntPtr? WindowHandle { get; init; }

    /// <summary>
    /// Timeout configuration. If null, defaults are used.
    /// </summary>
    public TimeoutSettings? Timeouts { get; init; }

    /// <summary>
    /// Test logger. If null, a no-op logger is used.
    /// </summary>
    public ITestLogger? Logger { get; init; }
}
