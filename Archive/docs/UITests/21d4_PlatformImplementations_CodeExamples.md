# 4. Platform Implementations - Code Examples

**Parent:** [Platform Implementations](21d4_PlatformImplementations.md)  
**Version:** 3.0 (Updated December 2025)

**Note (v3):** Platform projects are self-contained with their own base class hierarchies. They use native drivers directly - no adapter layer.

---

## 4.1 WPF - FlaUITestContext (Direct Driver Access)

```csharp
namespace Oravey.UITestFramework.Wpf.Infrastructure;

using System.Diagnostics;
using FlaUI.Core;
using FlaUI.UIA3;
using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Configuration;
using Oravey.UITestFramework.Core.Logging;

public class FlaUITestContext : ITestContext
{
    private readonly FlaUIDriverAdapter _driver;
    private readonly UIA3Automation _automation;
    
    public string TestName { get; set; } = "Unknown";
    public Platform Platform => Platform.Windows;
    public IDriverAdapter Driver => _driver;
    public ITestLogger Logger { get; }
    public TestConfiguration Configuration { get; }
    
    public int DefaultTimeoutMs => Configuration.DefaultTimeoutMs;
    public int ShortTimeoutMs => Configuration.ShortTimeoutMs;
    public int PollingIntervalMs => Configuration.PollingIntervalMs;
    
    public FlaUITestContext(string applicationPath, TestConfiguration? config = null)
    {
        Configuration = config ?? TestConfiguration.Load();
        Logger = new TestLogger(Configuration.LogFilePath);
        
        _automation = new UIA3Automation();
        _driver = new FlaUIDriverAdapter(applicationPath, _automation);
    }
    
    public bool WaitFor(Func<bool> condition, int? timeoutMs = null, string? description = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            if (condition())
            {
                Logger.LogInfo(TestName, null, $"Wait succeeded: {description ?? "condition"}");
                return true;
            }
            Thread.Sleep(PollingIntervalMs);
        }
        
        Logger.LogInfo(TestName, null, $"Wait timeout: {description ?? "condition"}");
        return false;
    }
    
    public IElementAdapter? WaitForElement(string automationId, int? timeoutMs = null)
    {
        IElementAdapter? result = null;
        var found = WaitFor(
            () => (result = Driver.FindElement(automationId)) != null,
            timeoutMs,
            $"element '{automationId}' to exist");
        return found ? result : null;
    }
    
    public IElementAdapter? WaitForElementVisible(string automationId, int? timeoutMs = null)
    {
        IElementAdapter? result = null;
        var found = WaitFor(
            () =>
            {
                result = Driver.FindElement(automationId);
                return result != null && Driver.IsVisible(result);
            },
            timeoutMs,
            $"element '{automationId}' to be visible");
        return found ? result : null;
    }
    
    public bool WaitForElementGone(string automationId, int? timeoutMs = null)
    {
        return WaitFor(
            () => !Driver.ElementExists(automationId),
            timeoutMs,
            $"element '{automationId}' to be gone");
    }
    
    public void Log(string message)
    {
        Logger.LogInfo(TestName, null, message);
    }
    
    public void Dispose()
    {
        _driver.Dispose();
        _automation.Dispose();
        Logger.Dispose();
    }
}
```

---

## 4.2 WPF - FlaUIDriverAdapter

