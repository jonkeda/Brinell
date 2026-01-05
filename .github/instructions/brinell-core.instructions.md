---
applyTo: "**/*Tests*/**/*.cs,**/*UITests*/**/*.cs"
description: "Core Brinell UI Testing Framework patterns and conventions"
---

# Brinell Core Testing Framework

## Overview

Brinell is a UI test automation framework that provides a unified API across MAUI, Blazor, and other platforms. It follows the Page Object Pattern with a consistent Is/Wait/Check/Assert method pattern.

## Framework Architecture

| Platform | Context Class | Page Base | Automation Driver |
|----------|--------------|-----------|-------------------|
| MAUI | `AppiumTestContext` | `PageBase` | Appium |
| HTML/Blazor | `SeleniumTestContext` | `PageBase` | Selenium |

## Core Concepts

### Test Context

The test context (`ITestContext`) manages the automation session:

```csharp
// MAUI
public class MyTests : MauiUITestBase
{
    // Context is available as _context
    protected AppiumTestContext _context;
}

// HTML/Blazor
public class MyTests : HtmlUITestBase
{
    // Context is available as _context
    protected SeleniumTestContext _context;
}
```

### Page Objects

Page objects inherit from `PageBase` and define controls as properties:

```csharp
public class MainPage : PageBase
{
    // Required: AutomationId for page identification
    public override string AutomationId => "MainPage";
    
    // Define controls as public properties
    public ButtonControl SubmitButton => new(_context, this, "SubmitButton");
    public LabelControl TitleLabel => new(_context, this, "TitleLabel");
    public EntryControl NameEntry => new(_context, this, "NameEntry");
    
    public MainPage(AppiumTestContext context) : base(context) { }
    
    // Override for custom page visibility check
    public override bool IsDisplayed()
    {
        return TitleLabel.IsVisible();
    }
}
```

### Control Pattern

All controls follow the Is/Wait/Check/Assert pattern:

| Method Prefix | Description | Returns | Throws |
|--------------|-------------|---------|--------|
| `Is*()` | Immediate state check, no waiting | `bool` | No |
| `Wait*()` | Poll until condition or timeout | `bool` | No |
| `Check*()` | Wait and throw if not met (screenshot on failure) | `void` | Yes |
| `Assert*()` | Test assertion with logging (screenshot on failure) | `void` | Yes |

---

## Control Base API

### All Controls (`ControlBase`)

**State Check Methods (Is*):**
```csharp
bool IsExists()         // Check if element exists
bool IsVisible()        // Check if element is displayed
bool IsEnabled()        // Check if element is enabled
string GetText()        // Get element text
int GetTextLength()     // Get text length
```

**Wait Methods (Wait*):**
```csharp
bool WaitExists(bool expected = true, int? timeoutMs = null)
bool WaitVisible(bool expected = true, int? timeoutMs = null)
bool WaitNotVisible(int? timeoutMs = null)
bool WaitEnabled(bool expected = true, int? timeoutMs = null)
bool WaitTextEquals(string expected, int? timeoutMs = null)
bool WaitTextContains(string expected, int? timeoutMs = null)
```

**Check Methods (Check*):**
```csharp
void CheckExists(bool expected = true, int? timeoutMs = null)
void CheckVisible(bool expected = true, int? timeoutMs = null)
void CheckEnabled(bool expected = true, int? timeoutMs = null)
```

**Assert Methods (Assert*):**
```csharp
void AssertExists(string? message = null)
void AssertNotExists(string? message = null)
void AssertVisible(string? message = null)
void AssertNotVisible(string? message = null)
void AssertEnabled(string? message = null)
void AssertDisabled(string? message = null)
void AssertTextEquals(string expected, string? message = null)
void AssertTextContains(string expected, string? message = null)
void AssertTextEmpty(string? message = null)
void AssertTextNotEmpty(string? message = null)
void AssertTextStartsWith(string prefix, string? message = null)
void AssertTextEndsWith(string suffix, string? message = null)
void AssertTextMatches(string pattern, string? message = null)
```

---

## Page Object API

### PageBase

