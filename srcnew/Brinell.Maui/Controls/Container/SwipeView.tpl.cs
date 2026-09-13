using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI SwipeView control: a container whose content reveals actions when swiped.
/// </summary>
/// <remarks>
/// <para>
/// <b>A container</b> (step 102, option B): it hosts content, so it is modelled the way
/// <c>ScrollView</c> and <c>Border</c> are - its children are found under it. It used to be a
/// view, which gave it no way to name what it holds.
/// </para>
/// <para>
/// Swiping is declared as a capability (<see cref="ISwipeableControlObject{TSelf}"/>) and
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
/// <typeparam name="TParent">The parent scope type.</typeparam>
/// <typeparam name="TSelf">The concrete container type.</typeparam>
public partial class SwipeView<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>,
    ISwipeableControlObject<TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : SwipeView<TParent, TSelf>
{
    /// <summary>
    /// Creates a new swipe view control within the specified scope.
    /// </summary>
    public SwipeView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a new swipe view control using the scope's default locator strategy.
    /// </summary>
    public SwipeView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>Whether the container reports itself enabled.</summary>
    /// <param name="element">The container's root, or null when absent.</param>
    /// <returns>The enabled state, or null when absent.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsEnabledCore(IMauiElement? element) => element?.Enabled;


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

/// <summary>
/// A <see cref="SwipeView{TParent, TSelf}"/> for use where no view-specific subclass is needed.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed partial class SwipeView<TParent> : SwipeView<TParent, SwipeView<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates a SwipeView container within the specified scope.</summary>
    public SwipeView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>Creates a SwipeView container using the scope's default locator strategy.</summary>
    public SwipeView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}
