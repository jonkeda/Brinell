# 231_003 Adapter Pattern

## pattern Adapter

- **title**: Adapter Pattern (Driver & Element Abstraction)
- **type**: Structural
- **purpose**: Abstract automation driver and element details behind platform-neutral interfaces

---

## Description

The Adapter pattern provides a layer of abstraction between the Brinell framework and the underlying automation drivers (Appium, Selenium, Playwright, FlaUI). This enables platform-specific implementations while maintaining consistent APIs for test code.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Direct coupling to automation drivers causes:
- Vendor lock-in to specific driver implementations
- Inability to switch between drivers (e.g., Selenium to Playwright)
- Platform-specific code scattered throughout tests
- Difficult testing of framework components

**Solution:** Create adapter interfaces that:
- Define platform-neutral contracts for element finding
- Abstract driver-specific element types via IElementAdapter
- Provide consistent timeout and waiting behavior
- Enable mock implementations for testing

---

## 2. Structure

### 2.1 Participants

| Participant | Role |
|-------------|------|
| ITestContext | Platform-neutral test context interface |
| IElementAdapter | Platform-neutral element wrapper |
| IMauiTestContext | MAUI-specific context extending ITestContext |
| IBlazorTestContext | Blazor-specific context extending ITestContext |
| IWpfTestContext | WPF-specific context extending ITestContext |
| IMauiElementAdapter | MAUI-specific element adapter extending IElementAdapter |
| IBlazorElementAdapter | Blazor-specific element adapter extending IElementAdapter |
| IWpfElementAdapter | WPF-specific element adapter extending IElementAdapter |
| AppiumElementAdapter | Concrete adapter wrapping AppiumElement |
| SeleniumElementAdapter | Concrete adapter wrapping IWebElement |
| PlaywrightElementAdapter | Concrete adapter wrapping ILocator |
| FlaUIElementAdapter | Concrete adapter wrapping AutomationElement |

### 2.2 Context Adapter Hierarchy

```
                     ITestContext
                          │
          ┌───────────────┼───────────────┐
          │               │               │
    IMauiTestContext  IBlazorTestContext  IWpfTestContext
          │               │               │
    MauiTestContext   BlazorTestContext   WpfTestContext
          │               │               │
          ▼               ▼               ▼
       Appium         Selenium          FlaUI
                    (or Playwright)
```

### 2.3 Element Adapter Hierarchy

```
                       IElementAdapter
                             │
          ┌──────────────────┼──────────────────┐
          │                  │                  │
  IMauiElementAdapter  IBlazorElementAdapter  IWpfElementAdapter
          │                  │                  │
  AppiumElementAdapter SeleniumElementAdapter FlaUIElementAdapter
          │                  │                  │
          ▼                  ▼                  ▼
     AppiumElement      IWebElement      AutomationElement
```

---

## 3. Implementation

### 3.1 Element Adapter Interface

```csharp
/// <summary>
/// Platform-neutral wrapper for automation elements.
/// Provides common operations across all platforms.
/// </summary>
public interface IElementAdapter
{
    /// <summary>
    /// Click the element.
    /// </summary>
    void Click();
    
    /// <summary>
    /// Double-click the element.
    /// </summary>
    void DoubleClick();
    
    /// <summary>
    /// Right-click the element.
    /// </summary>
    void RightClick();
    
    /// <summary>
    /// Get the text content of the element.
    /// </summary>
    string? GetText();
    
    /// <summary>
    /// Enter text into the element.
    /// </summary>
    void SendKeys(string text);
    
    /// <summary>
    /// Clear the element's text content.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Get an attribute value from the element.
    /// </summary>
    string? GetAttribute(string name);
    
    /// <summary>
    /// Check if the element is displayed/visible.
    /// </summary>
    bool IsDisplayed();
    
    /// <summary>
    /// Check if the element is enabled.
    /// </summary>
    bool IsEnabled();
    
    /// <summary>
    /// Check if the element is selected/checked.
    /// </summary>
    bool IsSelected();
    
    /// <summary>
    /// Find child element within this element.
    /// </summary>
    IElementAdapter? FindChild(Locator locator);
    
    /// <summary>
    /// Find all child elements within this element.
    /// </summary>
    IReadOnlyList<IElementAdapter> FindChildren(Locator locator);
}
```

### 3.2 Platform-Specific Element Interfaces