```csharp
string AutomationId { get; }       // Required: Page identifier
string Name { get; }               // Page name (default: class name)

bool IsDisplayed()                 // Check if page is shown
bool IsReady()                     // Check if page is ready for interaction
bool WaitForDisplayed(int? timeoutMs = null)
bool WaitForReady(int? timeoutMs = null)
void CheckDisplayed(int? timeoutMs = null)
void CheckReady(int? timeoutMs = null)
string? TakeScreenshot(string suffix = "")
```

### BusyPageBase (MAUI) / LoadingPageBase (HTML)

For pages with loading indicators:

```csharp
bool IsBusy()                      // Check if loading indicator is visible
bool WaitForNotBusy(int? timeoutMs = null)

// IsReady() returns true only when displayed AND not busy
```

---

## Test Structure

### xUnit Test Pattern

```csharp
public class CounterTests : MauiUITestBase // or HtmlUITestBase
{
    [Fact]
    public void IncrementButton_WhenClicked_IncrementsCounter()
    {
        // Arrange
        var mainPage = new MainPage(_context);
        mainPage.WaitForDisplayed();
        
        // Act
        mainPage.IncrementButton.Click();
        
        // Assert - use control Assert methods, not FluentAssertions
        mainPage.CounterLabel.AssertTextEquals("1");
    }
}
```

### Test Base Classes

- **MAUI:** Inherit from `MauiUITestBase`
- **HTML/Blazor:** Inherit from `HtmlUITestBase`

```csharp
public class MauiUITestBase : IAsyncLifetime
{
    protected AppiumTestContext _context;
    
    public async Task InitializeAsync() { /* setup */ }
    public async Task DisposeAsync() { /* cleanup */ }
}
```

---

## Best Practices

### ✅ DO

1. **Create separate page objects for each page**
2. **Use control assertions** (`control.AssertTextEquals()`) instead of FluentAssertions
3. **Wait for page to be ready** before interactions
4. **Use meaningful AutomationIds** that describe the control's purpose
5. **Override `IsDisplayed()`** if default page detection doesn't work
6. **Add workflow methods** to page objects for common operations

### ❌ DON'T

1. **Don't use FluentAssertions** - use Brinell control assertions
2. **Don't access _context directly** in tests - use page objects
3. **Don't hardcode timeouts** - use default or configurable values
4. **Don't skip WaitForDisplayed()** - pages need time to load
5. **Don't put test logic in page objects** - page objects are for actions only

---

## Workflow Methods

Add workflow methods to page objects for common multi-step operations:

```csharp
public class LoginPage : PageBase
{
    public EntryControl UsernameEntry => new(_context, this, "UsernameEntry");
    public EntryControl PasswordEntry => new(_context, this, "PasswordEntry");
    public ButtonControl LoginButton => new(_context, this, "LoginButton");
    
    // Workflow method - combines multiple actions
    public HomePage Login(string username, string password)
    {
        Log($"Login({username})");
        UsernameEntry.ClearAndEnter(username);
        PasswordEntry.ClearAndEnter(password);
        LoginButton.Click();
        
        var homePage = new HomePage(_context);
        homePage.WaitForDisplayed();
        return homePage;
    }
}
```

---

## Logging and Diagnostics

Brinell automatically logs:
- Actions (Click, Enter, etc.)
- Assertions (pass/fail with expected/actual)
- Wait results (success/timeout)
- Screenshots on failure

Use `Log()` method in page objects for custom logging:

```csharp
protected void Log(string message)
{
    _context.Log($"[{GetType().Name}] {message}");
}
```

---

## Exception Types

| Exception | When Thrown |
|-----------|-------------|
| `AssertionException` | Assert method fails |
| `CheckException` | Check method fails (timeout) |
| `PageNotDisplayedException` | Page not displayed when expected |
| `PageNotReadyException` | Page not ready when expected |
| `InvalidOperationException` | Control not in valid state for action |

All exceptions capture screenshots automatically.

---

## Version

- **Framework Version:** 1.0
- **Spec Reference:** SPEC-006 (ControlObject Framework)
