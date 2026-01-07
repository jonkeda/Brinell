# 203.003 Technology Layer

**Block Type:** LYR (Layer)  
**ID:** 203.003  
**Title:** Technology Layer Definition  
**Status:** Draft  
**Version:** 1.0

---

## 1. Overview

The Technology layer defines how external automation technologies (Appium, Selenium, Playwright) are integrated into the Brinell framework. This layer sits at the **boundary** between Brinell and the underlying automation drivers.

### Layer Identity

- **Technologies:** Appium, Selenium, Playwright, WinAppDriver
- **Integration Points:** TestContext, ElementFinder, DriverAdapter
- **Principle:** Isolation — automation libraries are wrapped, not exposed

---

## 2. Purpose

The Technology layer provides:

1. **Driver Abstraction** — Unified interface over different automation libraries
2. **Technology Isolation** — Changes in automation libraries don't affect test code
3. **Configuration Management** — Technology-specific settings and capabilities
4. **Connection Lifecycle** — Driver initialization, connection, and cleanup

---

## 3. Supported Technologies

### 3.1 Appium (MAUI, WPF)

**Use Case:** Native mobile and desktop applications

| Platform | Appium Driver |
|----------|---------------|
| Android | UiAutomator2 |
| iOS | XCUITest |
| Windows | WinAppDriver |
| Mac | Mac2 |

**Package:** `Appium.WebDriver`

**Integration:**
```
Brinell.MAUI
└── Uses Appium.WebDriver
    └── Connects to Appium Server
        └── Controls device/emulator
```

### 3.2 Selenium (Blazor)

**Use Case:** Web applications including Blazor

| Browser | Driver |
|---------|--------|
| Chrome | ChromeDriver |
| Firefox | GeckoDriver |
| Edge | EdgeDriver |
| Safari | SafariDriver |

**Package:** `Selenium.WebDriver`

**Integration:**
```
Brinell.Blazor
└── Uses Selenium.WebDriver
    └── Connects to Browser Driver
        └── Controls browser
```

### 3.3 Playwright (Blazor Alternative)

**Use Case:** Modern web applications with better async support

**Package:** `Microsoft.Playwright`

**Integration:**
```
Brinell.Blazor.Playwright
└── Uses Microsoft.Playwright
    └── Controls browser directly
```

**Note:** Playwright uses async/await natively. See [ADR-005 Async Support](../202_Decisions/202_005_AsyncSupport.spx.md).

---

## 4. Integration Architecture

### 4.1 TestContext Pattern

Each technology has a TestContext that manages the driver and element finding:

```csharp
// MAUI with Appium
public class AppiumTestContext : ITestContext
{
    private readonly AppiumDriver _driver;
    
    public AppiumTestContext(AppiumOptions options)
    {
        _driver = new AppiumDriver(new Uri(serverUrl), options);
    }
    
    internal AppiumElement FindElement(Locator locator)
    {
        // Locator translates to technology-specific lookup
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => _driver.FindElement(MobileBy.AccessibilityId(locator.Value)),
            LocatorStrategy.XPath => _driver.FindElement(MobileBy.XPath(locator.Value)),
            LocatorStrategy.Class => _driver.FindElement(MobileBy.ClassName(locator.Value)),
            _ => throw new NotSupportedException($"Locator strategy {locator.Strategy} not supported")
        };
    }
}
```

```csharp
// Blazor with Selenium
public class SeleniumTestContext : ITestContext
{
    private readonly IWebDriver _driver;
    
    public SeleniumTestContext(DriverOptions options)
    {
        _driver = new ChromeDriver(options);
    }
    
    internal IWebElement FindElement(Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.TestId => _driver.FindElement(By.CssSelector($"[data-testid='{locator.Value}']")),
            LocatorStrategy.CssSelector => _driver.FindElement(By.CssSelector(locator.Value)),
            LocatorStrategy.XPath => _driver.FindElement(By.XPath(locator.Value)),
            LocatorStrategy.Id => _driver.FindElement(By.Id(locator.Value)),
            _ => throw new NotSupportedException($"Locator strategy {locator.Strategy} not supported")
        };
    }
}
```

### 4.2 Element Abstraction

Controls use Locators to find elements. The Locator is defined in Core and translated to technology-specific lookups:

```csharp
public abstract class ControlBase : IControlObject
{
    protected readonly ITestContext _context;
    protected readonly Locator _locator;
    
    // Public API uses primitives
    public bool IsExists()
    {
        return TryFindElement() != null;
    }
    
    // Internal uses technology-specific types via Locator
    internal abstract object FindElementInternal();
}
```

### 4.3 Locator Strategy

Each technology supports multiple locator strategies. The **default strategy** is used when creating controls with just a string identifier:

| Technology | Default Strategy | Default Attribute | Other Strategies |
|------------|------------------|-------------------|------------------|
| Appium (MAUI) | AutomationId | AutomationId | XPath, ClassName |
| Appium (WPF) | AutomationId | AutomationProperties.AutomationId | XPath, ClassName |
| Selenium | TestId | data-testid | CssSelector, XPath, Id |
| Playwright | TestId | data-testid | Role, Text, Label |