```csharp
/// <summary>
/// MAUI-specific element adapter - provides mobile/desktop app operations.
/// </summary>
public interface IMauiElementAdapter : IElementAdapter
{
    /// <summary>
    /// Access to underlying Appium element for advanced operations.
    /// </summary>
    AppiumElement Element { get; }
    
    /// <summary>
    /// Scroll the element into view.
    /// </summary>
    void ScrollIntoView();
    
    /// <summary>
    /// Long press/tap on the element.
    /// </summary>
    void LongPress(int durationMs = 1000);
    
    /// <summary>
    /// Swipe from this element in a direction.
    /// </summary>
    void Swipe(SwipeDirection direction, int distance = 100);
    
    /// <summary>
    /// Get the element's bounds/location.
    /// </summary>
    Rectangle GetBounds();
}

/// <summary>
/// Blazor/Web-specific element adapter - provides web operations.
/// </summary>
public interface IBlazorElementAdapter : IElementAdapter
{
    /// <summary>
    /// Access to underlying WebElement for advanced operations.
    /// </summary>
    IWebElement Element { get; }
    
    /// <summary>
    /// Scroll the element into view.
    /// </summary>
    void ScrollIntoView();
    
    /// <summary>
    /// Execute JavaScript on this element.
    /// </summary>
    T ExecuteScript<T>(string script);
    
    /// <summary>
    /// Get a CSS property value.
    /// </summary>
    string? GetCssValue(string propertyName);
    
    /// <summary>
    /// Get the element's tag name.
    /// </summary>
    string TagName { get; }
    
    /// <summary>
    /// Hover over the element.
    /// </summary>
    void Hover();
    
    /// <summary>
    /// Drag this element to another element.
    /// </summary>
    void DragTo(IBlazorElementAdapter target);
}

/// <summary>
/// WPF-specific element adapter - provides desktop automation operations.
/// </summary>
public interface IWpfElementAdapter : IElementAdapter
{
    /// <summary>
    /// Access to underlying FlaUI element for advanced operations.
    /// </summary>
    AutomationElement Element { get; }
    
    /// <summary>
    /// Get the control type of this element.
    /// </summary>
    ControlType ControlType { get; }
    
    /// <summary>
    /// Get the element as a specific FlaUI pattern.
    /// </summary>
    T? AsPattern<T>() where T : class;
    
    /// <summary>
    /// Get automation properties.
    /// </summary>
    string? GetAutomationProperty(string propertyName);
    
    /// <summary>
    /// Focus this element.
    /// </summary>
    void Focus();
    
    /// <summary>
    /// Expand (for tree items, combo boxes).
    /// </summary>
    void Expand();
    
    /// <summary>
    /// Collapse (for tree items, combo boxes).
    /// </summary>
    void Collapse();
}
```

### 3.3 Platform Element Adapter Implementations

**Appium Element Adapter:**

```csharp
public class AppiumElementAdapter : IMauiElementAdapter
{
    private readonly AppiumElement _element;
    
    public AppiumElement Element => _element;
    
    public AppiumElementAdapter(AppiumElement element)
    {
        _element = element ?? throw new ArgumentNullException(nameof(element));
    }
    
    // IElementAdapter implementation
    public void Click() => _element.Click();
    public void DoubleClick() 
    {
        var actions = new Actions(_element.WrappedDriver);
        actions.DoubleClick(_element).Perform();
    }
    public void RightClick()
    {
        var actions = new Actions(_element.WrappedDriver);
        actions.ContextClick(_element).Perform();
    }
    
    public string? GetText() => _element.Text;
    public void SendKeys(string text) => _element.SendKeys(text);
    public void Clear() => _element.Clear();
    
    public string? GetAttribute(string name) => _element.GetAttribute(name);
    public bool IsDisplayed() => _element.Displayed;
    public bool IsEnabled() => _element.Enabled;
    public bool IsSelected() => _element.Selected;
    
    public IElementAdapter? FindChild(Locator locator)
    {
        try
        {
            var child = locator.Strategy switch
            {
                LocatorStrategy.AutomationId => _element.FindElement(MobileBy.AccessibilityId(locator.Value)),
                LocatorStrategy.XPath => _element.FindElement(By.XPath(locator.Value)),
                _ => throw new NotSupportedException()
            };
            return new AppiumElementAdapter(child);
        }
        catch (NoSuchElementException) { return null; }
    }
    
    public IReadOnlyList<IElementAdapter> FindChildren(Locator locator)
    {
        var by = locator.Strategy switch
        {
            LocatorStrategy.AutomationId => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.XPath => By.XPath(locator.Value),
            _ => throw new NotSupportedException()
        };
        return _element.FindElements(by)
            .Select(e => new AppiumElementAdapter(e))
            .ToList();
    }
    
    // IMauiElementAdapter implementation
    public void ScrollIntoView()
    {
        // Platform-specific scroll implementation
        var driver = _element.WrappedDriver;
        // Use mobile gestures to scroll element into view
    }
    
    public void LongPress(int durationMs = 1000)
    {
        var actions = new Actions(_element.WrappedDriver);
        actions.ClickAndHold(_element)
            .Pause(TimeSpan.FromMilliseconds(durationMs))
            .Release()
            .Perform();
    }
    
    public void Swipe(SwipeDirection direction, int distance = 100)
    {
        // Platform-specific swipe implementation
    }
    
    public Rectangle GetBounds()
    {
        return new Rectangle(_element.Location, _element.Size);
    }
}
```

