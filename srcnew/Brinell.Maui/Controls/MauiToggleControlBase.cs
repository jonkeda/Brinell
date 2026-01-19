namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with toggle capability.
/// Implements IToggleControlObject with Toggle, Check, Uncheck, SetChecked.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiToggleControlBase<TScope> : MauiControlBase<TScope>, IToggleControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new toggle control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    public MauiToggleControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new toggle control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiToggleControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
    
    #region IToggleControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope Toggle(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Toggle), timeoutMs, element =>
        {
            ToggleCore(element);
        });
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
        
        return RunWithElement(nameof(SetChecked), @checked, timeoutMs, element =>
        {
            SetCheckedCore(element, @checked.Value);
        });
    }
    
    #endregion
    
    #region Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Performs toggle on pre-found element. No logging - caller handles logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void ToggleCore(IMauiElement element)
    {
        element.Click();
    }
    
    /// <summary>
    /// Sets checked state on pre-found element. No-op if already in target state.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="checked">The desired checked state.</param>
    protected virtual void SetCheckedCore(IMauiElement element, bool @checked)
    {
        var current = IsCheckedCore(element);
        if (current != @checked)
        {
            ToggleCore(element);
        }
    }
    
    /// <summary>
    /// Gets checked state from pre-found element.
    /// Reads from ToggleState or checked attribute.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <returns>True if checked, false if unchecked, null if element is null.</returns>
    protected virtual bool? IsCheckedCore(IMauiElement? element)
    {
        if (element == null) return null;
        
        // Try ToggleState attribute first (Windows/MAUI)
        var toggleState = element.GetAttribute("ToggleState");
        if (!string.IsNullOrEmpty(toggleState))
        {
            return toggleState.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   toggleState.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                   toggleState.Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        
        // Try checked attribute (Android/iOS)
        var checkedAttr = element.GetAttribute("checked");
        if (!string.IsNullOrEmpty(checkedAttr))
        {
            return checkedAttr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        // Try IsChecked attribute
        var isCheckedAttr = element.GetAttribute("IsChecked");
        if (!string.IsNullOrEmpty(isCheckedAttr))
        {
            return isCheckedAttr.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        // Default to false if no attribute found
        return false;
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
