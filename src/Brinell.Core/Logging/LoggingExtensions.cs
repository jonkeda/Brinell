using Brinell.Core.Abstractions;
using Brinell.Core.Exceptions;

namespace Brinell.Core.Logging;

/// <summary>
/// Extension methods for log-and-throw pattern.
/// These ensure exceptions are always logged to CSV before being thrown.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Log a CheckFailedException to CSV and throw it.
    /// Use this instead of bare "throw new CheckFailedException(...)".
    /// </summary>
    /// <returns>Never returns - always throws.</returns>
    /// <exception cref="CheckFailedException">Always thrown after logging.</exception>
    public static CheckFailedException ThrowCheckFailed(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string controlId,
        string checkType,
        string message,
        ITestContext? context = null)
    {
        // Capture screenshot before throwing (only for failures)
        context?.CaptureFailureScreenshot($"check-failed-{SanitizeSuffix(controlId)}");
        
        var ex = new CheckFailedException(message, controlId, checkType);
        logger?.LogError(testName, pageName, controlId, $"Check.{checkType}", ex);
        throw ex;
    }
    
    /// <summary>
    /// Log an AssertionException to CSV and throw it.
    /// Use this instead of bare "throw new AssertionException(...)".
    /// </summary>
    /// <returns>Never returns - always throws.</returns>
    /// <exception cref="AssertionException">Always thrown after logging.</exception>
    public static AssertionException ThrowAssertionFailed(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue,
        string message,
        ITestContext? context = null)
    {
        // Capture screenshot before throwing (only for failures)
        context?.CaptureFailureScreenshot($"assert-failed-{SanitizeSuffix(controlId)}");
        
        logger?.LogAssertFail(testName, pageName, controlId, assertType, actualValue, expectedValue, message);
        throw new AssertionException(message);
    }
    
    /// <summary>
    /// Log a PageNotReadyException to CSV and throw it.
    /// </summary>
    /// <returns>Never returns - always throws.</returns>
    /// <exception cref="PageNotReadyException">Always thrown after logging.</exception>
    public static PageNotReadyException ThrowPageNotReady(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string pageId,
        string action,
        string message,
        ITestContext? context = null)
    {
        // Capture screenshot before throwing (only for failures)
        context?.CaptureFailureScreenshot($"page-not-ready-{SanitizeSuffix(pageId)}");
        
        var ex = new PageNotReadyException(message, pageName);
        logger?.LogError(testName, pageName, pageId, action, ex);
        throw ex;
    }
    
    /// <summary>
    /// Log a PageNotDisplayedException to CSV and throw it.
    /// </summary>
    /// <returns>Never returns - always throws.</returns>
    /// <exception cref="PageNotDisplayedException">Always thrown after logging.</exception>
    public static PageNotDisplayedException ThrowPageNotDisplayed(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string pageId,
        string action,
        string message,
        ITestContext? context = null)
    {
        // Capture screenshot before throwing (only for failures)
        context?.CaptureFailureScreenshot($"page-not-displayed-{SanitizeSuffix(pageId)}");
        
        var ex = new PageNotDisplayedException(pageName, message);
        logger?.LogError(testName, pageName, pageId, action, ex);
        throw ex;
    }
    
    /// <summary>
    /// Log any exception to CSV and throw it.
    /// Use this for generic exception types.
    /// </summary>
    /// <returns>Never returns - always throws.</returns>
    public static T LogAndThrow<T>(
        this ITestLogger? logger,
        string testName,
        string pageName,
        string controlId,
        string action,
        T exception,
        ITestContext? context = null) where T : Exception
    {
        // Capture screenshot before throwing (only for failures)
        context?.CaptureFailureScreenshot($"exception-{SanitizeSuffix(controlId)}");
        
        logger?.LogError(testName, pageName, controlId, action, exception);
        throw exception;
    }
    
    private static string SanitizeSuffix(string suffix)
    {
        if (string.IsNullOrEmpty(suffix))
            return "unknown";
            
        return suffix
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(":", "-")
            .Replace("[", "")
            .Replace("]", "");
    }
}
