# Best Practices

Guidelines for writing maintainable, reliable UI tests.

---

## Test Design

### Single Responsibility

Each test verifies ONE behavior:

```csharp
// ✅ Good - focused test
[Fact]
public void SaveButton_Enabled_After_DataEntry()
{
    var page = LaunchApp<EditorPage>();
    page.WaitForPageReady();
    
    page.NameInput.EnterText("Test");
    
    page.SaveButton.AssertEnabled();
}

// ❌ Bad - testing multiple things
[Fact]
public void Editor_Works_Correctly()
{
    // Tests navigation, input, validation, saving, etc.
}
```

### Test Independence

Tests must run independently in any order:

```csharp
// ✅ Good - sets up own state
[Fact]
public void CanDeleteUser()
{
    var user = CreateTestUser();  // Setup
    DeleteUser(user);              // Action
    VerifyUserDeleted(user);       // Assert
    CleanupUser(user);             // Cleanup
}

// ❌ Bad - depends on previous test
[Fact]
public void CanDeleteUserCreatedInPreviousTest() { }
```

### Deterministic Results

Tests produce same result every run:

```csharp
// ✅ Good - predictable data
var username = $"TestUser_{Guid.NewGuid():N}";

// ❌ Bad - time-dependent
if (DateTime.Now.Hour > 12) { }

// ❌ Bad - environment-dependent
var path = "C:\\MyPath\\file.txt";  // Use configuration instead
```

---

## Page Object Design

### Clear Encapsulation

```csharp
// ✅ Good - exposes both controls and behaviors
public class SettingsPage : BusyPageBase
{
    // Controls for direct access
    public TextBoxControl UsernameInput { get; }
    public ButtonControl SaveButton { get; }
    
    // Behaviors for complex actions
    public void UpdateAndSave(string username)
    {
        UsernameInput.EnterText(username);
        SaveButton.Click();
        WaitForNotBusy();
    }
}

// ❌ Bad - only exposes actions, hides controls
public class SettingsPage : BusyPageBase
{
    private TextBoxControl UsernameInput { get; }  // Private!
    
    public void UpdateUsername(string username) 
    {
        UsernameInput.EnterText(username);
    }
}
```

### Navigation Pattern (v3)

```csharp
// ✅ Good - navigation returns void
public void NavigateToSettings()
{
    Log("Navigating to Settings");
    SettingsButton.Click();
}

// In test:
home.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.WaitForPageReady();

// ❌ Bad - navigation creates and returns page
public SettingsPage NavigateToSettings()
{
    SettingsButton.Click();
    var settings = new SettingsPage(Context);
    settings.WaitForPageReady();
    return settings;  // Don't do this
}
```

### Always Wait After Navigation

```csharp
// ✅ Good - explicit wait for page ready
home.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.WaitForPageReady();
settings.SaveButton.Click();

// ❌ Bad - assume immediate ready
home.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.SaveButton.Click();  // May fail!
```

### Use Platform-Specific Context

```csharp
// ✅ Good - use concrete context type
public SettingsPage(FlaUITestContext context) 
    : base(context, "Settings")
{
    SaveButton = new ButtonControl(context, this, "SaveButton");
}

// ❌ Bad - use interface (loses native driver access)
public SettingsPage(ITestContext context) 
    : base(context, "Settings") { }
```

---

## Control Usage

### Prefer Background-Safe Windows Actions

On Windows MAUI/FlaUI, Brinell defaults to semantic interaction mode. Routine actions should use UI Automation patterns instead of the real desktop mouse, keyboard, foreground window, or clipboard.

```csharp
// Good - uses ValuePattern/semantic text setting where available
page.ApiKeyEntry.SetText("test-key");

// Good - Click first tries Invoke/SelectionItem/LegacyIAccessible patterns
page.SaveButton.Click();

// Use only when you intentionally need keystrokes
page.SaveButton.Press();
page.SearchBox.Submit();
```

Prefer native invokable controls (`Button`, `MenuItem`, `ToolbarItem`, `Switch`, `CheckBox`, `Entry`) for surfaces that tests need to activate. If a visual card uses a gesture recognizer, expose an invokable child button or automation peer so semantic mode can operate it without pointer input.

Interactive desktop input is opt-in:

```powershell
$env:BRINELL_WINDOWS_INTERACTION_MODE = "interactive"
```

Use interactive mode in a VM, CI desktop session, Windows Sandbox, or separate RDP/user session when a test truly needs pointer gestures, keyboard shortcuts, hover, long-press, right-click, double-click, clipboard paste, or raw typing.

### Let Controls Handle Waits

