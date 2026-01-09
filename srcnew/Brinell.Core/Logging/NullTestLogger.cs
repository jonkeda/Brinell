namespace Brinell.Core.Logging;

/// <summary>
/// Null logger implementation that discards all messages.
/// Used as default when no logger is configured.
/// </summary>
public class NullTestLogger : ITestLogger
{
    public static NullTestLogger Instance { get; } = new();
    
    public void LogInfo(string testName, string? pageName, string message) { }
    
    public void LogAction(string testName, string? pageName, string controlId, string action, string? value = null) { }
    
    public void LogAssert(string testName, string? pageName, string controlId, string assertion, 
                         object? expected, object? actual, bool passed) { }
    
    public void LogWait(string testName, string? pageName, string controlId, string waitType, 
                        bool succeeded, int elapsedMs) { }
    
    public void LogError(string testName, string? pageName, string? controlId, string action, Exception exception) { }
    
    public void LogNavigation(string testName, string destination) { }
    
    public void LogDebug(string message) { }
    
    public void LogWarning(string message) { }
}
