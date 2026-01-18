using Brinell.Maui.Wrappers;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Windows;

namespace Brinell.Maui.Context;

/// <summary>
/// MAUI test context implementation using Appium WebDriver.
/// Provides driver management and element finding from the driver root.
/// </summary>
public class MauiTestContext : IMauiTestContext
{
    private readonly AppiumDriver _rawDriver;
    private readonly IMauiDriver _driver;
    private readonly TimeoutSettings _timeouts;
    private readonly ITestLogger _logger;
    private bool _disposed;
    
    /// <summary>
    /// Creates a new MAUI test context with the specified options.
    /// </summary>
    /// <param name="options">Configuration options for the context.</param>
    public MauiTestContext(MauiTestContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AppiumOptions);
        
        _timeouts = options.Timeouts ?? TimeoutSettings.Default;
        _logger = options.Logger ?? NullTestLogger.Instance;
        
        // Create the appropriate driver based on platform capability
        var platformName = options.AppiumOptions.PlatformName?.ToLowerInvariant();
        _rawDriver = platformName switch
        {
            "android" => new AndroidDriver(options.AppiumServerUri, options.AppiumOptions),
            "ios" => new IOSDriver(options.AppiumServerUri, options.AppiumOptions),
            "windows" => new WindowsDriver(options.AppiumServerUri, options.AppiumOptions),
            _ => throw new ArgumentException(
                $"Unsupported platform: {platformName}. Use 'android', 'ios', or 'windows'.",
                nameof(options))
        };
        
        // Wrap the driver in our mockable interface
        _driver = new MauiDriver(_rawDriver);
        
        // Set implicit wait to 0 - framework handles all waiting explicitly
        // This prevents FindElements from blocking for the full timeout
        _rawDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
    }
    
    /// <inheritdoc />
    public IMauiDriver Driver => _driver;
    
    /// <inheritdoc />
    public IMauiTestContext Context => this;
    
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
        // Use FindElements with a very short timeout to check existence
        // Note: We don't manipulate ImplicitWait here as it can hang Windows Driver
        var by = locator.ToBy();
        
        try
        {
            // FindElements returns empty list (not exception) when nothing found
            var elements = _rawDriver.FindElements(by);
            return elements.Count > 0 ? new MauiElement(elements[0]) : null;
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
        
        var by = locator.ToBy();
        var timeout = TimeSpan.FromMilliseconds(_timeouts.ElementFind);
        var pollInterval = TimeSpan.FromMilliseconds(100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < timeout)
        {
            var elements = _rawDriver.FindElements(by);
            if (elements.Count > 0)
            {
                return new MauiElement(elements[0]);
            }
            Thread.Sleep(pollInterval);
        }
        
        throw new ElementNotFoundException(
            $"Element not found with locator: {locator} after {_timeouts.ElementFind}ms");
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {        
        ArgumentNullException.ThrowIfNull(locator);
        
        var by = locator.ToBy();
        return _rawDriver.FindElements(by)
            .Select(e => (IMauiElement)new MauiElement(e))
            .ToList();
    }
    
    /// <inheritdoc />
    public void NavigateTo(string destination)
    {        
        ArgumentNullException.ThrowIfNull(destination);
        
        _logger.LogNavigation("", "", destination);
        
        // For mobile apps, navigation might be handled differently
        // This is a basic URL navigation for hybrid apps
        _rawDriver.Navigate().GoToUrl(destination);
    }
    
    /// <inheritdoc />
    public void NavigateBack()
    {        
        _rawDriver.Navigate().Back();
    }
    
    /// <inheritdoc />
    public void Refresh()
    {        
        _rawDriver.Navigate().Refresh();
    }
    
    /// <inheritdoc />
    public byte[] TakeScreenshot()
    {        
        var screenshot = _rawDriver.GetScreenshot();
        return screenshot.AsByteArray;
    }
    
    /// <inheritdoc />
    public void SaveScreenshot(string path)
    {        
        ArgumentNullException.ThrowIfNull(path);
        
        var screenshot = _rawDriver.GetScreenshot();
        screenshot.SaveAsFile(path);
    }
    
    /// <inheritdoc />
    public void ResetAppState()
    {      
        
        // Reset app by terminating and re-launching
        // Note: ResetApp is deprecated in newer Appium versions
        var bundleId = _rawDriver.Capabilities.GetCapability("appPackage")?.ToString()
                    ?? _rawDriver.Capabilities.GetCapability("bundleId")?.ToString();
        
        if (!string.IsNullOrEmpty(bundleId))
        {
            _rawDriver.TerminateApp(bundleId);
            _rawDriver.ActivateApp(bundleId);
        }
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
        
        if (disposing)
        {
            try
            {
                _rawDriver?.Quit();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
        
        _disposed = true;
    }
}
