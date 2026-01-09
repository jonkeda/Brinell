using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Controls;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Pages;

/// <summary>
/// Base class for MAUI page objects. Provides common functionality for
/// page identification, element finding, and page state management.
/// </summary>
public abstract class MauiPageBase : IMauiPageObject
{
    private readonly IMauiTestContext _context;
    private const string TestName = "Test";

    /// <summary>
    /// Initializes a new instance of the MauiPageBase class.
    /// </summary>
    /// <param name="context">The test context for this page.</param>
    protected MauiPageBase(IMauiTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public IMauiTestContext Context => _context;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;

    /// <summary>
    /// Checks if the page is loaded. Override in derived classes.
    /// </summary>
    protected abstract bool CheckIsLoaded();

    /// <inheritdoc />
    public bool IsLoaded(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            return WaitLoaded(true, timeoutMs);
        }
        return CheckIsLoaded();
    }

    /// <inheritdoc />
    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true; // Skip if null
        
        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        var polling = _context.Timeouts.PollingInterval;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        _context.Logger.LogWait(TestName, Name, Name, "WaitLoaded", false, 0);

        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            if (CheckIsLoaded() == expected.Value)
            {
                _context.Logger.LogWait(TestName, Name, Name, "WaitLoaded", true, (int)stopwatch.ElapsedMilliseconds);
                return true;
            }

            Thread.Sleep(polling);
        }

        // Final check
        if (CheckIsLoaded() == expected.Value)
        {
            _context.Logger.LogWait(TestName, Name, Name, "WaitLoaded", true, (int)stopwatch.ElapsedMilliseconds);
            return true;
        }

        _context.Logger.LogWarning($"Page '{Name}' loaded state did not match {expected} within {timeout}ms");
        return false;
    }

    /// <inheritdoc />
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return; // Skip if null
        
        _context.Logger.LogAssert(TestName, Name, Name, "AssertLoaded", expected, null, true);
        
        if (!WaitLoaded(expected, timeoutMs))
        {
            _context.Logger.LogAssert(TestName, Name, Name, "AssertLoaded", expected, CheckIsLoaded(), false);
            throw new AssertionException(
                expected: expected.Value ? "Page is loaded" : "Page is not loaded",
                actual: CheckIsLoaded() ? "Page is loaded" : "Page is not loaded",
                message: message ?? $"Expected page '{Name}' loaded state to be {expected}");
        }
    }

    /// <inheritdoc />
    public virtual string? GetTitle(int? timeoutMs = null)
    {
        return null; // MAUI apps typically don't have page titles like web
    }

    /// <inheritdoc />
    public bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true; // Skip if null
        
        var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
        var polling = _context.Timeouts.PollingInterval;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            if (GetTitle() == expected)
                return true;

            Thread.Sleep(polling);
        }

        return GetTitle() == expected;
    }

    /// <inheritdoc />
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return; // Skip if null
        
        _context.Logger.LogAssert(TestName, Name, Name, "AssertTitle", expected, null, true);
        
        if (!WaitTitle(expected, timeoutMs))
        {
            var actual = GetTitle();
            _context.Logger.LogAssert(TestName, Name, Name, "AssertTitle", expected, actual, false);
            throw new AssertionException(
                expected: expected,
                actual: actual ?? "(no title)",
                message: message ?? $"Expected page title '{expected}' but got '{actual}'");
        }
    }

    /// <inheritdoc />
    public void TakeScreenshot(string? filename = null, int? timeoutMs = null)
    {
        if (string.IsNullOrEmpty(filename))
        {
            _context.TakeScreenshot();
        }
        else
        {
            _context.SaveScreenshot(filename);
        }
    }

    #region IElementScope Implementation

    /// <inheritdoc />
    public AppiumElement? TryFindElement(Locator locator)
    {
        try
        {
            var by = LocatorConverter.ToBy(locator, _context.Platform);
            return _context.Driver.FindElement(by) as AppiumElement;
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public AppiumElement FindElement(Locator locator)
    {
        var element = TryFindElement(locator);
        if (element == null)
        {
            throw new Core.Exceptions.ElementNotFoundException(locator,
                $"Element not found on page '{Name}': {locator}");
        }
        return element;
    }

    /// <inheritdoc />
    public IReadOnlyList<AppiumElement> FindElements(Locator locator)
    {
        try
        {
            var by = LocatorConverter.ToBy(locator, _context.Platform);
            return _context.Driver.FindElements(by).Cast<AppiumElement>().ToList();
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return Array.Empty<AppiumElement>();
        }
    }

    #endregion

    /// <summary>
    /// Creates a control of the specified type using this page as the scope.
    /// </summary>
    /// <typeparam name="TControl">The type of control to create.</typeparam>
    /// <param name="locator">The locator for the control.</param>
    /// <returns>A new control instance.</returns>
    protected TControl CreateControl<TControl>(Locator locator) where TControl : MauiControlBase
    {
        return (TControl)Activator.CreateInstance(typeof(TControl), locator, this)!;
    }
}