```csharp
namespace Oravey.UITestFramework.Wpf.Infrastructure;

using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using Oravey.UITestFramework.Core.Abstractions;

public class FlaUIDriverAdapter : IDriverAdapter
{
    private readonly Application _application;
    private readonly AutomationBase _automation;
    private readonly Window _mainWindow;
    
    public FlaUIDriverAdapter(string applicationPath, AutomationBase automation)
    {
        _automation = automation;
        _application = Application.Launch(applicationPath);
        
        // Wait for main window with timeout
        _mainWindow = _application.GetMainWindow(_automation, TimeSpan.FromSeconds(30))
            ?? throw new InvalidOperationException("Main window not found");
    }
    
    public IElementAdapter? FindElement(string automationId)
    {
        var condition = _automation.ConditionFactory.ByAutomationId(automationId);
        var element = _mainWindow.FindFirstDescendant(condition);
        return element != null ? new FlaUIElementAdapter(element) : null;
    }
    
    public IReadOnlyList<IElementAdapter> FindElements(string automationId)
    {
        var condition = _automation.ConditionFactory.ByAutomationId(automationId);
        var elements = _mainWindow.FindAllDescendants(condition);
        return elements.Select(e => new FlaUIElementAdapter(e)).ToList();
    }
    
    public bool ElementExists(string automationId)
    {
        return FindElement(automationId) != null;
    }
    
    public void Click(IElementAdapter element)
    {
        var flaElement = GetNativeElement(element);
        flaElement.Click();
    }
    
    public void DoubleClick(IElementAdapter element)
    {
        var flaElement = GetNativeElement(element);
        flaElement.DoubleClick();
    }
    
    public void SendKeys(IElementAdapter element, string text)
    {
        var flaElement = GetNativeElement(element);
        
        // Try TextBox pattern first
        if (flaElement.Patterns.Value.IsSupported)
        {
            flaElement.Patterns.Value.Pattern.SetValue(text);
        }
        else
        {
            // Fallback to keyboard input
            flaElement.Focus();
            FlaUI.Core.Input.Keyboard.Type(text);
        }
    }
    
    public void Clear(IElementAdapter element)
    {
        var flaElement = GetNativeElement(element);
        
        if (flaElement.Patterns.Value.IsSupported)
        {
            flaElement.Patterns.Value.Pattern.SetValue(string.Empty);
        }
    }
    
    public string GetText(IElementAdapter element)
    {
        var flaElement = GetNativeElement(element);
        
        // Try Value pattern
        if (flaElement.Patterns.Value.IsSupported)
        {
            return flaElement.Patterns.Value.Pattern.Value.Value;
        }
        
        // Try Text pattern
        if (flaElement.Patterns.Text.IsSupported)
        {
            return flaElement.Patterns.Text.Pattern.DocumentRange.GetText(-1);
        }
        
        // Fallback to Name property
        return flaElement.Properties.Name.ValueOrDefault ?? string.Empty;
    }
    
    public string? GetAttribute(IElementAdapter element, string attributeName)
    {
        var flaElement = GetNativeElement(element);
        
        return attributeName.ToLower() switch
        {
            "name" => flaElement.Properties.Name.ValueOrDefault,
            "classname" => flaElement.Properties.ClassName.ValueOrDefault,
            "controltype" => flaElement.Properties.ControlType.ValueOrDefault.ToString(),
            "automationid" => flaElement.Properties.AutomationId.ValueOrDefault,
            _ => null
        };
    }
    
    public bool IsVisible(IElementAdapter element)
    {
        var flaElement = GetNativeElement(element);
        return !flaElement.Properties.IsOffscreen.ValueOrDefault;
    }
    
    public bool IsEnabled(IElementAdapter element)
    {
        var flaElement = GetNativeElement(element);
        return flaElement.Properties.IsEnabled.ValueOrDefault;
    }
    
    public bool IsSelected(IElementAdapter element)
    {
        var flaElement = GetNativeElement(element);
        
        if (flaElement.Patterns.Toggle.IsSupported)
        {
            var state = flaElement.Patterns.Toggle.Pattern.ToggleState.Value;
            return state == FlaUI.Core.Definitions.ToggleState.On;
        }
        
        if (flaElement.Patterns.SelectionItem.IsSupported)
        {
            return flaElement.Patterns.SelectionItem.Pattern.IsSelected.Value;
        }
        
        return false;
    }
    
    public void TakeScreenshot(string filePath)
    {
        var capture = _mainWindow.Capture();
        capture.Save(filePath);
    }
    
    public void FocusMainWindow()
    {
        _mainWindow.SetForeground();
    }
    
    private static AutomationElement GetNativeElement(IElementAdapter element)
    {
        return ((FlaUIElementAdapter)element).Element;
    }
    
    public void Dispose()
    {
        _application.Close();
        _application.Dispose();
    }
}
```

---

## 4.3 WPF - FlaUIElementAdapter

