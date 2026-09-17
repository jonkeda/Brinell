using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI SwipeView control: a container whose content reveals actions when swiped.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not addressable on Windows.</b> SwipeView maps to the WinUI <c>SwipeControl</c>, whose
/// automation peer cannot be overridden without collapsing the UIA tree, so these members cannot
/// find the control in a Windows test.
/// </para>
/// <para>
/// To swipe it on Windows anyway, call <c>IMauiDriver.PerformGesture(id, gesture)</c> with its
/// <c>AutomationId</c>.
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


    // A touch swipe on Android and iOS; a verb carried to the app on Windows.

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
