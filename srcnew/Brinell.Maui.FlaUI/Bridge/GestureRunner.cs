using Brinell.Maui.Interfaces;
using Brinell.Uia;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace Brinell.Maui.FlaUI.Bridge;

/// <summary>
/// Performs a gesture through the bridge, given only an <c>AutomationId</c>.
/// </summary>
/// <remarks>
/// One implementation shared by the driver and the element. They are two entry points to the
/// same question - the driver's takes an id because the target may not be addressable at all,
/// the element's takes its own id for convenience - and having each do its own lookup is how
/// they would come to disagree about what "supported" means.
/// </remarks>
internal static class GestureRunner
{
    /// <summary>What the app under test declared this element can do.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <returns>The verbs, or empty if there is no bridge element for it.</returns>
    internal static IReadOnlyList<BrinellVerb> Verbs(
        AutomationElement root, UIA3Automation automation, string? automationId)
        => BridgeVerbRunner.Verbs(root, automation, automationId);

    /// <summary>Whether a gesture would work on this target.</summary>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="gesture">The gesture.</param>
    /// <returns>Whether the target declares the matching verb.</returns>
    internal static bool Supports(
        AutomationElement root, UIA3Automation automation, string? automationId, MauiGesture gesture)
        => BridgeVerbRunner.Supports(root, automation, automationId, gesture.ToVerb());

    /// <summary>
    /// Performs a gesture, or explains precisely why it could not.
    /// </summary>
    /// <remarks>
    /// Throws where the general runner returns, and that difference is the point of this type
    /// existing at all. A gesture is normally the subject of the test asking for it, so a
    /// failure should name the element and the gesture at the moment it happens rather than
    /// surface later as an assertion about state that never changed. The focus and text ladders
    /// want the opposite and use <see cref="BridgeVerbRunner"/> directly.
    /// </remarks>
    /// <param name="root">The app's top-level window.</param>
    /// <param name="automation">The session.</param>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target.</param>
    /// <param name="gesture">The gesture.</param>
    /// <exception cref="GestureUnavailableException">The gesture could not be performed.</exception>
    internal static void Perform(
        AutomationElement root, UIA3Automation automation, string? automationId, MauiGesture gesture)
    {
        var result = BridgeVerbRunner.Invoke(root, automation, automationId, gesture.ToVerb());

        if (!result.Delivered)
        {
            throw new GestureUnavailableException(
                automationId ?? "(no AutomationId)", gesture, result.Reason);
        }
    }
}
