# Playwright Testing Guide

## Overview

Brinell.Html.Playwright provides Microsoft Playwright-based browser automation as an alternative to Selenium. It offers faster execution, built-in auto-waiting, and powerful debugging features like tracing and video recording.

## When to Use Playwright vs Selenium

| Feature | Playwright | Selenium |
|---------|------------|----------|
| Speed | Faster (optimized for headless) | Moderate |
| Auto-waiting | Built-in | Manual waits required |
| Browser install | Automatic | Requires WebDriverManager |
| Tracing/debugging | Video, trace, console logs | Screenshots only |
| Network mocking | First-class support | Limited |
| Multi-browser | Chromium, Firefox, WebKit | Chrome, Firefox, Edge, Safari |
| API style | Async-first | Sync-first |

**Choose Playwright when:**
- You need faster test execution
- You want built-in auto-waiting (less flaky tests)
- You need network mocking or request interception
- You want tracing/video for debugging
- You're writing new tests from scratch

**Choose Selenium when:**
- You have existing Selenium tests
- You need Safari browser support
- You need synchronous API for legacy code integration

## Installation

### 1. Add Package Reference

```xml
<PackageReference Include="Brinell.Html.Playwright" />
```

### 2. Install Playwright Browsers

Run after building your test project:

```powershell
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

Or install all browsers:

```powershell
pwsh bin/Debug/net10.0/playwright.ps1 install
```

## Quick Start

### 1. Create Test Base Class

```csharp
using Brinell.Html.Playwright.Testing;
using Xunit.Abstractions;

public abstract class MyAppTestBase : PlaywrightUITestBase
{
    protected override string BaseUrl => 
        Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";

    protected MyAppTestBase(ITestOutputHelper output) : base(output.WriteLine)
    {
    }

    protected async Task NavigateToAsync(string path)
    {
        await NavigateToAsync($"{BaseUrl}{path}");
        await WaitForLoadStateAsync();
    }
}
```

### 2. Create Page Object

```csharp
using Brinell.Html.Playwright.Controls;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

public class LoginPage : PageBase
{
    public TextInputControl Username { get; }
    public TextInputControl Password { get; }
    public ButtonControl LoginButton { get; }
    public LabelControl ErrorMessage { get; }

    public LoginPage(PlaywrightTestContext context) : base(context)
    {
        Username = new TextInputControl(context, this, "#username");
        Password = new TextInputControl(context, this, "#password");
        LoginButton = new ButtonControl(context, this, "#login-btn");
        ErrorMessage = new LabelControl(context, this, ".error-message");
    }

    public override string AutomationId => "#login-form";

    public async Task<DashboardPage> LoginAsync(string user, string pass)
    {
        await Username.ClearAndEnterAsync(user);
        await Password.ClearAndEnterAsync(pass);
        await LoginButton.ClickAsync();
        var dashboard = new DashboardPage(_context);
        await dashboard.WaitForDisplayedAsync();
        return dashboard;
    }
}
```

### 3. Write Tests

```csharp
public class LoginTests : MyAppTestBase
{
    public LoginTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task Login_ValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToAsync("/login");
        var loginPage = new LoginPage(Context);
        await loginPage.WaitForDisplayedAsync();

        // Act
        var dashboard = await loginPage.LoginAsync("testuser", "password123");

