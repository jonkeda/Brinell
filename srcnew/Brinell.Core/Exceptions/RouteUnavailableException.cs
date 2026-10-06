namespace Brinell.Core.Exceptions;

/// <summary>
/// There is no route for what was asked: this element, driver or platform has no way to do it, and
/// will not grow one while the call runs. Ends the call at once; a plain
/// <see cref="NotSupportedException"/> is still retried.
/// </summary>
public sealed class RouteUnavailableException : NotSupportedException
{
    /// <summary>Creates the report of something the platform has no route for.</summary>
    /// <param name="message">What was asked, and what to use instead.</param>
    public RouteUnavailableException(string message) : base(message)
    {
    }
}
