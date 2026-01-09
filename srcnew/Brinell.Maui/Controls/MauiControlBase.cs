using System.Diagnostics;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Controls;

/// <summary>
/// Base class for all MAUI control objects. Provides common functionality for
/// finding elements, state checking, waiting, and assertions.
/// </summary>
public abstract class MauiControlBase : IControlObject
{
    private readonly IMauiElementScope _scope;

    /// <summary>
    /// Initializes a new instance of the MauiControlBase class.
    /// </summary>
    /// <param name="locator">The locator used to find this control.</param>
    /// <param name="scope">The element scope containing this control.</param>
    protected MauiControlBase(Locator locator, IMauiElementScope scope)
    {
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    /// <inheritdoc />
    public Locator Locator { get; }

    /// <inheritdoc />
    public IElementScope Scope => _scope;

    /// <summary>
    /// Gets the typed MAUI element scope for internal use.
    /// </summary>
    protected IMauiElementScope MauiScope => _scope;

    /// <summary>
    /// Gets the test context for accessing timeouts, logger, and driver.
    /// </summary>
    protected IMauiTestContext Context => _scope.Context;

    /// <inheritdoc />
    public IPageObject? Page => GetPage();

    /// <summary>
    /// Gets the page name for logging purposes.
    /// </summary>
    protected string? PageName => Page?.Name;

    /// <summary>
    /// Gets a test name for logging purposes.
    /// </summary>
    protected string TestName => "Test";

    /// <summary>
    /// Gets the control identifier for logging purposes.
    /// </summary>
    protected string ControlId => Locator.ToString();

    /// <summary>
    /// Gets the page object by traversing up the scope hierarchy.
    /// </summary>
    private IPageObject? GetPage()
    {
        IElementScope? currentScope = _scope;
        while (currentScope != null)
        {
            if (currentScope is IPageObject page)
                return page;
            
            if (currentScope is IControlObject control)
                currentScope = control.Scope;
            else
                break;
        }
        return null;
    }

    /// <summary>
    /// Finds the underlying Appium element. Returns null if not found.
    /// </summary>
    protected AppiumElement? FindElement()
    {
        return _scope.TryFindElement(Locator);
    }

    /// <summary>
    /// Finds the underlying Appium element, throwing if not found.
    /// </summary>
    protected AppiumElement GetElement()
    {
        var element = FindElement();
        if (element == null)
        {
            throw new ElementNotFoundException(Locator, $"Element not found with locator: {Locator}");
        }
        return element;
    }

    /// <inheritdoc />
    public bool IsExists()
    {
        try
        {
            return FindElement() != null;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool? IsVisible()
    {
        var element = FindElement();
        if (element == null)
            return null;

        try
        {
            return element.Displayed;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public bool? IsEnabled()
    {
        var element = FindElement();
        if (element == null)
            return null;

        try
        {
            return element.Enabled;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetText(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }

        var element = FindElement();
        if (element == null)
            return null;

        try
        {
            return element.Text;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string? GetAttribute(string name)
    {
        var element = FindElement();
        if (element == null)
            return null;

        try
        {
            return element.GetAttribute(name);
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }

    #region Wait Methods

    /// <inheritdoc />
    public bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true; // Skip if null
        
        var timeout = timeoutMs ?? Context.Timeouts.ElementFind;
        var polling = Context.Timeouts.PollingInterval;

        return WaitFor(() => IsExists() == expected.Value, timeout, polling);
    }

    /// <inheritdoc />
    public bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true; // Skip if null
        
        var timeout = timeoutMs ?? Context.Timeouts.ElementState;
        var polling = Context.Timeouts.PollingInterval;

        return WaitFor(() => IsVisible() == expected.Value, timeout, polling);
    }

    /// <inheritdoc />
    public bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true; // Skip if null
        
        var timeout = timeoutMs ?? Context.Timeouts.ElementState;
        var polling = Context.Timeouts.PollingInterval;

        return WaitFor(() => IsEnabled() == expected.Value, timeout, polling);
    }

    /// <inheritdoc />
    public bool WaitText(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true; // Skip if null
        
        var timeout = timeoutMs ?? Context.Timeouts.ElementState;
        var polling = Context.Timeouts.PollingInterval;

        return WaitFor(() => GetText() == expected, timeout, polling);
    }

    /// <summary>
    /// Polls a condition until it returns true or timeout expires.
    /// </summary>
    protected bool WaitFor(Func<bool> condition, int timeoutMs, int pollingMs)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition())
                    return true;
            }
            catch (StaleElementReferenceException)
            {
                // Element became stale, continue polling
            }
            catch (NoSuchElementException)
            {
                // Element not found yet, continue polling
            }

            Thread.Sleep(pollingMs);
        }

        // Final check
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

    #region Assert Methods

    /// <inheritdoc />
    public void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return; // Skip if null
        
        Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertExists", 
            expected, null, true);
        
        if (timeoutMs.HasValue || Context.Timeouts.ElementFind > 0)
        {
            WaitExists(expected, timeoutMs);
        }
        
        var actual = IsExists();
        if (actual != expected.Value)
        {
            Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertExists", 
                expected, actual, false);
            throw new AssertionException(
                expected: expected.Value ? "Element exists" : "Element does not exist",
                actual: actual ? "Element exists" : "Element not found",
                message: message ?? $"Expected element existence to be {expected}: {Locator}",
                controlLocator: Locator);
        }
    }