```csharp
namespace Oravey.UITestFramework.Wpf.Infrastructure;

using FlaUI.Core.AutomationElements;
using Oravey.UITestFramework.Core.Abstractions;

public class FlaUIElementAdapter : IElementAdapter
{
    public AutomationElement Element { get; }
    
    public FlaUIElementAdapter(AutomationElement element)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
    }
    
    public string AutomationId => Element.Properties.AutomationId.ValueOrDefault ?? string.Empty;
    
    public object NativeElement => Element;
}
```

---

## 4.4 MAUI - AppiumTestContext

```csharp
namespace Oravey.UITestFramework.Maui.Infrastructure;

using System.Diagnostics;
using OpenQA.Selenium.Appium;
using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Configuration;
using Oravey.UITestFramework.Core.Logging;

public class AppiumTestContext : ITestContext
{
    private readonly AppiumDriverAdapter _driver;
    
    public string TestName { get; set; } = "Unknown";
    public Platform Platform { get; }
    public IDriverAdapter Driver => _driver;
    public ITestLogger Logger { get; }
    public TestConfiguration Configuration { get; }
    
    public int DefaultTimeoutMs => Configuration.DefaultTimeoutMs;
    public int ShortTimeoutMs => Configuration.ShortTimeoutMs;
    public int PollingIntervalMs => Configuration.PollingIntervalMs;
    
    private AppiumTestContext(AppiumDriverAdapter driver, Platform platform, TestConfiguration config)
    {
        _driver = driver;
        Platform = platform;
        Configuration = config;
        Logger = new TestLogger(config.LogFilePath);
    }
    
    /// <summary>Create context for Windows MAUI app.</summary>
    public static AppiumTestContext CreateWindows(string appPath, TestConfiguration? config = null)
    {
        config ??= TestConfiguration.Load();
        var serverUri = new Uri(config.AppiumServerUrl ?? "http://127.0.0.1:4723");
        var driver = AppiumDriverAdapter.CreateWindows(appPath, serverUri);
        return new AppiumTestContext(driver, Platform.WindowsMaui, config);
    }
    
    /// <summary>Create context for Android app.</summary>
    public static AppiumTestContext CreateAndroid(string appPath, TestConfiguration? config = null)
    {
        config ??= TestConfiguration.Load();
        var serverUri = new Uri(config.AppiumServerUrl ?? "http://127.0.0.1:4723");
        var driver = AppiumDriverAdapter.CreateAndroid(appPath, serverUri);
        return new AppiumTestContext(driver, Platform.Android, config);
    }
    
    /// <summary>Create context for iOS app.</summary>
    public static AppiumTestContext CreateiOS(string appPath, TestConfiguration? config = null)
    {
        config ??= TestConfiguration.Load();
        var serverUri = new Uri(config.AppiumServerUrl ?? "http://127.0.0.1:4723");
        var driver = AppiumDriverAdapter.CreateiOS(appPath, serverUri);
        return new AppiumTestContext(driver, Platform.iOS, config);
    }
    
    public bool WaitFor(Func<bool> condition, int? timeoutMs = null, string? description = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            try
            {
                if (condition())
                {
                    Logger.LogInfo(TestName, null, $"Wait succeeded: {description ?? "condition"}");
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions during polling
            }
            Thread.Sleep(PollingIntervalMs);
        }
        
        Logger.LogInfo(TestName, null, $"Wait timeout: {description ?? "condition"}");
        return false;
    }
    
    public IElementAdapter? WaitForElement(string automationId, int? timeoutMs = null)
    {
        IElementAdapter? result = null;
        var found = WaitFor(
            () => (result = Driver.FindElement(automationId)) != null,
            timeoutMs,
            $"element '{automationId}' to exist");
        return found ? result : null;
    }
    
    public IElementAdapter? WaitForElementVisible(string automationId, int? timeoutMs = null)
    {
        IElementAdapter? result = null;
        var found = WaitFor(
            () =>
            {
                result = Driver.FindElement(automationId);
                return result != null && Driver.IsVisible(result);
            },
            timeoutMs,
            $"element '{automationId}' to be visible");
        return found ? result : null;
    }
    
    public bool WaitForElementGone(string automationId, int? timeoutMs = null)
    {
        return WaitFor(
            () => !Driver.ElementExists(automationId),
            timeoutMs,
            $"element '{automationId}' to be gone");
    }
    
    public void Log(string message)
    {
        Logger.LogInfo(TestName, null, message);
    }
    
    public void Dispose()
    {
        _driver.Dispose();
        Logger.Dispose();
    }
}
```

