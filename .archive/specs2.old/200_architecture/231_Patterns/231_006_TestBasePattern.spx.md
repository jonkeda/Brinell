# 231_006 Test Base Pattern

## pattern TestBase

- **title**: Test Base Pattern
- **type**: Structural
- **purpose**: Provide platform-specific test infrastructure without casting

---

## Description

The Test Base pattern provides platform-specific test base classes that give tests direct access to the appropriate context type. Tests inherit from their platform's base class rather than using a generic ITestContext, eliminating the need for runtime type checks and casting.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Using generic `ITestContext` in tests causes:
- Runtime casting to access platform-specific features
- Type checks scattered throughout test code
- No compile-time safety for platform operations
- Unclear which platform a test targets

**Solution:** Create platform-specific test base classes that:
- Provide the correct context type directly
- Handle setup and teardown consistently
- Expose platform features without casting
- Make test platform obvious from inheritance

---

## 2. Structure

### 2.1 Participants

| Participant | Role |
|-------------|------|
| TestBase | Abstract base with common test infrastructure |
| MauiTestBase | MAUI test base with IMauiTestContext |
| BlazorTestBase | Blazor test base with IBlazorTestContext |
| WpfTestBase | WPF test base with IWpfTestContext |
| LoginTests | Concrete test class inheriting platform base |

### 2.2 Test Base Hierarchy

```
                      TestBase (abstract)
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
   MauiTestBase     BlazorTestBase      WpfTestBase
         │                 │                 │
   IMauiTestContext  IBlazorTestContext  IWpfTestContext
         │                 │                 │
         ▼                 ▼                 ▼
   LoginMauiTests   LoginBlazorTests   LoginWpfTests
```

---

## 3. Implementation

### 3.1 Abstract Test Base

```csharp
/// <summary>
/// Abstract base for all UI tests.
/// Provides common infrastructure for logging, screenshots, and cleanup.
/// </summary>
public abstract class TestBase : IDisposable
{
    protected ITestLogger? Logger { get; private set; }
    protected string TestName { get; private set; } = string.Empty;
    
    /// <summary>
    /// Called before each test to set up the test context.
    /// </summary>
    protected virtual void SetUp(string testName)
    {
        TestName = testName;
        Logger?.Log($"Starting test: {testName}");
    }
    
    /// <summary>
    /// Called after each test for cleanup.
    /// </summary>
    protected virtual void TearDown()
    {
        Logger?.Log($"Completed test: {TestName}");
    }
    
    /// <summary>
    /// Capture screenshot on test failure.
    /// </summary>
    protected abstract void CaptureScreenshot(string name);
    
    public abstract void Dispose();
}
```

### 3.2 MAUI Test Base

```csharp
/// <summary>
/// Base class for MAUI UI tests using Appium.
/// Provides direct access to IMauiTestContext.
/// </summary>
public abstract class MauiTestBase : TestBase
{
    /// <summary>
    /// The MAUI test context - provides Appium driver access.
    /// </summary>
    protected IMauiTestContext Context { get; private set; } = null!;
    
    protected MauiTestBase()
    {
        Context = CreateContext();
        Context.TestName = GetType().Name;
    }
    
    /// <summary>
    /// Create the test context. Override to customize Appium options.
    /// </summary>
    protected virtual IMauiTestContext CreateContext()
    {
        var options = CreateAppiumOptions();
        var timeouts = CreateTimeoutSettings();
        return new MauiTestContext(options, timeouts);
    }
    
    /// <summary>
    /// Create Appium options for the test. Must be overridden.
    /// </summary>
    protected abstract AppiumOptions CreateAppiumOptions();
    
    /// <summary>
    /// Create timeout settings. Override to customize.
    /// </summary>
    protected virtual TimeoutSettings CreateTimeoutSettings()
    {
        return TimeoutSettings.Default;
    }
    
    /// <summary>
    /// Navigate back in the app.
    /// </summary>
    protected void NavigateBack() => Context.NavigateBack();
    
    /// <summary>
    /// Hide the keyboard if visible.
    /// </summary>
    protected void HideKeyboard() => Context.Driver.HideKeyboard();
    
    protected override void CaptureScreenshot(string name)
    {
        var screenshot = Context.TakeScreenshot();
        // Save to test output folder
    }
    
    public override void Dispose()
    {
        Context?.Dispose();
    }
}
```

