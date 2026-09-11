using Brinell.Uia;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Maui.FlaUI.Bridge;

/// <summary>
/// Sends one verb to the app under test, and says what came back.
/// </summary>
/// <remarks>
/// <para>
/// The general form. <see cref="GestureRunner"/> is this with a gesture's vocabulary on top and
/// an exception instead of a return value; the focus and text ladders in
/// <c>FlaUIMauiElement</c> use it directly, because there the bridge is the preferred rung of a
/// ladder rather than the only route, and a refusal is something to fall back from rather than
/// something to report.
/// </para>
/// <para>
/// <b>Nothing here throws for an ordinary negative.</b> No bridge in the app, no target for this
/// element, a verb the element does not answer: all three are answers, and all three are
/// distinguished in <see cref="BridgeVerbResult.Reason"/> rather than collapsed, because they
/// call for different fixes - build the app with the automation sources, add a declaration to
/// its markup, or use another route.
/// </para>
/// <para>
/// <b>And nothing is cached.</b> A page can be navigated away from and back; its bridge targets
/// are republished each time with new runtime ids, so an answer held over from the last visit
/// would be about elements that no longer exist.
/// </para>
/// </remarks>
internal static class BridgeVerbRunner
{
    /// <summary>What the app under test declared an element can do.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <returns>The verbs, or empty if there is no bridge target for it.</returns>
    internal static IReadOnlyList<BrinellVerb> Verbs(
        AutomationElement root, UIA3Automation automation, string? automationId)
    {
        if (string.IsNullOrWhiteSpace(automationId))
        {
            return [];
        }

        return BrinellBridgeLookup.Find(root, automation, automationId)?.SupportedVerbs() ?? [];
    }

    /// <summary>Whether an element declares a verb.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="verb">The verb.</param>
    /// <returns>Whether the app declared it for this element.</returns>
    internal static bool Supports(
        AutomationElement root,
        UIA3Automation automation,
        string? automationId,
        BrinellVerb verb)
        => Verbs(root, automation, automationId).Contains(verb);

    /// <summary>
    /// Sends a verb on whichever of the two contract methods it travels on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The entry point every ladder should use.</b> The method table has two entries, and a
    /// verb sent down the wrong one reaches a provider that has never heard of it - refused with
    /// <c>UIA_E_NOTSUPPORTED</c>, which is exactly what "this element does not support that"
    /// looks like. The failure is silent, plausible and points at the app rather than at the
    /// call, which is a bad combination; it cost a green test that was quietly falling back to
    /// the keyboard.
    /// </para>
    /// <para>
    /// So the choice is not made here or by the caller. <see cref="BrinellVerbs.TransportFor"/>
    /// is in the contracts assembly next to the numbers, which is the only place both ends can
    /// read it from.
    /// </para>
    /// </remarks>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="verb">The verb.</param>
    /// <param name="argument">Its argument, for a verb that takes one.</param>
    /// <returns>What happened, and what the app returned.</returns>
    internal static BridgeVerbResult Send(
        AutomationElement root,
        UIA3Automation automation,
        string? automationId,
        BrinellVerb verb,
        string argument = "")
        => BrinellVerbs.TransportFor(verb) == BrinellVerbTransport.Exchange
            ? Exchange(root, automation, automationId, verb, argument)
            : Invoke(root, automation, automationId, verb);

    /// <summary>
    /// Sends a verb that carries no strings.
    /// </summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="verb">The verb.</param>
    /// <param name="arg1">The verb's first argument, where it takes one.</param>
    /// <param name="arg2">The verb's second argument, where it takes one.</param>
    /// <returns>What happened.</returns>
    internal static BridgeVerbResult Invoke(
        AutomationElement root,
        UIA3Automation automation,
        string? automationId,
        BrinellVerb verb,
        int arg1 = 0,
        int arg2 = 0)
    {
        if (!TryResolve(root, automation, automationId, verb, out var pattern, out var failure))
        {
            return failure;
        }

        var hr = pattern!.Invoke((int)verb, arg1, arg2);

        return new BridgeVerbResult(
            BrinellVerbFailure.Succeeded(hr),
            hr,
            string.Empty,
            BrinellVerbFailure.Describe(verb, hr));
    }

