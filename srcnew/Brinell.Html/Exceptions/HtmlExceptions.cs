using Brinell.Core.Exceptions;
using Brinell.Core.Locators;

namespace Brinell.Html.Exceptions;

// The same set, and the same messages, as Brinell.Maui.Exceptions (.my/html/design.md 4.3).
// ScopeNotReadyException arrives with ScopeReadiness in plan step 5.

/// <summary>
/// The element was removed from the page or replaced. A signal, not a failure: the call finds its
/// element again (R5).
/// </summary>
public sealed class StaleElementException : BrinellException
{
    /// <summary>Reports a removed or replaced element.</summary>
    public StaleElementException(Locator? locator = null, Exception? platformError = null, string? detail = null)
        : base(
            (locator is null
                ? "The element is no longer in the page: it was removed or replaced."
                : $"The element found by '{locator}' is no longer in the page: it was removed or replaced.")
            + (string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}"),
            platformError ?? new InvalidOperationException("Element not available."))
    {
        Locator = locator;
    }

    /// <summary>The locator that found the element, when known.</summary>
    public Locator? Locator { get; }
}

/// <summary>Why a found element is not ready for an action.</summary>
public enum NotReadyReason
{
    /// <summary>Found, but not visible.</summary>
    NotVisible,

    /// <summary>Found, but disabled.</summary>
    Disabled,

    /// <summary>Found, but not ready for another reason.</summary>
    Other,
}

/// <summary>
/// The element was found but is not ready for the action; the action did not run.
/// </summary>
public sealed class ElementNotReadyException : BrinellException
{
    /// <summary>Reports an element that is not ready.</summary>
    public ElementNotReadyException(Locator locator, NotReadyReason reason, string? detail = null)
        : base(BuildMessage(locator, reason, detail))
    {
        Locator = locator;
        Reason = reason;
        Detail = detail;
    }

    /// <summary>The element's locator.</summary>
    public Locator Locator { get; }

    /// <summary>What was wrong.</summary>
    public NotReadyReason Reason { get; }

    /// <summary>What was seen, when there is more to say.</summary>
    public string? Detail { get; }

    private static string BuildMessage(Locator locator, NotReadyReason reason, string? detail)
    {
        var state = reason switch
        {
            NotReadyReason.NotVisible => "was found but is not visible",
            NotReadyReason.Disabled => "was found but is disabled",
            _ => "was found but is not ready for this action",
        };
        return string.IsNullOrWhiteSpace(detail)
            ? $"The element '{locator}' {state}."
            : $"The element '{locator}' {state}: {detail}";
    }
}

/// <summary>
/// The browser, its context or the page has closed. Ends the call at once: waiting cannot bring
/// it back (R0).
/// </summary>
public sealed class AppUnavailableException : BrinellException
{
    /// <summary>Reports that the app under test is gone.</summary>
    public AppUnavailableException(string detail, Exception? platformError = null)
        : base($"The app under test is no longer available: {detail}",
               platformError ?? new InvalidOperationException(detail))
    {
    }
}

/// <summary>
/// There is no route for this action on this element or driver. Ends the call at once; a plain
/// <see cref="NotSupportedException"/> is still retried.
/// </summary>
public sealed class RouteUnavailableException : NotSupportedException
{
    /// <summary>Reports a missing route.</summary>
    public RouteUnavailableException(string message)
        : base(message)
    {
    }
}
