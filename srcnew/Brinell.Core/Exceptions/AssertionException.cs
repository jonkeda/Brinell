namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when an assertion fails.
/// </summary>
public class AssertionException : BrinellException
{
    public object? Expected { get; }
    public object? Actual { get; }
    public string? ControlLocator { get; }
    
    public AssertionException(string message) : base(message) { }
    
    public AssertionException(string message, object? expected, object? actual) : base(message)
    {
        Expected = expected;
        Actual = actual;
    }
    
    public AssertionException(string message, object? expected, object? actual, string controlLocator) 
        : base(message)
    {
        Expected = expected;
        Actual = actual;
        ControlLocator = controlLocator;
    }
    
    public AssertionException(string message, Exception innerException) 
        : base(message, innerException) { }
}
