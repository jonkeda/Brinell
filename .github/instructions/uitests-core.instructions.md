---
applyTo: "**/UITests/**/*.cs"
description: "Brinell Core UI testing framework guidelines"
---

# Brinell Core UI Testing Guidelines

## Framework Architecture
Brinell provides a unified UI testing framework with platform-specific implementations:

| Platform | Package | Driver | Base Class |
|----------|---------|--------|------------|
| WPF | Brinell.Wpf | FlaUI | `WpfUITestBase` |
| HTML/Blazor | Brinell.Html | Selenium | `HtmlUITestBase` |
| MAUI | Brinell.Maui | Appium | `MauiUITestBase` |

## Common Abstractions (Brinell.Core)

### ITestContext
Base interface for all test contexts:
```csharp
public interface ITestContext
{
    string TestName { get; }
    int DefaultTimeoutMs { get; }
    ITestLogger? Logger { get; }
    
    void Log(string message);
    bool ElementExists(string automationId);
    bool ElementIsVisible(string automationId);
    bool WaitFor(Func<bool> condition, int timeoutMs, string description);
}
```

### IPageObject
Base interface for page objects:
```csharp
public interface IPageObject
{
    string AutomationId { get; }
    string Name { get; }
    ITestContext Context { get; }
    
    bool IsDisplayed();
    bool WaitForDisplayed(int? timeoutMs = null);
    void AssertDisplayed(string? message = null);
}
```

### IControlBase
Base interface for controls:
```csharp
public interface IControlBase
{
    string AutomationId { get; }
    IPageObject Parent { get; }
    
    bool IsVisible();
    bool IsEnabled();
    void Click();
    string GetText();
}
```

## Test Structure Pattern
All platforms follow the same test structure:

```csharp
[Collection("UITests")]
public class FeatureTests : PlatformUITestBase
{
    public FeatureTests(ITestOutputHelper output) : base(output.WriteLine)
    {
    }

    [Fact]
    public void Feature_Scenario_ExpectedResult()
    {
        // Arrange - Launch app and navigate to starting point
        LaunchApplication();  // or LaunchBrowser() for HTML
        var page = new FeaturePage(Context!);
        page.WaitForDisplayed();
        
        // Act - Perform the action being tested
        page.PerformAction();
        
        // Assert - Verify the expected result
        page.AssertExpectedState();
    }
}
```

## Page Object Pattern
All platforms use the same page object structure:

```csharp
public class FeaturePage : PageBase
{
    // 1. Control declarations as properties
    public ButtonControl ActionButton { get; }
    public TextInputControl InputField { get; }
    public LabelControl StatusLabel { get; }
    
    // 2. Constructor initializes all controls
    public FeaturePage(TestContext context) 
        : base(context, "FeaturePage")
    {
        ActionButton = new ButtonControl(context, this, "ActionButton");
        InputField = new TextInputControl(context, this, "InputField");
        StatusLabel = new LabelControl(context, this, "StatusLabel");
    }
    
    // 3. AutomationId identifies the page
    public override string AutomationId => "FeaturePage";
    
    // 4. IsDisplayed checks page visibility
    public override bool IsDisplayed()
    {
        return _context.ElementIsVisible(AutomationId);
    }
    
    // 5. Workflow methods for multi-step operations
    public FeaturePage PerformAction(string input)
    {
        Log($"PerformAction({input})");
        InputField.SetText(input);
        ActionButton.Click();
        return this;
    }
    
    // 6. Navigation methods return new page objects
    public NextPage NavigateToNext()
    {
        Log("NavigateToNext()");
        NextButton.Click();
        var page = new NextPage(_context);
        page.WaitForDisplayed();
        return page;
    }
}
```

## Logging and Diagnostics

### CSV Test Logger
All tests automatically log to CSV files:
```
TestResults/Logs/YYYY-MM-DD/TestName_HHMMSS.csv
```

Log columns:
- Timestamp
- Test Name
- Page/Control
- Action
- Result
- Details

### Screenshot Capture
Screenshots are captured on failures:
```
TestResults/Screenshots/YYYY-MM-DD/TestName_HHMMSS_failure.png
```

