namespace Brinell.Stride;

/// <summary>
/// Stride game engine-specific configuration options.
/// </summary>
public class StrideOptions
{
    /// <summary>
    /// Full path to the Stride game executable to test.
    /// Null = attach to running application.
    /// </summary>
    public string? AppPath { get; set; }

    /// <summary>
    /// Process name of running Stride game application.
    /// Used when AppPath is null to locate running app.
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// Attach to already-running application instead of launching new one.
    /// Default: false
    /// </summary>
    public bool AttachToRunning { get; set; }

    /// <summary>
    /// Enable automation support for Stride game testing.
    /// Default: false
    /// </summary>
    public bool AutomationEnabled { get; set; }
}
