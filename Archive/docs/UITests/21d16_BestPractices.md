# 16. Best Practices

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d16_BestPractices_CodeExamples.md](21d16_BestPractices_CodeExamples.md)  
**Previous:** [Running Tests](21d15_RunningTests.md)  
**Version:** 3.0 (Updated December 2025)

---

## 16.1 Overview

This document consolidates best practices for writing maintainable, reliable UI tests.

**Key Architecture (v3):**
- Core = interfaces only (no base classes, no adapters)
- Platform-specific base classes with native driver access
- Navigation methods return void (tests create target pages)
- Tests use platform-specific context types

---

## 16.2 Test Design Principles

### 16.2.1 Single Responsibility

Each test should verify ONE thing:

```csharp
// Good - focused test
[Fact]
public void Settings_Save_Button_Enabled_After_Change() { }

// Bad - testing multiple things
[Fact]
public void Settings_Page_Works_Correctly() { }
```

### 16.2.2 Independence

Tests must run independently in any order:

```csharp
// Good - sets up own state
[Fact]
public void Can_Delete_User()
{
    var user = CreateTestUser();  // Setup
    DeleteUser(user);              // Action
    VerifyUserDeleted(user);       // Assert
}

// Bad - depends on other test
[Fact]
public void Delete_User_Created_In_Previous_Test() { }
```

### 16.2.3 Deterministic

Tests produce same result every run:

```csharp
// Good - predictable data
var username = $"TestUser_{Guid.NewGuid():N}";

// Bad - time-dependent
if (DateTime.Now.Hour > 12) { }
```

---

## 16.3 Wait/Check/Is/Assert Pattern

### 16.3.1 Always Check Before Action

```csharp
// Good - verify state before acting
public void Click()
{
    CheckVisible();    // Wait + throw if not visible
    CheckEnabled();    // Wait + throw if not enabled
    PerformClick();    // Now safe to click
}

// Bad - click without verification
public void Click()
{
    PerformClick();  // May fail if not visible/enabled
}
```

### 16.3.2 Use Appropriate Method

| Method | Use When |
|--------|----------|
| `Is*()` | Query current state |
| `Wait*()` | Wait for condition, return bool |
| `Check*()` | Wait + throw on timeout |
| `Assert*()` | Verify with logging |

---

## 16.4 Page Object Guidelines

### 16.4.1 Encapsulation

```csharp
// Good - expose both behavior and controls
public class SettingsPage : BusyPageBase
{
    // Expose controls for direct assertions
    public TextBoxControl UsernameInput { get; }
    
    // Expose behavior for complex actions
    public void UpdateAndSave(string username)
    {
        UsernameInput.EnterText(username);
        SaveButton.Click();
        WaitForNotBusy();
    }
}
```

### 16.4.2 Navigation Returns Void (v3)

```csharp
// Good - navigation returns void, test creates target page
public void NavigateToSettings()
{
    SettingsButton.Click();
}

// In test:
shell.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.WaitForPageReady();

// Bad - navigation returns page object (old pattern)
public SettingsPage NavigateToSettings()
{
    SettingsButton.Click();
    var settings = new SettingsPage(Context);
    settings.WaitForPageReady();
    return settings;
}
```

### 16.4.3 Wait After Navigation

```csharp
// Good - ensure page ready in test
shell.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.WaitForPageReady();  // IsBusy + Displayed

// Bad - assume immediate ready
shell.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.SaveButton.Click();  // May fail
```

### 16.4.4 Use Platform-Specific Context

```csharp
// Good - use platform-specific context type
public class SettingsPage : BusyPageBase
{
    public SettingsPage(FlaUITestContext context) : base(context, "Settings")
    {
        SaveButton = new ButtonControl(context, this, "SaveButton");
    }
}

// Bad - use generic interface (loses native driver access)
public SettingsPage(ITestContext context) : base(context, "Settings") { }
```

---

## 16.5 Assertions

### 16.5.1 Use Framework Assertions

```csharp
// Good - use control assertions with logging
settings.UsernameInput.AssertText("admin");
settings.SaveButton.AssertEnabled(true);

// Less preferred - FluentAssertions directly
settings.UsernameInput.GetText().Should().Be("admin");
```

### 16.5.2 One Assert Per Behavior

```csharp
// Good - each assert is meaningful
settings.UsernameInput.AssertVisible();
settings.UsernameInput.AssertEnabled();
settings.UsernameInput.AssertText("");

// Bad - combined without clarity
(settings.UsernameInput.IsVisible() && 
 settings.UsernameInput.IsEnabled() &&
 settings.UsernameInput.GetText() == "").Should().BeTrue();
```

### 16.5.3 Don't Assert in Page Objects

```csharp
// Good - assertions in test
[Fact]
public void Username_Displays_Current_Value()
{
    var settings = shell.NavigateToSettings();
    settings.UsernameInput.AssertText("admin");
}

// Bad - assertion in page object
public void VerifyUsername(string expected)
{
    UsernameInput.AssertText(expected);  // Test logic in page object
}
```

