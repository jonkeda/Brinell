# 250.009 Platform Contexts Specification

**Block Type:** SPC (Specification)  
**ID:** 250.009  
**Title:** Platform-Specific Test Context Specification  
**Status:** Draft  
**Version:** 1.0  
**Level:** 0 - Foundation

---

## 1. Overview

This specification defines the platform-specific test context interfaces and implementations for MAUI, Blazor, and WPF platforms. Each context extends the generic `ITestContext<TElement>` with platform-specific element types and capabilities.

### Context Hierarchy

```
ITestContext (Base - no element finding)
│
└── ITestContext<TElement> : IElementScope<TElement> (typed element finding)
        │
        ├── IMauiTestContext : ITestContext<AppiumElement>, IMauiElementScope
        │   └── MauiTestContext
        │
        ├── IBlazorTestContext : ITestContext<IWebElement>, IBlazorElementScope
        │   └── BlazorTestContext
        │
        └── IWpfTestContext : ITestContext<AutomationElement>
            └── WpfTestContext
```

### Type Parameter Design

The generic type parameter `TElement` flows through the interface hierarchy:

```csharp
// Generic interfaces
ITestContext<TElement> : IElementScope<TElement>
IElementScope<TElement> → TryFindElement() returns TElement?

// Platform narrows TElement via inheritance
IMauiTestContext : ITestContext<AppiumElement>
// → TryFindElement() returns AppiumElement? - NO CASTING!

IBlazorTestContext : ITestContext<IWebElement>
// → TryFindElement() returns IWebElement? - NO CASTING!
```

---

## 2. IMauiTestContext

Platform context for MAUI applications using Appium. Implements `ITestContext<AppiumElement>` for typed element finding.

### Interface Definition

```csharp
namespace Brinell.Maui
{
    /// <summary>
    /// MAUI element scope - provides typed AppiumElement finding.
    /// </summary>
    public interface IMauiElementScope : IElementScope<AppiumElement>
    {
        /// <summary>
        /// Access to the context for driver operations.
        /// </summary>
        IMauiTestContext Context { get; }
    }
    
    /// <summary>
    /// MAUI test context with typed AppiumElement finding.
    /// </summary>
    public interface IMauiTestContext : ITestContext<AppiumElement>, IMauiElementScope
    {
        // Driver access
        AppiumDriver Driver { get; }
        
        // Platform info
        MauiPlatform Platform { get; }
        
        // Inherits from ITestContext<AppiumElement> / IElementScope<AppiumElement>:
        // AppiumElement? TryFindElement(Locator locator);
        // AppiumElement FindElement(Locator locator);
        // IReadOnlyList<AppiumElement> FindElements(Locator locator);
        
        // Override default locator strategy
        new LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
        
        // Platform-specific capabilities
        AppiumCapabilities Capabilities { get; }
        
        // Device interaction
        void RotateDevice(ScreenOrientation orientation);
        void SetLocation(double latitude, double longitude);
        void ShakeDevice();
        void HideKeyboard();
        bool IsKeyboardShown();
        
        // App lifecycle
        void BackgroundApp(TimeSpan duration);
        void ResetApp();
        void CloseApp();
        void LaunchApp();
        
        // IMauiElementScope - self-reference
        IMauiTestContext IMauiElementScope.Context => this;
    }
    
    public enum MauiPlatform
    {
        Android,
        iOS,
        Windows,
        MacCatalyst
    }
}
```

### Implementation

