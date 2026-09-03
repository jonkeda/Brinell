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
    /// Finds an element anywhere on the page, scrolling to it where the platform hides
    /// off-screen content from the accessibility tree. Does not poll.
    /// </summary>
    /// <remarks>
    /// The need is the backend's, not MAUI's: Android publishes accessibility nodes only for
    /// content inside the viewport, so a plain lookup reports a control that plainly exists as
    /// missing. UIA and the DOM keep off-screen content but drop virtualised content, which is
    /// the same question asked of a different backend — so a scope on another stack should
    /// expect to declare this too. It is declared here rather than on <c>IElementScope</c>
    /// only because MAUI is so far the one stack with a backend that needs it. See
    /// <c>.my/scroll/finding-why-android-hides-offscreen-controls.md</c>.
    /// </remarks>
    /// <param name="locator">The locator for the element.</param>
    /// <returns>The element, or null when it is not on the page.</returns>
    IMauiElement? TryFindElementAfterScroll(Locator locator);
}
