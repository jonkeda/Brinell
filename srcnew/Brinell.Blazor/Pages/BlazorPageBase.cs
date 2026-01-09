using Brinell.Blazor.Controls;
using Brinell.Blazor.Interfaces;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using OpenQA.Selenium;

namespace Brinell.Blazor.Pages;

/// <summary>
/// Base class for Blazor page objects. Provides common functionality for
/// page identification, element finding, and page state management.
/// </summary>
public abstract class BlazorPageBase : IBlazorPageObject
{
    private readonly IBlazorTestContext _context;

    /// <summary>
    /// Initializes a new instance of the BlazorPageBase class.
    /// </summary>
    /// <param name="context">The test context for this page.</param>
    protected BlazorPageBase(IBlazorTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public IBlazorTestContext Context => _context;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.Css;

    /// <inheritdoc />
    public virtual string? Path => null;

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

        _context.Logger.LogWait($"Waiting for page '{Name}' loaded={expected} (timeout: {timeout}ms)");

        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            // Wait for Blazor to be idle first
            _context.WaitForBlazorIdle(polling * 2);

            if (CheckIsLoaded() == expected.Value)
            {
                _context.Logger.LogInfo($"Page '{Name}' loaded state matches {expected} after {stopwatch.ElapsedMilliseconds}ms");
                return true;
            }

            Thread.Sleep(polling);
        }

        // Final check
        if (CheckIsLoaded() == expected.Value)
        {
            _context.Logger.LogInfo($"Page '{Name}' loaded state matches {expected} after {stopwatch.ElapsedMilliseconds}ms");
            return true;
        }

        _context.Logger.LogWarning($"Page '{Name}' loaded state did not match {expected} within {timeout}ms");
        return false;
    }

    /// <inheritdoc />
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return; // Skip if null
        
        _context.Logger.LogAssert($"Assert page '{Name}' loaded={expected}");
        
        if (!WaitLoaded(expected, timeoutMs))
        {
            throw new AssertionException(
                expected: expected.Value ? "Page is loaded" : "Page is not loaded",
                actual: CheckIsLoaded() ? "Page is loaded" : "Page is not loaded",
                message: message ?? $"Expected page '{Name}' loaded state to be {expected}");
        }
    }

    /// <inheritdoc />
    public virtual string? GetTitle(int? timeoutMs = null)
    {
        try
        {
            return _context.Driver.Title;
        }
        catch
        {
            return null;
        }
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
        
        _context.Logger.LogAssert($"Assert page title equals '{expected}'");
        
        if (!WaitTitle(expected, timeoutMs))
        {
            var actual = GetTitle();
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

    /// <inheritdoc />
    public void NavigateTo()
    {
        if (string.IsNullOrEmpty(Path))
        {
            throw new InvalidOperationException($"Page '{Name}' does not have a path defined for navigation.");
        }

        var url = CombineUrl(_context.BaseUrl, Path);
        _context.Logger.LogNavigation($"Navigate to page '{Name}' at {url}");
        _context.NavigateTo(url);
    }

    #region IElementScope Implementation

    /// <inheritdoc />
    public IWebElement? TryFindElement(Locator locator)
    {
        try
        {
            var by = LocatorConverter.ToBy(locator);
            return _context.Driver.FindElement(by);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IWebElement FindElement(Locator locator)
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
    public IReadOnlyList<IWebElement> FindElements(Locator locator)
    {
        try
        {
            var by = LocatorConverter.ToBy(locator);
            return _context.Driver.FindElements(by).ToList();
        }
        catch (NoSuchElementException)
        {
            return Array.Empty<IWebElement>();
        }
    }

    #endregion

    /// <summary>
    /// Creates a control of the specified type using this page as the scope.
    /// </summary>
    /// <typeparam name="TControl">The type of control to create.</typeparam>
    /// <param name="locator">The locator for the control.</param>
    /// <returns>A new control instance.</returns>
    protected TControl CreateControl<TControl>(Locator locator) where TControl : BlazorControlBase
    {
        return (TControl)Activator.CreateInstance(typeof(TControl), locator, this)!;
    }

    /// <summary>
    /// Combines a base URL with a relative path.
    /// </summary>
    private static string CombineUrl(string baseUrl, string path)
    {
        if (Uri.TryCreate(new Uri(baseUrl), path, out var result))
        {
            return result.ToString();
        }

        // Fallback to simple concatenation
        var trimmedBase = baseUrl.TrimEnd('/');
        var trimmedPath = path.TrimStart('/');
        return $"{trimmedBase}/{trimmedPath}";
    }
}
