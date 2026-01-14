using Brinell.Core.Configuration;
using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using Brinell.Maui.Extensions;
using Brinell.Maui.Interfaces;
using Brinell.Maui.Wrappers;
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
            _ => throw new ArgumentException(
                $"Unsupported platform: {platformName}. Use 'android' or 'ios'.",
                nameof(options))
        };
        
        // Wrap the driver in our mockable interface
        _driver = new MauiDriver(_rawDriver);
        
        // Configure implicit wait
        _rawDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(_timeouts.ElementFind);
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
    public IMauiElement? TryFindElement(Locator locator)
    {
        
        ArgumentNullException.ThrowIfNull(locator);
        
        try
        {
            // Temporarily disable implicit wait for immediate check
            var originalTimeout = _rawDriver.Manage().Timeouts().ImplicitWait;
            _rawDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            
            try
            {
                var by = locator.ToBy();
                var element = _rawDriver.FindElement(by);
                return new MauiElement(element);
            }
            finally
            {
                _rawDriver.Manage().Timeouts().ImplicitWait = originalTimeout;
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
    public IMauiElement FindElement(Locator locator)
    {        
        ArgumentNullException.ThrowIfNull(locator);
        
        var by = locator.ToBy();
        
        try
        {
            var element = _rawDriver.FindElement(by);
            return new MauiElement(element);
        }
        catch (NoSuchElementException ex)
        {
            throw new ElementNotFoundException(
                $"Element not found with locator: {locator}", ex);
        }
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
        
        _logger.LogNavigation("", destination);
        
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