**Selenium Element Adapter:**

```csharp
public class SeleniumElementAdapter : IBlazorElementAdapter
{
    private readonly IWebElement _element;
    private readonly IWebDriver _driver;
    
    public IWebElement Element => _element;
    public string TagName => _element.TagName;
    
    public SeleniumElementAdapter(IWebElement element, IWebDriver driver)
    {
        _element = element;
        _driver = driver;
    }
    
    // IElementAdapter implementation
    public void Click() => _element.Click();
    public void DoubleClick()
    {
        new Actions(_driver).DoubleClick(_element).Perform();
    }
    public void RightClick()
    {
        new Actions(_driver).ContextClick(_element).Perform();
    }
    
    public string? GetText() => _element.Text;
    public void SendKeys(string text) => _element.SendKeys(text);
    public void Clear() => _element.Clear();
    
    public string? GetAttribute(string name) => _element.GetAttribute(name);
    public bool IsDisplayed() => _element.Displayed;
    public bool IsEnabled() => _element.Enabled;
    public bool IsSelected() => _element.Selected;
    
    public IElementAdapter? FindChild(Locator locator)
    {
        try
        {
            var by = locator.Strategy switch
            {
                LocatorStrategy.CssSelector => By.CssSelector(locator.Value),
                LocatorStrategy.XPath => By.XPath(locator.Value),
                LocatorStrategy.Id => By.Id(locator.Value),
                _ => throw new NotSupportedException()
            };
            return new SeleniumElementAdapter(_element.FindElement(by), _driver);
        }
        catch (NoSuchElementException) { return null; }
    }
    
    public IReadOnlyList<IElementAdapter> FindChildren(Locator locator)
    {
        var by = locator.Strategy switch
        {
            LocatorStrategy.CssSelector => By.CssSelector(locator.Value),
            LocatorStrategy.XPath => By.XPath(locator.Value),
            _ => throw new NotSupportedException()
        };
        return _element.FindElements(by)
            .Select(e => new SeleniumElementAdapter(e, _driver))
            .ToList();
    }
    
    // IBlazorElementAdapter implementation
    public void ScrollIntoView()
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", _element);
    }
    
    public T ExecuteScript<T>(string script)
    {
        return (T)((IJavaScriptExecutor)_driver).ExecuteScript(script, _element);
    }
    
    public string? GetCssValue(string propertyName)
    {
        return _element.GetCssValue(propertyName);
    }
    
    public void Hover()
    {
        new Actions(_driver).MoveToElement(_element).Perform();
    }
    
    public void DragTo(IBlazorElementAdapter target)
    {
        if (target is SeleniumElementAdapter seleniumTarget)
        {
            new Actions(_driver)
                .DragAndDrop(_element, seleniumTarget._element)
                .Perform();
        }
    }
}
```

**FlaUI Element Adapter:**

