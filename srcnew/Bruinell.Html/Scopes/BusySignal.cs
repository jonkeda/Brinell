namespace Bruinell.Html.Scopes;

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