---

## 16.6 Waits and Timeouts

### 16.6.1 Never Use Thread.Sleep

```csharp
// Good - poll for condition
Context.WaitFor(() => element.IsVisible(), 5000);

// Bad - arbitrary sleep
Thread.Sleep(5000);
```

### 16.6.2 Appropriate Timeout Values

| Scenario | Timeout |
|----------|---------|
| Element visible | 5-10s |
| Page load | 10-30s |
| Save operation | 10-15s |
| API call (mocked) | 5s |
| API call (cloud) | 30s+ |

### 16.6.3 Configure Don't Hardcode

```csharp
// Good - use configuration
Context.WaitFor(() => IsVisible(), Context.DefaultTimeoutMs);

// Bad - hardcoded timeout
Context.WaitFor(() => IsVisible(), 10000);
```

---

## 16.7 Test Data

### 16.7.1 Use Unique Values

```csharp
// Good - unique per run
var username = $"TestUser_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}";

// Bad - static value (conflicts between runs)
var username = "TestUser";
```

### 16.7.2 External Test Data

```csharp
// Good - load from file
var user = TestData.Load<TestUser>("TestUsers.json", "validUser");

// OK for simple cases
var username = "admin";
var password = "Test123!";
```

### 16.7.3 Never Hardcode Secrets

```csharp
// Good - environment variable
var apiKey = Environment.GetEnvironmentVariable("API_KEY");

// Bad - hardcoded secret
var apiKey = "sk-1234567890abcdef";
```

---

## 16.8 Cleanup

### 16.8.1 Always Clean Up

```csharp
public void Dispose()
{
    try
    {
        CleanupTestData();
        CloseDialogs();
    }
    finally
    {
        Context?.Dispose();
    }
}
```

### 16.8.2 Screenshot on Failure

```csharp
public void Dispose()
{
    if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
    {
        TakeScreenshot("failure");
    }
    
    Context?.Dispose();
}
```

---

## 16.9 Logging

### 16.9.1 Log Significant Actions

```csharp
// Good - logged
Logger.LogAction(TestName, PageName, "UsernameInput", "EnterText", "admin", true, null);

// Also log navigation
Logger.LogNavigation(TestName, "Shell", "Settings", null);
```

### 16.9.2 Include Expected Values

```csharp
// Good - includes expected for debugging
Logger.LogAssertion(TestName, PageName, "UsernameInput", "AssertText", 
    actual: "Admin", 
    expected: "admin", 
    passed: false,
    message: "Case mismatch");
```

---

## 16.10 Anti-Patterns to Avoid

### 16.10.1 DON'T

| Anti-Pattern | Why |
|--------------|-----|
| `Thread.Sleep()` | Arbitrary, slows tests |
| Hardcoded paths | Breaks on different machines |
| Test interdependence | Flaky, hard to debug |
| Too many assertions | Hard to identify failure |
| Raw selectors in tests | Brittle, duplicated |
| Ignoring IsBusy | Race conditions |
| Console.WriteLine | Use structured logging |
| Catch all exceptions | Hide real failures |

### 16.10.2 DO

| Practice | Why |
|----------|-----|
| Polling waits | Efficient, reliable |
| Configuration | Flexible, maintainable |
| Independent tests | Reliable, parallel-safe |
| Focused assertions | Clear failure messages |
| Page objects | Reusable, maintainable |
| IsBusy checks | Deterministic timing |
| CSV logging | Structured, queryable |
| Specific exception handling | Clear failures |
| Navigation returns void | Test owns page lifecycle |
| Platform-specific context | Direct native driver access |

---

## 16.11 Architecture Best Practices (v3)

### 16.11.1 Project Structure

```
Platform/
├── Infrastructure/
│   └── [Platform]TestContext.cs       # Implements ITestContext
├── Controls/
│   ├── Base/                          # Platform-specific base classes
│   │   ├── ControlBase.cs
│   │   ├── PageBase.cs
│   │   └── [Capability]ControlBase.cs
│   └── [Specific controls]
└── Testing/
    └── [Platform]UITestBase.cs
```

### 16.11.2 Inheritance Guidance

| Base Class | When To Use |
|------------|-------------|
| `PageBase` | Simple pages without loading states |
| `BusyPageBase` | Pages with async loading/busy indicator |
| `ControlBase` | Read-only display controls |
| `ContentControlBase` | Clickable controls |
| `TextControlBase` | Text input controls |
| `ToggleControlBase` | Checkbox/switch controls |
| `SelectorControlBase` | Dropdown/list selection controls |

### 16.11.3 Direct Driver Access

```csharp
// Good - access native driver for platform-specific features
public void ScrollToElement()
{
    var element = _context.FindElement(_automationId);
    _context.Automation.Focus(element);  // Direct FlaUI call
}

// Bad - try to abstract platform details
public void ScrollToElement()
{
    Context.Driver.ScrollTo(ElementId);  // No adapter layer
}
```

---

*Next: [Troubleshooting](21d17_Troubleshooting.md)*
