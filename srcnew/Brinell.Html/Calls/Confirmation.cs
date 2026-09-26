using Brinell.Core.Locators;
using Brinell.Html.Exceptions;

namespace Brinell.Html.Calls;

/// <summary>How the wait for an action's effect ended.</summary>
public enum ConfirmationResult
{
    /// <summary>The effect was seen.</summary>
    Confirmed,

    /// <summary>The budget ran out without the effect.</summary>
    NotConfirmed,

    /// <summary>The element was replaced before the effect could be read on it.</summary>
    Replaced,
}

// R0: the action is never repeated - a control that ignores it is an app bug the test must
// report. See .docs/contracts/call-model.md and .docs/decisions/ad-009-app-bugs-stay-failures.md.

/// <summary>The outcome of a confirmation, with the last value and error it saw.</summary>
public readonly record struct Confirmation<T>(ConfirmationResult Result, T? LastValue, Exception? LastError)
{
    /// <summary>Whether the effect was seen.</summary>
    public bool IsConfirmed => Result == ConfirmationResult.Confirmed;

    /// <summary>The failure to throw for an unconfirmed action.</summary>
    public Exception Failure(Locator locator, string action, Func<Exception?, Exception> notConfirmed)
        => Result == ConfirmationResult.Replaced
            ? new StaleElementException(
                locator,
                LastError,
                $"It was replaced after {action}, which ran once, so its effect could not be confirmed on it. "
                + "The action is not repeated.")
            : notConfirmed(LastError);
}
