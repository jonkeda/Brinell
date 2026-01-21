using Brinell.Core;
using Brinell.Core.Exceptions;
using Brinell.Maui.Enums;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Brinell.Maui.Wrappers;

/// <summary>
/// Appium-based implementation of <see cref="IMauiDriver"/>.
/// Delegates all operations to the underlying AppiumDriver.
/// </summary>
/// <remarks>
/// This class will be moved to Brinell.Maui.Appium in a future refactoring.
/// </remarks>
public sealed class MauiDriver : IMauiDriver, IDisposable
{
    private readonly AppiumDriver _driver;
    private readonly MauiPlatform _platform;
    private bool _disposed;
    
    /// <summary>
    /// Creates a new MauiDriver wrapper.
    /// </summary>
    /// <param name="driver">The AppiumDriver to wrap.</param>
    /// <param name="platform">The platform this driver is connected to.</param>
    /// <exception cref="ArgumentNullException">Thrown when driver is null.</exception>
    public MauiDriver(AppiumDriver driver, MauiPlatform platform = MauiPlatform.Windows)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _platform = platform;
    }
    
    #region Platform
    
    /// <inheritdoc />
    public MauiPlatform Platform => _platform;
    
    #endregion
    
    #region Element Finding (IDriver<IMauiElement>)
    
    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator, int timeoutMs = 5000)
    {
        var by = locator.ToBy();
        
        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                var element = wait.Until(d => d.FindElement(by));
                return new MauiElement((AppiumElement)element, this);
            }
            catch (WebDriverTimeoutException)
            {
                throw new ElementNotFoundException(locator);
            }
        }
        
        try
        {
            return new MauiElement((AppiumElement)_driver.FindElement(by), this);
        }
        catch (NoSuchElementException)
        {
            throw new ElementNotFoundException(locator);
        }
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator, int timeoutMs = 0)
    {
        var by = locator.ToBy();
        
        if (timeoutMs > 0)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(timeoutMs));
            try
            {
                // Wait for at least one element to appear
                wait.Until(d => d.FindElements(by).Count > 0);
            }
            catch (WebDriverTimeoutException)
            {
                // No elements found within timeout, return empty list
                return Array.Empty<IMauiElement>();
            }
        }
        
        var elements = _driver.FindElements(by);
        return elements.Select(e => new MauiElement(e, this)).ToList();
    }
    
    /// <inheritdoc />
    public bool TryFindElement(Locator locator, out IMauiElement? element, int timeoutMs = 0)
    {
        try
        {
            element = FindElement(locator, timeoutMs);
            return true;
        }
        catch (ElementNotFoundException)
        {
            element = null;
            return false;
        }
    }
    
    #endregion
    
    #region Window Management
    
    /// <inheritdoc />
    public string CurrentWindowHandle => _driver.CurrentWindowHandle;
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> WindowHandles => _driver.WindowHandles;
    
    #endregion
    
    #region Session Management
    
    /// <inheritdoc />
    public void Quit() => _driver.Quit();
    
    /// <inheritdoc />
    public void Close() => _driver.Close();
    
    #endregion
    
    #region Screenshots
    
    /// <inheritdoc />
    public byte[] GetScreenshot() => _driver.GetScreenshot().AsByteArray;
    
    #endregion
    
    #region Context Switching
    
    /// <inheritdoc />
    public string Context
    {
        get => _driver.Context;
        set => _driver.Context = value;
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> Contexts => _driver.Contexts;
    
    #endregion
    
    #region Script Execution
    
    /// <inheritdoc />
    public object? ExecuteScript(string script, params object[] args)
    {
        return _driver.ExecuteScript(script, args);
    }
    
    #endregion
    
    #region IDiagnosticDriver
    
    /// <inheritdoc />
    public string GetPageSource() => _driver.PageSource;
    
    /// <inheritdoc />
    public string GetAutomationTree() => _driver.PageSource;
    
    #endregion
    
    #region Navigation
    
    /// <inheritdoc />
    public void NavigateTo(string destination) => _driver.Navigate().GoToUrl(destination);
    
    /// <inheritdoc />
    public void NavigateBack() => _driver.Navigate().Back();
    
    /// <inheritdoc />
    public void Refresh() => _driver.Navigate().Refresh();
    
    /// <inheritdoc />
    public byte[] TakeScreenshot() => GetScreenshot();
    
    /// <inheritdoc />
    public void ResetAppState()
    {
        var bundleId = _driver.Capabilities.GetCapability("appPackage")?.ToString()
                    ?? _driver.Capabilities.GetCapability("bundleId")?.ToString();
        
        if (!string.IsNullOrEmpty(bundleId))
        {
            _driver.TerminateApp(bundleId);
            _driver.ActivateApp(bundleId);
        }
    }
    
    #endregion
    
    #region Platform-Specific
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindByAndroidUIAutomator(string uiAutomatorQuery)
    {
        if (_platform != MauiPlatform.Android)
        {
            return Array.Empty<IMauiElement>();
        }
        
        try
        {
            var elements = _driver.FindElements(MobileBy.AndroidUIAutomator(uiAutomatorQuery));
            return elements.Select(e => new MauiElement(e, this)).ToList();
        }
        catch
        {
            return Array.Empty<IMauiElement>();
        }
    }
    
    #endregion
    
    #region IDisposable
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _driver.Quit();
            _disposed = true;
        }
    }
    
    #endregion
    
    #region Internal (for MauiElement access)
    
    /// <summary>
    /// Gets the underlying AppiumDriver for internal use.
    /// </summary>
    internal AppiumDriver Driver => _driver;
    
    #endregion
}