```csharp
namespace Brinell.Maui
{
    public class MauiTestContext : IMauiTestContext
    {
        private readonly AppiumDriver _driver;
        private readonly AppiumCapabilities _capabilities;
        private readonly ITestLogger _logger;
        private readonly TimeoutSettings _timeouts;
        private readonly MauiPlatform _platform;
        
        public MauiTestContext(AppiumDriver driver, MauiPlatform platform, ITestLogger? logger = null)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _platform = platform;
            _logger = logger ?? new NullLogger();
            _timeouts = new TimeoutSettings();
            _capabilities = new AppiumCapabilities(driver.Capabilities);
        }
        
        // ITestContext implementation
        public TimeoutSettings Timeouts => _timeouts;
        public ITestLogger Logger => _logger;
        
        // IMauiTestContext implementation
        public AppiumDriver Driver => _driver;
        public MauiPlatform Platform => _platform;
        public AppiumCapabilities Capabilities => _capabilities;
        public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
        
        // IMauiElementScope
        public IMauiTestContext Context => this;
        
        // Navigation
        public void NavigateTo(string destination)
        {
            // MAUI Shell navigation
            var url = destination.StartsWith("//") ? destination : $"//{destination}";
            _logger.LogNavigation("NavigateTo", destination);
        }
        
        public void NavigateBack()
        {
            _driver.Navigate().Back();
            _logger.LogNavigation("NavigateBack", "back");
        }
        
        public void Refresh()
        {
            // Not directly supported on mobile - app-specific implementation
            _logger.LogAction("context", null, "context", "Refresh", null);
        }
        
        // Typed element finding - IElementScope<AppiumElement>
        public AppiumElement? TryFindElement(Locator locator)
        {
            try
            {
                var by = ConvertLocatorToBy(locator);
                return _driver.FindElement(by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
        
        public AppiumElement FindElement(Locator locator)
        {
            var by = ConvertLocatorToBy(locator);
            return _driver.FindElement(by);
        }
        
        public IReadOnlyList<AppiumElement> FindElements(Locator locator)
        {
            var by = ConvertLocatorToBy(locator);
            return _driver.FindElements(by).ToList().AsReadOnly();
        }
        
        // Screenshots
        public byte[] TakeScreenshot()
        {
            var screenshot = _driver.GetScreenshot();
            return screenshot.AsByteArray;
        }
        
        public void SaveScreenshot(string path)
        {
            var screenshot = _driver.GetScreenshot();
            screenshot.SaveAsFile(path);
            _logger.LogAction("context", null, "context", "SaveScreenshot", path);
        }
        
        // App state
        public void ResetAppState()
        {
            ResetApp();
        }
        
        // Device interaction
        public void RotateDevice(ScreenOrientation orientation)
        {
            _driver.Orientation = orientation;
        }
        
        public void SetLocation(double latitude, double longitude)
        {
            _driver.Location = new Location(latitude, longitude, 0);
        }
        
        public void ShakeDevice()
        {
            // Platform-specific shake gesture
        }
        
        public void HideKeyboard()
        {
            if (_platform == MauiPlatform.Android)
                _driver.HideKeyboard();
            else if (_platform == MauiPlatform.iOS)
                _driver.HideKeyboard("Done");
        }
        
        public bool IsKeyboardShown()
        {
            return _driver.IsKeyboardShown();
        }
        
        // App lifecycle
        public void BackgroundApp(TimeSpan duration)
        {
            _driver.BackgroundApp(duration);
        }
        
        public void ResetApp()
        {
            _driver.ResetApp();
        }
        
        public void CloseApp()
        {
            _driver.CloseApp();
        }
        
        public void LaunchApp()
        {
            _driver.LaunchApp();
        }
        
        // Locator conversion
        private By ConvertLocatorToBy(Locator locator)
        {
            return locator.Strategy switch
            {
                LocatorStrategy.AutomationId => MobileBy.AccessibilityId(locator.Value),
                LocatorStrategy.Id => MobileBy.Id(locator.Value),
                LocatorStrategy.Name => MobileBy.Name(locator.Value),
                LocatorStrategy.ClassName => By.ClassName(locator.Value),
                LocatorStrategy.XPath => By.XPath(locator.Value),
                LocatorStrategy.Text => MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{locator.Value}\")"),
                _ => throw new NotSupportedException($"Locator strategy {locator.Strategy} not supported")
            };
        }
        
        public void Dispose()
        {
            _driver?.Quit();
        }
    }
}
```

---

## 3. IBlazorTestContext

Platform context for Blazor applications using Selenium. Implements `ITestContext<IWebElement>` for typed element finding.

### Interface Definition

