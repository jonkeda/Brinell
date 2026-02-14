# 211.004 Page/Context Module

**Block Type:** MOD (Module)  
**ID:** 211.004  
**Title:** Page and Context Module Definition  
**Status:** Draft  
**Version:** 1.0

---

## 1. Overview

The Page/Context module provides the infrastructure for organizing controls into page objects and managing the test execution context. This module implements the Page Object pattern and provides driver lifecycle management.

> **Note:** Code snippets in this document are illustrative examples showing the intended patterns and API design. Final implementations may vary in details.

### Module Identity

- **Packages:** `Brinell.Core` (interfaces), `Brinell.<Platform>` (implementations)
- **Namespace:** `Brinell.<Platform>.Context`, `Brinell.<Platform>.Pages`
- **Dependencies:** Core Interfaces, Controls Module
- **Consumers:** Test projects

---

## 2. Purpose

The Page/Context module provides:

1. **Page Objects** — Organize controls by screen/page for maintainability
2. **Test Context** — Manage driver lifecycle, configuration, and state
3. **Control Creation** — Page objects create controls via `new` pattern with page reference
4. **Navigation** — Move between pages/screens in the application

---

## 3. TestContext

The `ITestContext` interface manages the test execution environment. It owns the driver, provides configuration, creates controls, and handles navigation. Each platform provides a concrete implementation.

### 3.1 Interface Definition (Core)

Defined in `Brinell.Core`, this interface is technology-agnostic. Platform packages implement it with their specific driver types. **Note:** `ITestContext` does NOT track current page - controls receive their page via constructor parameter.

```csharp
public interface ITestContext : IDisposable
{
    // Configuration
    TimeoutSettings Timeouts { get; }
    ITestLogger Logger { get; }
    
    // Navigation
    void NavigateTo(string destination);
    void NavigateBack();
    void Refresh();
    
    // Screenshots
    byte[] TakeScreenshot();
    void SaveScreenshot(string path);
    
    // State management
    void ResetAppState();
}
```

### 3.2 Platform Implementations

Each platform provides a TestContext that implements both `ITestContext` and the platform-specific interface. This allows base classes to use the interface type instead of casting to concrete classes.

```csharp
// MAUI with Appium - implements IMauiTestContext
public class MauiTestContext : IMauiTestContext
{
    public AppiumDriver Driver { get; }
    public TimeoutSettings Timeouts { get; }
    public ITestLogger Logger { get; }
    
    public MauiTestContext(AppiumOptions options, TimeoutSettings? timeouts = null)
    {
        Driver = new AppiumDriver(new Uri(ServerUrl), options);
        Timeouts = timeouts ?? TimeoutSettings.Default;
        Logger = new ConsoleLogger();
    }
    
    // IMauiTestContext implementation - element finding
    public AppiumElement FindElement(Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => Driver.FindElement(MobileBy.AccessibilityId(locator.Value)),
            LocatorStrategy.XPath => Driver.FindElement(MobileBy.XPath(locator.Value)),
            _ => throw new NotSupportedException()
        };
    }
    
    public AppiumElement? TryFindElement(Locator locator)
    {
        try { return FindElement(locator); }
        catch (NoSuchElementException) { return null; }
    }
    
    public IReadOnlyList<AppiumElement> FindElements(Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => Driver.FindElements(MobileBy.AccessibilityId(locator.Value)).ToList(),
            LocatorStrategy.XPath => Driver.FindElements(MobileBy.XPath(locator.Value)).ToList(),
            _ => throw new NotSupportedException()
        };
    }
    
    // Navigation
    public void NavigateTo(string destination)
    {
        // MAUI apps typically navigate via shell routes or button clicks
        Logger.LogNavigation(destination);
    }
    
    public void NavigateBack()
    {
        Driver.Navigate().Back();
    }
    
    public void Refresh()
    {
        // MAUI apps typically refresh by re-loading page
    }
    
    // Screenshots
    public byte[] TakeScreenshot() => ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;
    
    public void SaveScreenshot(string path)
    {
        File.WriteAllBytes(path, TakeScreenshot());
    }
    
    public void ResetAppState()
    {
        Driver.ResetApp();
    }
    
    public void Dispose()
    {
        Driver?.Quit();
        Driver?.Dispose();
    }
}
```

