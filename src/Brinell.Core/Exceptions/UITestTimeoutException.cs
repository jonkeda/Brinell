namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when a UI test operation times out.
/// Implements FR-010.2: Exception Types.
/// </summary>
public class UITestTimeoutException : Exception
{
    /// <summary>
    /// The automation ID of the element that timed out.
    /// </summary>
    public string? AutomationId { get; }
    
    /// <summary>
    /// The timeout value in milliseconds.
    /// </summary>
    public int TimeoutMs { get; }
    
    /// <summary>
    /// The operation that timed out (e.g., "WaitVisible", "CheckExists").
    /// </summary>
    public string? Operation { get; }
    
    /// <summary>
    /// The current state of the element when timeout occurred.
    /// </summary>
    public string? CurrentState { get; }
    
    /// <summary>
    /// Create a timeout exception with a message.
    /// </summary>
    public UITestTimeoutException(string message) 
        : base(message) 
    { 
    }
    
    /// <summary>
    /// Create a timeout exception with full context.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="automationId">The automation ID of the element.</param>
    /// <param name="timeoutMs">The timeout value in milliseconds.</param>
    /// <param name="operation">The operation that timed out.</param>
    /// <param name="currentState">The current state when timeout occurred.</param>
    public UITestTimeoutException(
        string message, 
        string automationId, 
        int timeoutMs, 
        string? operation = null,
        string? currentState = null)
        : base(FormatMessage(message, automationId, timeoutMs, operation, currentState))
    {
        AutomationId = automationId;
        TimeoutMs = timeoutMs;
        Operation = operation;
        CurrentState = currentState;
    }
    
    /// <summary>
    /// Create a timeout exception with an inner exception.
    /// </summary>
    public UITestTimeoutException(string message, Exception innerException)
        : base(message, innerException) 
    { 
    }
    
    private static string FormatMessage(
        string message, 
        string automationId, 
        int timeoutMs, 
        string? operation, 
        string? currentState)
    {
        var formatted = $"{message} [AutomationId: {automationId}, Timeout: {timeoutMs}ms";
        if (!string.IsNullOrEmpty(operation))
            formatted += $", Operation: {operation}";
        if (!string.IsNullOrEmpty(currentState))
            formatted += $", CurrentState: {currentState}";
        formatted += "]";
        return formatted;
    }
}
