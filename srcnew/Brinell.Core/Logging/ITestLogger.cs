namespace Brinell.Core.Logging;

/// <summary>
/// Logger interface for test actions and diagnostics.
/// </summary>
public interface ITestLogger
{
    /// <summary>
    /// Log an informational message with test context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="message">Informational message.</param>
    void LogInfo(string testName, string? pageName, string message);
    
    /// <summary>
    /// Log an action performed by a control with full context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="controlId">Control AutomationId or locator string.</param>
    /// <param name="action">Action name (Click, Enter, etc.).</param>
    /// <param name="value">Optional action value.</param>
    void LogAction(string testName, string? pageName, string controlId, string action, string? value = null);
    
    /// <summary>
    /// Log an assertion result with full context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="controlId">Control AutomationId or locator string.</param>
    /// <param name="assertion">Assertion type (AssertText, AssertVisible, etc.).</param>
    /// <param name="expected">Expected value.</param>
    /// <param name="actual">Actual value.</param>
    /// <param name="passed">Whether assertion passed.</param>
    void LogAssert(string testName, string? pageName, string controlId, string assertion, 
                  object? expected, object? actual, bool passed);
    
    /// <summary>
    /// Log a wait operation result with full context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="controlId">Control AutomationId or locator string.</param>
    /// <param name="waitType">Wait operation type (WaitExists, WaitVisible, etc.).</param>
    /// <param name="succeeded">Whether wait completed successfully.</param>
    /// <param name="elapsedMs">Time elapsed waiting.</param>
    void LogWait(string testName, string? pageName, string controlId, string waitType, 
                 bool succeeded, int elapsedMs);
    
    /// <summary>
    /// Log an error with full context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="controlId">Control AutomationId or locator string.</param>
    /// <param name="action">Action that caused the error.</param>
    /// <param name="exception">The exception that occurred.</param>
    void LogError(string testName, string? pageName, string? controlId, string action, Exception exception);
    
    /// <summary>
    /// Log a navigation event.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="destination">Navigation target.</param>
    void LogNavigation(string testName, string destination);
    
    /// <summary>
    /// Log a debug message.
    /// </summary>
    /// <param name="message">Debug message.</param>
    void LogDebug(string message);
    
    /// <summary>
    /// Log a warning message.
    /// </summary>
    /// <param name="message">Warning message.</param>
    void LogWarning(string message);
}
