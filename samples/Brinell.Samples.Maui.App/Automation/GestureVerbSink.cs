using Brinell.Maui.AppSupport.Uia;
using Brinell.Samples.Maui.App.ViewModels;
using Brinell.Uia;

namespace Brinell.Samples.Maui.App.Automation;

/// <summary>
/// The app answering verbs the bridge has no MAUI API for.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what a sink is for, and the gestures page has two rows that prove it.</b>
/// <c>LongPress</c> has no MAUI gesture recognizer at all, and <c>Pan</c>'s <c>SendPan</c> is
/// internal in MAUI 10 - so neither verb can be delivered by anything the bridge is allowed to
/// call. Without a sink the only remaining route to either is a real pointer, which is the thing
/// stage B exists to stop needing.
/// </para>
/// <para>
/// <b>It writes to the view model rather than to the element</b>, because that is what the
/// gesture itself would have done. A sink that only proved it had been called could pass while
/// the app never learned anything happened; going through the view model means the row's status
/// label changes exactly as it would if a finger had done it.
/// </para>
/// </remarks>
/// <param name="model">The page's view model, which is where a gesture's outcome lands.</param>
public sealed class GestureVerbSink(GesturesViewModel model) : IBrinellGestureSink
{
    /// <inheritdoc />
    /// <remarks>
    /// Named, not guessed. These are the two verbs this sink owns on the elements it is attached
    /// to, and nothing else is consulted for them - so the list being wrong is a failure at
    /// startup rather than a gesture that quietly does something else.
    /// </remarks>
    public IReadOnlyCollection<BrinellVerb> Verbs { get; } =
        [BrinellVerb.LongPress, BrinellVerb.Pan];

    /// <inheritdoc />
    public int Invoke(VisualElement element, BrinellVerb verb, int arg1, int arg2)
    {
        switch (verb)
        {
            case BrinellVerb.LongPress:
                model.LongPressStatus = "Long-pressed";
                return HResults.S_OK;

            case BrinellVerb.Pan:
                // The arguments are the whole point of Pan: a pan with no distance is not a pan,
                // and a test that could not see the distance could not tell one from a tap.
                model.SinkStatus = $"Panned {arg1},{arg2}";
                return HResults.S_OK;

            default:
                // Unreachable through the bridge, which only routes what Verbs names. Throwing
                // rather than returning a refusal keeps it that way: a sink quietly answering a
                // verb it did not claim would be the old first-refusal behaviour growing back.
                throw new NotSupportedException(
                    $"{nameof(GestureVerbSink)} was asked for {verb}, which it does not name in "
                    + $"{nameof(Verbs)}.");
        }
    }
}
