namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when a UI element cannot be found.
/// </summary>
public class ElementNotFoundException : Exception
{
    public ElementNotFoundException(string message)
        : base(message)
    {
    }

    public ElementNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