### Using Log()
```csharp
// In page objects
Log("Starting login flow");
Log($"Entering credentials for {email}");

// In controls (automatically includes control info)
// [ButtonControl:LoginButton] Click()
```

## WaitFor Pattern
Use `WaitFor` for async/timing-sensitive operations:

```csharp
// Wait for condition with custom timeout
_context.WaitFor(
    () => StatusLabel.GetText() == "Complete",
    timeoutMs: 5000,
    description: "status to show Complete");

// Wait for element to appear
control.WaitForVisible(timeoutMs: 3000);

// Wait for element to be enabled
control.WaitForEnabled(timeoutMs: 3000);

// Wait for page to be displayed
page.WaitForDisplayed(timeoutMs: 10000);
```

## Assertion Methods
Controls and pages provide built-in assertions that offer:
- Better error messages with control context
- Automatic screenshot capture on failure
- CSV logging for test analytics
- Consistent wait-before-assert behavior

**Prefer control assertions over xUnit `Assert.*` for UI checks.**

### All Controls (ControlBase)
```csharp
control.AssertExists("message");
control.AssertNotExists("message");
control.AssertVisible("message");
control.AssertNotVisible("message");
control.AssertEnabled("message");
control.AssertDisabled("message");
control.AssertTextEquals("expected", "message");
control.AssertTextContains("expected", "message");
control.AssertTextEmpty("message");
control.AssertTextNotEmpty("message");
control.AssertTextStartsWith("prefix", "message");
control.AssertTextEndsWith("suffix", "message");
```

### Page Assertions
```csharp
page.AssertDisplayed("message");
page.AssertNotDisplayed("message");
```

### Migration from xUnit Assert
| Instead of | Use |
|------------|-----|
| `Assert.True(control.IsVisible())` | `control.AssertVisible()` |
| `Assert.False(control.IsVisible())` | `control.AssertNotVisible()` |
| `Assert.Equal(expected, control.GetText())` | `control.AssertTextEquals(expected)` |
| `Assert.Contains(expected, control.GetText())` | `control.AssertTextContains(expected)` |
| `Assert.Empty(control.GetText())` | `control.AssertTextEmpty()` |
| `Assert.NotEmpty(control.GetText())` | `control.AssertTextNotEmpty()` |
| `Assert.True(page.IsDisplayed())` | `page.AssertDisplayed()` |

### When to Still Use xUnit Assert
Use xUnit `Assert.*` only for:
- Non-UI assertions (business logic, calculations)
- Complex comparisons not covered by control assertions
- Collection assertions on non-control data

## Test Organization

### Test Collections
Use collections to control parallel execution:
```csharp
[Collection("WpfUITests")]    // WPF tests
[Collection("BlazorUITests")] // Blazor tests
[Collection("MauiUITests")]   // MAUI tests
```

### xunit.runner.json
Disable parallel execution for UI tests:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false
}
```

### Test Naming Convention
```
{Feature}_{Scenario}_{ExpectedResult}

Examples:
- Login_WithValidCredentials_NavigatesToDashboard
- Counter_ClickIncrement_IncreasesCount
- Form_SubmitWithEmptyFields_ShowsValidationErrors
```

## Best Practices

### Do
- Initialize controls in constructor
- Use `Log()` to document actions
- Return page objects from navigation methods (fluent pattern)
- Use `WaitFor*` methods before assertions
- Keep tests focused on single behaviors
- Use descriptive assertion messages
- Set automation IDs on all testable elements

### Don't
- Don't use Thread.Sleep() - use WaitFor instead
- Don't access raw driver/automation directly
- Don't share state between tests
- Don't rely on test execution order
- Don't hardcode timeouts - use configurable defaults
- Don't skip the page object pattern

## Project Structure
```
MyApp.UITests/
├── PageObjects/
│   ├── LoginPage.cs
│   ├── DashboardPage.cs
│   └── SettingsPage.cs
├── TestBase/
│   └── MyAppTestBase.cs
├── Tests/
│   ├── LoginTests.cs
│   ├── DashboardTests.cs
│   └── SettingsTests.cs
├── xunit.runner.json
└── MyApp.UITests.csproj
```
