namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with focus capability.
/// Implements IFocusableControlObject with Focus, Blur, IsFocused.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiFocusableControlBase<TScope> : MauiControlBase<TScope>, IFocusableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new focusable control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    public MauiFocusableControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new focusable control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiFocusableControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
    
    #region IFocusableControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope Focus(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Focus), timeoutMs, element =>
        {
            FocusCore(element);
        });
    }
    
    /// <inheritdoc />
    public TScope Blur(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Blur), timeoutMs, element =>
        {
            BlurCore(element);
        });
    }
    
    #endregion
    
    #region Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Sets focus to the control by clicking it.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void FocusCore(IMauiElement element)
    {
        element.Click();
    }
    
    /// <summary>
    /// Removes focus by sending Tab key or clicking elsewhere.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    protected virtual void BlurCore(IMauiElement element)
    {
        // Send Tab key to move focus away
        var unwrappedDriver = Context.Driver.UnwrapDriver();
        var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
        actions.SendKeys(OpenQA.Selenium.Keys.Tab).Perform();
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
    
    #region IsFocused
    
    /// <inheritdoc />
    public bool? IsFocused()
    {
        return IsFocusedCore(TryFindElement());
    }
    
    #endregion
    
    #region WaitFocused
    
    /// <summary>
    /// Waits for focus state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected focus state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitFocusedCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsFocusedCore(e) == expected,
            timeoutMs);
    }

    /// <inheritdoc />
    public bool WaitFocused(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        var element = TryFindElement();
        if (element == null)
        {
            return false;
        }
        
        return WaitFocusedCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
    
    #region AssertFocused
    
    /// <summary>
    /// Asserts the element has focus. Throws if it doesn't.
    /// </summary>
    /// <param name="message">Optional custom message for the assertion failure.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertFocused(string? message = null, int? timeoutMs = null)
        => AssertFocused(true, message, timeoutMs);
    
    /// <inheritdoc />
    public TScope AssertFocused(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertFocused), expected, () =>
        {
            WaitFocused(expected, timeoutMs);
            return IsFocused();
        }, message ?? $"Expected element {(expected.Value ? "to have focus" : "not to have focus")}. Locator: {Locator}");
    }
    
    #endregion
}