        // Assert
        await dashboard.AssertDisplayedAsync();
    }

    [Fact]
    public async Task Login_InvalidPassword_ShowsError()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToAsync("/login");
        var loginPage = new LoginPage(Context);

        // Act
        await loginPage.Username.EnterAsync("testuser");
        await loginPage.Password.EnterAsync("wrongpassword");
        await loginPage.LoginButton.ClickAsync();

        // Assert
        await loginPage.ErrorMessage.WaitVisibleAsync();
        loginPage.ErrorMessage.AssertTextContains("Invalid");
    }
}
```

## Available Controls

| Control | HTML Elements | Key Features |
|---------|--------------|--------------|
| `ButtonControl` | `<button>`, `<input type="button">` | Click, DoubleClick, IsEnabled |
| `LabelControl` | `<label>`, `<span>`, `<p>`, `<div>` | GetText, AssertTextEquals |
| `TextInputControl` | `<input type="text/email/etc">` | Enter, Clear, GetValue |
| `TextAreaControl` | `<textarea>` | Multi-line text, GetRows, AppendText |
| `CheckBoxControl` | `<input type="checkbox">` | Check, Uncheck, Toggle, IsChecked |
| `SelectControl` | `<select>` | SelectByValue/Text/Index, GetItems |
| `LinkControl` | `<a>` | GetHref, OpensInNewTab |
| `RangeInputControl` | `<input type="range">` | GetValue, SetValue, Increment |
| `ProgressControl` | `<progress>` | GetPercentage, WaitForComplete |

## Control Patterns

All controls follow the **Is/Wait/Check/Assert** pattern:

### Is Methods (Immediate state check)
```csharp
bool visible = control.IsVisible();
bool enabled = await control.IsEnabledAsync();
```

### Wait Methods (Poll until condition or timeout)
```csharp
bool appeared = control.WaitVisible(expected: true, timeoutMs: 5000);
bool textChanged = await control.WaitTextEqualsAsync("Expected");
```

### Check Methods (Throw if not met, with screenshot)
```csharp
control.CheckVisible();  // Throws if not visible
control.CheckEnabled();  // Throws if disabled
```

### Assert Methods (Test assertions with logging)
```csharp
control.AssertVisible("Should be visible after click");
control.AssertTextEquals("Expected Text");
await control.AssertEnabledAsync();
```

## Playwright-Specific Features

### Tracing

Capture detailed execution traces for debugging:

```csharp
[Fact]
public async Task ComplexWorkflow_WithTracing()
{
    await LaunchBrowserAsync();
    
    // Start tracing
    await StartTracingAsync("complex-workflow");
    
    try
    {
        // ... test steps ...
    }
    finally
    {
        // Save trace (viewable with Playwright Trace Viewer)
        await StopTracingAsync("traces/complex-workflow.zip");
    }
}
```

View traces:
```powershell
pwsh bin/Debug/net10.0/playwright.ps1 show-trace traces/complex-workflow.zip
```

### Network Mocking

Mock API responses:

```csharp
await Context.MockRouteAsync("**/api/users", async route =>
{
    await route.FulfillAsync(new RouteFulfillOptions
    {
        Body = "[{\"id\": 1, \"name\": \"Test User\"}]",
        ContentType = "application/json"
    });
});
```

Intercept and modify requests:

```csharp
await Context.MockRouteAsync("**/api/**", async route =>
{
    // Add auth header
    var headers = new Dictionary<string, string>(route.Request.Headers)
    {
        ["Authorization"] = "Bearer test-token"
    };
    await route.ContinueAsync(new RouteContinueOptions { Headers = headers });
});
```

### Browser Selection

```csharp
public class FirefoxTests : MyAppTestBase
{
    protected override BrowserType BrowserType => BrowserType.Firefox;
    
    // Tests run in Firefox instead of Chromium
}

public class WebKitTests : MyAppTestBase
{
    protected override BrowserType BrowserType => BrowserType.WebKit;
    
    // Tests run in WebKit (Safari engine)
}
```

### Headless Mode

```csharp
// Override in test base for debugging
protected override bool Headless => false;

// Or via environment variable
protected override bool Headless => 
    Environment.GetEnvironmentVariable("HEADLESS")?.ToLowerInvariant() != "false";
```

### Viewport Configuration

```csharp
protected override int ViewportWidth => 1920;
protected override int ViewportHeight => 1080;
```

### Slow Motion

Slow down execution for debugging:

```csharp
protected override int SlowMo => 500;  // 500ms delay between actions
```

## Selector Strategy

Playwright supports multiple selector types:

```csharp
// CSS selectors (recommended)
new ButtonControl(context, page, "#submit-btn");          // By ID
new ButtonControl(context, page, ".btn-primary");         // By class
new ButtonControl(context, page, "[data-testid='save']"); // By attribute

// The framework auto-detects selector type:
// - Starts with # → ID selector
// - Starts with . → Class selector
// - Starts with [ → Attribute selector
// - Otherwise → data-automation-id lookup
```

## Best Practices

### 1. Use Async Methods
```csharp
// Prefer async
await button.ClickAsync();
await textbox.EnterAsync("value");

// Avoid sync wrappers in new code
button.Click();  // Use only for legacy compatibility
```

### 2. Avoid Explicit Waits
```csharp
// Bad - Playwright auto-waits
Thread.Sleep(1000);
await Task.Delay(1000);

// Good - Use built-in waiting
await page.WaitForDisplayedAsync();
await element.WaitVisibleAsync();
```

### 3. Use Page Objects
```csharp
// Good - Encapsulates page structure
var dashboard = new DashboardPage(Context);
await dashboard.CreateReportAsync("Monthly");

// Avoid - Directly manipulating elements in tests
await Context.ClickElement("#create-report");
await Context.EnterText("#report-name", "Monthly");
```

### 4. Capture Screenshots on Failure
The framework automatically captures screenshots when assertions fail. They're saved to the test output directory.

### 5. Use Tracing for Complex Issues
When debugging flaky or complex tests, enable tracing to capture the full execution flow.

## Troubleshooting

### Browser Not Found
```
Error: Executable doesn't exist at ...
```
Run: `pwsh bin/Debug/net10.0/playwright.ps1 install`

### Element Not Found
- Check selector syntax
- Verify element exists in DOM
- Use `WaitForDisplayedAsync()` before interacting
- Enable non-headless mode to see what's happening

### Timeout Errors
- Increase timeout: `WaitVisible(timeoutMs: 10000)`
- Check if page is loading slowly
- Verify no JavaScript errors in console

### Test Isolation
Each test gets a fresh browser context. If tests interfere:
- Don't share static state
- Clean up test data in the application
- Use unique test data per test