    /// <summary>
    /// Sends a verb that carries a string in, a string out, or both.
    /// </summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="verb">The verb.</param>
    /// <param name="argument">The verb's argument, or empty where it takes none.</param>
    /// <returns>What happened, and what the app returned.</returns>
    internal static BridgeVerbResult Exchange(
        AutomationElement root,
        UIA3Automation automation,
        string? automationId,
        BrinellVerb verb,
        string argument = "")
    {
        if (!TryResolve(root, automation, automationId, verb, out var pattern, out var failure))
        {
            return failure;
        }

        var hr = pattern!.Exchange((int)verb, argument, out var value);

        return new BridgeVerbResult(
            BrinellVerbFailure.Succeeded(hr),
            hr,
            value,
            BrinellVerbFailure.Describe(verb, hr));
    }

    /// <summary>
    /// Sends a verb to whichever published element answers it, without naming one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For the verbs that are about the app rather than about a control. Going back is the first
    /// of them: the caller has no <c>AutomationId</c> for it, because the thing being asked for
    /// is not a control - and on Windows the affordance that would carry one, a
    /// <c>ToolbarItem</c>, is drawn into native chrome and cannot be activated by any automation
    /// pattern at all.
    /// </para>
    /// <para>
    /// <b>The first element to <i>perform</i> the verb wins, not the first to declare it.</b>
    /// That distinction is the whole correctness of this method. Several elements can answer a
    /// whole-app verb at once - every page that has been visited leaves a bridge target behind
    /// until its registration is withdrawn - and the walk finds the oldest first. Stopping at
    /// the first declaration meant asking a page that had long since been popped, taking its
    /// refusal as the app's answer, and reporting a failure the live page would have handled.
    /// </para>
    /// <para>
    /// So a refusal moves on to the next candidate, and only <see cref="HResults.S_OK"/> ends
    /// the loop. <see cref="HResults.S_FALSE"/> - succeeded and did nothing - is a refusal for
    /// this purpose: it is exactly what a stale page says.
    /// </para>
    /// </remarks>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="verb">The verb.</param>
    /// <returns>What happened, including that nothing answered.</returns>
    internal static BridgeVerbResult InvokeAnywhere(
        AutomationElement root, UIA3Automation automation, BrinellVerb verb)
    {
        var declined = 0;
        var lastReason = string.Empty;

        foreach (var target in BrinellBridgeLookup.Targets(root, automation))
        {
            if (!target.SupportedVerbs().Contains(verb)
                || !target.TryGetBrinellPattern(out var pattern))
            {
                continue;
            }

            var hr = pattern!.Invoke((int)verb, 0, 0);

            if (hr == HResults.S_OK)
            {
                return new BridgeVerbResult(true, hr, string.Empty, BrinellVerbFailure.Describe(verb, hr));
            }

            declined++;
            lastReason = BrinellVerbFailure.Describe(verb, hr);
        }

        return declined == 0
            ? new BridgeVerbResult(
                false,
                HResults.UIA_E_NOTSUPPORTED,
                string.Empty,
                $"no element published on the app's bridge answers {verb}.")
            : new BridgeVerbResult(
                false,
                HResults.S_FALSE,
                string.Empty,
                $"{declined} element(s) on the app's bridge were asked to {verb} and none did. "
                + $"The last said: {lastReason}");
    }

