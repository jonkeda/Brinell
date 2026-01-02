namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when a control is in an invalid state for the requested operation.
/// Implements FR-010.2: Exception Types.
/// </summary>
public class InvalidStateException : Exception
{
    /// <summary>
    /// The automation ID of the element.
    /// </summary>
    public string? AutomationId { get; }
    
    /// <summary>
    /// The current state of the element.
    /// </summary>
    public string? CurrentState { get; }
    
    /// <summary>
    /// The expected/required state for the operation.
    /// </summary>
    public string? ExpectedState { get; }
    
    /// <summary>
    /// The operation that was attempted.
    /// </summary>
    public string? Operation { get; }
    
    /// <summary>
    /// Create an invalid state exception with a message.
    /// </summary>
    public InvalidStateException(string message) 
        : base(message) 
    { 
    }
    
    /// <summary>
    /// Create an invalid state exception with full context.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="automationId">The automation ID of the element.</param>
    /// <param name="currentState">The current state of the element.</param>
    /// <param name="expectedState">The expected state for the operation.</param>
    /// <param name="operation">The operation that was attempted.</param>
    public InvalidStateException(
        string message, 
        string automationId, 
        string currentState, 
        string expectedState,
        string? operation = null)
        : base(FormatMessage(message, automationId, currentState, expectedState, operation))
    {
        AutomationId = automationId;
        CurrentState = currentState;
        ExpectedState = expectedState;
        Operation = operation;
    }
    
    /// <summary>
    /// Create an invalid state exception with an inner exception.
    /// </summary>
    public InvalidStateException(string message, Exception innerException)
        : base(message, innerException) 
    { 
    }
    
    private static string FormatMessage(
        string message, 
        string automationId, 
        string currentState, 
        string expectedState,
        string? operation)
    {
        var formatted = $"{message} [AutomationId: {automationId}, Current: {currentState}, Expected: {expectedState}";
        if (!string.IsNullOrEmpty(operation))
            formatted += $", Operation: {operation}";
        formatted += "]";
        return formatted;
    }
}
