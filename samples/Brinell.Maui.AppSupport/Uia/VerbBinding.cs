using Brinell.Uia;
using Microsoft.Maui.Controls;

namespace Brinell.Maui.AppSupport.Uia;

/// <summary>
/// One verb, and the single thing that performs it on one element.
/// </summary>
/// <param name="Verb">The verb this answers.</param>
/// <param name="Perform">
/// What to do when it arrives. Returns an HRESULT, because the answer is sometimes neither
/// success nor refusal: a command that exists but will not run right now is <c>S_FALSE</c>,
/// which is a fact about the app rather than about the bridge.
/// </param>
/// <remarks>
/// A delegate rather than a switch arm, because it closes over what the search found. The
/// recognizer, the swipe item or the sink is captured when the element is published, so nothing
/// is looked up again on the path a test waits on.
/// </remarks>
internal sealed record VerbBinding(BrinellVerb Verb, Func<VisualElement, int, int, int> Perform);

/// <summary>
/// Why a declared verb could not be bound - written for whoever has to fix the markup.
/// </summary>
/// <param name="Verb">The verb that was declared.</param>
/// <param name="Reason">
/// What was missing, in the app author's terms: the recognizer that was not there, or the fact
/// that MAUI offers nothing at all.
/// </param>
internal sealed record VerbRefusal(BrinellVerb Verb, string Reason);
