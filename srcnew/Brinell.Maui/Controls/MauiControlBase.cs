using System.Diagnostics;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Extensions;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for all MAUI controls implementing the Is/Wait/Assert pattern with fluent chaining.
/// Controls find elements within their scope (page, container, or list item).
/// </summary>
/// <typeparam name="TPage">The parent page type for fluent method chaining.</typeparam>
public class MauiControlBase<TPage> : IControlObject
    where TPage : IPageObject
{
    private readonly IMauiPagedScope<TPage> _scope;
    private readonly Locator _locator;
    
    /// <summary>
    /// Creates a new control within the specified scope.
    /// </summary>
    /// <param name="scope">The paged scope (page or container) providing element finding and page reference.</param>
    /// <param name="locator">The locator used to find the control element.</param>
    public MauiControlBase(IMauiPagedScope<TPage> scope, Locator locator)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }
    
    /// <summary>
    /// Gets the parent page for fluent chaining.
    /// </summary>
    public TPage Page => _scope.Page;
    
    /// <inheritdoc />
    IPageObject? IControlObject.Page => _scope.Page;
    
    /// <inheritdoc />
    public Locator Locator => _locator;
    
    /// <inheritdoc />
    public IElementScope Scope => _scope;
    
    /// <summary>
    /// Gets the MAUI test context.
    /// </summary>
    protected IMauiTestContext Context => _scope.Context;
    
    /// <summary>
    /// Gets the timeout settings from the context.
    /// </summary>
    protected int DefaultTimeoutMs => Context.Timeouts.DefaultWait;
    
    /// <summary>
    /// Gets the polling interval from the context.
    /// </summary>
    protected int PollingIntervalMs => Context.Timeouts.PollingInterval;
    
    #region Element Finding
    
    /// <summary>
    /// Tries to find the element within the scope.
    /// </summary>
    /// <returns>The element if found, null otherwise.</returns>
    protected IMauiElement? TryFindElement()
    {
        return _scope.TryFindElement(_locator);
    }
    
    /// <summary>
    /// Finds the element within the scope.
    /// </summary>
    /// <returns>The element.</returns>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    protected IMauiElement FindElement()
    {
        return _scope.FindElement(_locator);
    }
    
    #endregion
    
    #region State (Is methods - immediate, no waiting)
    
    /// <inheritdoc />
    public bool IsExists()
    {
        return TryFindElement() != null;
    }
    
    /// <inheritdoc />
    public bool? IsVisible()
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            return element.Displayed;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public bool? IsEnabled()
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            return element.Enabled;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    #endregion
    
    #region Waiting (poll until condition or timeout)
    
    /// <inheritdoc />
    public bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        return Poll(
            () => IsExists() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        return Poll(
            () => IsVisible() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        return Poll(
            () => IsEnabled() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
    
    #region Assertions (throw on failure)
    
    /// <inheritdoc />
    public void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitExists(expected, timeoutMs))
        {
            var actual = IsExists();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to exist" : "not to exist")} but it {(actual ? "exists" : "does not exist")}. Locator: {_locator}");
        }
    }
    
    /// <inheritdoc />
    public void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitVisible(expected, timeoutMs))
        {
            var actual = IsVisible();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be visible" : "not to be visible")} but visibility is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {_locator}");
        }
    }
    
    /// <inheritdoc />
    public void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitEnabled(expected, timeoutMs))
        {
            var actual = IsEnabled();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be enabled" : "to be disabled")} but enabled state is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {_locator}");
        }
    }
    
    #endregion
    
    #region Text
    
    /// <inheritdoc />
    public string? GetText(int? timeoutMs = null)
    {
        // Optionally wait for element to exist first
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            return element.Text;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public bool WaitText(string? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        return Poll(
            () => GetText() == expected,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public void AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitText(expected, timeoutMs))
        {
            var actual = GetText();
            throw new AssertionException(
                message ?? $"Expected text '{expected}' but got '{actual ?? "(null)"}'. Locator: {_locator}");
        }
    }
    
    /// <inheritdoc />
    public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        var passed = Poll(
            () => GetText()?.Contains(expected) == true,
            timeoutMs ?? DefaultTimeoutMs);
        
        if (!passed)
        {
            var actual = GetText();
            throw new AssertionException(
                message ?? $"Expected text to contain '{expected}' but got '{actual ?? "(null)"}'. Locator: {_locator}");
        }
    }
    
    #endregion
    
    #region Attributes
    
    /// <inheritdoc />
    public string? GetAttribute(string name)
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            return element.GetAttribute(name);
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    #endregion
    
    #region Polling Helper
    
    /// <summary>
    /// Polls a condition until it returns true or timeout is reached.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool Poll(Func<bool> condition, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions during polling, continue trying
            }
            
            Thread.Sleep(PollingIntervalMs);
        }
        
        // Final check after timeout
        try
        {
            return condition();
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
}

/// <summary>
/// Exception thrown when an assertion fails.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
    public AssertionException(string message, Exception innerException) : base(message, innerException) { }
}
