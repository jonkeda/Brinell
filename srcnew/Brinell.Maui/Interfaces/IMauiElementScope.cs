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

    /// <summary>
    /// Whether a control in this scope may scroll to look for itself when a plain lookup finds nothing.
    /// </summary>
    /// <remarks>
    /// A scroll lookup searches the whole scroller, not this scope, which is sound only where the
    /// locator is unique on the page. Rows of a collection repeat their children's ids, so inside a
    /// row a sweep finds another row's element: a row without a due date answered with the next
    /// row's (the Todo sample on Android). A realized row already holds its children, so there is
    /// nothing to scroll to.
    /// </remarks>
    bool AllowsScrollLookup => true;
}
