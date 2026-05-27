namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for MAUI controls with click capability.
/// Implements IClickableControlObject with Click, DoubleClick, RightClick, Hover, LongPress.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public abstract class ClickableControlBase<TScope> : ControlBase<TScope>, IClickableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new clickable control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the element.</param>
    public ClickableControlBase(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new clickable control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public ClickableControlBase(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
    
    #region IClickableControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope Click(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Click), timeoutMs, element =>
        {
            ClickCore(element, timeoutMs);
        });
    }

    /// <summary>
    /// Attempts to click the element if it exists.
    /// Returns false when the element is not found.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds used for enabled-state checking after the element is found.</param>
    /// <returns>True if the element was found and clicked; otherwise false.</returns>
    public bool TryClick(int? timeoutMs = null)
    {
        var element = TryFindElement();
        if (element == null)
        {
            return false;
        }

        EnsureVisible(element);
        Run(nameof(TryClick), () => ClickCore(element, timeoutMs));
        return true;
    }
    
    /// <inheritdoc />
    public TScope DoubleClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(DoubleClick), timeoutMs, element =>
        {
            DoubleClickCore(element, timeoutMs);
        });
    }
    
    /// <inheritdoc />
    public TScope RightClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(RightClick), timeoutMs, element =>
        {
            RightClickCore(element, timeoutMs);
        });
    }
    
    /// <inheritdoc />
    public TScope Hover(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Hover), timeoutMs, element =>
        {
            HoverCore(element);
        });
    }
    
    /// <inheritdoc />
    public TScope LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        return RunWithElement(nameof(LongPress), timeoutMs, element =>
        {
            LongPressCore(element, durationMs);
        });
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
        CheckClickableCore(element, timeoutMs);
        element.Click();
    }

    /// <summary>
    /// Performs double-click on pre-found element. No logging - caller handles logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for clickable check.</param>
    protected virtual void DoubleClickCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckClickableCore(element, timeoutMs);
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
        CheckClickableCore(element, timeoutMs);
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
    
    /// <summary>
    /// Verifies element is clickable using pre-found element. No logging.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected virtual void CheckClickableCore(IMauiElement element, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        
        // Check enabled state (element already exists, so skip WaitExists)
        if (IsEnabledCore(element) != true)
        {
            if (!WaitEnabledCore(element, true, timeout))
            {
                throw new TimeoutException(
                    $"Element was not enabled within {timeout}ms. Locator: {Locator}");
            }
        }
        
        // Check visibility, scroll if needed
        if (IsVisibleCore(element) != true)
        {
            element.ScrollIntoView();

            if (!WaitVisibleCore(element, true, timeout))
            {
                throw new TimeoutException(
                    $"Element was not visible within {timeout}ms after scrolling into view. Locator: {Locator}");
            }
        }
    }
    
    #endregion
    
    #region IsClickable
    
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
    
    /// <summary>
    /// Public CheckClickable - finds element and delegates to Core.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public void CheckClickable(int? timeoutMs = null)
    {
        var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
        CheckClickableCore(element, timeoutMs);
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
    protected bool WaitClickableCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsClickableCore(e) == expected,
            timeoutMs);
    }

    /// <inheritdoc />
    public bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        var element = TryFindElement();
        if (element == null)
        {
            // If element doesn't exist and we expect clickable=false, that's a match
            return expected.Value == false;
        }
        
        return WaitClickableCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
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
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertClickable), expected, () =>
        {
            WaitClickable(expected, timeoutMs);
            return IsClickable();
        }, message ?? $"Expected element {(expected.Value ? "to be clickable" : "not to be clickable")}. Locator: {Locator}");
    }
    
    #endregion
}
