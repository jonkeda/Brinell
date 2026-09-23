namespace Brinell.Core.Exceptions;

/// <summary>
/// Base exception for all Brinell framework exceptions.
/// </summary>
public class BrinellException : Exception
{
    public BrinellException(string message) : base(message) { }
    
    public BrinellException(string message, Exception innerException) 
        : base(message, innerException) { }
}
