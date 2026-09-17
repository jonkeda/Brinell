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

    /// <summary>
    /// The content went straight to a requested item rather than moving by one step.
    /// </summary>
    /// <remarks>
    /// Only <see cref="IMauiElement.ScrollTowards"/> returns this, and only where the platform
    /// could jump. It exists because a jump and a step need different waits afterwards: a jump
    /// lands somewhere new and the caller waits for progress and then for the realized rows to
    /// settle, while a step realizes at most a row or two and the caller just counts. Collapsing
    /// the two into <see cref="Moved"/> would put a step's outcome through the jump's wait.
    /// <para>
    /// <see cref="IMauiElement.ScrollContent"/> never returns it.
    /// </para>
    /// </remarks>
    Jumped,
}
