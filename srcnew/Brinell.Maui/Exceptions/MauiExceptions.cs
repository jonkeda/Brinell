namespace Brinell.Maui.Exceptions;

/// <summary>
/// The element a handle referred to is no longer in the UI tree: the platform removed or
/// replaced it.
/// </summary>
/// <remarks>
/// <para>
/// A signal, not a failure in itself (<c>.my/stale-readiness/design.md</c>, R5). A call's poll
/// finds the element again when it sees one. A confirmation that sees one reports that the element
/// was replaced, and never repeats the action.
/// </para>
/// <para>
/// Raised by the drivers, which translate their platform's own signal: UI Automation's
/// <c>UIA_E_ELEMENTNOTAVAILABLE</c> on Windows, Selenium's stale-reference error through Appium.
/// Nothing above the drivers names a platform type.
/// </para>
/// </remarks>
public sealed class StaleElementException : BrinellException
{
    /// <summary>Creates the signal for an element that is gone.</summary>
    /// <param name="locator">The locator the element was found by, when known.</param>
    /// <param name="platformError">The platform's own error, kept as the inner exception.</param>
    /// <param name="detail">What a call observed, for the message: for example how often it was replaced.</param>
    public StaleElementException(Locator? locator = null, Exception? platformError = null, string? detail = null)
        : base(
            (locator is null
                ? "The element is no longer in the UI tree: the platform removed or replaced it."
                : $"The element found by '{locator}' is no longer in the UI tree: the platform removed or replaced it.")
            + (string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}"),
            platformError ?? new InvalidOperationException("Element not available."))
    {
        Locator = locator;
    }

    /// <summary>The locator the element was found by, when known.</summary>
    public Locator? Locator { get; }
}

/// <summary>Why a found element is not ready for what a call wants.</summary>
public enum NotReadyReason
{
    /// <summary>It is not on screen, even after scrolling to it.</summary>
    NotVisible,

    /// <summary>It is shown but disabled.</summary>
    Disabled,

    /// <summary>Anything else a control requires before it can be acted on.</summary>
    Other
}

/// <summary>
/// The element was found, but is not ready for what the call wants: not visible, disabled, or
/// short of a control's own requirement.
/// </summary>
/// <remarks>
/// A check that makes one attempt raises this; the call's poll decides whether to try again. It
/// replaces the <c>TimeoutException</c> such checks used to raise, which claimed a timeout from a
/// check that never waited.
/// </remarks>
public sealed class ElementNotReadyException : BrinellException
{
    /// <summary>Creates the report of an element that is not ready.</summary>
    /// <param name="locator">The locator the element was found by.</param>
    /// <param name="reason">What it is short of.</param>
    /// <param name="detail">Anything more specific, for the message.</param>
    public ElementNotReadyException(Locator locator, NotReadyReason reason, string? detail = null)
        : base(BuildMessage(locator, reason, detail))
    {
        Locator = locator;
        Reason = reason;
        Detail = detail;
    }

    /// <summary>The locator the element was found by.</summary>
    public Locator Locator { get; }

    /// <summary>What the element is short of.</summary>
    public NotReadyReason Reason { get; }

    /// <summary>Anything more specific, or null.</summary>
    public string? Detail { get; }

    private static string BuildMessage(Locator locator, NotReadyReason reason, string? detail)
    {
        var state = reason switch
        {
            NotReadyReason.NotVisible => "was found but is not visible",
            NotReadyReason.Disabled => "was found but is disabled",
            _ => "was found but is not ready for this action"
        };

        return string.IsNullOrWhiteSpace(detail)
            ? $"The element '{locator}' {state}."
            : $"The element '{locator}' {state}: {detail}";
    }
}

/// <summary>
/// A scope a call stands in (a page, a container, a row) did not become ready within the call's
/// budget, or is misconfigured.
/// </summary>
/// <remarks>
/// Replaces the <c>PageLoadException</c> MAUI used to throw: the scope that was not ready may be a
/// container or a row, not only a page. <see cref="Readiness"/> says which, and what it found.
/// </remarks>
public sealed class ScopeNotReadyException : BrinellException
{
    /// <summary>Creates the report of a scope that was not ready.</summary>
    /// <param name="readiness">The last readiness answer.</param>
    /// <param name="message">The full message, naming the call and its budget.</param>
    public ScopeNotReadyException(Scopes.ScopeReadiness readiness, string message)
        : base(message)
    {
        Readiness = readiness;
    }

    /// <summary>The last readiness answer: which scope, and what it found.</summary>
    public Scopes.ScopeReadiness Readiness { get; }
}

/// <summary>
/// The app under test is gone: its process exited, or the driver's session ended.
/// </summary>
/// <remarks>
/// Never retried (<c>.my/stale-readiness/design.md</c>, R0): no later attempt can succeed, and
/// the crash or disconnect is itself the finding. Raised by the drivers.
/// </remarks>
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
