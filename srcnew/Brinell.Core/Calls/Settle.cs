namespace Brinell.Core.Calls;

/// <summary>
/// A driver's short wait below a call, for the effect of what it just did (a dropdown that opens,
/// a shell that settles): one confirmation (R3), its window cut to what the running call has left
/// (<see cref="CallBudget.Within"/>). What replaced the first model's <c>WaitHelper.WaitFor</c> in
/// the call-model stacks (AD-029).
/// </summary>
public static class Settle
{
    /// <summary>Polls <paramref name="condition"/> until it holds, within the window; false when it never did.</summary>
    /// <param name="condition">One look; an exception is a "not yet", kept for the caller's message.</param>
    /// <param name="windowMs">The wait's own window, outside a call.</param>
    /// <param name="pollingIntervalMs">The pause between looks.</param>
    public static bool Until(Func<bool> condition, int windowMs, int pollingIntervalMs = 50)
    {
        ArgumentNullException.ThrowIfNull(condition);
        return Confirmer.Run(condition, held => held, CallBudget.Within(windowMs), pollingIntervalMs).IsConfirmed;
    }
}
