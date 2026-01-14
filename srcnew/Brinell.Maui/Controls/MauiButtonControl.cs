using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Context;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Button control with click capability and fluent method chaining.
/// </summary>
/// <typeparam name="TPage">The parent page type for fluent chaining.</typeparam>
public class MauiButtonControl<TPage> : MauiControlBase<TPage>, IClickableControlObject<TPage>
    where TPage : IPageObject
{
    /// <summary>
    /// Creates a new button control within the specified scope.
    /// </summary>
    /// <param name="scope">The paged scope (page or container) providing element finding and page reference.</param>
    /// <param name="locator">The locator for the button element.</param>
    public MauiButtonControl(IMauiPagedScope<TPage> scope, Locator locator)
        : base(scope, locator)
    {
    }
    
    #region IClickableControlObject<TPage> Implementation
    
    /// <inheritdoc />
    public TPage Click(int? timeoutMs = null)
    {
        CheckClickable();
        var element = FindElement();
        element.Click();
        return Page;
    }
    
    /// <inheritdoc />
    public TPage DoubleClick(int? timeoutMs = null)
    {
        CheckClickable(timeoutMs);
        var element = FindElement();
        element.Click();
        element.Click();
        return Page;
    }
    
    /// <inheritdoc />
    public TPage RightClick(int? timeoutMs = null)
    {
        CheckClickable(timeoutMs);
        var element = FindElement();
        
        // Unwrap the element and driver for Actions class
        var unwrappedElement = element.UnwrapElement();
        var unwrappedDriver = Context.Driver.UnwrapDriver();
        
        var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
        actions.ContextClick(unwrappedElement).Perform();
        return Page;
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
        
        // Wait for element to be clickable
        if (!WaitClickable(true, timeout))
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
    public void AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
    {
        
        if (expected == null) return;
        
        if (!WaitClickable(expected, timeoutMs))
        {
            var actual = IsClickable();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be clickable" : "not to be clickable")} but clickable state is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }
    }
    
    #endregion
}
