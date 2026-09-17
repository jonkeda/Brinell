namespace Brinell.Maui.Interfaces;

/// <summary>
/// What is known about one <see cref="IMauiElement.ScrollContent"/> step.
/// </summary>
/// <remarks>
/// Three answers rather than a bool, because a swipe cannot report whether the content moved.
/// </remarks>
public enum ScrollStep
{
    /// <summary>The platform confirms the content moved.</summary>
    Moved,

    /// <summary>
    /// Nothing moved: the content was already at that end, or the element is too small to swipe.
    /// </summary>
    NotMoved,

    /// <summary>A swipe was performed, and nothing reports whether the content moved.</summary>
    Unconfirmed,

    /// <summary>
    /// The content went straight to a requested item rather than moving by one step.
    /// </summary>
    /// <remarks>
    /// Returned only by <see cref="IMauiElement.ScrollTowards"/>, where the platform could jump.
    /// A jump needs a different wait than a step: the new rows arrive after the call returns.
    /// </remarks>
    Jumped,
}
