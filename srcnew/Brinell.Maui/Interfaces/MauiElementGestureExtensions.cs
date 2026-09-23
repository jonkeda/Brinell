using Brinell.Maui.Configuration;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// Directional swipe gestures computed from an element's own bounds.
/// </summary>
/// <remarks>
/// <para>
/// Swipes are real pointer gestures: they either happen or throw.
/// </para>
/// <para>
/// A control object asks its element to perform a gesture and the element decides how; on Appium
/// it uses these. On Windows a gesture travels as a verb to the app instead.
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
    /// Use this when the four directions will not do. <c>IMauiElement.Swipe</c> takes absolute
    /// points; this translates from element-relative ones.
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
