# Playwright UI Test Instructions

## File Pattern
- applyTo: **/Playwright/**/*.cs, **/*PlaywrightTests*/**/*.cs

## Overview
These instructions apply to Playwright-based UI tests using the Brinell.Html.Playwright package.
Playwright tests use an async-first API with built-in auto-waiting.

## Project Structure

```
MyApp.PlaywrightTests/
├── PageObjects/           # Page object classes
│   ├── LoginPage.cs
│   └── DashboardPage.cs
├── TestBase/              # Test infrastructure
│   └── MyAppTestBase.cs
├── Tests/                 # Test classes
│   ├── LoginTests.cs
│   └── DashboardTests.cs
└── xunit.runner.json      # Sequential execution config
```

## Test Base Pattern

```csharp
public abstract class MyAppTestBase : PlaywrightUITestBase
{
    protected override string BaseUrl => 
        Environment.GetEnvironmentVariable("APP_URL") ?? "http://localhost:5000";

    protected override bool Headless => true;
    protected override BrowserType BrowserType => BrowserType.Chromium;

    protected MyAppTestBase(ITestOutputHelper output) : base(output.WriteLine) { }

    protected async Task NavigateToPageAsync(string relativePath)
    {
        await NavigateToAsync($"{BaseUrl}{relativePath}");
        await WaitForLoadStateAsync();
    }
}
```

## Async-First Pattern

All Playwright operations are natively async. Prefer async methods over sync wrappers:

```csharp
// ✅ Preferred - async
await button.ClickAsync();
await textbox.EnterAsync("value");
var text = await label.GetTextAsync();

// ⚠️ Avoid in new code - sync wrappers
button.Click();
textbox.Enter("value");
var text = label.GetText();
```

## Test Method Structure

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    await LaunchBrowserAsync();
    await NavigateToPageAsync("/path");
    var page = new MyPage(Context);
    await page.WaitForDisplayedAsync();

    // Act
    await page.DoSomethingAsync();

    // Assert
    await page.AssertSomethingAsync();
}
```

## Page Object Pattern

```csharp
public class LoginPage : PageBase
{
    public TextInputControl Username { get; }
    public TextInputControl Password { get; }
    public ButtonControl LoginButton { get; }

    public LoginPage(PlaywrightTestContext context) : base(context)
    {
        Username = new TextInputControl(context, this, "#username");
        Password = new TextInputControl(context, this, "#password");
        LoginButton = new ButtonControl(context, this, "#login-btn");
    }

    public override string AutomationId => "#login-form";

    public async Task<DashboardPage> LoginAsync(string user, string pass)
    {
        await Username.ClearAndEnterAsync(user);
        await Password.ClearAndEnterAsync(pass);
        await LoginButton.ClickAsync();
        return new DashboardPage(_context);
    }
}
```

## Selector Strategy

Prefer CSS selectors over XPath:

```csharp
// ✅ Good - CSS selectors
"#element-id"                    // ID
".class-name"                    // Class
"[data-testid='value']"          // Data attribute
"button.primary"                 // Element with class
"form input[type='email']"       // Nested selector

// ⚠️ Avoid - XPath (unless necessary)
"//div[@class='container']"
```

Use data attributes for test-specific selectors:
```html
<button data-automation-id="submit-order">Submit</button>
```

```csharp
new ButtonControl(context, this, "submit-order");  // Auto-resolves to data-automation-id
```

## Auto-Waiting

Playwright auto-waits for elements to be actionable. Don't add explicit waits unless testing timing:

```csharp
// ❌ Bad - unnecessary delay
await Task.Delay(1000);
Thread.Sleep(500);

// ✅ Good - let Playwright auto-wait
await button.ClickAsync();  // Waits for button to be clickable

// ✅ Good - explicit wait when needed
await page.WaitForDisplayedAsync();  // Wait for page to appear
await element.WaitVisibleAsync();     // Wait for element visibility
```

## Control Usage

### Is/Wait/Check/Assert Pattern

```csharp
// Is - immediate check, no wait
bool visible = await control.IsVisibleAsync();
bool enabled = await control.IsEnabledAsync();

// Wait - poll until condition or timeout
await control.WaitVisibleAsync(expected: true);
await control.WaitTextEqualsAsync("Expected");

// Check - throw if condition not met (with screenshot)
control.CheckVisible();
control.CheckEnabled();

// Assert - test assertion with logging
control.AssertTextEquals("Expected");
await control.AssertVisibleAsync();
```

### Common Controls

```csharp
// TextInputControl
await textbox.EnterAsync("value");
await textbox.ClearAsync();
await textbox.ClearAndEnterAsync("new value");
var value = await textbox.GetTextAsync();

// ButtonControl
await button.ClickAsync();
await button.DoubleClickAsync();

// CheckBoxControl
await checkbox.CheckAsync();
await checkbox.UncheckAsync();
await checkbox.ToggleAsync();
var isChecked = await checkbox.IsCheckedAsync();

// SelectControl
await select.SelectByValueAsync("option1");
await select.SelectByTextAsync("Option One");
await select.SelectByIndexAsync(0);
var items = await select.GetItemsAsync();
```

## Debugging

### Non-Headless Mode
```csharp
protected override bool Headless => false;
```

### Slow Motion
```csharp
protected override int SlowMo => 500;  // 500ms between actions
```

### Tracing
```csharp
await StartTracingAsync("test-name");
try
{
    // test code
}
finally
{
    await StopTracingAsync("traces/test-name.zip");
}
```

View trace: `pwsh playwright.ps1 show-trace traces/test-name.zip`

## Network Mocking

```csharp
await Context.MockRouteAsync("**/api/data", async route =>
{
    await route.FulfillAsync(new RouteFulfillOptions
    {
        Body = "{\"mocked\": true}",
        ContentType = "application/json"
    });
});
```

## Error Handling

Screenshots are automatically captured on assertion failures. For custom screenshots:

```csharp
var path = Context.TakeScreenshot("before-submit");
```

## Test Isolation

- Each test gets a fresh browser context
- Don't share state between tests
- Use unique test data per test
- Clean up test data in the application if needed

## xunit.runner.json

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeTestCollections": false
}
```

## Common Patterns

### Wait for Loading to Complete
```csharp
await Context.WaitForAsync(
    async () => !await loadingSpinner.IsVisibleAsync(),
    timeoutMs: 10000,
    "loading to complete");
```

### Navigate and Wait
```csharp
protected async Task NavigateToPageAsync<TPage>(string path) where TPage : PageBase
{
    await NavigateToAsync($"{BaseUrl}{path}");
    await WaitForLoadStateAsync();
}
```

### Conditional Actions
```csharp
if (await dialog.IsVisibleAsync())
{
    await dialog.CloseButton.ClickAsync();
}
```

## Naming Conventions

- Test class: `{Feature}Tests.cs`
- Test method: `{Method}_{Scenario}_{ExpectedResult}`
- Page object: `{PageName}Page.cs`
- Test base: `{App}TestBase.cs`