```csharp
namespace Brinell.Blazor
{
    /// <summary>
    /// Blazor element scope - provides typed IWebElement finding.
    /// </summary>
    public interface IBlazorElementScope : IElementScope<IWebElement>
    {
        /// <summary>
        /// Access to the context for driver operations.
        /// </summary>
        IBlazorTestContext Context { get; }
    }
    
    /// <summary>
    /// Blazor test context with typed IWebElement finding.
    /// </summary>
    public interface IBlazorTestContext : ITestContext<IWebElement>, IBlazorElementScope
    {
        // Driver access
        IWebDriver Driver { get; }
        
        // Browser info
        BrowserType Browser { get; }
        
        // Base URL
        string BaseUrl { get; }
        
        // Inherits from ITestContext<IWebElement> / IElementScope<IWebElement>:
        // IWebElement? TryFindElement(Locator locator);
        // IWebElement FindElement(Locator locator);
        // IReadOnlyList<IWebElement> FindElements(Locator locator);
        
        // Override default locator strategy
        new LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.DataTestId;
        
        // JavaScript execution
        T ExecuteScript<T>(string script, params object[] args);
        void ExecuteScript(string script, params object[] args);
        
        // Blazor-specific
        void WaitForBlazorReady(int? timeoutMs = null);
        bool IsBlazorReady();
        
        // Frame handling
        void SwitchToFrame(string frameId);
        void SwitchToDefaultContent();
        
        // Window handling
        void SwitchToWindow(string windowHandle);
        string GetCurrentWindowHandle();
        IReadOnlyList<string> GetWindowHandles();
        void CloseCurrentWindow();
        
        // Cookies
        void SetCookie(string name, string value);
        string? GetCookie(string name);
        void DeleteCookie(string name);
        void DeleteAllCookies();
        
        // Storage
        void SetLocalStorage(string key, string value);
        string? GetLocalStorage(string key);
        void ClearLocalStorage();
        void SetSessionStorage(string key, string value);
        string? GetSessionStorage(string key);
        void ClearSessionStorage();
        
        // IBlazorElementScope - self-reference
        IBlazorTestContext IBlazorElementScope.Context => this;
    }
    
    public enum BrowserType
    {
        Chrome,
        Firefox,
        Edge,
        Safari
    }
}
```

### Implementation

