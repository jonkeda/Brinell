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
Controls and pages provide built-in assertions:

```csharp
// Control assertions
control.AssertVisible("Button should be visible");
control.AssertNotVisible("Loading should disappear");
control.AssertEnabled("Button should be enabled");
control.AssertDisabled("Button should be disabled during loading");
control.AssertText("Expected Text", "Label should show expected text");

// Page assertions
page.AssertDisplayed("Login page should be displayed");
page.AssertNotDisplayed("Login page should not be visible after logout");
```

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
