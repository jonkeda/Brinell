using Brinell.Core.Configuration;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using Brinell.Maui.Extensions;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;

namespace Brinell.Maui.Context;

/// <summary>
/// MAUI test context implementation using Appium WebDriver.
/// Provides driver management and element finding from the driver root.
/// </summary>
public class MauiTestContext : IMauiTestContext
{
    private readonly AppiumDriver _driver;
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
        _driver = platformName switch
        {
            "android" => new AndroidDriver(options.AppiumServerUri, options.AppiumOptions),
            "ios" => new IOSDriver(options.AppiumServerUri, options.AppiumOptions),
            _ => throw new ArgumentException(
                $"Unsupported platform: {platformName}. Use 'android' or 'ios'.",
                nameof(options))
        };
        
        // Configure implicit wait
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(_timeouts.ElementFind);
    }
    
    /// <inheritdoc />
    public AppiumDriver Driver => _driver;
    
    /// <inheritdoc />
    public IMauiTestContext Context => this;
    
    /// <inheritdoc />
    public TimeoutSettings Timeouts => _timeouts;
    
    /// <inheritdoc />
    public ITestLogger Logger => _logger;
    
    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    /// <inheritdoc />
    public AppiumElement? TryFindElement(Locator locator)
    {
        
        ArgumentNullException.ThrowIfNull(locator);
        
        try
        {
            // Temporarily disable implicit wait for immediate check
            var originalTimeout = _driver.Manage().Timeouts().ImplicitWait;
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            
            try
            {
                var by = locator.ToBy();
                return _driver.FindElement(by);
            }
            finally
            {
                _driver.Manage().Timeouts().ImplicitWait = originalTimeout;
            }
        }
        catch (NoSuchElementException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public AppiumElement FindElement(Locator locator)
    {        
        ArgumentNullException.ThrowIfNull(locator);
        
        var by = locator.ToBy();
        
        try
        {
            return _driver.FindElement(by);
        }
        catch (NoSuchElementException ex)
        {
            throw new ElementNotFoundException(
                $"Element not found with locator: {locator}", ex);
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyList<AppiumElement> FindElements(Locator locator)
    {        
        ArgumentNullException.ThrowIfNull(locator);
        
        var by = locator.ToBy();
        return _driver.FindElements(by).ToList();
    }
    
    /// <inheritdoc />
    public void NavigateTo(string destination)
    {        
        ArgumentNullException.ThrowIfNull(destination);
        
        _logger.LogNavigation("", destination);
        
        // For mobile apps, navigation might be handled differently
        // This is a basic URL navigation for hybrid apps
        _driver.Navigate().GoToUrl(destination);
    }
    
    /// <inheritdoc />
    public void NavigateBack()
    {        
        _driver.Navigate().Back();
    }
    
    /// <inheritdoc />
    public void Refresh()
    {        
        _driver.Navigate().Refresh();
    }
    
    /// <inheritdoc />
    public byte[] TakeScreenshot()
    {        
        var screenshot = _driver.GetScreenshot();
        return screenshot.AsByteArray;
    }
    
    /// <inheritdoc />
    public void SaveScreenshot(string path)
    {        
        ArgumentNullException.ThrowIfNull(path);
        
        var screenshot = _driver.GetScreenshot();
        screenshot.SaveAsFile(path);
    }
    
    /// <inheritdoc />
    public void ResetAppState()
    {      
        
        // Reset app by terminating and re-launching
        // Note: ResetApp is deprecated in newer Appium versions
        var bundleId = _driver.Capabilities.GetCapability("appPackage")?.ToString()
                    ?? _driver.Capabilities.GetCapability("bundleId")?.ToString();
        
        if (!string.IsNullOrEmpty(bundleId))
        {
            _driver.TerminateApp(bundleId);
            _driver.ActivateApp(bundleId);
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
                _driver?.Quit();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
        
        _disposed = true;
    }
}

/// <summary>
/// Exception thrown when an element cannot be found.
/// </summary>
public class ElementNotFoundException : Exception
{
    public ElementNotFoundException(string message) : base(message) { }
    public ElementNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// No-op logger implementation.
/// </summary>
internal class NullTestLogger : ITestLogger
{
    public static readonly NullTestLogger Instance = new();
    
    private NullTestLogger() { }
    
    public void LogInfo(string testName, string? pageName, string message) { }
    public void LogAction(string testName, string? pageName, string controlId, string action, string? value = null) { }
    public void LogAssert(string testName, string? pageName, string controlId, string assertion, object? expected, object? actual, bool passed) { }
    public void LogWait(string testName, string? pageName, string controlId, string waitType, bool succeeded, int elapsedMs) { }
    public void LogError(string testName, string? pageName, string? controlId, string action, Exception exception) { }
    public void LogNavigation(string testName, string destination) { }
    public void LogDebug(string message) { }
    public void LogWarning(string message) { }
}
