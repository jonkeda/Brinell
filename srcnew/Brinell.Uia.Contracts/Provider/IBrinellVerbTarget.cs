namespace Brinell.Uia.Provider;

/// <summary>A rectangle in screen coordinates.</summary>
/// <param name="Left">Left edge, in physical pixels.</param>
/// <param name="Top">Top edge, in physical pixels.</param>
/// <param name="Width">Width, in physical pixels.</param>
/// <param name="Height">Height, in physical pixels.</param>
public readonly record struct BrinellBounds(double Left, double Top, double Width, double Height)
{
    /// <summary>A rectangle of no extent, for a target whose position is unknown.</summary>
    public static BrinellBounds Empty => new(0, 0, 0, 0);
}

/// <summary>
/// One thing the bridge can act on, as the hosting UI framework sees it.
/// </summary>
/// <remarks>
/// <para>
/// The seam between the UI Automation machinery and the UI framework. Everything above this
/// interface - the window, the fragment tree, the pattern, the marshalling - is framework
/// neutral and lives in this project; everything below it knows about MAUI, or WPF, or
/// whatever is being instrumented, and lives with that framework.
/// </para>
/// <para>
/// <b>Nothing here is read on a tree walk.</b> UI Automation reads an element's properties
/// constantly and calls its methods rarely, so every member of this interface that could cost a
/// hop onto the UI thread has been kept off the property side of the provider - see
/// <c>BridgeTargetProvider.BoundingRectangle</c> for what happens otherwise. Position is
/// answered by a verb, paid for by whoever asks.
/// </para>
/// <para>
/// <b>Implementations must not throw.</b> These methods are called from a COM dispatch and
/// return HRESULTs from <see cref="HResults"/>. They are also called on whatever thread UI
/// Automation happens to use, so marshalling to the UI thread is the implementation's job -
/// and so is bounding how long it waits, because a provider that blocks forever hangs the
/// client rather than failing it.
/// </para>
/// </remarks>
public interface IBrinellVerbTarget
{
    /// <summary>
    /// The <c>AutomationId</c> of the element this stands for.
    /// </summary>
    /// <remarks>
    /// The join between the two trees. The bridge element publishes
    /// <see cref="BrinellUiaIds.TargetAutomationIdFor"/> of this value, which is how a client
    /// that has found the real element locates its bridge element in one query.
    /// </remarks>
    string AutomationId { get; }

    /// <summary>What this target can actually do.</summary>
    /// <remarks>
    /// Meta verbs are not listed: the bridge answers those itself for every target, so a
    /// target that implemented them could only disagree.
    /// </remarks>
    IReadOnlyCollection<BrinellVerb> Capabilities { get; }

    /// <summary>
    /// Whether the underlying element still exists.
    /// </summary>
    /// <remarks>
    /// False once the element is unloaded. The bridge reports
    /// <see cref="HResults.UIA_E_ELEMENTNOTAVAILABLE"/> rather than pretending, so a stale test
    /// gets a retryable answer instead of a silent no-op.
    /// </remarks>
    bool IsAvailable { get; }

    /// <summary>Performs a numeric verb.</summary>
    /// <param name="verb">The verb to perform.</param>
    /// <param name="arg1">First argument, meaning defined per verb.</param>
    /// <param name="arg2">Second argument, meaning defined per verb.</param>
    /// <returns>An <see cref="HResults"/> value.</returns>
    int Invoke(BrinellVerb verb, int arg1, int arg2);

    /// <summary>Performs a verb that takes or returns text.</summary>
    /// <param name="verb">The verb to perform.</param>
    /// <param name="argument">The argument, possibly empty.</param>
    /// <param name="result">The result. Set to the empty string when there is none.</param>
    /// <returns>An <see cref="HResults"/> value.</returns>
    int Exchange(BrinellVerb verb, string argument, out string result);
}