    /// <inheritdoc />
    public void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return; // Skip if null
        
        Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertVisible", 
            expected, null, true);
        
        if (timeoutMs.HasValue || Context.Timeouts.ElementState > 0)
        {
            WaitVisible(expected, timeoutMs);
        }
        
        var actual = IsVisible();
        if (actual != expected.Value)
        {
            Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertVisible", 
                expected, actual, false);
            throw new AssertionException(
                expected: expected.Value ? "Element is visible" : "Element is not visible",
                actual: actual == null ? "Element not found" : (actual.Value ? "Element is visible" : "Element not visible"),
                message: message ?? $"Expected element visibility to be {expected}: {Locator}",
                controlLocator: Locator);
        }
    }

    /// <inheritdoc />
    public void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return; // Skip if null
        
        Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertEnabled", 
            expected, null, true);
        
        if (timeoutMs.HasValue || Context.Timeouts.ElementState > 0)
        {
            WaitEnabled(expected, timeoutMs);
        }
        
        var actual = IsEnabled();
        if (actual != expected.Value)
        {
            Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertEnabled", 
                expected, actual, false);
            throw new AssertionException(
                expected: expected.Value ? "Element is enabled" : "Element is disabled",
                actual: actual == null ? "Element not found" : (actual.Value ? "Element is enabled" : "Element is disabled"),
                message: message ?? $"Expected element enabled state to be {expected}: {Locator}",
                controlLocator: Locator);
        }
    }

    /// <inheritdoc />
    public void AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return; // Skip if null
        
        Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertText", 
            expected, null, true);
        
        if (timeoutMs.HasValue || Context.Timeouts.ElementState > 0)
        {
            WaitText(expected, timeoutMs);
        }
        
        var actual = GetText();
        if (actual != expected)
        {
            Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertText", 
                expected, actual, false);
            throw new AssertionException(
                expected: expected,
                actual: actual ?? "(element not found)",
                message: message ?? $"Expected text '{expected}' but got '{actual}'",
                controlLocator: Locator);
        }
    }

    /// <inheritdoc />
    public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return; // Skip if null
        
        Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertTextContains", 
            expected, null, true);
        
        var actual = GetText(timeoutMs);
        if (actual?.Contains(expected) != true)
        {
            Context.Logger.LogAssert(TestName, PageName, ControlId, "AssertTextContains", 
                expected, actual, false);
            throw new AssertionException(
                expected: $"Text containing '{expected}'",
                actual: actual ?? "(element not found)",
                message: message ?? $"Expected text to contain '{expected}' but got '{actual}'",
                controlLocator: Locator);
        }
    }

    #endregion
}
