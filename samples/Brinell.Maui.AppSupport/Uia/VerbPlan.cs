using Brinell.Uia;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// What one published element will answer, decided when it was published.
/// </summary>
/// <remarks>
/// <para>
/// <b>The point of the type is that it is finished.</b> Everything in it was worked out by
/// <see cref="VerbBindings.Resolve"/> from facts that cannot change while the element is on
/// screen, so the path a test waits on holds a lookup and nothing else - no search, no probe,
/// no sequence of things to attempt.
/// </para>
/// <para>
/// <b>The sink is held weakly</b>, for the same reason <see cref="MauiVerbTarget"/> holds its
/// element weakly: a sink is usually a view model, and a registration that was never withdrawn
/// must not keep one alive for the life of the window.
/// </para>
/// </remarks>
internal sealed class VerbPlan
{
    private readonly WeakReference<IBrinellGestureSink>? _sink;
    private readonly IReadOnlyDictionary<BrinellVerb, VerbBinding> _invocations;
    private readonly IReadOnlySet<BrinellVerb> _sinkOwned;

    internal VerbPlan(
        IBrinellGestureSink? sink,
        IReadOnlyDictionary<BrinellVerb, VerbBinding> invocations,
        IReadOnlySet<BrinellVerb> sinkOwned,
        IReadOnlyCollection<BrinellVerb> capabilities,
        IReadOnlyCollection<VerbRefusal> refusals)
    {
        _sink = sink is null ? null : new WeakReference<IBrinellGestureSink>(sink);
        _invocations = invocations;
        _sinkOwned = sinkOwned;

        Capabilities = capabilities;
        Refusals = refusals;
    }

    /// <summary>
    /// What the element answers - which is what it can do, not what it claimed.
    /// </summary>
    /// <remarks>
    /// This is served to <c>GetCapabilities</c>. Before bindings it was the declaration passed
    /// through, so an element that declared a verb its recognizers could not serve advertised
    /// the verb and then refused it. A declared verb that bound to nothing is in
    /// <see cref="Refusals"/> instead, and is not here.
    /// </remarks>
    internal IReadOnlyCollection<BrinellVerb> Capabilities { get; }

    /// <summary>Declared verbs that bound to nothing, and why.</summary>
    /// <remarks>
    /// Reported once, when the element is published. A test discovering this instead would be
    /// told only that the verb was unsupported, which is also what a typo looks like.
    /// </remarks>
    internal IReadOnlyCollection<VerbRefusal> Refusals { get; }

    /// <summary>What performs this verb, or null if the element does not answer it.</summary>
    /// <param name="verb">The verb.</param>
    /// <returns>The binding, or null.</returns>
    internal VerbBinding? For(BrinellVerb verb) => _invocations.GetValueOrDefault(verb);

    /// <summary>Whether a string-carrying verb belongs to the app's own sink.</summary>
    /// <param name="verb">The verb.</param>
    /// <returns>Whether the sink named it and is still alive.</returns>
    internal bool ExchangesThroughSink(BrinellVerb verb)
        => _sinkOwned.Contains(verb) && _sink is not null && _sink.TryGetTarget(out _);

    /// <summary>Hands a string-carrying verb to the sink that owns it.</summary>
    /// <param name="element">The element the verb was aimed at.</param>
    /// <param name="verb">The verb.</param>
    /// <param name="argument">The argument.</param>
    /// <returns>What the sink returned.</returns>
    /// <exception cref="InvalidOperationException">
    /// The sink has been collected. Callers ask <see cref="ExchangesThroughSink"/> first, so
    /// this means the sink died between the two calls - an element on its way out.
    /// </exception>
    internal string SinkExchange(VisualElement element, BrinellVerb verb, string argument)
        => _sink is not null && _sink.TryGetTarget(out var sink)
            ? sink.Exchange(element, verb, argument)
            : throw new InvalidOperationException(
                $"The sink owning {verb} has been collected.");
}
