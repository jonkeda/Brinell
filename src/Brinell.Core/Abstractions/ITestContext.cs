using Brinell.Core.Logging;
using Brinell.Core.Screenshots;

namespace Brinell.Core.Abstractions;

/// <summary>
/// Supported test platforms.
/// </summary>
public enum Platform
{
    /// <summary>WPF desktop on Windows using FlaUI.</summary>
    Windows,
    
    /// <summary>MAUI on Windows using Appium.</summary>
    WindowsMaui,
    
    /// <summary>Android using Appium.</summary>
    Android,
    
    /// <summary>iOS using Appium.</summary>
    iOS,
    
    /// <summary>Web browser using Selenium.</summary>
    Web
}

/// <summary>
/// Platform-agnostic test context interface.
/// All timeouts are in milliseconds.
/// </summary>
public interface ITestContext
{
    /// <summary>
    /// Name of the current test for logging context.
    /// </summary>
    string TestName { get; set; }
    
    /// <summary>
    /// Current platform enum value.
    /// </summary>
    Platform Platform { get; }
    
    /// <summary>
    /// Default timeout in milliseconds for wait operations.
    /// </summary>
    int DefaultTimeoutMs { get; }
    
    /// <summary>
    /// Short timeout in milliseconds for quick checks.
    /// </summary>
    int ShortTimeoutMs { get; }
    
    /// <summary>
    /// Polling interval in milliseconds for wait operations.
    /// </summary>
    int PollingIntervalMs { get; }
    
    /// <summary>
    /// Logger instance for CSV logging. May be null if logging is disabled.
    /// </summary>
    ITestLogger? Logger { get; }
    
    /// <summary>
    /// Set the CSV logger for this context.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    void SetLogger(ITestLogger logger);
    
    /// <summary>
    /// Logs a message with test context prefix.
    /// </summary>
    void Log(string message);
    
    /// <summary>
    /// Log an error with exception details.
    /// </summary>
    void LogError(Exception ex, string context);
    
    /// <summary>
    /// Waits for a condition to be true.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    /// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
    /// <param name="description">Description for logging.</param>
    /// <returns>True if condition met within timeout, false otherwise.</returns>
    bool WaitFor(Func<bool> condition, int? timeoutMs = null, string description = "condition");
    
    /// <summary>
    /// Take a screenshot and save to temp folder.
    /// </summary>
    string? TakeScreenshot(string name);
    
    /// <summary>
    /// Capture a failure screenshot. Call this before throwing exceptions.
    /// Only captures for failing tests, not during normal flow.
    /// </summary>
    /// <param name="suffix">Descriptive suffix for the screenshot file (e.g., "page-not-displayed").</param>
    /// <returns>Path to saved screenshot, or empty string if capture failed.</returns>
    string CaptureFailureScreenshot(string suffix = "failure");
}
