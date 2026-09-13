namespace Brinell.Maui.Containers;

/// <summary>
/// Element-level scrolling primitives shared by containers and collections.
/// </summary>
/// <remarks>
/// <para>
/// C# allows one base class, and a scrolling container needs both scroll behaviour and
/// container scoping, so the mechanics live here as static helpers over
/// <see cref="IMauiElement"/> and the container types delegate to them.
/// </para>
/// <para>
/// <b>No longer a ladder.</b> This used to try the Scroll pattern and swipe when it returned
/// false - but false meant both "cannot scroll" and "already at the end", so a list that had
/// finished was swiped anyway. A caller now asks <see cref="IMauiElement.SupportsScrollContent"/>
/// once and takes one route: <see cref="IMauiElement.ScrollContent"/>, or a swipe on a platform
/// that scrolls by swiping (step 105c).
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
    /// Asks an element to bring itself into view.
    /// </summary>
    /// <returns>True if the request was made; false for no element, or one with no route.</returns>
    /// <remarks>
    /// A true return means the request was accepted, <b>not</b> that the viewport moved.
    /// Callers that need to know must observe the resulting state themselves.
    /// </remarks>
    public static bool ScrollIntoView(IMauiElement? element)
    {
        if (element == null) return false;

        try
        {
            element.ScrollIntoView();
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Scrolls one step towards the end by whichever single route the element has.
    /// </summary>
    /// <returns>
    /// Whether anything moved, as far as can be known: the Scroll pattern reports it; a swipe
    /// cannot, so a performed swipe reports true and the caller checks the content.
    /// </returns>
    public static bool StepForward(IMauiElement? element) => Step(element, forward: true);

    /// <summary>Scrolls one step towards the start. See <see cref="StepForward"/>.</summary>
    public static bool StepBack(IMauiElement? element) => Step(element, forward: false);

    private static bool Step(IMauiElement? element, bool forward)
    {
        if (element == null) return false;

        if (element.SupportsScrollContent)
        {
            return element.ScrollContent(forward ? 1 : -1);
        }

        return Swipe(element, forward);
    }

    /// <summary>
    /// Swipes vertically across the element: from far to near drags content upward, revealing
    /// what follows.
    /// </summary>
    private static bool Swipe(IMauiElement element, bool forward)
    {
        var rect = element.Rect;
        if (rect.Height <= MinimumSwipeHeight) return false;

        var centerX = rect.X + (rect.Width / 2);
        var near = rect.Y + EdgeInset;
        var far = rect.Y + rect.Height - EdgeInset;

        if (forward)
            element.Swipe(centerX, far, centerX, near);
        else
            element.Swipe(centerX, near, centerX, far);

        return true;
    }
}
