using Brinell.Maui.Controls.Internal;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI SwipeView control: a container whose content reveals actions when swiped.
/// </summary>
/// <remarks>
/// <para>
/// Swiping is declared as a capability (<see cref="ISwipeableControlObject{TScope}"/>) and
/// delegated to <see cref="GestureHelper"/>, rather than inherited from a swipeable base
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
public class SwipeView<TScope> : Base.ViewBase<TScope>, ISwipeableControlObject<TScope>
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

    /// <inheritdoc />
    public TScope SwipeLeft(int? timeoutMs = null)
        => RunDoWithElement(element => GestureHelper.TrySwipeLeft(element), timeoutMs);

    /// <inheritdoc />
    public TScope SwipeRight(int? timeoutMs = null)
        => RunDoWithElement(element => GestureHelper.TrySwipeRight(element), timeoutMs);

    /// <inheritdoc />
    public TScope SwipeUp(int? timeoutMs = null)
        => RunDoWithElement(element => GestureHelper.TrySwipeUp(element), timeoutMs);

    /// <inheritdoc />
    public TScope SwipeDown(int? timeoutMs = null)
        => RunDoWithElement(element => GestureHelper.TrySwipeDown(element), timeoutMs);

    /// <inheritdoc />
    public TScope Swipe(int startX, int startY, int endX, int endY, int? timeoutMs = null)
        => RunDoWithElement(
            element => GestureHelper.TrySwipeRelative(element, startX, startY, endX, endY),
            timeoutMs);
}
