namespace Brinell.Core.Calls;

/// <summary>
/// What is left of the running call's budget, for a wait below the call: a driver passes it to
/// every platform wait, so no wait outlasts the caller's timeout (?A5, R2, R3).
/// </summary>
public static class CallBudget
{
    /// <summary>
    /// The running call's remaining budget, at least 1 ms, or <paramref name="outsideACallMs"/>
    /// when no call is running (a plain <c>Is*</c> member).
    /// </summary>
    public static int RemainingOr(int outsideACallMs)
        => AttemptContext.Current is { } call ? Math.Max(1, call.Deadline.RemainingMs) : outsideACallMs;
}