### 3.3 Blazor Test Base

```csharp
/// <summary>
/// Base class for Blazor UI tests using Selenium.
/// Provides direct access to IBlazorTestContext.
/// </summary>
public abstract class BlazorTestBase : TestBase
{
    /// <summary>
    /// The Blazor test context - provides WebDriver access.
    /// </summary>
    protected IBlazorTestContext Context { get; private set; } = null!;
    
    protected BlazorTestBase()
    {
        Context = CreateContext();
        Context.TestName = GetType().Name;
    }
    
    /// <summary>
    /// Create the test context. Override to customize.
    /// </summary>
    protected virtual IBlazorTestContext CreateContext()
    {
        var driver = CreateWebDriver();
        return new BlazorTestContext(driver, BaseUrl);
    }
    
    /// <summary>
    /// Create the WebDriver. Override for different browsers.
    /// </summary>
    protected virtual IWebDriver CreateWebDriver()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        return new ChromeDriver(options);
    }
    
    /// <summary>
    /// Base URL for the web application. Must be overridden.
    /// </summary>
    protected abstract string BaseUrl { get; }
    
    /// <summary>
    /// Navigate to a path relative to BaseUrl.
    /// </summary>
    protected void NavigateTo(string path) => Context.NavigateTo(path);
    
    /// <summary>
    /// Navigate back in the browser.
    /// </summary>
    protected void NavigateBack() => Context.NavigateBack();
    
    /// <summary>
    /// Refresh the current page.
    /// </summary>
    protected void Refresh() => Context.Driver.Navigate().Refresh();
    
    /// <summary>
    /// Execute JavaScript in the browser.
    /// </summary>
    protected T ExecuteScript<T>(string script)
    {
        return (T)((IJavaScriptExecutor)Context.Driver).ExecuteScript(script);
    }
    
    protected override void CaptureScreenshot(string name)
    {
        var screenshot = Context.TakeScreenshot();
        // Save to test output folder
    }
    
    public override void Dispose()
    {
        Context?.Dispose();
    }
}
```

### 3.4 WPF Test Base

```csharp
/// <summary>
/// Base class for WPF UI tests using FlaUI.
/// Provides direct access to IWpfTestContext.
/// </summary>
public abstract class WpfTestBase : TestBase
{
    /// <summary>
    /// The WPF test context - provides FlaUI access.
    /// </summary>
    protected IWpfTestContext Context { get; private set; } = null!;
    
    protected WpfTestBase()
    {
        Context = CreateContext();
        Context.TestName = GetType().Name;
    }
    
    /// <summary>
    /// Create the test context. Override to customize.
    /// </summary>
    protected virtual IWpfTestContext CreateContext()
    {
        var app = LaunchApplication();
        return new WpfTestContext(app);
    }
    
    /// <summary>
    /// Launch the application under test.
    /// </summary>
    protected virtual Application LaunchApplication()
    {
        return Application.Launch(AppPath);
    }
    
    /// <summary>
    /// Path to the application executable. Must be overridden.
    /// </summary>
    protected abstract string AppPath { get; }
    
    /// <summary>
    /// Get the main window of the application.
    /// </summary>
    protected Window MainWindow => Context.MainWindow;
    
    /// <summary>
    /// Close the application gracefully.
    /// </summary>
    protected void CloseApplication()
    {
        Context.Application.Close();
    }
    
    protected override void CaptureScreenshot(string name)
    {
        var screenshot = Context.TakeScreenshot();
        // Save to test output folder
    }
    
    public override void Dispose()
    {
        Context?.Dispose();
    }
}
```

---

## 4. Usage

### 4.1 MAUI Test Example

