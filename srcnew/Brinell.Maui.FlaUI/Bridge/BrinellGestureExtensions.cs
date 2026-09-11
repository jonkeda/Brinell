using Brinell.Maui.Interfaces;
using Brinell.Uia;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Maui.FlaUI.Bridge;

/// <summary>
/// Drives the Brinell UI Automation bridge from a FlaUI element.
/// </summary>
/// <remarks>
/// <para>
/// <b>No fork of FlaUI, and no reflection into it.</b> Everything here needs one thing FlaUI
/// already exposes publicly - <c>UIA3FrameworkAutomationElement.NativeElement</c> - and gets the
/// pattern through UI Automation's own <c>GetCurrentPattern</c>. FlaUI has no idea the pattern
/// exists and does not need to.
/// </para>
/// </remarks>
public static class BrinellGestureExtensions
{
    /// <summary>The wire verb a test-facing gesture travels as.</summary>
    /// <remarks>
    /// The one place the two enums meet. Everything above this speaks
    /// <see cref="MauiGesture"/>; everything below speaks numbers frozen into the contract.
    /// </remarks>
    /// <param name="gesture">The gesture.</param>
    /// <returns>The verb.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The gesture has no verb.</exception>
    internal static BrinellVerb ToVerb(this MauiGesture gesture) => gesture switch
    {
        MauiGesture.Tap => BrinellVerb.Tap,
        MauiGesture.DoubleTap => BrinellVerb.DoubleTap,
        MauiGesture.LongPress => BrinellVerb.LongPress,
        MauiGesture.SwipeLeft => BrinellVerb.SwipeLeft,
        MauiGesture.SwipeRight => BrinellVerb.SwipeRight,
        MauiGesture.SwipeUp => BrinellVerb.SwipeUp,
        MauiGesture.SwipeDown => BrinellVerb.SwipeDown,
        MauiGesture.Pan => BrinellVerb.Pan,
        MauiGesture.Pinch => BrinellVerb.Pinch,
        _ => throw new ArgumentOutOfRangeException(
            nameof(gesture), gesture, "No Brinell verb is mapped to this gesture."),
    };

    /// <summary>Gets the Brinell pattern from a bridge element, if it carries one.</summary>
    /// <param name="bridgeElement">An element found by <see cref="BrinellBridgeLookup"/>.</param>
    /// <param name="pattern">The pattern, when this returns true.</param>
    /// <returns>Whether the element carries the pattern.</returns>
    internal static bool TryGetBrinellPattern(
        this AutomationElement bridgeElement, out IBrinellAutomationPattern? pattern)
    {
        pattern = null;

        if (bridgeElement.FrameworkAutomationElement is not UIA3FrameworkAutomationElement framework)
        {
            return false;
        }

        return BrinellUiaClient.TryGetPattern(
            framework.NativeElement.GetCurrentPattern, out pattern);
    }

    /// <summary>Reads what an element's bridge target says it can do.</summary>
    /// <param name="bridgeElement">The bridge element.</param>
    /// <returns>The verbs it answers, or empty if it carries no pattern.</returns>
    internal static IReadOnlyList<BrinellVerb> SupportedVerbs(this AutomationElement bridgeElement)
    {
        if (!bridgeElement.TryGetBrinellPattern(out var pattern))
        {
            return [];
        }

        var hr = pattern!.Exchange((int)BrinellVerb.GetCapabilities, string.Empty, out var list);

        return BrinellVerbFailure.Succeeded(hr)
            ? BrinellVerbs.ParseCapabilities(list)
            : [];
    }
}

/// <summary>
/// A gesture could not be performed semantically.
/// </summary>
/// <remarks>
/// Carries the element, the gesture and the reason separately, because the three failures look
/// identical from a test and want different responses: the app has no bridge at all (nothing to
/// fix in the test), the element was never declared (add one attribute to the app's markup), or
/// the verb was refused (the element genuinely cannot do it).
/// </remarks>
public sealed class GestureUnavailableException : NotSupportedException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="automationId">The element the gesture was aimed at.</param>
    /// <param name="gesture">The gesture.</param>
    /// <param name="reason">Why it could not be performed, and what to do about it.</param>
    public GestureUnavailableException(string automationId, MauiGesture gesture, string reason)
        : base($"Cannot {gesture} '{automationId}': {reason}")
    {
        AutomationId = automationId;
        Gesture = gesture;
        Reason = reason;
    }

    /// <summary>The element the gesture was aimed at.</summary>
    public string AutomationId { get; }

    /// <summary>The gesture that could not be performed.</summary>
    public MauiGesture Gesture { get; }

    /// <summary>Why it could not be performed.</summary>
    public string Reason { get; }
}
