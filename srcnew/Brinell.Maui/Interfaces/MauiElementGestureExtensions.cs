using Brinell.Maui.Configuration;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// Directional swipe gestures computed from an element's own bounds.
/// </summary>
/// <remarks>
/// <para>
/// Public extensions rather than internal helpers, so a control object outside this assembly
/// can use them.
/// </para>
/// <para>
/// <b>Why here and not in <c>Brinell.Core</c>.</b> The geometry is platform-neutral and would
/// generalize, so this is placement by convenience rather than necessity. The reason it used
/// to be necessary is gone: these once had to catch a policy refusal that only
/// <c>Brinell.Maui</c> could name. When a second platform needs swipes, move them.
/// </para>
/// <para>
/// <b>Pointer input, and no <c>Try</c>.</b> Swipes are real pointer gestures. They either happen
/// or throw - which is what the names now say. They were <c>TrySwipeLeft</c> and the rest, each
/// returning a <c>bool</c> that was <c>true</c> whenever the element was non-null: a return value
/// carrying no information, and an invitation to write the <c>else</c> that would make this a
/// ladder. See <c>.my/fix/design-actions-do-not-try.md</c>.
/// </para>
/// <para>
/// <b>Who calls these.</b> A control object asks its element to perform a gesture and the element
/// decides how its platform does that; on Appium, that is these. They are not the route on
/// Windows, where a gesture travels as a verb to the app itself.
/// </para>
/// <para>
/// <b>Largely unexercised.</b> The controls that use these — <c>SwipeView</c> and
/// <c>RefreshView</c> — are not addressable by AutomationId on Windows, so this logic has
/// never run in a passing test. It was carried over verbatim rather than simplified, because
/// Android and iOS are where it will first be exercised.
/// </para>
/// </remarks>
public static class MauiElementGestureExtensions
{
    /// <summary>Fraction of the element's extent a directional swipe starts from.</summary>
    private const double FarEdge = 0.8;

    /// <summary>Fraction of the element's extent a directional swipe ends at.</summary>
    private const double NearEdge = 0.2;

    /// <summary>Swipes right-to-left across the element's middle.</summary>
    public static void SwipeLeft(this IMauiElement element)
    {
        var rect = element.Rect;
        var centerY = rect.Y + (rect.Height / 2);

        element.Swipe(
            rect.X + (int)(rect.Width * FarEdge), centerY,
            rect.X + (int)(rect.Width * NearEdge), centerY);
    }

    /// <summary>Swipes left-to-right across the element's middle.</summary>
    public static void SwipeRight(this IMauiElement element)
    {
        var rect = element.Rect;
        var centerY = rect.Y + (rect.Height / 2);

        element.Swipe(
            rect.X + (int)(rect.Width * NearEdge), centerY,
            rect.X + (int)(rect.Width * FarEdge), centerY);
    }

    /// <summary>Swipes bottom-to-top down the element's middle.</summary>
    public static void SwipeUp(this IMauiElement element)
    {
        var rect = element.Rect;
        var centerX = rect.X + (rect.Width / 2);

        element.Swipe(
            centerX, rect.Y + (int)(rect.Height * FarEdge),
            centerX, rect.Y + (int)(rect.Height * NearEdge));
    }

    /// <summary>Swipes top-to-bottom down the element's middle.</summary>
    public static void SwipeDown(this IMauiElement element)
    {
        var rect = element.Rect;
        var centerX = rect.X + (rect.Width / 2);

        element.Swipe(
            centerX, rect.Y + (int)(rect.Height * NearEdge),
            centerX, rect.Y + (int)(rect.Height * FarEdge));
    }

    /// <summary>
    /// Swipes between two points expressed relative to the element's top-left corner.
    /// </summary>
    /// <remarks>
    /// The only one of these that takes coordinates, and the only one a test should reach for
    /// when the four directions will not do. <c>IMauiElement.Swipe</c> takes absolute points;
    /// this is the translation, and it is the whole reason the method exists.
    /// </remarks>
    public static void SwipeRelative(this IMauiElement element,
        int startX, int startY, int endX, int endY)
    {
        var rect = element.Rect;

        element.Swipe(
            rect.X + startX, rect.Y + startY,
            rect.X + endX, rect.Y + endY);
    }
}