```csharp
public class LoginMauiTests : MauiTestBase
{
    protected override AppiumOptions CreateAppiumOptions()
    {
        var options = new AppiumOptions();
        options.AddAdditionalCapability("app", "com.company.myapp");
        options.AddAdditionalCapability("platformName", "Android");
        options.AddAdditionalCapability("deviceName", "Pixel_6_API_33");
        return options;
    }
    
    [Fact]
    public void Login_ValidCredentials_ShowsHomePage()
    {
        // Context is IMauiTestContext - no casting needed
        var loginPage = new LoginPage(Context);
        
        loginPage.UsernameEntry.Enter("testuser");
        loginPage.PasswordEntry.Enter("password123");
        loginPage.LoginButton.Click();
        
        // Platform-specific operations available directly
        HideKeyboard();
        
        var homePage = new HomePage(Context);
        homePage.WelcomeLabel.AssertTextContains("Welcome");
    }
    
    [Fact]
    public void Login_ThenBack_ReturnsToLogin()
    {
        var loginPage = new LoginPage(Context);
        loginPage.LoginButton.Click();
        
        // Platform method available without casting
        NavigateBack();
        
        loginPage.UsernameEntry.AssertExists();
    }
    
    [Fact]
    public void Login_SwipeToRefresh_RefreshesPage()
    {
        var loginPage = new LoginPage(Context);
        
        // Access Appium driver directly if needed
        var element = Context.Driver.FindElement(MobileBy.AccessibilityId("RefreshArea"));
        // Perform swipe gesture
    }
}
```

### 4.2 Blazor Test Example

```csharp
public class LoginBlazorTests : BlazorTestBase
{
    protected override string BaseUrl => "https://localhost:5001";
    
    protected override IWebDriver CreateWebDriver()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--no-sandbox");
        return new ChromeDriver(options);
    }
    
    [Fact]
    public void Login_ValidCredentials_ShowsDashboard()
    {
        // Navigate directly - no casting needed
        NavigateTo("/login");
        
        // Context is IBlazorTestContext
        var loginPage = new LoginPage(Context);
        
        loginPage.UsernameInput.Enter("testuser");
        loginPage.PasswordInput.Enter("password123");
        loginPage.SubmitButton.Click();
        
        var dashboard = new DashboardPage(Context);
        dashboard.WaitForPage();
        dashboard.UserName.AssertTextEquals("testuser");
    }
    
    [Fact]
    public void Login_WithRememberMe_SetsCookie()
    {
        NavigateTo("/login");
        
        var loginPage = new LoginPage(Context);
        loginPage.RememberMeCheckbox.SetChecked(true);
        loginPage.SubmitButton.Click();
        
        // Execute JavaScript directly
        var cookies = ExecuteScript<object>("return document.cookie;");
        Assert.Contains("remember_token", cookies.ToString());
    }
    
    [Fact]
    public void Dashboard_ScrollToBottom_LoadsMoreData()
    {
        NavigateTo("/dashboard");
        
        var dashboard = new DashboardPage(Context);
        dashboard.WaitForPage();
        
        // Access WebDriver for advanced operations
        ((IJavaScriptExecutor)Context.Driver).ExecuteScript(
            "window.scrollTo(0, document.body.scrollHeight);");
        
        dashboard.LoadMoreIndicator.WaitVisible(false);
    }
}
```

### 4.3 WPF Test Example

```csharp
public class LoginWpfTests : WpfTestBase
{
    protected override string AppPath => @"C:\Apps\MyApp\MyApp.exe";
    
    protected override Application LaunchApplication()
    {
        // Custom launch with arguments
        return Application.Launch(AppPath, "/test-mode");
    }
    
    [Fact]
    public void Login_ValidCredentials_ShowsMainWindow()
    {
        // Context is IWpfTestContext
        var loginPage = new LoginPage(Context);
        
        loginPage.UsernameTextBox.Enter("admin");
        loginPage.PasswordBox.Enter("secret");
        loginPage.LoginButton.Click();
        
        var mainWindow = new MainPage(Context);
        mainWindow.WaitForPage();
        mainWindow.StatusBar.AssertTextContains("Logged in");
    }
    
    [Fact]
    public void MainWindow_Close_PromptsToSave()
    {
        var loginPage = new LoginPage(Context);
        loginPage.LoginButton.Click();
        
        // Access FlaUI main window directly
        MainWindow.Close();
        
        var saveDialog = new SavePromptDialog(Context);
        saveDialog.AssertExists();
        saveDialog.CancelButton.Click();
    }
    
    [Fact]
    public void TreeView_ExpandNode_ShowsChildren()
    {
        var mainPage = new MainPage(Context);
        mainPage.WaitForPage();
        
        // Access FlaUI automation patterns
        var treeView = Context.MainWindow.FindFirstDescendant(
            cf => cf.ByAutomationId("CategoryTree"));
        var rootNode = treeView.AsTree().Items.First();
        rootNode.Expand();
        
        Assert.True(rootNode.Items.Any());
    }
}
```

