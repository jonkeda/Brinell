namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when a page is not displayed as expected.
/// </summary>
public class PageNotDisplayedException : Exception
{
    public string PageName { get; }
    
    public PageNotDisplayedException(string pageName)
        : base($"Page '{pageName}' is not displayed.")
    {
        PageName = pageName;
    }
    
    public PageNotDisplayedException(string pageName, string message)
        : base(message)
    {
        PageName = pageName;
    }
    
    public PageNotDisplayedException(string pageName, string message, Exception inner)
        : base(message, inner)
    {
        PageName = pageName;
    }
}