```csharp
namespace Brinell.Blazor
{
    public class BlazorTestContext : IBlazorTestContext
    {
        private readonly IWebDriver _driver;
        private readonly BrowserType _browser;
        private readonly ITestLogger _logger;
        private readonly TimeoutSettings _timeouts;
        private readonly IJavaScriptExecutor _jsExecutor;
        private readonly string _baseUrl;
        
        public BlazorTestContext(IWebDriver driver, BrowserType browser, string baseUrl, ITestLogger? logger = null)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _browser = browser;
            _baseUrl = baseUrl;
            _logger = logger ?? new NullLogger();
            _timeouts = new TimeoutSettings();
            _jsExecutor = (IJavaScriptExecutor)driver;
        }
        
        // ITestContext implementation
        public TimeoutSettings Timeouts => _timeouts;
        public ITestLogger Logger => _logger;
        
        // IBlazorTestContext implementation
        public IWebDriver Driver => _driver;
        public BrowserType Browser => _browser;
        public string BaseUrl => _baseUrl;
        public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.DataTestId;
        
        // IBlazorElementScope
        public IBlazorTestContext Context => this;
        
        // Navigation
        public void NavigateTo(string destination)
        {
            var url = destination.StartsWith("http") ? destination : $"{_baseUrl}{destination}";
            _driver.Navigate().GoToUrl(url);
            WaitForBlazorReady();
            _logger.LogNavigation("NavigateTo", destination);
        }
        
        public void NavigateBack()
        {
            _driver.Navigate().Back();
            WaitForBlazorReady();
            _logger.LogNavigation("NavigateBack", "back");
        }
        
        public void Refresh()
        {
            _driver.Navigate().Refresh();
            WaitForBlazorReady();
            _logger.LogAction("context", null, "context", "Refresh", null);
        }
        
        // Typed element finding - IElementScope<IWebElement>
        public IWebElement? TryFindElement(Locator locator)
        {
            try
            {
                var by = ConvertLocatorToBy(locator);
                return _driver.FindElement(by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
        
        public IWebElement FindElement(Locator locator)
        {
            var by = ConvertLocatorToBy(locator);
            return _driver.FindElement(by);
        }
        
        public IReadOnlyList<IWebElement> FindElements(Locator locator)
        {
            var by = ConvertLocatorToBy(locator);
            return _driver.FindElements(by).ToList().AsReadOnly();
        }
        
        // JavaScript execution
        public T ExecuteScript<T>(string script, params object[] args)
        {
            return (T)_jsExecutor.ExecuteScript(script, args);
        }
        
        public void ExecuteScript(string script, params object[] args)
        {
            _jsExecutor.ExecuteScript(script, args);
        }
        
        // Screenshots
        public byte[] TakeScreenshot()
        {
            var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
            return screenshot.AsByteArray;
        }
        
        public void SaveScreenshot(string path)
        {
            var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
            screenshot.SaveAsFile(path);
        }
        
        // App state
        public void ResetAppState()
        {
            ClearLocalStorage();
            ClearSessionStorage();
            DeleteAllCookies();
            Refresh();
        }
        
        // Blazor-specific
        public void WaitForBlazorReady(int? timeoutMs = null)
        {
            var timeout = timeoutMs ?? _timeouts.PageLoad;
            var wait = new WebDriverWait(_driver, TimeSpan.FromMilliseconds(timeout));
            wait.Until(_ => IsBlazorReady());
        }
        
        public bool IsBlazorReady()
        {
            try
            {
                var result = _jsExecutor.ExecuteScript(
                    "return window.Blazor && document.readyState === 'complete';");
                return result is bool ready && ready;
            }
            catch
            {
                return false;
            }
        }
        
        // Frame handling
        public void SwitchToFrame(string frameId)
        {
            _driver.SwitchTo().Frame(frameId);
        }
        
        public void SwitchToDefaultContent()
        {
            _driver.SwitchTo().DefaultContent();
        }
        
        // Window handling
        public void SwitchToWindow(string windowHandle)
        {
            _driver.SwitchTo().Window(windowHandle);
        }
        
        public string GetCurrentWindowHandle() => _driver.CurrentWindowHandle;
        
        public IReadOnlyList<string> GetWindowHandles()
            => _driver.WindowHandles.ToList().AsReadOnly();
        
        public void CloseCurrentWindow() => _driver.Close();
        
        // Cookies
        public void SetCookie(string name, string value)
            => _driver.Manage().Cookies.AddCookie(new Cookie(name, value));
        
        public string? GetCookie(string name)
            => _driver.Manage().Cookies.GetCookieNamed(name)?.Value;
        
        public void DeleteCookie(string name)
            => _driver.Manage().Cookies.DeleteCookieNamed(name);
        
        public void DeleteAllCookies()
            => _driver.Manage().Cookies.DeleteAllCookies();
        
        // Storage
        public void SetLocalStorage(string key, string value)
            => _jsExecutor.ExecuteScript($"localStorage.setItem('{key}', '{value}');");
        
        public string? GetLocalStorage(string key)
            => _jsExecutor.ExecuteScript($"return localStorage.getItem('{key}');") as string;
        
        public void ClearLocalStorage()
            => _jsExecutor.ExecuteScript("localStorage.clear();");
        
        public void SetSessionStorage(string key, string value)
            => _jsExecutor.ExecuteScript($"sessionStorage.setItem('{key}', '{value}');");
        
        public string? GetSessionStorage(string key)
            => _jsExecutor.ExecuteScript($"return sessionStorage.getItem('{key}');") as string;
        
        public void ClearSessionStorage()
            => _jsExecutor.ExecuteScript("sessionStorage.clear();");
        
        // Locator conversion
        private By ConvertLocatorToBy(Locator locator)
        {
            return locator.Strategy switch
            {
                LocatorStrategy.Css => By.CssSelector(locator.Value),
                LocatorStrategy.Id => By.Id(locator.Value),
                LocatorStrategy.Name => By.Name(locator.Value),
                LocatorStrategy.ClassName => By.ClassName(locator.Value),
                LocatorStrategy.XPath => By.XPath(locator.Value),
                LocatorStrategy.LinkText => By.LinkText(locator.Value),
                LocatorStrategy.PartialLinkText => By.PartialLinkText(locator.Value),
                LocatorStrategy.TagName => By.TagName(locator.Value),
                LocatorStrategy.DataTestId => By.CssSelector($"[data-testid='{locator.Value}']"),
                _ => throw new NotSupportedException($"Locator strategy {locator.Strategy} not supported")
            };
        }
        
        public void Dispose()
        {
            _driver?.Quit();
        }
    }
}
```

