namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI SwipeView control: a container whose content reveals actions when swiped.
/// </summary>
/// <remarks>
/// <para>
/// Swiping is declared as a capability (<see cref="ISwipeableControlObject{TScope}"/>) and
/// delegated to the element gesture extensions, rather than inherited from a swipeable base
/// class. C# allows one base class, and a control may need swiping alongside another
/// capability; composing interfaces keeps that open.
/// </para>
/// <para>
/// <b>Not addressable on Windows.</b> SwipeView maps to the WinUI <c>SwipeControl</c>, whose
/// automation peer must not be overridden — doing so collapses the entire UIA tree. Its
/// <c>AutomationId</c> is therefore invisible, and because every member here finds its element
/// first, none of them can run in a Windows test.
/// </para>
/// <para>
/// <b>The gesture still reaches it on Windows; the control object does not.</b> The bridge
/// publishes a separate element carrying the verb and is addressed by <c>AutomationId</c>, so
/// <c>IMauiDriver.PerformGesture(id, gesture)</c> works on a control nothing can find. Closing
/// that gap means members that do not look for an element at all, which is a change to what the
/// generator emits rather than to this file — see step 19 in <c>.my/extension/steps.md</c>.
/// </para>
/// </remarks>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class SwipeView<TScope> : Base.ViewBase<TScope>, ISwipeableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new swipe view control within the specified scope.
    /// </summary>
    public SwipeView(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new swipe view control using the scope's default locator strategy.
    /// </summary>
    public SwipeView(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    // The control names the gesture; the element decides how its platform performs it. On
    // Appium that is a real touch swipe computed from the element's bounds; on Windows it is a
    // verb carried to the app. Calling the pointer swipe from here, as these once did, made the
    // control object state a mechanism it has no business choosing.

    /// <summary>Swipes right-to-left across the element.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeLeftCore(IMauiElement element, int? timeoutMs = null)
        => element.PerformGesture(MauiGesture.SwipeLeft);

    /// <summary>Swipes left-to-right across the element.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeRightCore(IMauiElement element, int? timeoutMs = null)
        => element.PerformGesture(MauiGesture.SwipeRight);

    /// <summary>Swipes bottom-to-top across the element.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeUpCore(IMauiElement element, int? timeoutMs = null)
        => element.PerformGesture(MauiGesture.SwipeUp);

    /// <summary>Swipes top-to-bottom across the element.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeDownCore(IMauiElement element, int? timeoutMs = null)
        => element.PerformGesture(MauiGesture.SwipeDown);

    /// <summary>Swipes between two points relative to the element's top-left corner.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="startX">Start X, relative to the element.</param>
    /// <param name="startY">Start Y, relative to the element.</param>
    /// <param name="endX">End X, relative to the element.</param>
    /// <param name="endY">End Y, relative to the element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeCore(IMauiElement element,
        int startX, int startY, int endX, int endY, int? timeoutMs = null)
        => element.SwipeRelative(startX, startY, endX, endY);

    #endregion
}
