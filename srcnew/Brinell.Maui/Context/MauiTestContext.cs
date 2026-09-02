using Brinell.Core.Utilities;
using Brinell.Maui.Configuration;

namespace Brinell.Maui.Context;

/// <summary>
/// MAUI test context implementation supporting multiple drivers (Appium, FlaUI).
/// Uses MauiDriverFactory to create the appropriate driver for the platform.
/// </summary>
public class MauiTestContext : IMauiTestContext
{
    private readonly Interfaces.IMauiDriver _driver;
    private readonly TimeoutSettings _timeouts;
    private readonly ITestLogger _logger;
    private readonly MauiPlatform _platform;
    private readonly bool _ownsDriver;
    private bool _disposed;
    
    /// <summary>
    /// Creates a new MAUI test context with the specified options.
    /// </summary>
    /// <param name="options">Configuration options for the context.</param>
    public MauiTestContext(MauiTestContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        _timeouts = options.Timeouts ?? TimeoutSettings.Default;
        _logger = options.Logger ?? NullTestLogger.Instance;
        
        // Use injected driver if provided, otherwise use factory
        if (options.Driver != null)
        {
            _driver = options.Driver;
            _platform = _driver.Platform;
            _ownsDriver = false; // Caller owns injected driver
        }
        else
        {
            ArgumentNullException.ThrowIfNull(options.DriverOptions, nameof(options.DriverOptions));
            
            var driverOptions = options.DriverOptions;
            // Apply overrides from context options
            driverOptions.Timeouts ??= options.Timeouts;
            driverOptions.Logger ??= options.Logger;
            
            _driver = MauiDriverFactory.Create(driverOptions);
            _platform = driverOptions.Platform;
            _ownsDriver = true; // We own factory-created driver
        }
    }
    
    /// <inheritdoc />
    public Interfaces.IMauiDriver Driver => _driver;
    
    /// <inheritdoc />
    public IMauiTestContext Context => this;
    
    /// <inheritdoc />
    public MauiPlatform Platform => _platform;
    
    /// <inheritdoc />
    public TimeoutSettings Timeouts => _timeouts;
    
    /// <inheritdoc />
    public ITestLogger Logger => _logger;
    
    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    /// <inheritdoc />
    /// <remarks>
    /// Test context root scope has no associated page.
    /// </remarks>
    public IPageObject? Page => null;
    
    /// <inheritdoc />
    /// <remarks>
    /// Test context root is always ready (driver is connected).
    /// </remarks>
    public bool IsReady(int? timeoutMs = null) => !_disposed;
    
    /// <inheritdoc />
    /// <remarks>
    /// Test context root is always ready (driver is connected).
    /// </remarks>
    public bool WaitReady(int? timeoutMs = null) => !_disposed;
    
    /// <inheritdoc />
    public IMauiElement? TryFindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        
        try
        {
            var elements = _driver.FindElements(locator);
            return elements.Count > 0 ? elements[0] : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator)
    {        
        ArgumentNullException.ThrowIfNull(locator);
        
        var timeout = TimeSpan.FromMilliseconds(_timeouts.ElementFind);
        var pollInterval = TimeSpan.FromMilliseconds(100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < timeout)
        {
            var elements = _driver.FindElements(locator);
            if (elements.Count > 0)
            {
                return elements[0];
            }
            WaitHelper.Pause((int)pollInterval.TotalMilliseconds);
        }
        
        // For Android, try UiScrollable to scroll and find element
        if (_platform == MauiPlatform.Android)
        {
            var scrollableElement = TryFindWithUiScrollable(locator);
            if (scrollableElement != null)
            {
                return scrollableElement;
            }
        }
        
        throw new ElementNotFoundException(
            $"Element not found with locator: {locator} after {_timeouts.ElementFind}ms");
    }
    
    /// <summary>
    /// Tries to find an element using Android's UiScrollable to scroll into view.
    /// </summary>
    /// <remarks>
    /// Scrolls <c>scrollable(true).instance(0)</c> — the first scrollable container on the page.
    /// That is an assumption, and a known limit: where an outer <c>ScrollView</c> wraps the
    /// container that actually scrolls, this scrolls the wrong one. Trying each container in
    /// turn was measured and did not help, while making every failed lookup roughly ten times
    /// slower, so the simpler query stands until something proves the extra work earns its cost.
    /// See <c>Tests/Scroll/ScrollTests.cs</c>.
    /// </remarks>
    /// <param name="locator">The locator for the element.</param>
    /// <returns>The element if found, null otherwise.</returns>
    private IMauiElement? TryFindWithUiScrollable(Locator locator)
    {
        var locatorValue = locator.Value;

        // resource-id first, then content-desc: MAUI maps AutomationId to one or the other
        // depending on the control.
        string[] matchers =
        [
            $"new UiSelector().resourceIdMatches(\".*{locatorValue}\")",
            $"new UiSelector().description(\"{locatorValue}\")"
        ];

        foreach (var matcher in matchers)
        {
            try
            {
                var query =
                    "new UiScrollable(new UiSelector().scrollable(true).instance(0))" +
                    $".scrollIntoView({matcher})";

                var elements = _driver.FindByAndroidUIAutomator(query);
                if (elements.Count > 0)
                {
                    return ReResolveAfterScrolling(locator) ?? elements[0];
                }
            }
            catch
            {
                // Try the next matcher.
            }
        }

        return null;
    }

    /// <summary>
    /// Looks the element up again by its own locator once <c>UiScrollable</c> has brought it
    /// into view.
    /// </summary>
    /// <remarks>
    /// <c>UiScrollable(...).scrollIntoView(...)</c> is a scrolling command that happens to
    /// return a node. Acting on that node is not the same as acting on the element: it is
    /// matched by a <c>resourceIdMatches</c> regex during the scroll rather than by the caller's
    /// own locator. Re-resolving costs one lookup, now that the element is on screen, and gives
    /// the caller the element it actually asked for.
    /// </remarks>
    /// <param name="locator">The caller's locator.</param>
    /// <returns>The freshly resolved element, or null to fall back to the scroll result.</returns>
    private IMauiElement? ReResolveAfterScrolling(Locator locator)
    {
        try
        {
            var elements = _driver.FindElements(locator);
            return elements.Count > 0 ? elements[0] : null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {        
        ArgumentNullException.ThrowIfNull(locator);
        
        return _driver.FindElements(locator);
    }
    
    /// <inheritdoc />
    public void NavigateTo(string destination)
    {        
        ArgumentNullException.ThrowIfNull(destination);
        
        _logger.LogNavigation("", "", destination);
        _driver.NavigateTo(destination);
    }
    
    /// <inheritdoc />
    public void NavigateBack()
    {        
        _driver.NavigateBack();
    }
    
    /// <inheritdoc />
    public void Refresh()
    {        
        _driver.Refresh();
    }
    
    /// <inheritdoc />
    public byte[] TakeScreenshot()
    {        
        return _driver.TakeScreenshot();
    }
    
    /// <inheritdoc />
    public void SaveScreenshot(string path)
    {        
        ArgumentNullException.ThrowIfNull(path);
        
        var screenshot = _driver.TakeScreenshot();
        File.WriteAllBytes(path, screenshot);
    }
    
    /// <inheritdoc />
    public void ResetAppState()
    {      
        _driver.ResetAppState();
    }
    
    /// <summary>
    /// Disposes the test context and quits the driver.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing && _ownsDriver)
        {
            try
            {
                _driver?.Dispose();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
        
        _disposed = true;
    }
}
