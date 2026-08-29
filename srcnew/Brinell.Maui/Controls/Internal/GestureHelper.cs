using Brinell.Maui.Configuration;

namespace Brinell.Maui.Controls.Internal;

/// <summary>
/// Element-level gesture primitives shared by controls that swipe or pull to refresh.
/// </summary>
/// <remarks>
/// <para>
/// C# allows one base class, and swiping, refreshing, and expanding are orthogonal
/// capabilities rather than levels in a hierarchy — a control can plausibly need two of
/// them. Keeping the mechanics here as statics over <see cref="IMauiElement"/> lets a
/// control declare the capability interfaces it wants and delegate, instead of choosing a
/// single base class. This is the same shape <c>Containers/ScrollHelper</c> already uses.
/// </para>
/// <para>
/// <b>Pointer input.</b> Swipes are pointer gestures, policy-gated on Windows. Each method
/// here reports whether the gesture was performed rather than throwing, so a caller on a
/// platform that forbids pointer input gets a false rather than an exception.
/// </para>
/// <para>
/// <b>Untested on Windows.</b> The controls that use these — <c>SwipeView</c> and
/// <c>RefreshView</c> — are not addressable by AutomationId on Windows, so none of this has
/// run in a passing test. The logic was preserved verbatim from the base classes it
/// replaced rather than simplified, because the Android/iOS phase is where it will first be
/// exercised.
/// </para>
/// </remarks>
internal static class GestureHelper
{
    /// <summary>Fraction of the element's extent a directional swipe starts from.</summary>
    private const double FarEdge = 0.8;

    /// <summary>Fraction of the element's extent a directional swipe ends at.</summary>
    private const double NearEdge = 0.2;

    /// <summary>Swipes right-to-left across the element's middle.</summary>
    public static bool TrySwipeLeft(IMauiElement? element)
    {
        if (element == null) return false;

        var rect = element.Rect;
        var centerY = rect.Y + (rect.Height / 2);

        return TrySwipe(element,
            rect.X + (int)(rect.Width * FarEdge), centerY,
            rect.X + (int)(rect.Width * NearEdge), centerY);
    }

    /// <summary>Swipes left-to-right across the element's middle.</summary>
    public static bool TrySwipeRight(IMauiElement? element)
    {
        if (element == null) return false;

        var rect = element.Rect;
        var centerY = rect.Y + (rect.Height / 2);

        return TrySwipe(element,
            rect.X + (int)(rect.Width * NearEdge), centerY,
            rect.X + (int)(rect.Width * FarEdge), centerY);
    }

    /// <summary>Swipes bottom-to-top down the element's middle.</summary>
    public static bool TrySwipeUp(IMauiElement? element)
    {
        if (element == null) return false;

        var rect = element.Rect;
        var centerX = rect.X + (rect.Width / 2);

        return TrySwipe(element,
            centerX, rect.Y + (int)(rect.Height * FarEdge),
            centerX, rect.Y + (int)(rect.Height * NearEdge));
    }

    /// <summary>Swipes top-to-bottom down the element's middle.</summary>
    public static bool TrySwipeDown(IMauiElement? element)
    {
        if (element == null) return false;

        var rect = element.Rect;
        var centerX = rect.X + (rect.Width / 2);

        return TrySwipe(element,
            centerX, rect.Y + (int)(rect.Height * NearEdge),
            centerX, rect.Y + (int)(rect.Height * FarEdge));
    }

    /// <summary>
    /// Swipes between two points expressed relative to the element's top-left corner.
    /// </summary>
    public static bool TrySwipeRelative(IMauiElement? element,
        int startX, int startY, int endX, int endY)
    {
        if (element == null) return false;

        var rect = element.Rect;

        return TrySwipe(element,
            rect.X + startX, rect.Y + startY,
            rect.X + endX, rect.Y + endY);
    }

    /// <summary>
    /// Performs the pull-to-refresh gesture: a downward swipe from the element's top.
    /// </summary>
    public static bool TryPullToRefresh(IMauiElement? element) => TrySwipeDown(element);

    /// <summary>
    /// Reads an element's refreshing state.
    /// </summary>
    /// <returns>
    /// True or false from the platform attribute; null only when the element is null.
    /// An element that reports no refresh attribute at all is treated as not refreshing.
    /// </returns>
    public static bool? IsRefreshing(IMauiElement? element)
    {
        if (element == null) return null;

        // MAUI surfaces this under either name depending on the platform mapping.
        foreach (var name in new[] { "IsRefreshing", "Refreshing" })
        {
            var value = element.GetAttribute(name);
            if (!string.IsNullOrEmpty(value))
            {
                return value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    /// <summary>
    /// Swipes between two absolute points, reporting whether the gesture was performed.
    /// </summary>
    private static bool TrySwipe(IMauiElement element,
        int startX, int startY, int endX, int endY)
    {
        try
        {
            element.Swipe(startX, startY, endX, endY);
            return true;
        }
        catch (WindowsInteractionPolicyException)
        {
            // Pointer input is not permitted in this run. Not an error: the caller decides
            // whether a gesture it cannot perform matters.
            return false;
        }
    }
}
