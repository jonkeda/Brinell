using Brinell.Maui.Configuration;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for MAUI controls with toggle capability.
/// Implements IToggleControlObject with Toggle, Check, Uncheck, SetChecked.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract partial class ToggleControlBase<TScope> : ClickableControlBase<TScope>,
    IToggleControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new toggle control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    protected ToggleControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new toggle control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    protected ToggleControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Performs toggle on pre-found element with state verification and retry.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void ToggleCore(IMauiElement element, int? timeoutMs = null)
    {
        var beforeState = IsCheckedCore(element);
        EnsureVisible(element, timeoutMs ?? DefaultTimeoutMs);

        if (TryToggleByPattern(element, beforeState, timeoutMs)
            || TryToggleByActivation(element, beforeState, timeoutMs)
            || TryToggleByKeyboard(element, beforeState, timeoutMs))
            return;

        throw new InvalidOperationException(
            $"Could not toggle element without pointer input. Locator: {Locator}");
    }

    private bool TryToggleByPattern(IMauiElement element, bool? beforeState, int? timeoutMs = null)
    {
        return element is ITogglePatternElement toggle
               && toggle.SupportsTogglePattern
               && toggle.TogglePattern()
               && WaitForStateChange(element, beforeState, timeoutMs);
    }

    /// <remarks>
    /// Uses the inherited activation ladder rather than a shared click helper, so a toggle
    /// control that activates through a different child (a template's inner checkbox, say)
    /// overrides <c>TryActivateByPattern</c> once and both click and toggle follow it.
    /// </remarks>
    private bool TryToggleByActivation(IMauiElement element, bool? beforeState, int? timeoutMs = null)
    {
        if (!TryActivateByPattern(element))
        {
            element.Click();
        }

        return WaitForStateChange(element, beforeState, timeoutMs);
    }

    private bool TryToggleByKeyboard(IMauiElement element, bool? beforeState, int? timeoutMs = null)
    {
        try
        {
            element.SendKeys(OpenQA.Selenium.Keys.Space);
            return WaitForStateChange(element, beforeState, timeoutMs);
        }
        catch (WindowsInteractionPolicyException)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool WaitForStateChange(IMauiElement element, bool? beforeState, int? timeoutMs = null)
    {
        if (beforeState == null)
            return true;
        return RunWaitWithElement(!beforeState, e => IsCheckedCore(e) != beforeState, timeoutMs);
    }

    /// <summary>
    /// Sets checked state on pre-found element. No-op if already in the target state.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="checked">The desired checked state. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    protected virtual void SetCheckedCore(IMauiElement element, bool? @checked, int? timeoutMs = null)
    {
        if (@checked == null)
            return;

        var current = IsCheckedCore(element);
        if (current == @checked)
            return;

        ToggleCore(element, timeoutMs);
    }

    /// <summary>
    /// Gets checked state from pre-found element.
    /// Reads from various toggle state attributes used by different platforms.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if checked, false if unchecked, null if element is null.</returns>
    protected virtual bool? IsCheckedCore(IMauiElement? element)
    {
        if (element == null) return null;

        if (element is ITogglePatternElement toggle && toggle.SupportsTogglePattern)
        {
            var checkedViaPattern = toggle.IsTogglePatternChecked();
            if (checkedViaPattern != null)
                return checkedViaPattern;
        }

        // Windows UIA patterns - try multiple attribute name formats
        // Different Appium Windows driver versions may expose these differently
        string[] toggleStateAttributes = {
            "ToggleState",           // Standard Windows UIA
            "Toggle.ToggleState",    // Namespaced format
            "toggle",                // Lowercase variant
        };

        foreach (var attrName in toggleStateAttributes)
        {
            var toggleState = element.GetAttribute(attrName);
            if (!string.IsNullOrEmpty(toggleState))
            {
                // Windows UIA ToggleState: 0=Off, 1=On, 2=Indeterminate
                return toggleState.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                       toggleState.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                       toggleState.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                       toggleState.Equals("ToggleState_On", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Windows UIA SelectionItem pattern (used by RadioButton)
        string[] selectionAttributes = {
            "SelectionItem.IsSelected",  // Windows UIA SelectionItem pattern
            "IsSelected",                // Shorthand
        };

        foreach (var attrName in selectionAttributes)
        {
            var selectedAttr = element.GetAttribute(attrName);
            if (!string.IsNullOrEmpty(selectedAttr))
            {
                return selectedAttr.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                       selectedAttr.Equals("1", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Try checked/selected attributes (Android/iOS/Web)
        string[] checkedAttributes = { "checked", "IsChecked", "Selected", "selected", "IsOn" };

        foreach (var attrName in checkedAttributes)
        {
            var checkedAttr = element.GetAttribute(attrName);
            if (!string.IsNullOrEmpty(checkedAttr))
            {
                return checkedAttr.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       checkedAttr.Equals("1", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Try the Selenium Selected property as fallback
        // This often works for toggle controls in Windows
        return element.Selected;
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Sets the control to the checked state. Convenience alias for SetChecked(true).
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Check(int? timeoutMs = null) => SetChecked(true, timeoutMs);

    /// <summary>
    /// Sets the control to the unchecked state. Convenience alias for SetChecked(false).
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Uncheck(int? timeoutMs = null) => SetChecked(false, timeoutMs);

    /// <summary>
    /// Asserts the element is checked. Throws if it isn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertChecked(string? message, int? timeoutMs = null)
        => AssertChecked(true, message, timeoutMs);

    #endregion
}
