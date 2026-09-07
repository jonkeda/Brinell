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
    /// Focuses the control, preferring the platform's own focus over a click.
    /// </summary>
    /// <remarks>
    /// Clicking to focus is a side effect standing in for the real operation: it also activates
    /// the control, which is wrong for anything that opens on activation, and it needs the element
    /// visible and unobstructed. UIA can focus directly, so it does. A WebDriver-backed element
    /// cannot, and falls back to the click.
    /// </remarks>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void FocusCore(IMauiElement element, int? timeoutMs = null)
    {
        if (element is IFocusPatternElement focusable
            && focusable.SupportsSetFocus
            && focusable.SetFocus())
        {
            return;
        }

        element.Click();
    }

    /// <summary>
    /// Removes focus by sending Tab key or clicking elsewhere.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void BlurCore(IMauiElement element, int? timeoutMs = null)
    {
        // Send Tab key to move focus away
        element.SendKeys(OpenQA.Selenium.Keys.Tab);
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