---

## 5. Configuration

### 5.1 Appium Configuration

```json
{
  "appium": {
    "serverUrl": "http://localhost:4723",
    "capabilities": {
      "platformName": "Android",
      "automationName": "UiAutomator2",
      "app": "/path/to/app.apk",
      "deviceName": "emulator-5554"
    }
  }
}
```

### 5.2 Selenium Configuration

```json
{
  "selenium": {
    "browser": "chrome",
    "headless": false,
    "baseUrl": "http://localhost:5000",
    "implicitWait": 10
  }
}
```

### 5.3 Playwright Configuration

```json
{
  "playwright": {
    "browser": "chromium",
    "headless": true,
    "baseUrl": "http://localhost:5000",
    "timeout": 30000
  }
}
```

---

## 6. Lifecycle Management

The driver and application lifecycle is controlled by **run settings**. Two modes are supported:

### 6.1 Session-Per-Run Mode (Default)

Driver and application stay open for the entire test run. Faster execution, shared state.

```
Test Run Start
├── Create TestContext with configuration
├── Initialize driver connection
├── Launch application
└── Ready for test execution

Test 1 → Test 2 → Test 3 → ...  (app stays open)

Test Run End (or crash)
├── Capture screenshot (on failure)
├── Close application
├── Quit driver
└── Dispose TestContext
```

**Crash Recovery:** If the application crashes, a new session is started automatically.

### 6.2 Session-Per-Test Mode

New driver and application for each test. Isolated state, slower execution.

```
Each Test:
├── Create TestContext
├── Initialize driver
├── Launch application
├── Execute test
├── Capture screenshot (on failure)
├── Close application
├── Quit driver
└── Dispose TestContext
```

### 6.3 Configuration

```json
{
  "lifecycle": {
    "sessionMode": "perRun",  // "perRun" or "perTest"
    "restartOnCrash": true,
    "resetAppState": false    // Clear app data between tests (perRun mode)
  }
}
```

### 6.4 Test Fixture Pattern

```csharp
// Session-per-run: Fixture shared across tests
[Collection("AppCollection")]
public class MyTests
{
    private readonly TestFixture _fixture;
    
    public MyTests(TestFixture fixture)
    {
        _fixture = fixture;
    }
}

// Session-per-test: New fixture each test
public class MyIsolatedTests : IDisposable
{
    private readonly TestFixture _fixture;
    
    public MyIsolatedTests()
    {
        _fixture = new TestFixture();
    }
    
    public void Dispose() => _fixture.Dispose();
}
```

---

## 7. Technology-Specific Concerns

### 7.1 Appium

| Concern | Handling |
|---------|----------|
| Server lifecycle | Started before tests, stopped after |
| Device/emulator | Must be running and accessible |
| App installation | Can install APK/IPA as part of capability |
| Element finding | Uses Locator (default: AutomationId via AccessibilityId) |

### 7.2 Selenium

| Concern | Handling |
|---------|----------|
| Driver management | WebDriverManager recommended |
| Browser versions | Must match driver version |
| Waits | Explicit waits preferred over implicit |
| Element finding | Uses Locator (default: TestId via CSS selector) |

### 7.3 Playwright

| Concern | Handling |
|---------|----------|
| Installation | `playwright install` for browsers |
| Async model | All operations are async |
| Auto-waiting | Built-in intelligent waiting |
| Element finding | Uses Locator (default: TestId via GetByTestId) |

---

## 8. Error Handling

### 8.1 Common Exceptions

| Automation Exception | Brinell Exception |
|---------------------|-------------------|
| NoSuchElementException | ControlNotFoundException |
| ElementNotVisibleException | ControlNotVisibleException |
| ElementNotInteractableException | ControlNotEnabledException |
| TimeoutException | TimeoutException |
| WebDriverException | DriverException |

### 8.2 Exception Translation

```csharp
protected AppiumElement FindElement()
{
    try
    {
        return _context.FindElement(_locator);
    }
    catch (NoSuchElementException ex)
    {
        throw new ControlNotFoundException(_locator.ToString(), ex);
    }
}
```

---

## 9. Validation Rules

The technology integration is valid when:

- [ ] Automation library types are not in public API
- [ ] TestContext manages driver lifecycle
- [ ] Controls use Locator for element finding (not hardcoded strings)
- [ ] Exceptions are translated to Brinell exceptions
- [ ] Configuration is externalized (not hardcoded)
- [ ] Locator strategy is consistent within platform
- [ ] Lifecycle mode (perRun/perTest) is configurable

---

## Related Documents

- [Platform Layer](203_002_PlatformLayer.spx.md)
- [220 External Systems](../220_External/220_INDEX.md)
- [ADR-005 Async Support](../202_Decisions/202_005_AsyncSupport.spx.md)
- [221 Foundation - Timeout](../221_Foundation/221_004_Timeout.spx.md)
