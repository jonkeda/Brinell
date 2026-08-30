using Brinell.Maui.Configuration;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// Directional swipe gestures computed from an element's own bounds.
/// </summary>
/// <remarks>
/// <para>
/// These replace the former <c>Controls.Internal.GestureHelper</c>. The behaviour is
/// unchanged; what changed is reach — they are <c>public</c> extensions rather than an
/// <c>internal static</c>, so a control object outside this assembly can use them. That is
/// goal 13, and it is the same reason phase 1 made the geometry and search helpers public.
/// </para>
/// <para>
/// <b>Why here and not in <c>Brinell.Core</c>.</b> The geometry is platform-neutral and would
/// generalize, but the pointer-policy refusal these must swallow —
/// <see cref="WindowsInteractionPolicyException"/> — is defined in <c>Brinell.Maui</c> and is
/// sealed, so Core cannot catch it by type. Moving these would mean either catching
/// <c>InvalidOperationException</c> broadly (which would hide real faults) or lifting the
/// policy exception into Core. Neither is worth doing speculatively; when a second platform
/// needs swipes, that is the moment to lift the exception and these with it.
/// </para>
/// <para>
/// <b>Pointer input.</b> Swipes are pointer gestures, policy-gated on Windows. Each method
/// reports whether the gesture was performed rather than throwing, so a caller on a platform
/// that forbids pointer input gets <c>false</c> instead of an exception and decides for
/// itself whether that matters.
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
    public static bool TrySwipeLeft(this IMauiElement? element)
    {
        if (element == null) return false;

        var rect = element.Rect;
        var centerY = rect.Y + (rect.Height / 2);

        return element.TrySwipe(
            rect.X + (int)(rect.Width * FarEdge), centerY,
            rect.X + (int)(rect.Width * NearEdge), centerY);
    }

    /// <summary>Swipes left-to-right across the element's middle.</summary>
    public static bool TrySwipeRight(this IMauiElement? element)
    {
        if (element == null) return false;

        var rect = element.Rect;
        var centerY = rect.Y + (rect.Height / 2);

        return element.TrySwipe(
            rect.X + (int)(rect.Width * NearEdge), centerY,
            rect.X + (int)(rect.Width * FarEdge), centerY);
    }

    /// <summary>Swipes bottom-to-top down the element's middle.</summary>
    public static bool TrySwipeUp(this IMauiElement? element)
    {
        if (element == null) return false;

        var rect = element.Rect;
        var centerX = rect.X + (rect.Width / 2);

        return element.TrySwipe(
            centerX, rect.Y + (int)(rect.Height * FarEdge),
            centerX, rect.Y + (int)(rect.Height * NearEdge));
    }

    /// <summary>Swipes top-to-bottom down the element's middle.</summary>
    public static bool TrySwipeDown(this IMauiElement? element)
    {
        if (element == null) return false;

        var rect = element.Rect;
        var centerX = rect.X + (rect.Width / 2);

        return element.TrySwipe(
            centerX, rect.Y + (int)(rect.Height * NearEdge),
            centerX, rect.Y + (int)(rect.Height * FarEdge));
    }

    /// <summary>
    /// Swipes between two points expressed relative to the element's top-left corner.
    /// </summary>
    public static bool TrySwipeRelative(this IMauiElement? element,
        int startX, int startY, int endX, int endY)
    {
        if (element == null) return false;

        var rect = element.Rect;

        return element.TrySwipe(
            rect.X + startX, rect.Y + startY,
            rect.X + endX, rect.Y + endY);
    }

    /// <summary>
    /// Swipes between two absolute points, reporting whether the gesture was performed.
    /// </summary>
    /// <remarks>
    /// Catches only the pointer-policy refusal. Any other failure is a real fault and is
    /// allowed to surface — the same rule the click ladder follows, and for the same reason:
    /// a swallowed failure resurfaces later as an unrelated assertion failure.
    /// </remarks>
    private static bool TrySwipe(this IMauiElement element,
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
