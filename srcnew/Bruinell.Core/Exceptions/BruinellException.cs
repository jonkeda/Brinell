namespace Bruinell.Core.Exceptions;

/// <summary>
/// Base exception for all Bruinell framework exceptions.
/// </summary>
public class BruinellException : Exception
{
    public BruinellException(string message) : base(message) { }
    
    public BruinellException(string message, Exception innerException) 
        : base(message, innerException) { }
}
