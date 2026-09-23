namespace Brinell.Core.Interfaces;

/// <summary>
/// Defines whether a page must expose a page-local busy signal.
/// </summary>
public enum BusySignalPolicy
{
    Disabled,
    Required
}

/// <summary>
/// The last observable state returned by a raw page readiness probe.
/// </summary>
public enum PageReadinessState
{
    MissingRoot,
    StaleRoot,
    MissingBusySignal,
    InvalidBusySignal,
    Busy,
    Ready
}

/// <summary>
/// An instantaneous snapshot of page identity and activity.
/// </summary>
public readonly record struct PageReadinessSnapshot(
    string PageName,
    PageReadinessState State,
    BusySignalPolicy BusySignalPolicy,
    string? BusySignalValue = null,
    bool RootReacquired = false)
{
    public bool IsLoaded => State is not (PageReadinessState.MissingRoot or PageReadinessState.StaleRoot);
    public bool IsBusy => State == PageReadinessState.Busy;
    public bool IsReady => State == PageReadinessState.Ready;
}