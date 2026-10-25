namespace Bruinell.Core.Exceptions;

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
/// short of a control's own requirement. The action did not run.
/// </summary>
public sealed class ElementNotReadyException : BruinellException
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
