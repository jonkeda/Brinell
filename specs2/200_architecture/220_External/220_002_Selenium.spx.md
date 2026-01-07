# 220.002 Selenium WebDriver

**Block Type:** EXT (External Dependency)  
**ID:** 220.002  
**Title:** Selenium WebDriver Integration  
**Status:** Draft  
**Version:** 1.0

---

## 1. Overview

Selenium WebDriver is the primary automation driver used by `Brinell.Blazor` for testing Blazor web applications in browsers.

> **Note:** Code snippets in this document are illustrative examples. Final implementations may vary.

### Integration Identity

- **Package:** `Brinell.Blazor`
- **NuGet Dependency:** `Selenium.WebDriver`
- **Minimum Version:** 4.0.0
- **Namespace:** `OpenQA.Selenium`

---

## 2. Purpose

Selenium WebDriver provides:

1. **Cross-Browser Automation** — Chrome, Firefox, Edge, Safari
2. **Element Finding** — Locate elements by ID, CSS, XPath, etc.
3. **Browser Control** — Navigation, cookies, window management
4. **JavaScript Execution** — Run scripts in browser context

---

## 3. Key Types Used

### 3.1 Driver Types

```csharp
// Core driver interface and implementations
IWebDriver                 // Base interface
ChromeDriver               // Google Chrome
FirefoxDriver              // Mozilla Firefox
EdgeDriver                 // Microsoft Edge
SafariDriver               // Apple Safari (macOS only)
RemoteWebDriver            // Remote/Grid execution
```

### 3.2 Element Types

```csharp
// Element types
IWebElement                // UI element interface
```

### 3.3 Locator Strategies

```csharp
// By locator strategies
By.Id(string id)                    // Element ID attribute
By.Name(string name)                // Element name attribute
By.ClassName(string className)      // CSS class name
By.TagName(string tagName)          // HTML tag name
By.CssSelector(string selector)     // CSS selector
By.XPath(string xpath)              // XPath expression
By.LinkText(string text)            // Exact link text
By.PartialLinkText(string text)     // Partial link text
```

---

## 4. Brinell Integration

### 4.1 IBlazorTestContext

The `IBlazorTestContext` interface wraps Selenium driver operations:

```csharp
public interface IBlazorTestContext : ITestContext
{
    // Expose driver for advanced scenarios
    IWebDriver Driver { get; }
    
    // Base URL for relative navigation
    string BaseUrl { get; }
    
    // Element finding (abstracts By usage)
    IWebElement FindElement(Locator locator);
    IWebElement? TryFindElement(Locator locator);
    IReadOnlyList<IWebElement> FindElements(Locator locator);
}
```

### 4.2 Locator Mapping

Brinell's `Locator` class maps to Selenium's `By`:

| Brinell Locator | Selenium By |
|-----------------|-------------|
| `LocatorStrategy.AutomationId` | `By.CssSelector("[data-automation-id='...']")` |
| `LocatorStrategy.Id` | `By.Id()` |
| `LocatorStrategy.XPath` | `By.XPath()` |
| `LocatorStrategy.CssSelector` | `By.CssSelector()` |
| `LocatorStrategy.ClassName` | `By.ClassName()` |
| `LocatorStrategy.Name` | `By.Name()` |

### 4.3 Element Property Mapping

| Brinell Property | Selenium Property |
|------------------|-------------------|
| `IsVisible()` | `element.Displayed` |
| `IsEnabled()` | `element.Enabled` |
| `GetText()` | `element.Text` |
| `GetAttribute(name)` | `element.GetAttribute(name)` |

---

## 5. Driver Setup

### 5.1 Browser Drivers

Each browser requires its corresponding driver:

```bash
# ChromeDriver - must match Chrome version
# Download from: https://chromedriver.chromium.org/

# GeckoDriver for Firefox
# Download from: https://github.com/mozilla/geckodriver/releases

# EdgeDriver - usually included with Edge
# Download from: https://developer.microsoft.com/en-us/microsoft-edge/tools/webdriver/
```

### 5.2 WebDriver Manager (Recommended)

Use `WebDriverManager` NuGet package for automatic driver management:

```csharp
// Automatically downloads and configures driver
new WebDriverManager.DriverManager().SetUpDriver(new ChromeConfig());
var driver = new ChromeDriver();
```

---

## 6. Driver Configuration

### 6.1 Chrome Options