```csharp
// ✅ Good - control waits automatically
button.Click();  // Waits for visible + enabled
textBox.EnterText("value");  // Waits for enabled

// ❌ Bad - manual waits before actions
element.WaitVisible(true);
element.WaitEnabled(true);
element.Click();  // Redundant - Click() already does this
```

### Use Appropriate Assertion Methods

```csharp
// ✅ Good - use control assertions
label.AssertText("Expected");
button.AssertEnabled();

// ⚠️ Acceptable but less preferred
label.GetText().Should().Be("Expected");

// ❌ Bad - manual comparison
Assert.Equal("Expected", label.GetText());
```

### Don't Assert in Page Objects

```csharp
// ✅ Good - assertions in test
[Fact]
public void UsernameDisplaysCorrectly()
{
    var settings = LaunchApp<SettingsPage>();
    settings.WaitForPageReady();
    
    settings.UsernameLabel.AssertText("admin");
}

// ❌ Bad - assertion in page object
public class SettingsPage : BusyPageBase
{
    public void VerifyUsername(string expected)
    {
        UsernameLabel.AssertText(expected);  // Test logic in page!
    }
}
```

---

## Waits and Timeouts

### Never Use Thread.Sleep

```csharp
// ✅ Good - poll for condition
Context.WaitFor(() => element.IsVisible(), 5000);
element.WaitVisible(true);

// ❌ Bad - arbitrary sleep
Thread.Sleep(5000);
```

### Use Appropriate Timeouts

| Scenario | Recommended Timeout |
|----------|-------------------|
| Element visible | 5-10 seconds |
| Page load | 10-30 seconds |
| Save operation | 10-15 seconds |
| API call (mocked) | 5 seconds |
| API call (cloud) | 30+ seconds |

### Configure, Don't Hardcode

```csharp
// ✅ Good - use configuration
element.WaitVisible(true, Context.DefaultTimeoutMs);

// ⚠️ Acceptable for special cases
element.WaitVisible(true, timeoutMs: 30000);  // Long operation

// ❌ Bad - hardcoded without reason
element.WaitVisible(true, 10000);
```

---

## Test Data Management

### Use Unique Values

```csharp
// ✅ Good - unique per run
var username = $"TestUser_{Guid.NewGuid():N}";
var email = $"test_{DateTime.Now:yyyyMMddHHmmss}@example.com";

// ❌ Bad - static value
var username = "TestUser";  // Conflicts between runs
```

### External Test Data

```csharp
// ✅ Good - load from file
var testData = LoadTestData<UserData>("TestData.json");

// ✅ Good - use test data builder
var user = new UserBuilder()
    .WithUsername("testuser")
    .WithEmail("test@example.com")
    .Build();

// ⚠️ Acceptable for simple tests
var username = "admin";
var password = "Test123!";
```

### Never Hardcode Secrets

```csharp
// ✅ Good - environment variable
var apiKey = Environment.GetEnvironmentVariable("API_KEY");
if (apiKey == null) 
{
    throw new InvalidOperationException("API_KEY not configured");
}

// ✅ Good - user secrets
var apiKey = Configuration["ApiKey"];

// ❌ Bad - hardcoded secret
var apiKey = "sk-1234567890abcdef";
```

---

## Cleanup and Disposal

### Always Clean Up

```csharp
public class MyTests : WpfUITestBase
{
    public override void Dispose()
    {
        try
        {
            // Clean up test data
            CleanupTestFiles();
            CloseAnyOpenDialogs();
        }
        finally
        {
            // Always dispose context
            base.Dispose();
        }
    }
}
```

### Screenshot on Failure

```csharp
public override void Dispose()
{
    // Take screenshot if test failed
    if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
    {
        var screenshot = Context.TakeScreenshot("failure");
        TestContext.Out.WriteLine($"Screenshot: {screenshot}");
    }
    
    base.Dispose();
}
```

---

## Logging

### Log Significant Actions

```csharp
public void ImportData(string filePath)
{
    Log($"Importing data from: {filePath}");
    
    ImportButton.Click();
    FilePathInput.EnterText(filePath);
    ConfirmButton.Click();
    
    WaitForNotBusy();
    
    Log("Import completed");
}
```

### Include Context in Errors

```csharp
try
{
    element.Click();
}
catch (Exception ex)
{
    Logger.LogError(
        TestName, 
        PageName, 
        element.AutomationId, 
        $"Failed to click: {ex.Message}"
    );
    throw;
}
```

---

## Anti-Patterns to Avoid

### DON'T Do These

