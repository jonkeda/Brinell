namespace Brinell.Stride.Infrastructure;

/// <summary>
/// Configuration options for Stride UI tests.
/// </summary>
public class StrideTestOptions
{
    #region Timeouts

    /// <summary>
    /// Default timeout in milliseconds for wait operations.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Short timeout in milliseconds for quick checks.
    /// </summary>
    public int ShortTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Polling interval in milliseconds for wait operations.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 250;

    /// <summary>
    /// Timeout in milliseconds for game startup.
    /// </summary>
    public int StartupTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Timeout in milliseconds for connecting to automation channel.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 10000;

    #endregion

    #region Input Simulation

    /// <summary>
    /// Delay in milliseconds before clicking.
    /// </summary>
    public int ClickDelayMs { get; set; } = 50;

    /// <summary>
    /// Delay in milliseconds after clicking.
    /// </summary>
    public int PostClickDelayMs { get; set; } = 100;

    /// <summary>
    /// Delay in milliseconds between key presses when typing.
    /// </summary>
    public int KeyPressDelayMs { get; set; } = 20;

    #endregion

    #region Game Configuration

    /// <summary>
    /// Path to the game executable. If null, will attempt to find automatically.
    /// </summary>
    public string? GameExecutablePath { get; set; }

    /// <summary>
    /// Arguments to pass to the game executable.
    /// </summary>
    public string[] GameArguments { get; set; } = [];

    /// <summary>
    /// If true, attach to an existing game instance instead of starting a new one.
    /// </summary>
    public bool AttachToExisting { get; set; }

    /// <summary>
    /// Name of the named pipe for automation communication.
    /// </summary>
    public string PipeName { get; set; } = "Brinell.Stride.Automation";

    #endregion

    #region Screenshots

    /// <summary>
    /// Directory for saving screenshots.
    /// </summary>
    public string ScreenshotDirectory { get; set; } = "TestResults/Screenshots";

    /// <summary>
    /// Whether to capture screenshots on test failure.
    /// </summary>
    public bool CaptureScreenshotOnFailure { get; set; } = true;

    #endregion

    #region Logging

    /// <summary>
    /// Directory for saving test logs.
    /// </summary>
    public string LogDirectory { get; set; } = "TestResults/Logs";

    /// <summary>
    /// Whether to enable CSV logging.
    /// </summary>
    public bool EnableCsvLogging { get; set; } = true;

    #endregion
}
