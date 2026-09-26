using Brinell.Html.Exceptions;
using Brinell.Html.Interfaces;
using Brinell.Html.Scopes;

namespace Brinell.Html.Calls;

/// <summary>
/// The readiness step every attempt of a call starts with, and the failure it produces.
/// </summary>
internal static class ScopeGate
{
    /// <summary>One probe of <paramref name="scope"/>: null when it is ready, otherwise the observation to report.</summary>
    /// <exception cref="ScopeNotReadyException">
    /// The scope is misconfigured (a busy signal that cannot be read). No amount of waiting fixes
    /// that, so the call fails at once (R0).
    /// </exception>
    public static Observation? Check(IHtmlElementScope scope) => Check(scope.ProbeReadiness());

    /// <inheritdoc cref="Check(IHtmlElementScope)"/>
    public static Observation? Check(ScopeReadiness readiness)
    {
        if (readiness.IsReady)
        {
            return null;
        }

        if (readiness.IsConfigurationError)
        {
            throw Misconfigured(readiness);
        }

        return Observation.ScopeNotReady(readiness);
    }

    /// <summary>The failure of a scope that is misconfigured, whatever call asked.</summary>
    public static ScopeNotReadyException Misconfigured(ScopeReadiness readiness)
        => new(readiness, $"Scope '{readiness.ScopeName}' is misconfigured ({readiness.State}): {readiness.Detail}");

    /// <summary>The failure of a call whose scope never became ready.</summary>
    public static ScopeNotReadyException NotReady(ScopeReadiness readiness, string caller, string subject, int budgetMs)
        => new(readiness,
            $"Scope '{readiness.ScopeName}' did not become ready for {caller} on {subject} within {budgetMs} ms. " +
            $"Last readiness state: {readiness.State}" +
            (readiness.Detail is null ? "." : $" ({readiness.Detail})."));
}
