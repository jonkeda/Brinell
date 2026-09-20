namespace Brinell.Maui.Scopes;

/// <summary>What one readiness probe of a scope found.</summary>
public enum ScopeReadinessState
{
    /// <summary>The scope can be used.</summary>
    Ready,

    /// <summary>The scope's root element is not there.</summary>
    MissingRoot,

    /// <summary>The root was there, went stale, and was not there again when looked for once more.</summary>
    StaleRoot,

    /// <summary>The page's root is there, but the page does not report itself loaded.</summary>
    NotLoaded,

    /// <summary>The page reports itself busy.</summary>
    Busy,

    /// <summary>The scope's own content check does not hold yet (a spinner, a row count).</summary>
    ContentNotReady,

    /// <summary>A row now holds another item than the one it was created for.</summary>
    ItemChanged,

    /// <summary>The page requires a busy signal and has none: a configuration error.</summary>
    MissingBusySignal,

    /// <summary>The page's busy signal is not "True" or "False": a configuration error.</summary>
    InvalidBusySignal
}

/// <summary>
/// Which scope answered a readiness probe, and why it is not ready. A scope that asks its parent
/// first returns the parent's answer unchanged when the parent is not ready, so a failure names
/// the scope that actually was not ready.
/// </summary>
/// <param name="ScopeName">The scope that answered.</param>
/// <param name="State">What it found.</param>
/// <param name="Detail">What a message should repeat, such as the busy signal's value.</param>
/// <param name="RootReacquired">Whether the root was found again during this probe.</param>
public readonly record struct ScopeReadiness(
    string ScopeName,
    ScopeReadinessState State,
    string? Detail = null,
    bool RootReacquired = false)
{
    /// <summary>Whether the scope can be used.</summary>
    public bool IsReady => State == ScopeReadinessState.Ready;

    /// <summary>Whether the scope is there and loaded (it may still be busy).</summary>
    public bool IsLoaded => State is not (ScopeReadinessState.MissingRoot
        or ScopeReadinessState.StaleRoot
        or ScopeReadinessState.NotLoaded);

    /// <summary>Whether the page reports itself busy.</summary>
    public bool IsBusy => State == ScopeReadinessState.Busy;

    /// <summary>
    /// Whether the answer is a configuration error, which no amount of waiting fixes: the call
    /// fails at once (R0).
    /// </summary>
    public bool IsConfigurationError => State is ScopeReadinessState.MissingBusySignal
        or ScopeReadinessState.InvalidBusySignal;

    /// <summary>A ready answer from <paramref name="scopeName"/>.</summary>
    public static ScopeReadiness Ready(string scopeName) => new(scopeName, ScopeReadinessState.Ready);

    /// <summary>"TestPage: Busy (busy value: 'True')", for messages.</summary>
    public override string ToString()
        => Detail is null ? $"{ScopeName}: {State}" : $"{ScopeName}: {State} ({Detail})";
}