---

## 4. IWpfTestContext

Platform context for WPF applications using FlaUI. Implements `ITestContext<AutomationElement>` for typed element finding.

### Interface Definition

```csharp
namespace Brinell.Wpf
{
    /// <summary>
    /// WPF test context with typed AutomationElement finding.
    /// </summary>
    public interface IWpfTestContext : ITestContext<AutomationElement>
    {
        // Application access
        Application Application { get; }
        AutomationBase Automation { get; }
        Window MainWindow { get; }
        
        // Inherits from ITestContext<AutomationElement> / IElementScope<AutomationElement>:
        // AutomationElement? TryFindElement(Locator locator);
        // AutomationElement FindElement(Locator locator);
        // IReadOnlyList<AutomationElement> FindElements(Locator locator);
        
        // Override default locator strategy
        new LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
        
        // Window management
        Window? GetWindow(string title);
        IReadOnlyList<Window> GetAllWindows();
        void FocusWindow(Window window);
        void CloseAllPopups();
        
        // Modal dialog handling
        Window? GetModalDialog(int? timeoutMs = null);
        void DismissModalDialog();
        
        // Input simulation
        IKeyboard Keyboard { get; }
        IMouse Mouse { get; }
    }
}
```

### Implementation

```csharp
namespace Brinell.Wpf
{
    public class WpfTestContext : IWpfTestContext
    {
        private readonly Application _application;
        private readonly AutomationBase _automation;
        private readonly ITestLogger _logger;
        private readonly TimeoutSettings _timeouts;
        private Window? _mainWindow;
        
        public WpfTestContext(Application application, ITestLogger? logger = null)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _automation = new UIA3Automation();
            _logger = logger ?? new NullLogger();
            _timeouts = new TimeoutSettings();
        }
        
        // ITestContext implementation
        public TimeoutSettings Timeouts => _timeouts;
        public ITestLogger Logger => _logger;
        
        // IWpfTestContext implementation
        public Application Application => _application;
        public AutomationBase Automation => _automation;
        
        public Window MainWindow
        {
            get
            {
                if (_mainWindow == null || _mainWindow.IsClosed)
                {
                    _mainWindow = _application.GetMainWindow(_automation, TimeSpan.FromMilliseconds(_timeouts.PageLoad));
                }
                return _mainWindow;
            }
        }
        
        public IKeyboard Keyboard => FlaUI.Core.Input.Keyboard.Instance;
        public IMouse Mouse => FlaUI.Core.Input.Mouse.Instance;
        
        // Navigation (WPF doesn't have URL navigation)
        public void NavigateTo(string destination)
        {
            // Implementation depends on app structure
            // Could trigger navigation through menu or control
            _logger.LogAction("NavigateTo", null, destination);
        }
        
        public void NavigateBack()
        {
            // Implementation depends on app structure
            Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.LEFT);
            _logger.LogAction("NavigateBack", null);
        }
        
        public void Refresh()
        {
            // Implementation depends on app structure
            Keyboard.Type(VirtualKeyShort.F5);
            _logger.LogAction("Refresh", null);
        }
        
        // Element finding
        public AutomationElement? TryFindElement(Locator locator)
        {
            var condition = ConvertLocatorToCondition(locator);
            return MainWindow.FindFirstDescendant(condition);
        }
        
        public AutomationElement FindElement(Locator locator)
        {
            var condition = ConvertLocatorToCondition(locator);
            var result = Retry.WhileNull(
                () => MainWindow.FindFirstDescendant(condition),
                TimeSpan.FromMilliseconds(_timeouts.ElementFind),
                TimeSpan.FromMilliseconds(_timeouts.PollingInterval));
            
            return result.Result ?? throw new ElementNotFoundException($"Element not found: {locator}");
        }
        
        public IReadOnlyList<AutomationElement> FindElements(Locator locator)
        {
            var condition = ConvertLocatorToCondition(locator);
            return MainWindow.FindAllDescendants(condition).ToList().AsReadOnly();
        }
        
        // Screenshots
        public byte[] TakeScreenshot()
        {
            using var bitmap = Capture.MainScreen();
            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream.ToArray();
        }
        
        public void SaveScreenshot(string path)
        {
            using var bitmap = Capture.MainScreen();
            bitmap.Save(path);
            _logger.LogAction("SaveScreenshot", null, path);
        }
        
        // App state
        public void ResetAppState()
        {
            CloseAllPopups();
            // Navigate to initial state if applicable
        }
        
        // Window management
        public Window? GetWindow(string title)
        {
            return _application.GetAllTopLevelWindows(_automation)
                .FirstOrDefault(w => w.Title.Contains(title));
        }
        
        public IReadOnlyList<Window> GetAllWindows()
        {
            return _application.GetAllTopLevelWindows(_automation).ToList().AsReadOnly();
        }
        
        public void FocusWindow(Window window)
        {
            window.Focus();
            _logger.LogAction("FocusWindow", null, window.Title);
        }
        
        public void CloseAllPopups()
        {
            var windows = GetAllWindows();
            foreach (var window in windows.Where(w => w != MainWindow))
            {
                try { window.Close(); }
                catch { /* Ignore if already closed */ }
            }
            _logger.LogAction("CloseAllPopups", null);
        }
        
        // Modal dialog handling
        public Window? GetModalDialog(int? timeoutMs = null)
        {
            var timeout = timeoutMs ?? _timeouts.DefaultWait;
            var result = Retry.WhileNull(
                () => GetAllWindows().FirstOrDefault(w => w.IsModal),
                TimeSpan.FromMilliseconds(timeout),
                TimeSpan.FromMilliseconds(_timeouts.PollingInterval));
            
            return result.Result;
        }
        
        public void DismissModalDialog()
        {
            var dialog = GetModalDialog();
            if (dialog != null)
            {
                // Try to find OK/Close button
                var button = dialog.FindFirstDescendant(cf => 
                    cf.ByControlType(ControlType.Button)
                      .And(cf.ByName("OK").Or(cf.ByName("Close"))));
                
                if (button != null)
                {
                    button.Click();
                }
                else
                {
                    dialog.Close();
                }
            }
            _logger.LogAction("DismissModalDialog", null);
        }
        
        // Locator conversion
        private ConditionFactory ConvertLocatorToCondition(Locator locator)
        {
            var cf = new ConditionFactory(_automation.PropertyLibrary);
            
            return locator.Strategy switch
            {
                LocatorStrategy.AutomationId => cf.ByAutomationId(locator.Value),
                LocatorStrategy.Name => cf.ByName(locator.Value),
                LocatorStrategy.ClassName => cf.ByClassName(locator.Value),
                LocatorStrategy.ControlType => cf.ByControlType(ParseControlType(locator.Value)),
                _ => throw new NotSupportedException($"Locator strategy {locator.Strategy} not supported")
            };
        }
        
        private ControlType ParseControlType(string value)
        {
            return Enum.Parse<ControlType>(value, ignoreCase: true);
        }
        
        public void Dispose()
        {
            _application?.Close();
            _automation?.Dispose();
        }
    }
}
```