    /// <summary>
    /// Asks any element on the app's bridge a question about the app itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reading counterpart of <see cref="InvokeAnywhere"/>, and it can be simpler than that
    /// method for a reason worth stating: <b>staleness does not matter here.</b> A popped page
    /// that still answers will report the <i>live</i> navigation stack, because that is what its
    /// <c>Navigation</c> property refers to - so the first target to answer gives the same answer
    /// as the last. Going back is the opposite: a stale page agrees to pop and then pops nothing,
    /// which is why that method has to keep walking past a refusal.
    /// </para>
    /// <para>
    /// Only <see cref="HResults.S_OK"/> counts as an answer. Anything else means this target did
    /// not know, and the next one is asked.
    /// </para>
    /// </remarks>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="verb">The verb.</param>
    /// <param name="argument">The argument, if the verb takes one.</param>
    /// <returns>What was answered, including that nothing answered.</returns>
    internal static BridgeVerbResult ExchangeAnywhere(
        AutomationElement root,
        UIA3Automation automation,
        BrinellVerb verb,
        string argument = "")
    {
        foreach (var target in BrinellBridgeLookup.Targets(root, automation))
        {
            if (!target.SupportedVerbs().Contains(verb)
                || !target.TryGetBrinellPattern(out var pattern))
            {
                continue;
            }

            var hr = pattern!.Exchange((int)verb, argument, out var answer);

            if (hr == HResults.S_OK)
            {
                return new BridgeVerbResult(
                    true, hr, answer, BrinellVerbFailure.Describe(verb, hr));
            }
        }

        return new BridgeVerbResult(
            false,
            HResults.UIA_E_NOTSUPPORTED,
            string.Empty,
            $"no element published on the app's bridge answers {verb}.");
    }

    /// <summary>
    /// Finds the pattern for an element, or explains which of the ways it was missing.
    /// </summary>
    /// <remarks>
    /// The capability list is not consulted. It says what the app <i>declared</i>; the verb's
    /// own return value says what the app <i>did</i>, and asking for the declaration first would
    /// cost a round trip to learn something the call is about to establish anyway. A verb sent
    /// to an element that never declared it comes back <c>UIA_E_NOTSUPPORTED</c>, which is the
    /// same answer with one less call.
    /// </remarks>
    private static bool TryResolve(
        AutomationElement root,
        UIA3Automation automation,
        string? automationId,
        BrinellVerb verb,
        out IBrinellAutomationPattern? pattern,
        out BridgeVerbResult failure)
    {
        pattern = null;

        if (string.IsNullOrWhiteSpace(automationId))
        {
            failure = new BridgeVerbResult(
                false,
                HResults.E_INVALIDARG,
                string.Empty,
                "the target has no AutomationId, which is the only join between an element and its "
                + "bridge element. Give it one in the app's markup.");
            return false;
        }

        var element = BrinellBridgeLookup.Find(root, automation, automationId);

        if (element is null)
        {
            failure = new BridgeVerbResult(
                false,
                HResults.UIA_E_ELEMENTNOTAVAILABLE,
                string.Empty,
                BrinellBridgeLookup.HasBridge(root, automation)
                    ? "the app under test publishes a Brinell bridge but this element is not on it. "
                      + "Add GestureAutomation.Verbs to the element in the app's markup."
                    : "the app under test publishes no Brinell bridge. It needs the "
                      + "Brinell.Maui.AppSupport UI Automation sources, and at least one element "
                      + "declaring GestureAutomation.Verbs.");
            return false;
        }

        if (!element.TryGetBrinellPattern(out pattern))
        {
            failure = new BridgeVerbResult(
                false,
                HResults.E_FAIL,
                string.Empty,
                "its bridge element carries no Brinell pattern. The app under test and this test "
                + "assembly were built against different contract GUIDs.");
            return false;
        }

        failure = default;
        return true;
    }
}

/// <summary>
/// What one verb did.
/// </summary>
/// <remarks>
/// <see cref="Reason"/> is filled in on success too, and says so. A caller that logs the reason
/// unconditionally then reports something true either way, which is what the bridge diagnostics
/// want; a caller that only reports failures tests <see cref="Delivered"/> first.
/// </remarks>
/// <param name="Delivered">Whether the app performed the verb.</param>
/// <param name="HResult">What the provider returned, for the cases that want the exact value.</param>
/// <param name="Value">What the app returned, for <c>Exchange</c>; empty otherwise.</param>
/// <param name="Reason">A sentence naming the verb, the outcome and what to do about it.</param>
internal readonly record struct BridgeVerbResult(
    bool Delivered, int HResult, string Value, string Reason);
