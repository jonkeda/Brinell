namespace Brinell.Core.Exceptions;

/// <summary>
/// The app under test is gone: its process exited, the browser or page closed, or the driver's
/// session ended. Ends the call at once: waiting cannot bring it back (R0).
/// </summary>
public sealed class AppUnavailableException : BrinellException
{
    /// <summary>Creates the report of an app that is gone.</summary>
    /// <param name="detail">What the driver observed.</param>
    /// <param name="platformError">The platform's own error, kept as the inner exception.</param>
    public AppUnavailableException(string detail, Exception? platformError = null)
        : base($"The app under test is no longer available: {detail}",
               platformError ?? new InvalidOperationException(detail))
    {
    }
}