---

## 5. Platform Context Comparison

| Feature | MAUI | Blazor | WPF |
|---------|------|--------|-----|
| Generic Interface | `ITestContext<AppiumElement>` | `ITestContext<IWebElement>` | `ITestContext<AutomationElement>` |
| Element Scope | `IMauiElementScope` | `IBlazorElementScope` | N/A |
| Driver/App | AppiumDriver | IWebDriver | Application |
| Element Type | AppiumElement | IWebElement | AutomationElement |
| Navigation | Shell routes | URLs | Custom |
| Default Locator | AutomationId | DataTestId | AutomationId |
| Screenshots | Driver API | ITakesScreenshot | Capture class |
| Keyboard | HideKeyboard | Keys class | Keyboard class |
| Storage | N/A | LocalStorage/SessionStorage | N/A |
| Cookies | N/A | Cookie API | N/A |
| Windows | N/A | Window handles | Window class |
| JavaScript | N/A | IJavaScriptExecutor | N/A |
| Modals | Alert API | Alert API | Modal windows |
| Device Features | Location, Rotation | N/A | N/A |

---

## 6. Locator Strategy Support

| Strategy | MAUI | Blazor | WPF |
|----------|------|--------|-----|
| AutomationId | ✅ AccessibilityId | ❌ | ✅ |
| Id | ✅ | ✅ | ❌ |
| Name | ✅ | ✅ | ✅ |
| ClassName | ✅ | ✅ | ✅ |
| Css | ❌ | ✅ | ❌ |
| XPath | ✅ | ✅ | ❌ |
| Text | ✅ (Android) | ❌ | ❌ |
| LinkText | ❌ | ✅ | ❌ |
| DataTestId | ❌ | ✅ | ❌ |
| ControlType | ❌ | ❌ | ✅ |

