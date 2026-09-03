namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for MAUI controls with click capability.
/// Implements IClickableControlObject with Click, DoubleClick, RightClick, Hover, LongPress.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class ClickableControlBase<TScope> : FocusableControlBase<TScope>,
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

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Performs click on pre-found element. No logging - caller handles logging.
    /// </summary>
    /// <remarks>
    /// Walks the activation ladder (<see cref="TryActivateByPattern"/>) before falling back to
    /// a pointer click. A control whose view activates differently overrides this or the
    /// ladder, rather than a shared helper deciding for every control.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);

        if (TryActivateByPattern(element))
            return;

        element.Click();
    }

    /// <summary>
    /// Activates the element through an automation pattern, when the platform exposes one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows UIA reaches a control's command more reliably than a synthetic pointer click,
    /// which can be swallowed by an overlay or land on the wrong visual child. On platforms
    /// without these patterns — Appium on Android and iOS — every probe reports unsupported
    /// and the caller falls through to <see cref="IElement{TSelf}.Click"/>, which is the
    /// correct mobile behaviour.
    /// </para>
    /// <para>
    /// Deliberately does not catch exceptions: a pattern that is present but fails is a real
    /// fault, and swallowing it turns a broken click into an unrelated assertion failure later.
    /// </para>
    /// <para>
    /// LegacyIAccessible is deliberately <em>not</em> in this ladder. A WinUI toggle advertises
    /// it and its <c>DoDefaultAction</c> reports success without changing the control's state,
    /// so including it makes <c>Click</c> silently do nothing on a Switch. Controls that
    /// genuinely need that rung add it by overriding this method.
    /// </para>
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True when a pattern was available and reported success.</returns>
    protected virtual bool TryActivateByPattern(IMauiElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (element is ISelectionItemPatternElement { SupportsSelectionItemPattern: true } selectionItem
            && selectionItem.SelectItemPattern())
        {
            return true;
        }

        if (element is IInvokePatternElement { SupportsInvokePattern: true } invoke
            && invoke.InvokePattern())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Performs double-click on pre-found element. No logging - caller handles logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void DoubleClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.DoubleClick();
    }

    /// <summary>
    /// Performs right-click on pre-found element. No logging - caller handles logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void RightClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.RightClick();
    }

    /// <summary>
    /// Core implementation of Hover using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void HoverCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.Hover();
    }

    /// <summary>
    /// Core implementation of LongPress using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="durationMs">Duration of the press in milliseconds.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void LongPressCore(IMauiElement element, int? durationMs = null, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        var duration = durationMs ?? 1000; // Default 1 second
        element.LongPress(duration);
    }

    /// <summary>
    /// Activates the button through keyboard input after focusing it.
    /// Useful for MAUI/WinUI button surfaces where UIA Invoke reports success
    /// without dispatching the app command.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void PressCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.SendKeys(Keys.Space);
    }

    #endregion

    #region Guards

    /// <summary>
    /// Throws when the element is not enabled.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void EnsureEnabledCore(IMauiElement element)
    {
        if (IsEnabledCore(element) != true)
        {
            throw new TimeoutException(
                $"Element was not enabled. Locator: {Locator}");
        }
    }

    /// <summary>
    /// Verifies element is clickable using pre-found element. No logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void EnsureClickableCore(IMauiElement element)
    {
        EnsureEnabledCore(element);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A clickable control must also be enabled. Checked here, inside the readiness poll, so a
    /// control enabled by a binding that resolves a frame later is waited for rather than
    /// failed against — <c>ClickCore</c> re-checks it, but by then the retry loop has ended.
    /// </remarks>
    protected override void EnsureReadyForActionCore(IMauiElement element)
    {
        EnsureClickableCore(element);
    }

    #endregion

    #region Clickable

    /// <summary>
    /// Checks clickable state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if clickable (visible and enabled), null if element is null.</returns>
    protected virtual bool? IsClickableCore(IMauiElement? element)
    {
        var isVisible = IsVisibleCore(element);
        var isEnabled = IsEnabledCore(element);

        if (isVisible == null || isEnabled == null)
            return null;

        return isVisible.Value && isEnabled.Value;
    }

    #endregion

    #region Pressed

    /// <summary>
    /// Checks whether the control is pressed, from the pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if pressed, false otherwise, null if element is null.</returns>
    protected virtual bool? IsPressedCore(IMauiElement? element)
    {
        if (element == null) return null;

        var attr = element.GetAttribute("IsPressed");
        if (!string.IsNullOrEmpty(attr))
        {
            return attr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    #endregion
}
