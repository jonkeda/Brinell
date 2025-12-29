namespace Brinell.Core.Exceptions;

/// <summary>
/// Exception thrown when a page is not ready for interaction.
/// Pages may not be ready if they are still loading, busy processing, or not displayed.
/// </summary>
public class PageNotReadyException : Exception
{
    /// <summary>
    /// The name of the page that was not ready.
    /// </summary>
    public string? PageName { get; }
    
    /// <summary>
    /// The control that attempted interaction on the unready page.
    /// </summary>
    public string? ControlId { get; }
    
    public PageNotReadyException(string message) 
        : base(message) 
    {
    }
    
    public PageNotReadyException(string message, Exception inner) 
        : base(message, inner) 
    {
    }
    
    public PageNotReadyException(string message, string? pageName, string? controlId = null) 
        : base(message)
    {
        PageName = pageName;
        ControlId = controlId;
    }
    
    public PageNotReadyException(string message, string? pageName, string? controlId, Exception inner) 
        : base(message, inner)
    {
        PageName = pageName;
        ControlId = controlId;
    }
}