```csharp
public class FlaUIElementAdapter : IWpfElementAdapter
{
    private readonly AutomationElement _element;
    
    public AutomationElement Element => _element;
    public ControlType ControlType => _element.ControlType;
    
    public FlaUIElementAdapter(AutomationElement element)
    {
        _element = element;
    }
    
    // IElementAdapter implementation
    public void Click() => _element.Click();
    public void DoubleClick() => _element.DoubleClick();
    public void RightClick() => _element.RightClick();
    
    public string? GetText() => _element.AsTextBox()?.Text ?? _element.Name;
    public void SendKeys(string text) => _element.AsTextBox().Enter(text);
    public void Clear() => _element.AsTextBox().Text = string.Empty;
    
    public string? GetAttribute(string name) => _element.Properties.GetPropertyValue<string>(name);
    public bool IsDisplayed() => !_element.IsOffscreen;
    public bool IsEnabled() => _element.IsEnabled;
    public bool IsSelected() => _element.AsCheckBox()?.IsChecked ?? false;
    
    public IElementAdapter? FindChild(Locator locator)
    {
        var child = _element.FindFirstDescendant(cf => 
            cf.ByAutomationId(locator.Value));
        return child != null ? new FlaUIElementAdapter(child) : null;
    }
    
    public IReadOnlyList<IElementAdapter> FindChildren(Locator locator)
    {
        return _element.FindAllDescendants(cf => cf.ByAutomationId(locator.Value))
            .Select(e => new FlaUIElementAdapter(e))
            .ToList();
    }
    
    // IWpfElementAdapter implementation
    public T? AsPattern<T>() where T : class
    {
        // Return FlaUI pattern wrappers
        return typeof(T).Name switch
        {
            nameof(ITextBox) => _element.AsTextBox() as T,
            nameof(IButton) => _element.AsButton() as T,
            nameof(ICheckBox) => _element.AsCheckBox() as T,
            nameof(IComboBox) => _element.AsComboBox() as T,
            _ => null
        };
    }
    
    public string? GetAutomationProperty(string propertyName)
    {
        return _element.Properties.GetPropertyValue<string>(propertyName);
    }
    
    public void Focus()
    {
        _element.Focus();
    }
    
    public void Expand()
    {
        _element.AsComboBox()?.Expand();
        // Or for tree items: _element.AsTreeItem()?.Expand();
    }
    
    public void Collapse()
    {
        _element.AsComboBox()?.Collapse();
    }
}
```

### 3.4 Core Context Interface

```csharp
public interface ITestContext : IDisposable
{
    /// <summary>
    /// Default timeout for element operations in milliseconds.
    /// </summary>
    int DefaultTimeoutMs { get; }
    
    /// <summary>
    /// Configuration settings for the test context.
    /// </summary>
    UITestConfiguration Configuration { get; }
    
    /// <summary>
    /// Logger for test operations.
    /// </summary>
    ITestLogger? Logger { get; }
    
    /// <summary>
    /// Current test name for logging.
    /// </summary>
    string? TestName { get; set; }
    
    /// <summary>
    /// Find element by locator. Returns null if not found.
    /// </summary>
    IElementAdapter? FindElement(Locator locator);
    
    /// <summary>
    /// Find all elements matching locator.
    /// </summary>
    IReadOnlyList<IElementAdapter> FindElements(Locator locator);
    
    /// <summary>
    /// Find element within a container scope.
    /// </summary>
    IElementAdapter? FindElement(Locator locator, IElementAdapter container);
    
    /// <summary>
    /// Capture screenshot of current state.
    /// </summary>
    byte[] TakeScreenshot();
    
    /// <summary>
    /// Wait for a condition to be true.
    /// </summary>
    bool WaitFor(Func<bool> condition, int timeoutMs, string? description = null);
}
```

### 3.4 Platform-Specific Context Interfaces

```csharp
/// <summary>
/// MAUI test context - wraps Appium driver.
/// </summary>
public interface IMauiTestContext : ITestContext
{
    /// <summary>
    /// Access to underlying Appium driver for advanced operations.
    /// Prefer using IElementAdapter methods over direct driver access.
    /// </summary>
    AppiumDriver Driver { get; }
    
    /// <summary>
    /// Timeout settings for various operations.
    /// </summary>
    TimeoutSettings Timeouts { get; }
    
    /// <summary>
    /// Navigate back in the app.
    /// </summary>
    void NavigateBack();
}

/// <summary>
/// Blazor test context - wraps Selenium WebDriver.
/// </summary>
public interface IBlazorTestContext : ITestContext
{
    /// <summary>
    /// Access to underlying WebDriver for advanced operations.
    /// Prefer using IElementAdapter methods over direct driver access.
    /// </summary>
    IWebDriver Driver { get; }
    
    /// <summary>
    /// Base URL for the web application.
    /// </summary>
    string BaseUrl { get; }
    
    /// <summary>
    /// Navigate to a URL path.
    /// </summary>
    void NavigateTo(string path);
    
    /// <summary>
    /// Navigate back in the browser.
    /// </summary>
    void NavigateBack();
}

/// <summary>
/// WPF test context - wraps FlaUI automation.
/// </summary>
public interface IWpfTestContext : ITestContext
{
    /// <summary>
    /// Access to underlying FlaUI application.
    /// </summary>
    Application Application { get; }
    
    /// <summary>
    /// Main window of the application.
    /// </summary>
    Window MainWindow { get; }
}
```