---

## 5. Fixture Integration

### 5.1 xUnit Class Fixture

```csharp
/// <summary>
/// Shared fixture for MAUI tests - app launched once per test class.
/// </summary>
public class MauiAppFixture : IDisposable
{
    public IMauiTestContext Context { get; }
    
    public MauiAppFixture()
    {
        var options = new AppiumOptions();
        options.AddAdditionalCapability("app", "com.company.myapp");
        options.AddAdditionalCapability("platformName", "Android");
        Context = new MauiTestContext(options);
    }
    
    public void Dispose() => Context?.Dispose();
}

/// <summary>
/// MAUI test base using shared fixture.
/// </summary>
public abstract class MauiTestBaseWithFixture : IClassFixture<MauiAppFixture>
{
    protected IMauiTestContext Context { get; }
    
    protected MauiTestBaseWithFixture(MauiAppFixture fixture)
    {
        Context = fixture.Context;
    }
}

// Usage
public class LoginTests : MauiTestBaseWithFixture
{
    public LoginTests(MauiAppFixture fixture) : base(fixture) { }
    
    [Fact]
    public void Login_Works()
    {
        var loginPage = new LoginPage(Context);
        // ...
    }
}
```

### 5.2 xUnit Collection Fixture

```csharp
/// <summary>
/// Collection fixture for sharing context across multiple test classes.
/// </summary>
[CollectionDefinition("Blazor Tests")]
public class BlazorTestCollection : ICollectionFixture<BlazorAppFixture>
{
}

public class BlazorAppFixture : IDisposable
{
    public IBlazorTestContext Context { get; }
    
    public BlazorAppFixture()
    {
        var driver = new ChromeDriver();
        Context = new BlazorTestContext(driver, "https://localhost:5001");
    }
    
    public void Dispose() => Context?.Dispose();
}

// All tests in collection share the context
[Collection("Blazor Tests")]
public class LoginTests
{
    private readonly IBlazorTestContext _context;
    
    public LoginTests(BlazorAppFixture fixture)
    {
        _context = fixture.Context;
    }
}

[Collection("Blazor Tests")]
public class DashboardTests
{
    private readonly IBlazorTestContext _context;
    
    public DashboardTests(BlazorAppFixture fixture)
    {
        _context = fixture.Context;
    }
}
```

---

## 6. Platform-Specific Helpers

### 6.1 MAUI Test Helpers

```csharp
public abstract class MauiTestBase : TestBase
{
    // ... base implementation ...
    
    /// <summary>
    /// Wait for app to be in foreground.
    /// </summary>
    protected void WaitForAppReady()
    {
        Context.WaitFor(() => 
            Context.Driver.CurrentActivity != null, 
            5000, 
            "app ready");
    }
    
    /// <summary>
    /// Set device orientation.
    /// </summary>
    protected void SetOrientation(ScreenOrientation orientation)
    {
        Context.Driver.Orientation = orientation;
    }
    
    /// <summary>
    /// Simulate pressing the device back button.
    /// </summary>
    protected void PressBackButton()
    {
        Context.Driver.Navigate().Back();
    }
    
    /// <summary>
    /// Put app in background for specified duration.
    /// </summary>
    protected void BackgroundApp(TimeSpan duration)
    {
        Context.Driver.BackgroundApp(duration);
    }
}
```

### 6.2 Blazor Test Helpers

```csharp
public abstract class BlazorTestBase : TestBase
{
    // ... base implementation ...
    
    /// <summary>
    /// Wait for Blazor to finish rendering.
    /// </summary>
    protected void WaitForBlazorReady()
    {
        ExecuteScript<object>(
            "return new Promise(resolve => setTimeout(resolve, 100));");
    }
    
    /// <summary>
    /// Clear browser local storage.
    /// </summary>
    protected void ClearLocalStorage()
    {
        ExecuteScript<object>("localStorage.clear();");
    }
    
    /// <summary>
    /// Set a value in local storage.
    /// </summary>
    protected void SetLocalStorage(string key, string value)
    {
        ExecuteScript<object>($"localStorage.setItem('{key}', '{value}');");
    }
    
    /// <summary>
    /// Get all cookies.
    /// </summary>
    protected IReadOnlyCollection<Cookie> GetCookies()
    {
        return Context.Driver.Manage().Cookies.AllCookies;
    }
    
    /// <summary>
    /// Delete all cookies.
    /// </summary>
    protected void ClearCookies()
    {
        Context.Driver.Manage().Cookies.DeleteAllCookies();
    }
}
```

