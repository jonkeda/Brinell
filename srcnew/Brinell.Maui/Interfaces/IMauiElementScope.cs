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
    /// <para>
    /// For platforms that drop off-screen content from the accessibility tree: Android publishes
    /// nodes only for what is inside the viewport, so a control that plainly exists reports as
    /// missing until something scrolls to it. A control that finds nothing asks this element to
    /// scroll looking for it (<see cref="IMauiElement.TryFindByScrolling"/>), or the app element
    /// when this is null. Windows keeps off-screen elements in the tree and scrolls nothing.
    /// </para>
    /// <para>
    /// <b>This replaced <c>TryFindElementAfterScroll(locator)</c></b>, a lookup every scope had to
    /// implement. Four did; the one on containers - and so on every page - was a plain lookup, so on
    /// Android a control inside a container was never scrolled into the tree at all (step 100a).
    /// A scope now only says which element scrolls, and the control does the lookup once.
    /// </para>
    /// </remarks>
    IMauiElement? ScrollingRoot => null;
}