---

## 4. Test Base Classes

**Tests should use platform-specific base classes, NOT generic ITestContext.**

### 4.1 Platform Test Base Classes

```csharp
/// <summary>
/// Base class for MAUI UI tests.
/// </summary>
public abstract class MauiTestBase : IDisposable
{
    protected readonly IMauiTestContext Context;
    
    protected MauiTestBase()
    {
        Context = CreateContext();
    }
    
    protected virtual IMauiTestContext CreateContext()
    {
        var options = CreateAppiumOptions();
        return new MauiTestContext(options);
    }
    
    protected abstract AppiumOptions CreateAppiumOptions();
    
    public virtual void Dispose() => Context?.Dispose();
}

/// <summary>
/// Base class for Blazor UI tests.
/// </summary>
public abstract class BlazorTestBase : IDisposable
{
    protected readonly IBlazorTestContext Context;
    
    protected BlazorTestBase()
    {
        Context = CreateContext();
    }
    
    protected virtual IBlazorTestContext CreateContext()
    {
        var driver = CreateWebDriver();
        return new BlazorTestContext(driver, BaseUrl);
    }
    
    protected abstract IWebDriver CreateWebDriver();
    protected abstract string BaseUrl { get; }
    
    public virtual void Dispose() => Context?.Dispose();
}

/// <summary>
/// Base class for WPF UI tests.
/// </summary>
public abstract class WpfTestBase : IDisposable
{
    protected readonly IWpfTestContext Context;
    
    protected WpfTestBase()
    {
        Context = CreateContext();
    }
    
    protected virtual IWpfTestContext CreateContext()
    {
        var app = Application.Launch(AppPath);
        return new WpfTestContext(app);
    }
    
    protected abstract string AppPath { get; }
    
    public virtual void Dispose() => Context?.Dispose();
}
```

### 4.2 Test Class Examples

**MAUI Test:**

```csharp
public class LoginTests : MauiTestBase
{
    protected override AppiumOptions CreateAppiumOptions()
    {
        var options = new AppiumOptions();
        options.AddAdditionalCapability("app", "com.company.myapp");
        options.AddAdditionalCapability("platformName", "Android");
        return options;
    }
    
    [Fact]
    public void Login_ValidCredentials_Succeeds()
    {
        var loginPage = new LoginPage(Context);  // Uses IMauiTestContext directly
        loginPage.UsernameEntry.Enter("user");
        loginPage.PasswordEntry.Enter("pass");
        loginPage.LoginButton.Click();
        
        var homePage = new HomePage(Context);
        homePage.WelcomeLabel.AssertTextContains("Welcome");
    }
    
    [Fact]
    public void Login_ThenBack_ReturnsToLogin()
    {
        var loginPage = new LoginPage(Context);
        loginPage.LoginButton.Click();
        
        // Platform-specific navigation is available directly
        Context.NavigateBack();
        
        loginPage.UsernameEntry.AssertExists();
    }
}
```

**Blazor Test:**

```csharp
public class DashboardTests : BlazorTestBase
{
    protected override string BaseUrl => "https://localhost:5001";
    
    protected override IWebDriver CreateWebDriver()
    {
        return new ChromeDriver(new ChromeOptions());
    }
    
    [Fact]
    public void Dashboard_NavigateToSettings_ShowsSettings()
    {
        // Platform-specific navigation available directly
        Context.NavigateTo("/dashboard");
        
        var dashboard = new DashboardPage(Context);  // Uses IBlazorTestContext
        dashboard.SettingsLink.Click();
        
        var settings = new SettingsPage(Context);
        settings.WaitForPage();
        settings.TitleLabel.AssertTextEquals("Settings");
    }
}
```

---

## 5. Locator Mapping

Each adapter maps framework locators to driver-specific selectors:

### 5.1 Locator Strategy

```csharp
public enum LocatorStrategy
{
    AutomationId,    // Platform accessibility ID
    Name,            // Element name/label
    XPath,           // XPath expression
    CssSelector,     // CSS selector (web only)
    ClassName,       // Element class name
    Id,              // HTML id attribute (web only)
    LinkText,        // Link text (web only)
    TagName          // HTML tag name (web only)
}
```

