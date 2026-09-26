namespace Brinell.Html.Scopes;

// The same type as Brinell.Maui.Scopes.ScopeReadiness (.my/html/design.md 7.1), so both stacks
// report readiness in the same words and the move down to Core has one shape to take.

/// <summary>What one readiness probe of a scope found.</summary>
public enum ScopeReadinessState
{
    /// <summary>The scope can be used.</summary>
    Ready,

    /// <summary>The scope's root element is not there.</summary>
    MissingRoot,

    /// <summary>The root was there, went stale, and was not there again when looked for once more.</summary>
    StaleRoot,

    /// <summary>The page is there, but does not report itself loaded (its URL, its load state, its busy signal not yet set).</summary>
    NotLoaded,

    /// <summary>The page reports itself busy.</summary>
    Busy,

    /// <summary>The scope's own content check does not hold yet (a spinner, a row count).</summary>
    ContentNotReady,

    /// <summary>A row now holds another item than the one it was created for.</summary>
    ItemChanged,

    /// <summary>The page requires a busy signal and has none: a configuration error.</summary>
    MissingBusySignal,

    /// <summary>The page's busy signal is not "true" or "false": a configuration error.</summary>
    InvalidBusySignal,
}

/// <summary>
/// Which scope answered a readiness probe, and why it is not ready. A scope that asks its parent
/// first returns the parent's answer unchanged when the parent is not ready, so a failure names
/// the scope that actually was not ready.
/// </summary>
/// <param name="ScopeName">The scope that answered.</param>
/// <param name="State">What it found.</param>
/// <param name="Detail">What a message should repeat, such as the busy signal's value.</param>
public readonly record struct ScopeReadiness(
    string ScopeName,
    ScopeReadinessState State,
    string? Detail = null)
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

    /// <summary>"LoginPage: Busy (data-busy: 'true')", for messages.</summary>
    public override string ToString()
        => Detail is null ? $"{ScopeName}: {State}" : $"{ScopeName}: {State} ({Detail})";
}

/// <summary>
/// How a page reads the <c>data-busy</c> attribute on <c>&lt;body&gt;</c> (design 7.2, ?A8).
/// </summary>
/// <remarks>
/// Core's <c>BusySignalPolicy</c> has only Disabled and Required. Html needs a third meaning, and
/// its Required differs: a Blazor app sets the attribute when it becomes interactive, so a missing
/// attribute means "not loaded yet", not a configuration error.
/// </remarks>
public enum BusySignal
{
    /// <summary>The attribute is not read.</summary>
    Ignore,

    /// <summary>"true" is busy; "false" or no attribute is idle; anything else is a configuration error.</summary>
    Optional,

    /// <summary>
    /// "true" is busy and "false" idle. No attribute means the page is not loaded yet: the app sets
    /// it once it is interactive, so a prerendered page is not acted on before then.
    /// </summary>
    Required,
}
