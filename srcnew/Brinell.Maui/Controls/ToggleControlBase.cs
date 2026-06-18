namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with toggle capability.
/// Implements IToggleControlObject with Toggle, Check, Uncheck, SetChecked.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract class ToggleControlBase<TScope> : ClickableControlBase<TScope>, IToggleControlObject<TScope>
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
    
    #region IToggleControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope Toggle(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            ToggleCore(element);
        }, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope Check(int? timeoutMs = null)
    {
        return SetChecked(true, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope Uncheck(int? timeoutMs = null)
    {
        return SetChecked(false, timeoutMs);
    }
    
    /// <inheritdoc />
    public TScope SetChecked(bool? @checked, int? timeoutMs = null)
    {
        if (@checked == null) return ContainingScope;
        
        return RunSetWithElement(@checked, element =>
        {
            SetCheckedCore(element, @checked.Value);
        }, timeoutMs);
    }
    
    #endregion
    
    #region Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Performs toggle on pre-found element with state verification and retry.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void ToggleCore(IMauiElement element)
    {
        var beforeState = IsCheckedCore(element);
        EnsureVisible(element);

        if (TryToggleByPattern(element, beforeState)
            || TryToggleByActivation(element, beforeState)
            || TryToggleByKeyboard(element, beforeState))
            return;

        throw new InvalidOperationException(
            $"Could not toggle element without pointer input. Locator: {Locator}");
    }

    private bool TryToggleByPattern(IMauiElement element, bool? beforeState)
    {
        return element is ITogglePatternElement toggle
               && toggle.SupportsTogglePattern
               && toggle.TogglePattern()
               && WaitForStateChange(element, beforeState);
    }

    private bool TryToggleByActivation(IMauiElement element, bool? beforeState)
    {
        return ElementActivator.TryActivate(element)
               && WaitForStateChange(element, beforeState);
    }

    private bool TryToggleByKeyboard(IMauiElement element, bool? beforeState)
    {
        try
        {
            element.SendKeys(OpenQA.Selenium.Keys.Space);
            return WaitForStateChange(element, beforeState);
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

    private bool WaitForStateChange(IMauiElement element, bool? beforeState)
    {
        if (beforeState == null)
            return true;

        return PollWithElement(
            element,
            e => IsCheckedCore(e) != beforeState,
            500);
    }
    
    /// <summary>
    /// Sets checked state on pre-found element. No-op if already in target state.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="checked">The desired checked state.</param>
    protected virtual void SetCheckedCore(IMauiElement element, bool @checked)
    {
        var current = IsCheckedCore(element);
        if (current == @checked)
            return;

        EnsureVisible(element);

        if (element is ITogglePatternElement toggle
            && toggle.SupportsTogglePattern
            && toggle.SetToggleStatePattern(@checked)
            && WaitCheckedCore(element, @checked, 500))
            return;

        if (current != @checked)
            ToggleCore(element);
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
            "IsSelected",                 // Shorthand
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
    
    #region IsChecked
    
    /// <inheritdoc />
    public bool? IsChecked()
    {
        return IsCheckedCore(TryFindElement());
    }
    
    #endregion
    
    #region WaitChecked
    
    /// <summary>
    /// Waits for checked state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected checked state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitCheckedCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsCheckedCore(e) == expected,
            timeoutMs);
    }

    /// <inheritdoc />
    public bool WaitChecked(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        var element = TryFindElement();
        if (element == null)
        {
            // If element doesn't exist, can't match expected state
            return false;
        }
        
        return WaitCheckedCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
    
    #region AssertChecked
    
    /// <summary>
    /// Asserts the element is checked. Throws if it isn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertChecked(string? message = null, int? timeoutMs = null)
        => AssertChecked(true, message, timeoutMs);
    
    /// <inheritdoc />
    public TScope AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertChecked), expected, () =>
        {
            WaitChecked(expected, timeoutMs);
            return IsChecked();
        }, message ?? $"Expected element {(expected.Value ? "to be checked" : "to be unchecked")}. Locator: {Locator}");
    }
    
    #endregion
}
