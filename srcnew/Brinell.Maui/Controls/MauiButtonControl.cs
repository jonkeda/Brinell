namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Button control with click capability and fluent method chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiButtonControl<TScope> : MauiControlBase<TScope>, IClickableControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new button control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the button element.</param>
    public MauiButtonControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new button control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiButtonControl(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }
    
    #region IClickableControlObject<TScope> Implementation
    
    /// <inheritdoc />
    public TScope Click(int? timeoutMs = null)
    {
        Run(nameof(Click), () =>
        {
            CheckClickable();
            var element = FindElement();
            element.Click();
        });
        return ContainingScope;
    }
    
    /// <inheritdoc />
    public TScope DoubleClick(int? timeoutMs = null)
    {
        Run(nameof(DoubleClick), () =>
        {
            CheckClickable(timeoutMs);
            var element = FindElement();
            element.Click();
            element.Click();
        });
        return ContainingScope;
    }
    
    /// <inheritdoc />
    public TScope RightClick(int? timeoutMs = null)
    {
        Run(nameof(RightClick), () =>
        {
            CheckClickable(timeoutMs);
            var element = FindElement();
            
            // Unwrap the element and driver for Actions class
            var unwrappedElement = element.UnwrapElement();
            var unwrappedDriver = Context.Driver.UnwrapDriver();
            
            var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
            actions.ContextClick(unwrappedElement).Perform();
        });
        return ContainingScope;
    }
    
    /// <inheritdoc />
    public bool? IsClickable()
    {
        var isVisible = IsVisible();
        var isEnabled = IsEnabled();
        
        // If element doesn't exist, return null
        if (isVisible == null || isEnabled == null)
        {
            return null;
        }
        
        return isVisible.Value && isEnabled.Value;
    }
    
    public void CheckClickable(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        
        // First, wait for element to exist
        if (!WaitExists(true, timeout))
        {
            throw new TimeoutException(
                $"Element not found within {timeout}ms. Locator: {Locator}");
        }
        
        // Wait for element to be enabled
        if (!WaitEnabled(true, timeout))
        {
            throw new TimeoutException(
                $"Element was not enabled within {timeout}ms. Locator: {Locator}");
        }
        
        // If element is not visible (off-screen), try to scroll it into view
        var element = TryFindElement();
        if (element != null && IsVisible() != true)
        {
                element.ScrollIntoView(Context.Driver);
            // Give the UI a moment to settle after scrolling
            Thread.Sleep(200);
        }
        
        // Final check - element should now be clickable
        // If still not visible after scroll attempt, we'll try clicking anyway
        // as some drivers allow clicking non-visible elements
        if (!WaitEnabled(true, timeout / 2))
        {
            throw new TimeoutException(
                $"Element was not clickable within {timeout}ms. Locator: {Locator}");
        }
    }

    /// <inheritdoc />
    public bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        
        if (expected == null) return true;
        
        return Poll(
            () => IsClickable() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
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
