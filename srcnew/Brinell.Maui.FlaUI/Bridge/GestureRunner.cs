using System.Drawing;
using Brinell.Maui.Interfaces;
using Brinell.Uia;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Maui.FlaUI.Bridge;

/// <summary>
/// Performs a gesture through the bridge, given only an <c>AutomationId</c>.
/// </summary>
internal static class GestureRunner
{
    /// <summary>What the app under test declared this element can do.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="near">The real element's bounds, to pick among targets sharing the id.</param>
    /// <returns>The verbs, or empty if there is no bridge element for it.</returns>
    internal static IReadOnlyList<BrinellVerb> Verbs(
        AutomationElement root, UIA3Automation automation, string? automationId, Func<Rectangle>? near = null)
        => BridgeVerbRunner.Verbs(root, automation, automationId, near);

    /// <summary>Whether a gesture would work on this target.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="gesture">The gesture.</param>
    /// <param name="near">The real element's bounds, to pick among targets sharing the id.</param>
    /// <returns>Whether the target declares the matching verb.</returns>
    internal static bool Supports(
        AutomationElement root,
        UIA3Automation automation,
        string? automationId,
        MauiGesture gesture,
        Func<Rectangle>? near = null)
        => BridgeVerbRunner.Supports(root, automation, automationId, gesture.ToVerb(), near);

    /// <summary>
    /// Performs a gesture, or explains precisely why it could not.
    /// </summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="gesture">The gesture.</param>
    /// <exception cref="GestureUnavailableException">The gesture could not be performed.</exception>
    /// <param name="arg1">First argument, meaning defined per gesture; zero for most.</param>
    /// <param name="arg2">Second argument, meaning defined per gesture; zero for most.</param>
    /// <param name="near">The real element's bounds, to pick among targets sharing the id.</param>
    internal static void Perform(
        AutomationElement root,
        UIA3Automation automation,
        string? automationId,
        MauiGesture gesture,
        int arg1 = 0,
        int arg2 = 0,
        Func<Rectangle>? near = null)
    {
        var result = BridgeVerbRunner.Invoke(
            root, automation, automationId, gesture.ToVerb(), arg1, arg2, near, onlyRoute: true);

        if (!result.Delivered)
        {
            throw new GestureUnavailableException(
                automationId ?? "(no AutomationId)", gesture, result.Reason);
        }
    }
}
