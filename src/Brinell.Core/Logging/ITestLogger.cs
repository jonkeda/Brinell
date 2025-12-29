namespace Brinell.Core.Logging;

/// <summary>
/// Unified test logger interface for CSV logging.
/// All methods write to CSV with consistent columns:
/// Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
/// </summary>
public interface ITestLogger : IDisposable
{
    #region Core Log Method
    
    /// <summary>
    /// Core log method - all other methods call this.
    /// </summary>
    void Log(
        string testName,
        string pageName,
        string controlId,
        string action,
        string? value,
        string? expectedValue,
        LogResult result,
        string? message);
    
    #endregion
    
    #region Action Logging
    
    /// <summary>
    /// Log a control action (Click, EnterText, etc.) that succeeded.
    /// </summary>
    void LogAction(
        string testName,
        string pageName,
        string controlId,
        string action,
        string? value = null);
    
    #endregion
    
    #region Assertion Logging
    
    /// <summary>
    /// Log a successful assertion.
    /// </summary>
    void LogAssertPass(
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue);
    
    /// <summary>
    /// Log a failed assertion.
    /// </summary>
    void LogAssertFail(
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue,
        string? message = null);
    
    #endregion
    
    #region Wait Logging
    
    /// <summary>
    /// Log a wait operation result.
    /// </summary>
    void LogWait(
        string testName,
        string pageName,
        string controlId,
        string waitType,
        bool success,
        int elapsedMs);
    
    #endregion
    
    #region Navigation Logging
    
    /// <summary>
    /// Log navigation between pages.
    /// </summary>
    void LogNavigation(
        string testName,
        string sourcePage,
        string targetPage);
    
    /// <summary>
    /// Log a navigation action (page-level action like NavigateTo, GoBack, etc.)
    /// </summary>
    void LogNavigation(
        string testName,
        string pageName,
        string pageId,
        string action,
        string? value = null);
    
    #endregion
    
    #region Info and Error Logging
    
    /// <summary>
    /// Log an informational message.
    /// </summary>
    void LogInfo(
        string testName,
        string pageName,
        string message);
    
    /// <summary>
    /// Log an error/exception.
    /// </summary>
    void LogError(
        string testName,
        string pageName,
        string controlId,
        string action,
        Exception ex);
    
    #endregion
    
    /// <summary>
    /// Flush buffered entries to disk.
    /// </summary>
    void Flush();
}
