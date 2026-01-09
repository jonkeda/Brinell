namespace Brinell.Core.Logging;

/// <summary>
/// Console logger implementation that writes to standard output.
/// </summary>
public class ConsoleTestLogger : ITestLogger
{
    public void LogInfo(string testName, string? pageName, string message)
        => Console.WriteLine($"[INFO] [{testName}]{(pageName != null ? $"[{pageName}]" : "")} {message}");
    
    public void LogAction(string testName, string? pageName, string controlId, string action, string? value = null)
        => Console.WriteLine($"[ACTION] [{testName}]{(pageName != null ? $"[{pageName}]" : "")} {controlId}.{action}({value ?? ""})");
    
    public void LogAssert(string testName, string? pageName, string controlId, string assertion, 
                         object? expected, object? actual, bool passed)
        => Console.WriteLine($"[ASSERT] [{testName}]{(pageName != null ? $"[{pageName}]" : "")} {controlId}.{assertion} " +
                            $"Expected: {expected}, Actual: {actual}, {(passed ? "PASSED" : "FAILED")}");
    
    public void LogWait(string testName, string? pageName, string controlId, string waitType, 
                        bool succeeded, int elapsedMs)
        => Console.WriteLine($"[WAIT] [{testName}]{(pageName != null ? $"[{pageName}]" : "")} {controlId}.{waitType} " +
                            $"{(succeeded ? "succeeded" : "failed")} in {elapsedMs}ms");
    
    public void LogError(string testName, string? pageName, string? controlId, string action, Exception exception)
        => Console.WriteLine($"[ERROR] [{testName}]{(pageName != null ? $"[{pageName}]" : "")}" +
                            $"{(controlId != null ? $" {controlId}" : "")} {action}: {exception.Message}");
    
    public void LogNavigation(string testName, string destination)
        => Console.WriteLine($"[NAV] [{testName}] Navigating to: {destination}");
    
    public void LogDebug(string message)
        => Console.WriteLine($"[DEBUG] {message}");
    
    public void LogWarning(string message)
        => Console.WriteLine($"[WARN] {message}");
}
