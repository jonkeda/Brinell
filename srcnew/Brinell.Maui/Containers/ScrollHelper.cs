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
/// <b>No route choice here.</b> This used to ask <c>SupportsScrollContent</c> and, on false,
/// compute a swipe across the element's bounds. On Windows that swipe became the bridge's verb and
/// on Android a drag, so the question only chose the platform. The element scrolls one step by
/// whatever route it has, and says what it knows about the result (step 105c, then
/// <c>.my/ControlFlow/design-every-call-through-the-element.md</c>).
/// </para>
/// </remarks>
public static class ScrollHelper
{
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
    /// Scrolls one step towards the end by the element's route.
    /// </summary>
    /// <returns>
    /// Whether anything may have moved: false only when the platform confirms nothing did. A swipe
    /// cannot confirm either way, so it reports true and the caller checks the content.
    /// </returns>
    public static bool StepForward(IMauiElement? element) => Step(element, forward: true);

    /// <summary>Scrolls one step towards the start. See <see cref="StepForward"/>.</summary>
    public static bool StepBack(IMauiElement? element) => Step(element, forward: false);

    private static bool Step(IMauiElement? element, bool forward)
    {
        if (element == null) return false;

        return element.ScrollContent(forward ? 1 : -1) != ScrollStep.NotMoved;
    }
}
