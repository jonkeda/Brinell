namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when a Check* method fails (condition not met within timeout).
/// Check methods wait for a condition and throw this exception if it's not met.
/// </summary>
public class CheckFailedException : Exception
{
    /// <summary>
    /// The control's AutomationId where the check failed.
    /// </summary>
    public string? AutomationId { get; }
    
    /// <summary>
    /// The type of check that failed.
    /// </summary>
    public string? CheckType { get; }
    
    public CheckFailedException(string message) 
        : base(message) 
    {
    }
    
    public CheckFailedException(string message, Exception inner) 
        : base(message, inner) 
    {
    }
    
    public CheckFailedException(string message, string? automationId, string? checkType = null) 
        : base(message)
    {
        AutomationId = automationId;
        CheckType = checkType;
    }
    
    public CheckFailedException(string message, string? automationId, string? checkType, Exception inner) 
        : base(message, inner)
    {
        AutomationId = automationId;
        CheckType = checkType;
    }
}
