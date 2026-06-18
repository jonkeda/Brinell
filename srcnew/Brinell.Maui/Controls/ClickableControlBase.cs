namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with click capability.
/// Implements IClickableControlObject with Click, DoubleClick, RightClick, Hover, LongPress.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract class ClickableControlBase<TScope> : FocusableControlBase<TScope>,
    IClickableControlObject<TScope>,
    IPressableControl<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new clickable control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    protected ClickableControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new clickable control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    protected ClickableControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region IClickableControlObject<TScope> Implementation

    /// <inheritdoc />
    public TScope Click(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            EnsureClickableCore(element);
            ClickCore(element);
        }, timeoutMs);
    }

    /// <inheritdoc />
    public TScope DoubleClick(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            EnsureClickableCore(element);
            DoubleClickCore(element);
        }, timeoutMs);
    }

    /// <inheritdoc />
    public TScope RightClick(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            EnsureClickableCore(element);
            RightClickCore(element);
        }, timeoutMs);
    }

    /// <inheritdoc />
    public TScope Hover(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            EnsureClickableCore(element);
            HoverCore(element);
        }, timeoutMs);
    }

    /// <inheritdoc />
    public TScope LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            EnsureClickableCore(element);
            LongPressCore(element);
        }, timeoutMs);
    }

    #endregion

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Performs click on pre-found element. No logging - caller handles logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
    }

    /// <summary>
    /// Performs double-click on pre-found element. No logging - caller handles logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void DoubleClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Click();
        element.Click();
    }

    /// <summary>
    /// Performs right-click on pre-found element. No logging - caller handles logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void RightClickCore(IMauiElement element, int? timeoutMs = null)
    {
        element.RightClick();
    }

    /// <summary>
    /// Core implementation of Hover using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void HoverCore(IMauiElement element)
    {
        element.Hover();
    }

    /// <summary>
    /// Core implementation of LongPress using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="durationMs">Duration of the press in milliseconds.</param>
    protected virtual void LongPressCore(IMauiElement element, int? durationMs = null)
    {
        var duration = durationMs ?? 1000; // Default 1 second
        element.LongPress(duration);
    }

    #endregion

    #region IsClickable

    /// <summary>
    /// Verifies element is clickable using pre-found element. No logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected virtual void EnsureClickableCore(IMauiElement element)
    {
        EnsureEnabledCore(element);
    }

    /// <summary>
    /// Checks clickable state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if clickable (visible and enabled), null if element is null.</returns>
    protected bool? IsClickableCore(IMauiElement? element)
    {
        var isVisible = IsVisibleCore(element);
        var isEnabled = IsEnabledCore(element);

        if (isVisible == null || isEnabled == null)
            return null;

        return isVisible.Value && isEnabled.Value;
    }

    /// <inheritdoc />
    public bool? IsClickable()
    {
        return IsClickableCore(TryFindElement());
    }

    #endregion

    #region WaitClickable

    /// <summary>
    /// Waits for clickable state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected clickable state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitClickableCore(IMauiElement element, bool expected)
    {
        return IsClickableCore(element) == expected;
    }

    /// <inheritdoc />
    public bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        return RunWaitWithElement(element =>
        {
            return WaitClickableCore(element, expected.Value);
        }, timeoutMs);
    }

    #endregion

    #region AssertClickable

    /// <summary>
    /// Asserts the element is clickable (visible and enabled). Throws if it isn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertClickable(string? message = null, int? timeoutMs = null)
        => AssertClickable(true, message, timeoutMs);

    /// <inheritdoc />
    public TScope AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
            return ContainingScope;

        return RunAssert(nameof(AssertClickable), expected, () =>
        {
            WaitClickable(expected, timeoutMs);
            return IsClickable();
        }, message ?? $"Expected element {(expected.Value ? "to be clickable" : "not to be clickable")}. Locator: {Locator}");
    }

    #endregion


    #region
    
    /// <summary>
    /// Activates the button through keyboard input after focusing it.
    /// Useful for MAUI/WinUI button surfaces where UIA Invoke reports success
    /// without dispatching the app command.
    /// </summary>
    public TScope Press(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            EnsureClickableCore(element);
            PressCore(element);
        }, timeoutMs);
    }

    private void PressCore(IMauiElement element)
    {
        EnsureClickableCore(element);
        element.SendKeys(Keys.Space);
    }

    #endregion
}