```csharp
// Blazor with Selenium - implements IBlazorTestContext
public class BlazorTestContext : IBlazorTestContext
{
    public IWebDriver Driver { get; }
    public string BaseUrl { get; }
    public TimeoutSettings Timeouts { get; }
    public ITestLogger Logger { get; }
    
    public BlazorTestContext(DriverOptions options, string baseUrl)
    {
        Driver = CreateDriver(options);
        BaseUrl = baseUrl;
        Timeouts = TimeoutSettings.Default;
        Logger = new ConsoleLogger();
    }
    
    // IBlazorTestContext implementation - element finding
    public IWebElement FindElement(Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => Driver.FindElement(By.CssSelector($"[data-automation-id='{locator.Value}']")),
            LocatorStrategy.Id => Driver.FindElement(By.Id(locator.Value)),
            LocatorStrategy.XPath => Driver.FindElement(By.XPath(locator.Value)),
            LocatorStrategy.CssSelector => Driver.FindElement(By.CssSelector(locator.Value)),
            _ => throw new NotSupportedException()
        };
    }
    
    public IWebElement? TryFindElement(Locator locator)
    {
        try { return FindElement(locator); }
        catch (NoSuchElementException) { return null; }
    }
    
    public IReadOnlyList<IWebElement> FindElements(Locator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => Driver.FindElements(By.CssSelector($"[data-automation-id='{locator.Value}']")).ToList(),
            LocatorStrategy.Id => Driver.FindElements(By.Id(locator.Value)).ToList(),
            LocatorStrategy.XPath => Driver.FindElements(By.XPath(locator.Value)).ToList(),
            LocatorStrategy.CssSelector => Driver.FindElements(By.CssSelector(locator.Value)).ToList(),
            _ => throw new NotSupportedException()
        };
    }
    
    // Navigation
    public void NavigateTo(string destination)
    {
        var url = destination.StartsWith("http") ? destination : $"{BaseUrl}{destination}";
        Driver.Navigate().GoToUrl(url);
        Logger.LogNavigation(url);
    }
    
    public void NavigateBack()
    {
        Driver.Navigate().Back();
    }
    
    public void Refresh()
    {
        Driver.Navigate().Refresh();
    }
    
    // Screenshots
    public byte[] TakeScreenshot() => ((ITakesScreenshot)Driver).GetScreenshot().AsByteArray;
    
    public void SaveScreenshot(string path)
    {
        File.WriteAllBytes(path, TakeScreenshot());
    }
    
    public void ResetAppState()
    {
        Driver.Manage().Cookies.DeleteAllCookies();
        NavigateTo("/");
    }
    
    public void Dispose()
    {
        Driver?.Quit();
        Driver?.Dispose();
    }
}
```

---

## 4. PageObject

Page objects organize controls by screen or page. They provide a clean API for tests and encapsulate the details of locating and interacting with controls. Controls are created via the `new` pattern with a page reference.

### 4.1 Interface Definition (Core)

The interface defines the contract for all page objects. It provides page identification and screenshot capability. Navigation is platform-specific and not part of the core interface.

```csharp
public interface IPageObject
{
    string Name { get; }                    // Page name for logging and identification
    LocatorStrategy DefaultLocatorStrategy { get; }  // Default strategy for string-based locators
    
    // Page operations
    void TakeScreenshot(string? filename = null);    // Capture screenshot
}
```

### 4.2 Base Implementation

`PageObjectBase` provides common page object functionality. Derived classes create controls via `new` pattern in constructor. Page load checking uses controls with `WaitExists`/`AssertExists` methods from `ControlBase`.

```csharp
public abstract class PageObjectBase : IPageObject
{
    protected readonly ITestContext _context;
    
    protected PageObjectBase(ITestContext context, string name)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    public string Name { get; }
    
    // Default locator strategy - derived classes can override
    public virtual LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    public void TakeScreenshot(string? filename = null)
    {
        var name = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        _context.SaveScreenshot(name);
    }
    
    // Helper to create locator using page's default strategy
    protected Locator Locate(string value) => new Locator(DefaultLocatorStrategy, value);
}
```

### 4.3 MAUI Page Object Base

MAUI-specific page object base with access to `IMauiTestContext`.

```csharp
public abstract class MauiPageObjectBase : PageObjectBase
{
    protected readonly IMauiTestContext _mauiContext;
    
    protected MauiPageObjectBase(IMauiTestContext context, string name)
        : base(context, name)
    {
        _mauiContext = context;
    }
}
```

---

