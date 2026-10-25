using System.Drawing;

namespace Bruinell.Core.Interfaces;

/// <summary>
/// Geometry every stack uses. The helpers over the first model's <c>IElement</c> are in
/// <c>Bruinell.Core.Legacy</c> (<c>LegacyElementGeometryExtensions</c>).
/// </summary>
public static class ElementGeometryExtensions
{
    /// <summary>
    /// The centre point of a rectangle.
    /// </summary>
    public static Point CenterOf(Rectangle rectangle)
        => new(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
}