```csharp
var options = new ChromeOptions();
options.AddArgument("--headless");           // Run without UI
options.AddArgument("--disable-gpu");        // Disable GPU acceleration
options.AddArgument("--window-size=1920,1080");
options.AddArgument("--no-sandbox");         // Required in some CI environments
options.AddArgument("--disable-dev-shm-usage");

var driver = new ChromeDriver(options);
```

### 6.2 Firefox Options

```csharp
var options = new FirefoxOptions();
options.AddArgument("-headless");
options.SetPreference("browser.download.folderList", 2);
options.SetPreference("browser.download.dir", "/path/to/downloads");

var driver = new FirefoxDriver(options);
```

### 6.3 Edge Options

```csharp
var options = new EdgeOptions();
options.AddArgument("--headless");
options.AddArgument("--window-size=1920,1080");

var driver = new EdgeDriver(options);
```

---

## 7. Common Operations

### 7.1 Navigation

```csharp
driver.Navigate().GoToUrl("https://example.com");
driver.Navigate().Back();
driver.Navigate().Forward();
driver.Navigate().Refresh();
```

### 7.2 Element Interaction

```csharp
// Find and click
var button = driver.FindElement(By.Id("submit-button"));
button.Click();

// Enter text
var input = driver.FindElement(By.Name("username"));
input.Clear();
input.SendKeys("testuser");

// Get text
var label = driver.FindElement(By.CssSelector(".welcome-message"));
string text = label.Text;

// Get attribute
var link = driver.FindElement(By.TagName("a"));
string href = link.GetAttribute("href");
```

### 7.3 Waits

```csharp
// Implicit wait (applies to all FindElement calls)
driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

// Explicit wait (recommended)
var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
var element = wait.Until(d => d.FindElement(By.Id("dynamic-element")));

// Wait for condition
wait.Until(ExpectedConditions.ElementIsVisible(By.Id("loading")));
wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("button")));
```

### 7.4 JavaScript Execution

```csharp
var executor = (IJavaScriptExecutor)driver;

// Execute script
executor.ExecuteScript("window.scrollTo(0, document.body.scrollHeight)");

// Return value
var title = executor.ExecuteScript("return document.title");

// Pass element
executor.ExecuteScript("arguments[0].click()", element);
```

### 7.5 Screenshots

```csharp
var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
screenshot.SaveAsFile("screenshot.png");
// Or as bytes
byte[] bytes = screenshot.AsByteArray;
```

---

## 8. Blazor-Specific Considerations

### 8.1 Blazor Server vs WebAssembly

Both Blazor hosting models work with Selenium, but timing considerations differ:

**Blazor Server:**
- SignalR connection delay
- May need longer waits for UI updates

**Blazor WebAssembly:**
- Initial load time for .NET runtime
- Once loaded, interactions are faster

### 8.2 Waiting for Blazor

```csharp
// Wait for Blazor to finish processing
var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
wait.Until(d => 
{
    var blazorReady = (bool)((IJavaScriptExecutor)d)
        .ExecuteScript("return window.Blazor !== undefined");
    return blazorReady;
});
```

### 8.3 Data Attributes

Blazor apps should use `data-automation-id` for test identification:

```html
<button data-automation-id="submit-button">Submit</button>
<input data-automation-id="username-input" />
```

```csharp
// Find by automation ID
var button = driver.FindElement(By.CssSelector("[data-automation-id='submit-button']"));
```

---

## 9. Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Driver version mismatch | Browser updated | Update driver or use WebDriverManager |
| Element not interactable | Hidden or covered | Scroll into view, wait for visible |
| Stale element reference | DOM changed | Re-find element |
| Timeout | Slow page load | Increase wait timeout |
| SSL certificate error | Self-signed cert | Add capability to ignore SSL errors |

---

## 10. Version Compatibility

| Brinell Version | Selenium.WebDriver | Browser Support |
|-----------------|-------------------|-----------------|
| 1.0.x | 4.x | Chrome 100+, Firefox 100+, Edge 100+ |

---

## Related Documents

- [211.004 Page/Context Module](../211_Modules/211_004_PageContext.spx.md) — BlazorTestContext implementation
- [FR-103 Interface Hierarchy](../../100_requirements/120_functional/120_103_InterfaceHierarchy.spx.md) — IBlazorTestContext interface
- [220.003 Playwright](220_003_Playwright.spx.md) — Alternative browser automation