## 5. Page Object Pattern

The Page Object pattern separates test logic from page structure. Tests interact with page methods, while pages handle the details of control interaction.

### 5.1 Page Definition

A page object exposes controls as properties (created via `new` in constructor) and provides high-level methods for common operations. Controls support convenience constructors that take a string and use the page's `DefaultLocatorStrategy`.

```csharp
public class LoginPage : MauiPageObjectBase
{
    // Controls use convenience constructor with string (uses page's DefaultLocatorStrategy)
    public EntryControl UsernameField { get; }
    public EntryControl PasswordField { get; }
    public ButtonControl LoginButton { get; }
    public LabelControl ErrorMessage { get; }
    
    public LoginPage(IMauiTestContext context) : base(context, "Login")
    {
        // String constructor uses page's DefaultLocatorStrategy (AutomationId by default)
        UsernameField = new EntryControl(context, "UsernameEntry", this);
        PasswordField = new EntryControl(context, "PasswordEntry", this);
        LoginButton = new ButtonControl(context, "LoginButton", this);
        ErrorMessage = new LabelControl(context, "ErrorLabel", this);
    }
    
    // Wait for page to be ready (uses control existence as indicator)
    public void WaitForPage(int? timeoutMs = null)
    {
        LoginButton.WaitExists(true, timeoutMs);
    }
    
    // Page-specific action methods (do not return other pages)
    public void Login(string username, string password)
    {
        UsernameField.Enter(username);
        PasswordField.Enter(password);
        LoginButton.Click();
    }
}
```

**Control Convenience Constructors:**

Controls provide string-based constructors that create locators using the page's default strategy:

```csharp
public class EntryControl : EditableTextControlBase
{
    // Full constructor with explicit Locator
    public EntryControl(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    // Convenience constructor with string - uses page's DefaultLocatorStrategy
    public EntryControl(IMauiTestContext context, string locatorValue, IPageObject page)
        : this(context, new Locator(page.DefaultLocatorStrategy, locatorValue), page) { }
}
```

### 5.2 Test Usage

Tests create page objects and wait for them to be ready. Navigation happens via platform-appropriate means (app launch, button clicks, etc.). Action methods perform operations without returning other pages.

```csharp
[Test]
public void ValidLogin_NavigatesToHomePage()
{
    using var context = new MauiTestContext(options);
    
    // App launches to login page
    var loginPage = new LoginPage(context);
    loginPage.WaitForPage();
    
    // Perform login action
    loginPage.Login("testuser", "password123");
    
    // Create and verify home page
    var homePage = new HomePage(context);
    homePage.WaitForPage();
    homePage.WelcomeMessage.AssertTextContains("Welcome");
}
```

---

## 6. Container Scoping

Container scoping limits control searches to a specific region of the UI. This improves performance and enables finding controls with duplicate identifiers in different containers.

Container controls (like lists, grids) are `IItemsControlObject` implementations that provide item access methods. Items are themselves control objects that can contain child controls.

```csharp
public class ProductListPage : MauiPageObjectBase
{
    // List control that contains product items
    public ListViewControl ProductList { get; }
    
    public ProductListPage(IMauiTestContext context) : base(context, "ProductList")
    {
        ProductList = new ListViewControl(context, "ProductGrid", this);
    }
}

// ProductCard is a control that represents an item in the list
public class ProductCard : ContainerControlBase
{
    public LabelControl Name { get; }
    public LabelControl Price { get; }
    public ButtonControl AddToCart { get; }
    
    public ProductCard(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        // Child controls are scoped to this container's locator
        Name = new LabelControl(context, Locator.ByAutomationId("ProductName").ScopedTo(locator), page);
        Price = new LabelControl(context, Locator.ByAutomationId("ProductPrice").ScopedTo(locator), page);
        AddToCart = new ButtonControl(context, Locator.ByAutomationId("AddToCartButton").ScopedTo(locator), page);
    }
}

// Usage in tests - generic item access via IItemsControlObject
[Test]
public void CanAddProductToCart()
{
    var productPage = new ProductListPage(context);
    productPage.NavigateTo();
    
    // Get item by index - returns the underlying element locator
    var firstProduct = new ProductCard(context, productPage.ProductList.GetItemLocator(0), productPage);
    firstProduct.AddToCart.Click();
    
    // Or find by content
    var phoneLocator = productPage.ProductList.FindItemLocator("iPhone");
    var phoneCard = new ProductCard(context, phoneLocator, productPage);
    phoneCard.Price.AssertText("$999");
}
```