| Anti-Pattern | Why It's Bad | Do Instead |
|--------------|--------------|------------|
| `Thread.Sleep()` | Arbitrary, slows tests | Use waits with conditions |
| Hardcoded paths | Breaks on different machines | Use configuration |
| Test interdependence | Flaky, hard to debug | Independent tests |
| Too many assertions | Hard to identify failure | One behavior per test |
| Raw selectors in tests | Brittle, duplicated | Use page objects |
| Ignoring IsBusy | Race conditions | Check `WaitForNotBusy()` |
| `Console.WriteLine()` | Unstructured | Use structured logging |
| Catch-all exceptions | Hide real failures | Specific exception handling |
| Magic numbers | Unclear intent | Named constants |

---

## Code Organization

### Project Structure

```
MyApp.UITests/
├── PageObjects/
│   ├── MainWindowPage.cs
│   ├── SettingsPage.cs
│   ├── Dialogs/
│   │   ├── ConfirmDialog.cs
│   │   └── ErrorDialog.cs
│   └── Regions/
│       ├── HeaderRegion.cs
│       └── SidebarRegion.cs
├── Tests/
│   ├── NavigationTests.cs
│   ├── SettingsTests.cs
│   └── DataEntryTests.cs
├── TestData/
│   ├── Users.json
│   └── Settings.json
├── Fixtures/
│   └── TestDataFixture.cs
├── appsettings.json
└── MyApp.UITests.csproj
```

### Namespace Organization

```csharp
// Page objects
namespace MyApp.UITests.PageObjects;

// Tests
namespace MyApp.UITests.Tests;

// Test data
namespace MyApp.UITests.TestData;

// Fixtures
namespace MyApp.UITests.Fixtures;
```

---

## Performance Optimization

### Slow Tests

| Cause | Solution |
|-------|----------|
| Too many waits | Use smart waits, not fixed delays |
| Large page objects | Lazy initialization of controls |
| Repeated app launch | Use `[Collection]` for shared fixture |
| Screenshot overhead | Only on failure |
| Long timeouts | Reduce for fast operations |

### Memory Management

```csharp
// ✅ Good - dispose properly
public override void Dispose()
{
    Context?.Dispose();
    base.Dispose();
}

// Clean up old screenshots
[OneTimeTearDown]
public void GlobalCleanup()
{
    CleanupOldScreenshots(days: 7);
    CleanupOldLogs(days: 7);
}
```

---

## CI/CD Considerations

### Environment Consistency

```yaml
# Ensure consistent environment
env:
  UITEST_PLATFORM: Windows
  UITEST_TIMEOUT: 30000
  UITEST_LOG_OUTPUT: both
```

### Parallel Execution

```csharp
// Mark tests that can't run in parallel
[Collection("Sequential")]
public class DatabaseTests { }

// Most UI tests should be independent and parallelizable
[Collection("Parallel")]
public class NavigationTests { }
```

### Artifact Collection

```yaml
- name: Upload test results
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: |
      **/TestResults/**
      **/logs/**
      **/screenshots/**
```

---

## Maintainability

### Clear Naming

```csharp
// ✅ Good - describes what, scenario, and expectation
[Fact]
public void SaveButton_DisabledWhenNoChanges() { }

[Fact]
public void UsernameField_ShowsErrorForInvalidFormat() { }

// ❌ Bad - unclear intent
[Fact]
public void Test1() { }

[Fact]
public void TestButton() { }
```

### Reusable Test Helpers

```csharp
public abstract class TestBase : WpfUITestBase
{
    protected void LoginAsAdmin()
    {
        var login = LaunchApp<LoginPage>();
        login.Login("admin", "admin123");
    }
    
    protected void NavigateToFeature(string featureName)
    {
        var shell = new ShellPage(Context);
        shell.NavigateTo(featureName);
    }
}
```

### Documentation

```csharp
/// <summary>
/// Tests the user settings management workflow including:
/// - Navigation to settings
/// - Updating user preferences  
/// - Saving and verifying changes
/// </summary>
[Fact]
public void UserCanUpdateSettings() { }
```

---

## Review Checklist

Before committing UI tests, verify:

- [ ] Test names follow `Action_Scenario_ExpectedResult` pattern
- [ ] Each test has single responsibility
- [ ] Tests are independent (can run in any order)
- [ ] Page objects used for all element access
- [ ] `WaitForPageReady()` called after navigation
- [ ] No `Thread.Sleep()` or arbitrary delays
- [ ] No hardcoded paths or secrets
- [ ] Assertions use framework methods
- [ ] Proper cleanup in Dispose
- [ ] Screenshots taken on failure
- [ ] Logging for significant actions
- [ ] Configuration for timeouts
- [ ] Test data is unique per run
- [ ] Platform-specific context type used

---

*See also: [Test Writing Guide](15-test-writing-guide.md) | [Troubleshooting](13-troubleshooting.md)*
