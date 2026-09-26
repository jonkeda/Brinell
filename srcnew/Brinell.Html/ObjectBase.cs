using System.Runtime.CompilerServices;
using Brinell.Html.Calls;
using Brinell.Html.Interfaces;
using Brinell.Html.Scopes;

namespace Brinell.Html;

/// <summary>
/// Base class for HTML page objects, containers and controls.
/// </summary>
public abstract class ObjectBase
{
    public abstract IHtmlTestContext Context { get; }

    protected int DefaultTimeoutMs => Context.Timeouts.DefaultWait;

    protected int PollingIntervalMs => Context.Timeouts.PollingInterval;

    /// <summary>The name this object's own calls are logged under.</summary>
    protected virtual string ScopeName => GetType().Name;

    /// <summary>
    /// A scope-level wait as one call (R1): polls <paramref name="probe"/> until it reports Done or
    /// the budget runs out. Answers false when it ran out, or throws what
    /// <paramref name="fail"/> builds from the last readiness seen.
    /// </summary>
    private protected bool RunProbe(
        Func<Observation> probe,
        int budgetMs,
        Func<ScopeReadiness, Exception>? fail = null,
        [CallerMemberName] string? caller = null)
    {
        caller ??= nameof(RunProbe);
        return new ControlCall(Context, ScopeName, ScopeName).Run(caller, null, budgetMs, Context.Timeouts.Animation, attempt =>
        {
            if (Poller.Until(_ => probe(), attempt, PollingIntervalMs))
            {
                return true;
            }

            var last = attempt.Log.Last;
            if (fail != null)
            {
                throw fail(last.Scope ?? new ScopeReadiness(ScopeName, ScopeReadinessState.NotLoaded, last.ToString()));
            }

            return last.Kind == ObservationKind.Failed && last.Error is { } error
                ? throw attempt.Log.Unexpected(error, $"'{ScopeName}'", budgetMs)
                : false;
        }, succeeded: ok => ok);
    }
}