---

## 4.5 MAUI - AppiumDriverAdapter

```csharp
namespace Oravey.UITestFramework.Maui.Infrastructure;

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using OpenQA.Selenium.Appium.Windows;
using Oravey.UITestFramework.Core.Abstractions;

public class AppiumDriverAdapter : IDriverAdapter
{
    private readonly AppiumDriver _driver;
    
    private AppiumDriverAdapter(AppiumDriver driver)
    {
        _driver = driver;
    }
    
    /// <summary>Create driver for Windows MAUI app.</summary>
    public static AppiumDriverAdapter CreateWindows(string appPath, Uri serverUri)
    {
        var options = new AppiumOptions
        {
            PlatformName = "Windows",
            AutomationName = "Windows"
        };
        options.AddAdditionalAppiumOption("app", appPath);
        options.AddAdditionalAppiumOption("ms:waitForAppLaunch", 30);
        
        var driver = new WindowsDriver(serverUri, options);
        return new AppiumDriverAdapter(driver);
    }
    
    /// <summary>Create driver for Android app.</summary>
    public static AppiumDriverAdapter CreateAndroid(string appPath, Uri serverUri)
    {
        var options = new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = "UiAutomator2"
        };
        options.AddAdditionalAppiumOption("app", appPath);
        options.AddAdditionalAppiumOption("appWaitActivity", "*");
        options.AddAdditionalAppiumOption("autoGrantPermissions", true);
        
        var driver = new AndroidDriver(serverUri, options);
        return new AppiumDriverAdapter(driver);
    }
    
    /// <summary>Create driver for iOS app.</summary>
    public static AppiumDriverAdapter CreateiOS(string appPath, Uri serverUri)
    {
        var options = new AppiumOptions
        {
            PlatformName = "iOS",
            AutomationName = "XCUITest"
        };
        options.AddAdditionalAppiumOption("app", appPath);
        options.AddAdditionalAppiumOption("autoAcceptAlerts", true);
        
        var driver = new IOSDriver(serverUri, options);
        return new AppiumDriverAdapter(driver);
    }
    
    public IElementAdapter? FindElement(string automationId)
    {
        try
        {
            var element = _driver.FindElement(MobileBy.AccessibilityId(automationId));
            return new AppiumElementAdapter(element);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }
    
    public IReadOnlyList<IElementAdapter> FindElements(string automationId)
    {
        var elements = _driver.FindElements(MobileBy.AccessibilityId(automationId));
        return elements.Select(e => new AppiumElementAdapter(e)).ToList();
    }
    
    public bool ElementExists(string automationId)
    {
        return FindElement(automationId) != null;
    }
    
    public void Click(IElementAdapter element)
    {
        GetNativeElement(element).Click();
    }
    
    public void DoubleClick(IElementAdapter element)
    {
        var appiumElement = GetNativeElement(element);
        // Use Actions for double-click
        new OpenQA.Selenium.Interactions.Actions(_driver)
            .DoubleClick(appiumElement)
            .Perform();
    }
    
    public void SendKeys(IElementAdapter element, string text)
    {
        GetNativeElement(element).SendKeys(text);
    }
    
    public void Clear(IElementAdapter element)
    {
        GetNativeElement(element).Clear();
    }
    
    public string GetText(IElementAdapter element)
    {
        return GetNativeElement(element).Text;
    }
    
    public string? GetAttribute(IElementAdapter element, string attributeName)
    {
        return GetNativeElement(element).GetAttribute(attributeName);
    }
    
    public bool IsVisible(IElementAdapter element)
    {
        return GetNativeElement(element).Displayed;
    }
    
    public bool IsEnabled(IElementAdapter element)
    {
        return GetNativeElement(element).Enabled;
    }
    
    public bool IsSelected(IElementAdapter element)
    {
        return GetNativeElement(element).Selected;
    }
    
    public void TakeScreenshot(string filePath)
    {
        var screenshot = _driver.GetScreenshot();
        screenshot.SaveAsFile(filePath);
    }
    
    public void FocusMainWindow()
    {
        // Not applicable for mobile
    }
    
    private static AppiumElement GetNativeElement(IElementAdapter element)
    {
        return ((AppiumElementAdapter)element).Element;
    }
    
    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
```

