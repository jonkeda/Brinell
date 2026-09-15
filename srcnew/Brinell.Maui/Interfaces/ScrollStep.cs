namespace Brinell.Maui.Interfaces;

/// <summary>
/// What is known about one <see cref="IMauiElement.ScrollContent"/> step.
/// </summary>
/// <remarks>
/// Three answers rather than a bool, because a swipe cannot report whether the content moved.
/// With a bool, "moved" and "swiped, cannot tell" had to share <c>true</c>, and a caller looping
/// until the content stops had to know which platform it was on to avoid looping forever - which
/// is what the <c>SupportsScrollContent</c> question used to be for.
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
}
