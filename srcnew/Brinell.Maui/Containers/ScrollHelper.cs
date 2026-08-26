using Brinell.Maui.Configuration;

namespace Brinell.Maui.Containers;

/// <summary>
/// Element-level scrolling primitives shared by containers and collections.
/// </summary>
/// <remarks>
/// <para>
/// C# allows one base class, and a scrolling container needs both scroll behaviour and
/// container scoping. Rather than duplicating the scroll members onto
/// <see cref="ContainerObjectBase{TParent, TSelf}"/> or bloating that base with members
/// most containers cannot honour, the mechanics live here as static helpers over
/// <see cref="IMauiElement"/> and the container types delegate to them.
/// </para>
/// <para>
/// Every method here is <b>UI Automation first</b>, with pointer input as a guarded
/// fallback. Pointer input is policy-gated on Windows, so each pointer path catches
/// <see cref="WindowsInteractionPolicyException"/> and reports failure rather than
/// letting it escape. Callers treat a false return as "scrolling made no progress",
/// never as an error.
/// </para>
/// </remarks>
public static class ScrollHelper
{
    /// <summary>Margin in pixels kept away from an element's edges when swiping.</summary>
    private const int EdgeInset = 20;

    /// <summary>
    /// An element shorter than this cannot be swiped meaningfully - the start and end
    /// points would collapse onto each other.
    /// </summary>
    private const int MinimumSwipeHeight = 40;

    /// <summary>
    /// Asks an element to bring itself into view using the platform's scroll-item
    /// pattern.
    /// </summary>
    /// <returns>True if the request was made; false if the element does not support it.</returns>
    /// <remarks>
    /// A true return means the request was accepted, <b>not</b> that the viewport moved.
    /// An element already on screen is a no-op. Callers that need to know whether
    /// anything changed must observe the resulting state themselves.
    /// </remarks>
    public static bool TryScrollIntoView(IMauiElement? element)
    {
        if (element == null) return false;

        try
        {
            element.ScrollIntoView();
            return true;
        }
        catch
        {
            // Not every element implements the scroll-item pattern. This is an expected
            // negative, not a fault: callers fall back to a swipe.
            return false;
        }
    }

    /// <summary>
    /// Swipes an element's content upward, revealing content further down.
    /// </summary>
    /// <returns>True if the swipe was performed; false if it was not possible.</returns>
    public static bool TrySwipeForward(IMauiElement? element)
        => TrySwipeVertical(element, forward: true);

    /// <summary>
    /// Swipes an element's content downward, revealing content further up.
    /// </summary>
    /// <returns>True if the swipe was performed; false if it was not possible.</returns>
    public static bool TrySwipeBack(IMauiElement? element)
        => TrySwipeVertical(element, forward: false);

    private static bool TrySwipeVertical(IMauiElement? element, bool forward)
    {
        if (element == null) return false;

        try
        {
            var rect = element.Rect;
            if (rect.Height <= MinimumSwipeHeight) return false;

            var centerX = rect.X + (rect.Width / 2);
            var near = rect.Y + EdgeInset;
            var far = rect.Y + rect.Height - EdgeInset;

            // Swiping from far to near drags content upward, revealing what follows.
            if (forward)
                element.Swipe(centerX, far, centerX, near);
            else
                element.Swipe(centerX, near, centerX, far);

            return true;
        }
        catch (WindowsInteractionPolicyException)
        {
            // Pointer input is not permitted in this run. Not an error: the caller has
            // already tried the automation route and will report no progress.
            return false;
        }
    }
}
