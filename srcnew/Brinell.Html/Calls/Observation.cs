using Brinell.Html.Exceptions;

namespace Brinell.Html.Calls;

/// <summary>What one attempt of a call saw.</summary>
internal enum ObservationKind
{
    /// <summary>The attempt succeeded; the call is done.</summary>
    Done,

    /// <summary>The scope the call stands in (a page or container) was not ready.</summary>
    ScopeNotReady,

    /// <summary>The element was not found.</summary>
    Missing,

    /// <summary>The element was found but not ready: not visible, disabled, or short of a control requirement.</summary>
    NotReady,

    /// <summary>The element was found, then turned out to be gone.</summary>
    Stale,

    /// <summary>A comparison did not hold (an assertion's actual value was not the expected one).</summary>
    Mismatch,

    /// <summary>Nothing wrong, but not there yet: a predicate is still false.</summary>
    Pending,

    /// <summary>An unexpected exception.</summary>
    Failed,
}

/// <summary>What one attempt of a call saw.</summary>
/// <param name="Kind">The outcome.</param>
/// <param name="InstanceKey">The element seen, when one was found and could say who it was.</param>
/// <param name="Reason">For <see cref="ObservationKind.NotReady"/>: what the element was short of.</param>
/// <param name="Detail">Anything a message should repeat, in words.</param>
/// <param name="Error">The exception behind the observation, when there was one.</param>
internal readonly record struct Observation(
    ObservationKind Kind,
    string? InstanceKey = null,
    NotReadyReason? Reason = null,
    string? Detail = null,
    Exception? Error = null)
{
    public static Observation Done(string? instanceKey = null) => new(ObservationKind.Done, instanceKey);

    public static Observation Missing(string? detail = null) => new(ObservationKind.Missing, Detail: detail);

    public static Observation Pending(string? detail = null, string? instanceKey = null)
        => new(ObservationKind.Pending, instanceKey, Detail: detail);

    public static Observation ScopeNotReady(string scopeName) => new(ObservationKind.ScopeNotReady, Detail: scopeName);

    public static Observation NotReady(ElementNotReadyException error, string? instanceKey)
        => new(ObservationKind.NotReady, instanceKey, error.Reason, error.Detail, error);

    public static Observation Stale(StaleElementException error, string? instanceKey)
        => new(ObservationKind.Stale, instanceKey, Error: error);

    public static Observation Mismatch(Exception error, string? instanceKey)
        => new(ObservationKind.Mismatch, instanceKey, Error: error);

    public static Observation Failed(Exception error) => new(ObservationKind.Failed, Error: error);

    /// <summary>"NotReady(Disabled)", "Missing", "Failed(InvalidOperationException)", for summaries.</summary>
    public override string ToString() => Kind switch
    {
        ObservationKind.NotReady => $"NotReady({Reason})",
        ObservationKind.Failed or ObservationKind.Mismatch when Error != null => $"{Kind}({Error.GetType().Name})",
        _ when Detail != null => $"{Kind}({Detail})",
        _ => Kind.ToString(),
    };
}
