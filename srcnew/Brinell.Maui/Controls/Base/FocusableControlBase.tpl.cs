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
    /// Sets focus to the control by clicking it.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void FocusCore(IMauiElement element, int? timeoutMs = null)
    {
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
    /// Gets focus state from pre-found element.
    /// Reads from HasKeyboardFocus or focused attribute.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if focused, false if not, null if element is null.</returns>
    protected virtual bool? IsFocusedCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try HasKeyboardFocus attribute (Windows/MAUI)
        var hasFocus = element.GetAttribute("HasKeyboardFocus");
        if (!string.IsNullOrEmpty(hasFocus))
        {
            return hasFocus.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // Try focused attribute
        var focused = element.GetAttribute("focused");
        if (!string.IsNullOrEmpty(focused))
        {
            return focused.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // Try IsFocused attribute
        var isFocused = element.GetAttribute("IsFocused");
        if (!string.IsNullOrEmpty(isFocused))
        {
            return isFocused.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        // Default to false if no attribute found
        return false;
    }

    #endregion
}
