namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for MAUI controls with focus capability.
/// Implements IFocusableControlObject with Focus, Blur, IsFocused.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class FocusableControlBase<TScope> : ViewBase<TScope>, IFocusableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new focusable control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    protected FocusableControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new focusable control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    protected FocusableControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Focuses the control.
    /// </summary>
    /// <remarks>
    /// The element picks the route. On Windows that is the app's <c>Focus</c> verb, or a tap where
    /// the element declares only <c>Tap</c>, and otherwise a throw naming the verb. On Android and
    /// iOS it is an ordinary tap.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void FocusCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Focus();
    }

    /// <summary>
    /// Removes focus from the control.
    /// </summary>
    /// <remarks>
    /// The element picks the route. Windows drops focus through the app's <c>Unfocus</c> verb. Android
    /// and iOS send Tab, which is a stand-in rather than the operation: it moves focus on to the next
    /// control, so a control with a focus-out handler and the control after it both see something the
    /// test did not ask for.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void BlurCore(IMauiElement element, int? timeoutMs = null)
    {
        element.ClearFocus();
    }

    /// <summary>
    /// Gets focus state from the pre-found element.
    /// </summary>
    /// <remarks>
    /// Null when there is no element - unknown, rather than "not focused". This used to probe
    /// three attribute names and return false when none answered, which on Windows was always:
    /// none of them is a name UIA publishes, so a focused control reported itself unfocused.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if focused, false if not, null if there is no element.</returns>
    protected virtual bool? IsFocusedCore(IMauiElement? element)
        => element?.Focused;

    #endregion
}
