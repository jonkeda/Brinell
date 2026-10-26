namespace Bruinet.Core.Exceptions;

/// <summary>
/// The element a handle referred to is no longer there: the platform removed or replaced it. A
/// signal, not a failure: the call finds its element again (R5).
/// </summary>
public sealed class StaleElementException : BruinetException
{
    /// <summary>Creates the signal for an element that is gone.</summary>
    /// <param name="locator">The locator the element was found by, when known.</param>
    /// <param name="platformError">The platform's own error, kept as the inner exception.</param>
    /// <param name="detail">What a call observed, for the message: for example how often it was replaced.</param>
    public StaleElementException(Locator? locator = null, Exception? platformError = null, string? detail = null)
        : base(
            (locator is null
                ? "The element is no longer there: it was removed or replaced."
                : $"The element found by '{locator}' is no longer there: it was removed or replaced.")
            + (string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}"),
            platformError ?? new InvalidOperationException("Element not available."))
    {
        Locator = locator;
    }

    /// <summary>The locator the element was found by, when known.</summary>
    public Locator? Locator { get; }
}