---

## 7. Navigation

Navigation is platform-specific and varies significantly between MAUI, Blazor, and WPF. Page objects provide a `WaitForPage()` method to wait until the page is ready, but how you get to the page depends on the platform.

### 7.1 Platform-Specific Navigation

**MAUI:** Navigation via Shell routes, button clicks, or programmatic navigation.

```csharp
// MAUI - navigation happens via app interaction or shell
var loginPage = new LoginPage(context);
loginPage.WaitForPage();  // App starts on login

loginPage.Login("user", "pass");

var homePage = new HomePage(context);
homePage.WaitForPage();  // Now on home page after login
```

**Blazor:** Navigation via URL using context's `NavigateTo()`.

```csharp
// Blazor - URL-based navigation via context
context.NavigateTo("/login");
var loginPage = new LoginPage(context);
loginPage.WaitForPage();

loginPage.Login("user", "pass");

context.NavigateTo("/home");  // Or navigation happens automatically after login
var homePage = new HomePage(context);
homePage.WaitForPage();
```

**WPF:** Navigation via button clicks or frame navigation.

```csharp
// WPF - navigation via UI interaction
var mainWindow = new MainWindow(context);
mainWindow.LoginMenuItem.Click();

var loginPage = new LoginPage(context);
loginPage.WaitForPage();
```

### 7.2 Page Transitions

Page action methods perform operations but do not return the target page. Tests create the next page object explicitly after the action, which keeps the test flow clear.

```csharp
public class HomePage : MauiPageObjectBase
{
    public ButtonControl SettingsButton { get; }
    
    public HomePage(IMauiTestContext context) : base(context, "Home")
    {
        SettingsButton = new ButtonControl(context, "SettingsButton", this);
    }
    
    public void WaitForPage(int? timeoutMs = null)
    {
        WelcomeMessage.WaitExists(true, timeoutMs);
    }
    
    // Action method - clicks settings but doesn't return page
    public void ClickSettings()
    {
        SettingsButton.Click();
    }
}

// Test flow is explicit
[Test]
public void CanOpenSettings()
{
    var homePage = new HomePage(context);
    homePage.WaitForPage();
    
    homePage.ClickSettings();
    
    var settingsPage = new SettingsPage(context);
    settingsPage.WaitForPage();
}
```

---

## 8. Configuration

Configuration classes provide defaults for timeouts and other settings. Tests can override defaults for specific scenarios (e.g., faster timeouts for quick checks).

### 8.1 TimeoutSettings

Centralizes all timeout values used by the framework. Predefined profiles (Default, Fast) provide common configurations.

```csharp
public class TimeoutSettings
{
    public int DefaultWait { get; set; } = 10000;      // 10 seconds
    public int PageLoad { get; set; } = 30000;         // 30 seconds
    public int ElementFind { get; set; } = 5000;       // 5 seconds
    public int Animation { get; set; } = 500;          // 500ms
    
    public static TimeoutSettings Default => new();
    
    public static TimeoutSettings Fast => new()
    {
        DefaultWait = 5000,
        PageLoad = 15000,
        ElementFind = 2000
    };
}
```

---

## 9. Validation Rules

The Page/Context module is valid when:

- [ ] TestContext implements ITestContext and manages driver lifecycle
- [ ] TestContext does NOT track CurrentPage (controls receive page via constructor)
- [ ] PageObjectBase provides Name and DefaultLocatorStrategy properties
- [ ] Page objects create controls via `new` pattern with page reference
- [ ] Controls support convenience constructors with string locator
- [ ] Page objects implement WaitForPage() using control existence checks
- [ ] Navigation is platform-specific (not in core IPageObject interface)
- [ ] Action methods do NOT return other page objects
- [ ] Container scoping uses Locator.ScopedTo() pattern
- [ ] Timeouts are configurable through settings

---

## Related Documents

- [Controls Module](211_003_Controls.spx.md)
- [Interfaces Module](211_001_Interfaces.spx.md)
- [FR-101 Page Object](../../100_requirements/120_functional/120_101_PageObject.spx.md)
- [FR-102 Container Object](../../100_requirements/120_functional/120_102_ContainerObject.spx.md)
- [231 Patterns - Page Object Pattern](../231_Patterns/231_002_PageObjectPattern.spx.md)
