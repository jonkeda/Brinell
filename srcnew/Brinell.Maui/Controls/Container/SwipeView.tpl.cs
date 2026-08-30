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
/// <b>Not addressable on Windows.</b> SwipeView maps to the WinUI <c>SwipeControl</c>,
/// whose automation peer must not be overridden — doing so collapses the entire UIA tree.
/// Its <c>AutomationId</c> is therefore invisible and none of the members here can run in a
/// Windows test. They exist for the planned Android/iOS phase, where swipe is a native
/// gesture and the control is addressable.
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

    /// <summary>Swipes right-to-left across the element.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeLeftCore(IMauiElement element, int? timeoutMs = null)
        => element.TrySwipeLeft();

    /// <summary>Swipes left-to-right across the element.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeRightCore(IMauiElement element, int? timeoutMs = null)
        => element.TrySwipeRight();

    /// <summary>Swipes bottom-to-top across the element.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeUpCore(IMauiElement element, int? timeoutMs = null)
        => element.TrySwipeUp();

    /// <summary>Swipes top-to-bottom across the element.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeDownCore(IMauiElement element, int? timeoutMs = null)
        => element.TrySwipeDown();

    /// <summary>Swipes between two points relative to the element's top-left corner.</summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="startX">Start X, relative to the element.</param>
    /// <param name="startY">Start Y, relative to the element.</param>
    /// <param name="endX">End X, relative to the element.</param>
    /// <param name="endY">End Y, relative to the element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void SwipeCore(IMauiElement element,
        int startX, int startY, int endX, int endY, int? timeoutMs = null)
        => element.TrySwipeRelative(startX, startY, endX, endY);

    #endregion
}
