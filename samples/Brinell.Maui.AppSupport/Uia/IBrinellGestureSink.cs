using Brinell.Uia;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// Lets an app answer a verb itself, ahead of anything the bridge would do.
/// </summary>
/// <remarks>
/// <para>
/// The escape hatch, and the first rung of the dispatcher ladder. A custom control whose
/// gesture handling the bridge cannot see - a drawing surface, a chart, anything with its own
/// hit-testing - implements this and gets the verb directly. Without it the only way to
/// automate such a control is the pointer, which is the thing this whole programme exists to
/// stop needing.
/// </para>
/// <para>
/// <b>Implement it on the control, or on its view model, or on any object at all</b> - it is
/// attached with <see cref="GestureAutomation.SinkProperty"/> and does not have to be the
/// element itself.
/// </para>
/// <para>
/// Called on the UI thread, inside the bridge's timeout budget. Returning false means "I did
/// not handle this", and the bridge carries on down the ladder; it is not a failure.
/// </para>
/// </remarks>
public interface IBrinellGestureSink
{
    /// <summary>Handles a numeric verb, or declines it.</summary>
    /// <param name="element">The element the verb was aimed at.</param>
    /// <param name="verb">The verb.</param>
    /// <param name="arg1">First argument, meaning defined per verb.</param>
    /// <param name="arg2">Second argument, meaning defined per verb.</param>
    /// <returns>Whether this sink handled the verb.</returns>
    bool TryInvoke(VisualElement element, BrinellVerb verb, int arg1, int arg2);

    /// <summary>Handles a verb that takes or returns text, or declines it.</summary>
    /// <param name="element">The element the verb was aimed at.</param>
    /// <param name="verb">The verb.</param>
    /// <param name="argument">The argument, possibly empty.</param>
    /// <param name="result">What to return, when this returns true.</param>
    /// <returns>Whether this sink handled the verb.</returns>
    bool TryExchange(VisualElement element, BrinellVerb verb, string argument, out string result);
}