---

## 7. Acceptance Criteria

### 7.1 Context Initialization

```gherkin
Scenario: MAUI context initializes with driver
  Given an Appium driver instance
  When MauiTestContext is created
  Then Driver property returns the driver
  And Platform property is set correctly
  And Timeouts uses default settings

Scenario: Blazor context initializes with driver
  Given a Selenium WebDriver instance
  When BlazorTestContext is created
  Then Driver property returns the driver
  And JavaScript execution is available
  And Timeouts uses default settings

Scenario: WPF context initializes with application
  Given a FlaUI Application instance
  When WpfTestContext is created
  Then Application property returns the app
  And MainWindow is accessible
  And Timeouts uses default settings
```

### 7.2 Element Finding

```gherkin
Scenario: TryFindElement returns null for missing element
  Given any platform context
  When TryFindElement is called with non-existent locator
  Then null is returned
  And no exception is thrown

Scenario: FindElement throws for missing element
  Given any platform context
  When FindElement is called with non-existent locator
  Then ElementNotFoundException is thrown
```

### 7.3 Screenshots

```gherkin
Scenario: TakeScreenshot returns image data
  Given any platform context
  When TakeScreenshot is called
  Then byte array with image data is returned

Scenario: SaveScreenshot writes to file
  Given any platform context
  When SaveScreenshot is called with path
  Then screenshot is saved to that path
```

---

## 8. Validation Checklist

- [ ] All platform contexts implement `ITestContext<TElement>` with their element type
- [ ] Platform element scope interfaces defined (`IMauiElementScope`, `IBlazorElementScope`)
- [ ] Element finding methods return typed elements (no casting)
- [ ] Driver/Application access provided via typed property
- [ ] `DefaultLocatorStrategy` overridden for each platform
- [ ] Screenshot capabilities available
- [ ] Navigation methods functional
- [ ] Logging integrated
- [ ] Timeout settings respected
- [ ] Dispose properly cleans up resources
- [ ] Context provides self-reference via element scope interface

---

## Related Documents

- [ITestContext Base](250_004_TestContext.spx.md)
- [MAUI Base Classes](250_006_MauiBaseClasses.spx.md)
- [Blazor Base Classes](250_007_BlazorBaseClasses.spx.md)
- [WPF Base Classes](250_008_WpfBaseClasses.spx.md)
