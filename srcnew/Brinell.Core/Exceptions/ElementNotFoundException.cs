namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when an element cannot be found in the UI tree.
/// </summary>
public class ElementNotFoundException : BrinellException
{
    public string? Locator { get; }
    
    public ElementNotFoundException(string message) : base(message) { }
    
    public ElementNotFoundException(string message, string locator) : base(message)
    {
        Locator = locator;
    }
    
    public ElementNotFoundException(string message, Exception innerException) 
        : base(message, innerException) { }
}