---

## 4.6 HTML - SeleniumTestContext

```csharp
namespace Oravey.UITestFramework.Html.Infrastructure;

using System.Diagnostics;
using OpenQA.Selenium;
using Oravey.UITestFramework.Core.Abstractions;
using Oravey.UITestFramework.Core.Configuration;
using Oravey.UITestFramework.Core.Logging;

public class SeleniumTestContext : ITestContext
{
    private readonly SeleniumDriverAdapter _driver;
    
    public string TestName { get; set; } = "Unknown";
    public Platform Platform => Platform.Web;
    public IDriverAdapter Driver => _driver;
    public ITestLogger Logger { get; }
    public TestConfiguration Configuration { get; }
    
    public int DefaultTimeoutMs => Configuration.DefaultTimeoutMs;
    public int ShortTimeoutMs => Configuration.ShortTimeoutMs;
    public int PollingIntervalMs => Configuration.PollingIntervalMs;
    
    public SeleniumTestContext(string baseUrl, string browserType = "Chrome", TestConfiguration? config = null)
    {
        Configuration = config ?? TestConfiguration.Load();
        Logger = new TestLogger(Configuration.LogFilePath);
        _driver = new SeleniumDriverAdapter(browserType, baseUrl);
    }
    
    /// <summary>Create context for cloud provider (BrowserStack, SauceLabs).</summary>
    public static SeleniumTestContext CreateCloud(CloudProviderConfig cloudConfig, TestConfiguration? config = null)
    {
        config ??= TestConfiguration.Load();
        var context = new SeleniumTestContext(
            config.BaseUrl ?? throw new InvalidOperationException("BaseUrl required"),
            "Chrome",
            config);
        
        // Replace driver with cloud driver
        context._driver.Dispose();
        // ... create cloud driver
        
        return context;
    }
    
    public bool WaitFor(Func<bool> condition, int? timeoutMs = null, string? description = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            try
            {
                if (condition())
                {
                    Logger.LogInfo(TestName, null, $"Wait succeeded: {description ?? "condition"}");
                    return true;
                }
            }
            catch (StaleElementReferenceException)
            {
                // Element was removed from DOM, continue polling
            }
            Thread.Sleep(PollingIntervalMs);
        }
        
        Logger.LogInfo(TestName, null, $"Wait timeout: {description ?? "condition"}");
        return false;
    }
    
    public IElementAdapter? WaitForElement(string automationId, int? timeoutMs = null)
    {
        IElementAdapter? result = null;
        var found = WaitFor(
            () => (result = Driver.FindElement(automationId)) != null,
            timeoutMs,
            $"element '{automationId}' to exist");
        return found ? result : null;
    }
    
    public IElementAdapter? WaitForElementVisible(string automationId, int? timeoutMs = null)
    {
        IElementAdapter? result = null;
        var found = WaitFor(
            () =>
            {
                result = Driver.FindElement(automationId);
                return result != null && Driver.IsVisible(result);
            },
            timeoutMs,
            $"element '{automationId}' to be visible");
        return found ? result : null;
    }
    
    public bool WaitForElementGone(string automationId, int? timeoutMs = null)
    {
        return WaitFor(
            () => !Driver.ElementExists(automationId),
            timeoutMs,
            $"element '{automationId}' to be gone");
    }
    
    public void Log(string message)
    {
        Logger.LogInfo(TestName, null, message);
    }
    
    public void Dispose()
    {
        _driver.Dispose();
        Logger.Dispose();
    }
}
```

---

## 4.7 HTML - SeleniumDriverAdapter