### 5.2 Platform Mapping

| LocatorStrategy | Appium (MAUI) | Selenium (Blazor) | FlaUI (WPF) |
|-----------------|---------------|-------------------|-------------|
| AutomationId | AccessibilityId | data-testid or [automation-id] | AutomationId |
| Name | By.Name | By.Name | By.Name |
| XPath | By.XPath | By.XPath | XPath condition |
| CssSelector | N/A | By.CssSelector | N/A |
| ClassName | By.ClassName | By.ClassName | By.ClassName |
| Id | N/A | By.Id | N/A |

---

## 6. Benefits

### 6.1 Testability

Framework components can be tested with mock contexts:

```csharp
public class MockElementAdapter : IElementAdapter
{
    public bool WasClicked { get; private set; }
    public string? TextToReturn { get; set; }
    
    public void Click() => WasClicked = true;
    public string? GetText() => TextToReturn;
    // ... other members
}

public class MockTestContext : ITestContext
{
    public Dictionary<string, MockElementAdapter> Elements { get; } = new();
    
    public IElementAdapter? FindElement(Locator locator)
    {
        return Elements.TryGetValue(locator.Value, out var element) ? element : null;
    }
    // ...
}

[Fact]
public void ButtonControl_Click_FindsAndClicksElement()
{
    var mockContext = new MockTestContext();
    var mockElement = new MockElementAdapter();
    mockContext.Elements["SubmitButton"] = mockElement;
    
    var button = new ButtonControl(mockContext, "SubmitButton");
    button.Click();
    
    Assert.True(mockElement.WasClicked);
}
```

### 6.2 Driver Flexibility

Switch drivers without changing test code:

```csharp
// Same tests work with different drivers
IBlazorTestContext context = usePlaywright 
    ? new PlaywrightTestContext(browser) 
    : new SeleniumTestContext(driver);
```

---

## 7. Anti-Patterns

### 7.1 Don't Use Generic ITestContext in Tests

```csharp
// ❌ BAD: Generic context requires casting
public class LoginTests
{
    private readonly ITestContext _context;
    
    [Fact]
    public void Test()
    {
        if (_context is IBlazorTestContext blazor)
            blazor.NavigateTo("/login");  // Casting needed!
    }
}

// ✅ GOOD: Use platform-specific base class
public class LoginTests : BlazorTestBase
{
    [Fact]
    public void Test()
    {
        Context.NavigateTo("/login");  // Direct access, no casting
    }
}
```

### 7.2 Don't Leak Driver Types

```csharp
// ❌ BAD: Driver type in public API
public AppiumElement GetRawElement() => _driver.FindElement(...);

// ✅ GOOD: Return adapter types only
public IElementAdapter? FindElement(Locator locator) => ...;
```

### 7.3 Don't Bypass Adapter

```csharp
// ❌ BAD: Direct driver access in control
var element = ((IMauiTestContext)_context).Driver.FindElement(By.Id("x"));

// ✅ GOOD: Use adapter methods
var element = _context.FindElement(new Locator(LocatorStrategy.AutomationId, "x"));
element?.Click();
```

### 7.4 Don't Mix Platform Code in Controls

```csharp
// ❌ BAD: Platform check in control
public void Click()
{
    if (_element is AppiumElementAdapter appium)
        // Appium-specific code
    else if (_element is SeleniumElementAdapter selenium)
        // Selenium-specific code
}

// ✅ GOOD: Use adapter interface
public void Click()
{
    _element.Click();  // IElementAdapter handles differences
}
```

---

## 8. Validation Rules

The Adapter pattern is valid when:

- [ ] ITestContext defines platform-neutral API
- [ ] IElementAdapter abstracts all element operations
- [ ] Platform interfaces extend ITestContext
- [ ] Concrete adapters wrap specific drivers
- [ ] Tests use platform-specific base classes (not generic ITestContext)
- [ ] Driver and element types are not exposed in public APIs
- [ ] Context can be mocked for unit testing
- [ ] Platform-specific code stays in adapters

---

## Related Documents

- [220 External](../220_External/220_INDEX.md) - External driver dependencies
- [211_004 PageContext](../211_Modules/211_004_PageContext.spx.md)
- [FR-103 Interface Hierarchy](../../100_requirements/120_functional/120_103_InterfaceHierarchy.spx.md)
