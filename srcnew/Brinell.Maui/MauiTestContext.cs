using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Core.Logging;
using Brinell.Maui.Controls;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;

namespace Brinell.Maui;

/// <summary>
/// MAUI test context implementation. Manages the Appium driver lifecycle
/// and provides access to configuration, logging, and element finding.
/// </summary>
public class MauiTestContext : IMauiTestContext, IDisposable
{
    private bool _disposed;
    private const string ContextName = "MauiTestContext";

    /// <summary>
    /// Initializes a new instance of the MauiTestContext class with an existing driver.
    /// </summary>
    /// <param name="driver">The Appium driver to use.</param>
    /// <param name="platform">The MAUI platform being tested.</param>
    /// <param name="timeouts">Optional timeout settings. Uses defaults if not specified.</param>
    /// <param name="logger">Optional logger. Uses NullTestLogger if not specified.</param>
    public MauiTestContext(
        AppiumDriver driver,
        MauiPlatform platform,
        TimeoutSettings? timeouts = null,
        ITestLogger? logger = null)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
        Platform = platform;
        Timeouts = timeouts ?? TimeoutSettings.Default;
        Logger = logger ?? NullTestLogger.Instance;

        Logger.LogInfo(ContextName, null, $"MauiTestContext initialized for platform: {platform}");
    }

    /// <inheritdoc />
    public AppiumDriver Driver { get; }

    /// <inheritdoc />
    public MauiPlatform Platform { get; }

    /// <inheritdoc />
    public TimeoutSettings Timeouts { get; }

    /// <inheritdoc />
    public ITestLogger Logger { get; }

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;

    #region IElementScope Implementation

    /// <inheritdoc />
    public AppiumElement? TryFindElement(Locator locator)
    {
        try
        {
            var by = LocatorConverter.ToBy(locator, Platform);
            return Driver.FindElement(by) as AppiumElement;
        }
        catch (NoSuchElementException)
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
                $"Element not found: {locator}");
        }
        return element;
    }

    /// <inheritdoc />
    public IReadOnlyList<AppiumElement> FindElements(Locator locator)
    {
        try
        {
            var by = LocatorConverter.ToBy(locator, Platform);
            return Driver.FindElements(by).Cast<AppiumElement>().ToList();
        }
        catch (NoSuchElementException)
        {
            return Array.Empty<AppiumElement>();
        }
    }

    #endregion

    #region Navigation

    /// <inheritdoc />
    public void NavigateTo(string destination)
    {
        Logger.LogNavigation(ContextName, destination);
        
        // For MAUI apps, navigation typically happens through app actions
        // This is a placeholder - actual implementation depends on app architecture
        throw new NotSupportedException(
            "Direct URL navigation is not supported for MAUI apps. " +
            "Use page object methods or deep links instead.");
    }

    /// <inheritdoc />
    public void NavigateBack()
    {
        Logger.LogNavigation(ContextName, "back");
        Driver.Navigate().Back();
    }

    /// <inheritdoc />
    public void Refresh()
    {
        Logger.LogAction(ContextName, null, "App", "Refresh");
        // MAUI apps don't have a refresh concept like web apps
        // This could trigger a page reload mechanism if implemented by the app
        Logger.LogWarning("Refresh is not natively supported for MAUI apps");
    }

    #endregion

    #region Screenshots

    /// <inheritdoc />
    public byte[] TakeScreenshot()
    {
        try
        {
            var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
            Logger.LogInfo(ContextName, null, "Screenshot captured");
            return screenshot.AsByteArray;
        }
        catch (Exception ex)
        {
            Logger.LogError(ContextName, null, null, "TakeScreenshot", ex);
            return Array.Empty<byte>();
        }
    }

    /// <inheritdoc />
    public void SaveScreenshot(string path)
    {
        var bytes = TakeScreenshot();
        if (bytes.Length > 0)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, bytes);
            Logger.LogInfo(ContextName, null, $"Screenshot saved to: {path}");
        }
    }

    #endregion

    #region App State

    /// <inheritdoc />
    public void ResetAppState()
    {
        Logger.LogAction(ContextName, null, "App", "ResetAppState");
        try
        {
            // Platform-specific app reset
            // Note: ResetApp() is deprecated in newer Appium versions
            // Using terminate/activate app pattern instead
            switch (Platform)
            {
                case MauiPlatform.Android:
                    if (Driver is AndroidDriver androidDriver)
                    {
                        var appPackage = androidDriver.Capabilities.GetCapability("appPackage")?.ToString();
                        if (!string.IsNullOrEmpty(appPackage))
                        {
                            androidDriver.TerminateApp(appPackage);
                            androidDriver.ActivateApp(appPackage);
                        }
                    }
                    break;
                case MauiPlatform.iOS:
                    if (Driver is IOSDriver iosDriver)
                    {
                        var bundleId = iosDriver.Capabilities.GetCapability("bundleId")?.ToString();
                        if (!string.IsNullOrEmpty(bundleId))
                        {
                            iosDriver.TerminateApp(bundleId);
                            iosDriver.ActivateApp(bundleId);
                        }
                    }
                    break;
                case MauiPlatform.Windows:
                    // Windows apps don't have a standard reset mechanism
                    Logger.LogWarning("Reset app state is not fully supported for Windows apps");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ContextName, null, null, "ResetAppState", ex);
        }
    }

    #endregion

    #region Disposal

    /// <summary>
    /// Disposes the test context and the underlying driver.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Logger.LogInfo(ContextName, null, "Disposing MauiTestContext");
                try
                {
                    Driver?.Quit();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ContextName, null, null, "Dispose", ex);
                }
            }
            _disposed = true;
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a MAUI test context for Android testing.
    /// </summary>
    /// <param name="appiumServerUrl">The Appium server URL.</param>
    /// <param name="options">Android driver options.</param>
    /// <param name="timeouts">Optional timeout settings.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>A new MauiTestContext configured for Android.</returns>
    public static MauiTestContext CreateAndroid(
        Uri appiumServerUrl,
        AndroidDriver driver,
        TimeoutSettings? timeouts = null,
        ITestLogger? logger = null)
    {
        return new MauiTestContext(driver, MauiPlatform.Android, timeouts, logger);
    }

    /// <summary>
    /// Creates a MAUI test context for iOS testing.
    /// </summary>
    /// <param name="appiumServerUrl">The Appium server URL.</param>
    /// <param name="driver">iOS driver.</param>
    /// <param name="timeouts">Optional timeout settings.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>A new MauiTestContext configured for iOS.</returns>
    public static MauiTestContext CreateiOS(
        Uri appiumServerUrl,
        IOSDriver driver,
        TimeoutSettings? timeouts = null,
        ITestLogger? logger = null)
    {
        return new MauiTestContext(driver, MauiPlatform.iOS, timeouts, logger);
    }

    #endregion
}
