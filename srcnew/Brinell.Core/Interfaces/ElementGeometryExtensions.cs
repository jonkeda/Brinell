using System.Drawing;

namespace Brinell.Core.Interfaces;

/// <summary>
/// Geometry helpers over the element contract.
/// </summary>
/// <remarks>
/// These replace the former <c>ElementSearch</c> static helper in <c>Brinell.Maui</c>. They
/// live here, public and generic, because they depend on nothing but <see cref="IElement{TSelf}"/>:
/// every platform needs them, and a control object outside the Brinell assemblies must be able
/// to call them.
/// </remarks>
public static class ElementGeometryExtensions
{
    /// <summary>
    /// Whether the element is visible and has a non-empty bounding rectangle.
    /// </summary>
    /// <remarks>
    /// An element can report <c>Visible</c> while occupying no space — collapsed rows and
    /// zero-height templates both do. Callers that are about to click or measure want both
    /// conditions, so they are asked together.
    /// <para>
    /// Returns false rather than throwing when the element is stale: this is a predicate used
    /// while polling, where a torn-down element means "not usable yet", not "test error".
    /// </para>
    /// </remarks>
    /// <param name="element">The element to check. May be null.</param>
    /// <returns>True when the element is visible with a non-empty rectangle.</returns>
    public static bool HasUsableBounds<TSelf>(this IElement<TSelf>? element)
        where TSelf : IElement<TSelf>
    {
        try
        {
            return element?.Visible == true
                && element.Rect is { Width: > 0, Height: > 0 };
        }
        catch
        {
            // Stale or torn-down element: not usable, and not a failure worth raising here.
            return false;
        }
    }

    /// <summary>
    /// Whether <paramref name="child"/>'s centre point falls inside <paramref name="parent"/>.
    /// </summary>
    /// <remarks>
    /// Containment is tested by centre point rather than full overlap because automation
    /// rectangles routinely overhang their logical parent by a pixel or two.
    /// </remarks>
    public static bool ContainsCenter<TSelf>(this IElement<TSelf> parent, IElement<TSelf> child)
        where TSelf : IElement<TSelf>
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);

        return parent.Rect.Contains(CenterOf(child.Rect));
    }

    /// <summary>
    /// The centre point of a rectangle.
    /// </summary>
    public static Point CenterOf(Rectangle rectangle)
        => new(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);

    /// <summary>
    /// The area of the element's bounding rectangle, used to prefer the tightest match.
    /// </summary>
    public static long Area<TSelf>(this IElement<TSelf> element)
        where TSelf : IElement<TSelf>
    {
        ArgumentNullException.ThrowIfNull(element);

        var rect = element.Rect;
        return (long)rect.Width * rect.Height;
    }
}
