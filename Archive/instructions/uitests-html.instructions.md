---
applyTo: "**/UITests/**/*.cs"
description: "Brinell HTML/Blazor UI testing framework guidelines"
---

# Brinell HTML/Blazor UI Testing Guidelines

## Framework Overview
- Use Brinell.Html with Selenium WebDriver for web automation
- Base class for tests: `HtmlUITestBase`
- Base class for page objects: `PageBase` / `LoadingPageBase`
- Test context: `SeleniumTestContext`
- WebDriverManager handles driver downloads automatically

## Page Object Structure
```csharp
using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

public class LoginPage : LoadingPageBase
{
    // Controls - initialized in constructor using control classes
    public ButtonControl LoginButton { get; }
    public TextInputControl EmailInput { get; }
    public TextInputControl PasswordInput { get; }
    public LabelControl ErrorMessage { get; }
    public LabelControl SuccessMessage { get; }
    
    public LoginPage(SeleniumTestContext context) 
        : base(context)
    {
        LoginButton = new ButtonControl(context, this, "#login-btn");
        EmailInput = new TextInputControl(context, this, "#email-input");
        PasswordInput = new TextInputControl(context, this, "#password-input");
        ErrorMessage = new LabelControl(context, this, "#error-message");
        SuccessMessage = new LabelControl(context, this, "#success-message");
    }
    
    // CSS selector that identifies this page
    public override string AutomationId => "#login-title";
    
    // Optional: Loading indicator for async operations
    protected override string? LoadingIndicatorSelector => "#loading-spinner";
    
    public override bool IsDisplayed()
    {
        return _context.ElementIsVisible(AutomationId);
    }
    
    // Workflow methods
    public LoginPage EnterCredentials(string email, string password)
    {
        Log($"EnterCredentials({email}, ***)");
        EmailInput.SetText(email);
        PasswordInput.SetText(password);
        return this;
    }
    
    public DashboardPage SubmitValidLogin(string email, string password)
    {
        Log($"SubmitValidLogin({email}, ***)");
        EnterCredentials(email, password);
        LoginButton.Click();
        WaitForLoaded();  // Wait for loading spinner to disappear
        
        var dashboard = new DashboardPage(_context);
        dashboard.WaitForDisplayed();
        return dashboard;
    }
}
```

## Test Class Structure for Blazor
```csharp
using Brinell.Html.Testing;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

public abstract class BlazorTestBase : HtmlUITestBase
{
    protected BlazorTestBase(ITestOutputHelper output) : base(output.WriteLine)
    {
    }

    protected override string BaseUrl => 
        Environment.GetEnvironmentVariable("BLAZOR_APP_URL") ?? "http://localhost:5180";

    protected override bool Headless => 
        Environment.GetEnvironmentVariable("HEADLESS")?.ToLowerInvariant() == "true";

    // Wait for Blazor SignalR connection
    protected void WaitForBlazorReady(int? timeoutMs = null)
    {
        WaitForDocumentReady(timeoutMs ?? 10000);
        WaitForBlazorConnection(timeoutMs ?? 10000);
    }

    protected void NavigateToPage(string relativePath)
    {
        NavigateTo(relativePath);
        WaitForBlazorReady();
    }
}

[Collection("BlazorUITests")]
public class LoginTests : BlazorTestBase
{
    public LoginTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Login_WithValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/login");
        
        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();
        
        // Act
        var dashboard = loginPage.SubmitValidLogin("test@example.com", "password123");
        
        // Assert
        dashboard.AssertDisplayed("Dashboard should be displayed after login");
    }
}
```

## HTML Control Types
- `ButtonControl` - Buttons, submit buttons
- `TextInputControl` - Text inputs, email, password fields
- `LabelControl` - Labels, spans, divs with text
- `LinkControl` - Anchor links
- `CheckboxControl` - Checkboxes
- `SelectControl` - Select/dropdown elements
- `ProgressControl` - Progress bars

## Selector Support
The framework supports multiple selector types:
- **CSS selectors**: `#id`, `.class`, `[attribute]`
- **ID attribute**: `element-id` (falls back to `id="element-id"`)
- **Data attribute**: `data-automation-id="value"` (default)

```csharp
// CSS selector (starts with #, ., or [)
new ButtonControl(context, this, "#login-btn");
new LabelControl(context, this, ".error-message");
new TextInputControl(context, this, "[data-testid='email']");

// ID fallback
new ButtonControl(context, this, "login-btn");  // finds id="login-btn"
```

## Control Methods
- `Click()` - Click the control
- `SetText(string)` - Set text value
- `Clear()` - Clear text content
- `GetText()` - Get text value
- `GetAttribute(string)` - Get HTML attribute value
- `IsVisible()` - Check if control is visible
- `IsEnabled()` - Check if control is enabled
- `WaitForVisible()` - Wait for element to be visible

## Assertion Methods
Prefer control assertions over xUnit `Assert.*` for UI checks.

### All Controls
```csharp
control.AssertVisible("message");
control.AssertNotVisible("message");
control.AssertEnabled("message");
control.AssertDisabled("message");
control.AssertTextEquals("expected", "message");
control.AssertTextContains("expected", "message");
control.AssertTextEmpty("message");
control.AssertTextNotEmpty("message");
```

### HTML-Specific Assertions
```csharp
control.AssertHasClass("class-name", "message");
control.AssertNotHasClass("class-name", "message");
control.AssertAttribute("name", "expected", "message");
control.AssertHasPlaceholder("message");
```

### TextInputControl Assertions
```csharp
textInput.AssertInputType("password", "message");
textInput.AssertPlaceholder("Enter email", "message");
textInput.AssertIsReadOnly("message");
textInput.AssertIsNotReadOnly("message");
```

### URL/Title Assertions (TestBase)
```csharp
AssertUrl("http://...", "message");
AssertUrlContains("/path", "message");
AssertTitle("Page Title", "message");
AssertTitleContains("Title", "message");
```

## Blazor-Specific Considerations

### Async Rendering
Blazor renders asynchronously. Always wait for expected state:
```csharp
// Click and wait for count to update
counterPage.ClickIncrement();
counterPage.WaitForCount(1);  // Don't immediately assert!
counterPage.GetCurrentCount().Should().Be(1);
```

### Loading States
Use `LoadingPageBase` for pages with loading indicators:
```csharp
public class DashboardPage : LoadingPageBase
{
    protected override string? LoadingIndicatorSelector => ".spinner-border";
    
    // WaitForLoaded() will wait for spinner to disappear
}
```

### Form Validation
Wait for validation messages after form submission:
```csharp
loginPage.ClickLogin();
loginPage.WaitForError();  // Wait for error message to appear
loginPage.GetErrorMessage().Should().Contain("Invalid");
```

## HTML Element Setup
Add id attributes to your Blazor/HTML elements:
```html
<h1 id="login-title">Login</h1>
<input id="email-input" type="email" @bind="Email" />
<button id="login-btn" @onclick="HandleLogin">Login</button>
<div id="error-message" class="alert alert-danger">@ErrorMessage</div>
```

## Disable Parallel Execution
Add `xunit.runner.json` to prevent test interference:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false
}
```

## Best Practices
- Controls are instantiated in constructor
- Use `Log()` method to record actions for debugging
- Return page objects from navigation methods (fluent pattern)
- Use `WaitFor*` methods before assertions (async rendering)
- Use CSS selectors starting with `#` for clarity
- Add loading indicators for async operations
- Disable parallel test execution for Blazor Server apps
- Tests should be independent and not rely on order