```csharp
namespace Oravey.UITestFramework.Html.Infrastructure;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using Oravey.UITestFramework.Core.Abstractions;

public class SeleniumDriverAdapter : IDriverAdapter
{
    private readonly IWebDriver _driver;
    
    public SeleniumDriverAdapter(string browserType, string baseUrl)
    {
        _driver = CreateDriver(browserType);
        _driver.Navigate().GoToUrl(baseUrl);
        _driver.Manage().Window.Maximize();
    }
    
    private static IWebDriver CreateDriver(string browserType)
    {
        return browserType.ToLower() switch
        {
            "chrome" => new ChromeDriver(CreateChromeOptions()),
            "firefox" => new FirefoxDriver(),
            "edge" => new EdgeDriver(),
            _ => throw new ArgumentException($"Unsupported browser: {browserType}")
        };
    }
    
    private static ChromeOptions CreateChromeOptions()
    {
        var options = new ChromeOptions();
        options.AddArgument("--disable-extensions");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        return options;
    }
    
    public IElementAdapter? FindElement(string automationId)
    {
        try
        {
            // Try data-automation-id first
            var element = _driver.FindElement(
                By.CssSelector($"[data-automation-id='{automationId}']"));
            return new SeleniumElementAdapter(element, automationId);
        }
        catch (NoSuchElementException)
        {
            try
            {
                // Fallback to id
                var element = _driver.FindElement(By.Id(automationId));
                return new SeleniumElementAdapter(element, automationId);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }
    
    public IReadOnlyList<IElementAdapter> FindElements(string automationId)
    {
        var elements = _driver.FindElements(
            By.CssSelector($"[data-automation-id='{automationId}']"));
        
        if (elements.Count == 0)
        {
            elements = _driver.FindElements(By.Id(automationId));
        }
        
        return elements.Select(e => new SeleniumElementAdapter(e, automationId)).ToList();
    }
    
    public bool ElementExists(string automationId)
    {
        return FindElement(automationId) != null;
    }
    
    public void Click(IElementAdapter element)
    {
        GetNativeElement(element).Click();
    }
    
    public void DoubleClick(IElementAdapter element)
    {
        var webElement = GetNativeElement(element);
        new OpenQA.Selenium.Interactions.Actions(_driver)
            .DoubleClick(webElement)
            .Perform();
    }
    
    public void SendKeys(IElementAdapter element, string text)
    {
        GetNativeElement(element).SendKeys(text);
    }
    
    public void Clear(IElementAdapter element)
    {
        GetNativeElement(element).Clear();
    }
    
    public string GetText(IElementAdapter element)
    {
        var webElement = GetNativeElement(element);
        
        // For input elements, get value attribute
        var tagName = webElement.TagName.ToLower();
        if (tagName == "input" || tagName == "textarea")
        {
            return webElement.GetAttribute("value") ?? string.Empty;
        }
        
        return webElement.Text;
    }
    
    public string? GetAttribute(IElementAdapter element, string attributeName)
    {
        return GetNativeElement(element).GetAttribute(attributeName);
    }
    
    public bool IsVisible(IElementAdapter element)
    {
        return GetNativeElement(element).Displayed;
    }
    
    public bool IsEnabled(IElementAdapter element)
    {
        return GetNativeElement(element).Enabled;
    }
    
    public bool IsSelected(IElementAdapter element)
    {
        return GetNativeElement(element).Selected;
    }
    
    public void TakeScreenshot(string filePath)
    {
        var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
        screenshot.SaveAsFile(filePath);
    }
    
    public void FocusMainWindow()
    {
        _driver.SwitchTo().Window(_driver.WindowHandles[0]);
    }
    
    private static IWebElement GetNativeElement(IElementAdapter element)
    {
        return ((SeleniumElementAdapter)element).Element;
    }
    
    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
```

---

## 4.8 HTML - SeleniumElementAdapter

```csharp
namespace Oravey.UITestFramework.Html.Infrastructure;

using OpenQA.Selenium;
using Oravey.UITestFramework.Core.Abstractions;

public class SeleniumElementAdapter : IElementAdapter
{
    public IWebElement Element { get; }
    
    public SeleniumElementAdapter(IWebElement element, string automationId)
    {
        Element = element ?? throw new ArgumentNullException(nameof(element));
        AutomationId = automationId;
    }
    
    public string AutomationId { get; }
    
    public object NativeElement => Element;
}
```

---

*Related: [Multi-Platform Support Code Examples](21d5_MultiPlatformSupport_CodeExamples.md)*
