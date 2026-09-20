namespace Brinell.Maui.Controls.Base;

/// <summary>How the wait for an action's effect ended.</summary>
public enum ConfirmationResult
{
    /// <summary>The effect was seen.</summary>
    Confirmed,

    /// <summary>The budget ran out and the effect was not seen: the action did nothing visible.</summary>
    NotConfirmed,

    /// <summary>
    /// The element was replaced while the effect was awaited, so this handle can no longer say
    /// whether it happened.
    /// </summary>
    Replaced
}

// R0: the action is never repeated - a control that ignores it is an app bug the test must
// report. See .docs/contracts/call-model.md and .docs/decisions/ad-009-app-bugs-stay-failures.md.
/// <summary>
/// What a Core method saw while it waited for the effect of an action it had already done. It
/// words its failure by <see cref="Result"/>, usually through <see cref="Failure"/>.
/// </summary>
/// <typeparam name="T">The value read.</typeparam>
/// <param name="Result">How the wait ended.</param>
/// <param name="LastValue">The last value read.</param>
/// <param name="LastError">The last read's exception, or null.</param>
public readonly record struct Confirmation<T>(ConfirmationResult Result, T? LastValue, Exception? LastError)
{
    /// <summary>Whether the effect was seen.</summary>
    public bool IsConfirmed => Result == ConfirmationResult.Confirmed;

    /// <summary>
    /// The exception for a wait that did not confirm: <see cref="StaleElementException"/> when the
    /// element was replaced, otherwise the control's own <paramref name="notConfirmed"/>.
    /// </summary>
    /// <param name="locator">The control's locator.</param>
    /// <param name="action">The action, for the message: "Toggle", "the swipe".</param>
    /// <param name="notConfirmed">
    /// The failure of a control that ignored the action, given the last read's error.
    /// </param>
    public Exception Failure(Locator locator, string action, Func<Exception?, Exception> notConfirmed)
        => Result == ConfirmationResult.Replaced
            ? new StaleElementException(
                locator,
                LastError,
                $"It was replaced after {action}, which ran once, so its effect could not be confirmed on it. "
                + "The action is not repeated.")
            : notConfirmed(LastError);
}
