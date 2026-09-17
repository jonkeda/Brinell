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
    /// Invokes the element: the Invoke pattern on Windows, a tap on Android and iOS. Controls that
    /// activate differently override this - <c>ToggleControlBase</c> toggles, <c>RadioButton</c>
    /// selects, <c>ToolbarButton</c> raises a toolbar item.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.Invoke();
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
    /// </summary>
    /// <remarks>
    /// Not available on Windows, where the driver does not type; use <c>ToolbarButton</c> for a
    /// toolbar item. On Android and iOS the key is delivered by Appium.
    /// </remarks>
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
    /// Also requires the control to be enabled, so a control enabled a moment late is waited for.
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


    #endregion
}
