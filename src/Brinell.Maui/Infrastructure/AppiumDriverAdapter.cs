using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using Brinell.Core.Abstractions;

namespace Brinell.Maui.Infrastructure;

/// <summary>
/// Appium driver adapter for MAUI app automation.
/// Supports Windows, Android, and iOS platforms.
/// </summary>
public class AppiumDriverAdapter : IDriverAdapter
{
    private readonly AppiumDriver _driver;
    private readonly string _platform;

    /// <summary>
    /// The underlying Appium driver.
    /// </summary>
    public AppiumDriver Driver => _driver;
    
    /// <summary>
    /// The platform being tested (Windows, Android, iOS).
    /// </summary>
    public string Platform => _platform;

    /// <summary>
    /// Create driver for Windows MAUI app.
    /// </summary>
    public AppiumDriverAdapter(string appPath, Uri appiumServerUri, TimeSpan? commandTimeout = null)
    {
        var options = new AppiumOptions
        {
            App = appPath,
            PlatformName = "Windows",
            AutomationName = "Windows"
        };
        
        var timeout = commandTimeout ?? TimeSpan.FromMinutes(2);
        _driver = new WindowsDriver(appiumServerUri, options, timeout);
        _platform = "Windows";
    }

    /// <summary>
    /// Create driver with pre-configured options.
    /// </summary>
    public AppiumDriverAdapter(AppiumDriver driver, string platform)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _platform = platform;
    }

    /// <summary>
    /// Create driver for Android MAUI app.
    /// </summary>
    public static AppiumDriverAdapter CreateAndroid(
        string appPath, 
        Uri appiumServerUri,
        string? deviceName = null,
        TimeSpan? commandTimeout = null)
    {
        var options = new AppiumOptions
        {
            App = appPath,
            PlatformName = "Android",
            AutomationName = "UiAutomator2"
        };
        
        if (!string.IsNullOrEmpty(deviceName))
            options.DeviceName = deviceName;
        
        var timeout = commandTimeout ?? TimeSpan.FromMinutes(2);
        var driver = new AndroidDriver(appiumServerUri, options, timeout);
        return new AppiumDriverAdapter(driver, "Android");
    }

    /// <summary>
    /// Create driver for iOS MAUI app.
    /// </summary>
    public static AppiumDriverAdapter CreateiOS(
        string appPath,
        Uri appiumServerUri,
        string? deviceName = null,
        string? platformVersion = null,
        TimeSpan? commandTimeout = null)
    {
        var options = new AppiumOptions
        {
            App = appPath,
            PlatformName = "iOS",
            AutomationName = "XCUITest"
        };
        
        if (!string.IsNullOrEmpty(deviceName))
            options.DeviceName = deviceName;
        if (!string.IsNullOrEmpty(platformVersion))
            options.PlatformVersion = platformVersion;
        
        var timeout = commandTimeout ?? TimeSpan.FromMinutes(2);
        var driver = new IOSDriver(appiumServerUri, options, timeout);
        return new AppiumDriverAdapter(driver, "iOS");
    }

    public IElementAdapter? FindElement(string automationId)
    {
        try
        {
            // Try accessibility ID first (MAUI AutomationId maps to this)
            var element = _driver.FindElement(MobileBy.AccessibilityId(automationId));
            return element != null ? new AppiumElementAdapter((AppiumElement)element) : null;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Find element and return the raw AppiumElement (for direct Appium access).
    /// </summary>
    public AppiumElement? FindElementDirect(string automationId)
    {
        try
        {
            var element = _driver.FindElement(MobileBy.AccessibilityId(automationId));
            return element as AppiumElement;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    public IElementAdapter? FindElementByXPath(string xpath)
    {
        try
        {
            var element = _driver.FindElement(By.XPath(xpath));
            return element != null ? new AppiumElementAdapter((AppiumElement)element) : null;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    public IReadOnlyCollection<IElementAdapter> FindElements(string automationId)
    {
        try
        {
            var elements = _driver.FindElements(MobileBy.AccessibilityId(automationId));
            return elements.Select(e => new AppiumElementAdapter((AppiumElement)e)).ToList();
        }
        catch (NoSuchElementException)
        {
            return Array.Empty<IElementAdapter>();
        }
    }

    public void Click(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            ae.Click();
        }
    }

    public void SendKeys(IElementAdapter element, string text)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            ae.Clear();
            ae.SendKeys(text);
        }
    }

    public void Clear(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            ae.Clear();
        }
    }

    public string? GetText(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            // Try Text attribute first, then fall back to GetAttribute
            return ae.Text ?? ae.GetAttribute("text") ?? ae.GetAttribute("value");
        }
        return null;
    }

    public string? GetAttribute(IElementAdapter element, string name)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            return ae.GetAttribute(name);
        }
        return null;
    }

    public bool IsDisplayed(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            return ae.Displayed;
        }
        return false;
    }

    public bool IsEnabled(IElementAdapter element)
    {
        if (element.NativeElement is AppiumElement ae)
        {
            return ae.Enabled;
        }
        return false;
    }

    /// <summary>
    /// Find element within a container element by AutomationId.
    /// </summary>
    /// <param name="container">The container element to search within.</param>
    /// <param name="automationId">The AutomationId to find.</param>
    /// <returns>The element if found, null otherwise.</returns>
    public AppiumElement? FindElementInContainer(AppiumElement container, string automationId)
    {
        try
        {
            return container.FindElement(MobileBy.AccessibilityId(automationId)) as AppiumElement;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Find all elements within a container element by AutomationId.
    /// </summary>
    /// <param name="container">The container element to search within.</param>
    /// <param name="automationId">The AutomationId to find.</param>
    /// <returns>Collection of matching elements.</returns>
    public IReadOnlyCollection<AppiumElement> FindElementsInContainer(AppiumElement container, string automationId)
    {
        try
        {
            return container.FindElements(MobileBy.AccessibilityId(automationId))
                .Cast<AppiumElement>()
                .ToList();
        }
        catch (NoSuchElementException)
        {
            return Array.Empty<AppiumElement>();
        }
    }

    public void Dispose()
    {
        try
        {
            _driver?.Quit();
        }
        catch
        {
            // Ignore errors during cleanup
        }
        finally
        {
            _driver?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
