namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when an Assert* method fails.
/// Assert methods perform immediate semantic checks and throw this exception when the condition is not met.
/// </summary>
public class AssertionException : Exception
{
    /// <summary>
    /// The control's AutomationId where the assertion failed.
    /// </summary>
    public string? AutomationId { get; }
    
    /// <summary>
    /// The type of assertion that failed.
    /// </summary>
    public string? AssertionType { get; }
    
    public AssertionException(string message) 
        : base(message) 
    {
    }
    
    public AssertionException(string message, Exception inner) 
        : base(message, inner) 
    {
    }
    
    public AssertionException(string message, string? automationId, string? assertionType = null) 
        : base(message)
    {
        AutomationId = automationId;
        AssertionType = assertionType;
    }
    
    public AssertionException(string message, string? automationId, string? assertionType, Exception inner) 
        : base(message, inner)
    {
        AutomationId = automationId;
        AssertionType = assertionType;
    }
}