### 6.3 WPF Test Helpers

```csharp
public abstract class WpfTestBase : TestBase
{
    // ... base implementation ...
    
    /// <summary>
    /// Wait for main window to be ready.
    /// </summary>
    protected void WaitForMainWindow()
    {
        Context.WaitFor(() => 
            Context.MainWindow != null && Context.MainWindow.IsEnabled,
            10000,
            "main window ready");
    }
    
    /// <summary>
    /// Send keyboard shortcut.
    /// </summary>
    protected void SendShortcut(VirtualKeyShort modifier, VirtualKeyShort key)
    {
        Keyboard.TypeSimultaneously(modifier, key);
    }
    
    /// <summary>
    /// Get all modal dialogs.
    /// </summary>
    protected Window[] GetModalWindows()
    {
        return Context.Application.GetAllTopLevelWindows(Context.Automation)
            .Where(w => w.IsModal)
            .ToArray();
    }
    
    /// <summary>
    /// Close all modal dialogs.
    /// </summary>
    protected void CloseAllModals()
    {
        foreach (var modal in GetModalWindows())
        {
            modal.Close();
        }
    }
}
```

---

## 7. Anti-Patterns

### 7.1 Don't Use Generic ITestContext

```csharp
// ❌ BAD: Generic context requires casting
public class LoginTests
{
    private readonly ITestContext _context;
    
    public LoginTests(ITestContext context)
    {
        _context = context;
    }
    
    [Fact]
    public void Test()
    {
        // Must cast for platform operations
        if (_context is IBlazorTestContext blazor)
            blazor.NavigateTo("/login");
    }
}

// ✅ GOOD: Inherit from platform base
public class LoginTests : BlazorTestBase
{
    protected override string BaseUrl => "https://localhost:5001";
    
    [Fact]
    public void Test()
    {
        // Direct access, no casting
        NavigateTo("/login");
    }
}
```

### 7.2 Don't Create Context Manually in Tests

```csharp
// ❌ BAD: Manual context creation
public class LoginTests
{
    [Fact]
    public void Test()
    {
        var options = new AppiumOptions();
        // ... setup options ...
        using var context = new MauiTestContext(options);
        
        var page = new LoginPage(context);
    }
}

// ✅ GOOD: Let base class manage context
public class LoginTests : MauiTestBase
{
    protected override AppiumOptions CreateAppiumOptions()
    {
        var options = new AppiumOptions();
        // ... setup options ...
        return options;
    }
    
    [Fact]
    public void Test()
    {
        var page = new LoginPage(Context);  // Context managed by base
    }
}
```

### 7.3 Don't Mix Platform Tests

```csharp
// ❌ BAD: Same test class for multiple platforms
public class LoginTests
{
    [Fact]
    public void Login_Maui_Works() { /* MAUI test */ }
    
    [Fact]
    public void Login_Blazor_Works() { /* Blazor test */ }
}

// ✅ GOOD: Separate test classes per platform
public class LoginMauiTests : MauiTestBase { /* MAUI tests */ }
public class LoginBlazorTests : BlazorTestBase { /* Blazor tests */ }
```

---

## 8. Validation Rules

The Test Base pattern is valid when:

- [ ] Tests inherit from platform-specific base class
- [ ] Context type matches platform (IMauiTestContext, IBlazorTestContext, IWpfTestContext)
- [ ] Platform operations available without casting
- [ ] Base class manages context lifecycle (create/dispose)
- [ ] Platform-specific helpers defined in base class
- [ ] No ITestContext used directly in test classes
- [ ] Fixtures use platform-specific context types

---

## Related Documents

- [231_003 Adapter Pattern](231_003_AdapterPattern.spx.md)
- [231_002 Page Object Pattern](231_002_PageObjectPattern.spx.md)
- [221_001 Logging](../221_Foundation/221_001_Logging.spx.md)
- [FR-500 Logging](../../100_requirements/120_functional/120_500_Logging.spx.md)
