namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific element scope that provides access to the test context.
/// Extends the generic element scope with IMauiElement as the element type.
/// </summary>
public interface IMauiElementScope : IElementScope<IMauiElement>
{
    /// <summary>
    /// Gets the MAUI test context for this scope.
    /// </summary>
    IMauiTestContext Context { get; }

    /// <summary>
    /// The element that scrolls for this scope, or null to let the driver pick the one on screen.
    /// </summary>
    /// <remarks>
    /// Android publishes nodes only for what is inside the viewport, so a control that finds
    /// nothing asks this element to scroll looking for it
    /// (<see cref="IMauiElement.TryFindByScrolling"/>), or the app element when this is null.
    /// Windows keeps off-screen elements in the tree and scrolls nothing.
    /// </remarks>
    IMauiElement? ScrollingRoot => null;
}
