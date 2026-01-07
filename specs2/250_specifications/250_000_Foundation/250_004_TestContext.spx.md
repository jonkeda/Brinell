# 250.004 TestContext Specification

**Block Type:** SPC (Specification)  
**ID:** 250.004  
**Title:** ITestContext Interface Specification  
**Status:** Draft  
**Version:** 1.0  
**Level:** 0 - Foundation

---

## 1. Overview

`ITestContext` manages the test execution environment including driver lifecycle, configuration, navigation, and screenshots. Each platform provides a concrete implementation that wraps the underlying automation driver.

### Interface Identity

- **Package:** `Brinell.Core`
- **Namespace:** `Brinell.Core.Interfaces`
- **Dependencies:** `TimeoutSettings`, `ITestLogger`
- **Implementors:** `MauiTestContext`, `BlazorTestContext`, `WpfTestContext`

---

## 2. Behavior

### 2.1 Core Interface Definition

```csharp
public interface ITestContext : IDisposable
{
    /// <summary>
    /// Timeout configuration for this test context.
    /// </summary>
    TimeoutSettings Timeouts { get; }
    
    /// <summary>
    /// Logger for test actions and diagnostics.
    /// </summary>
    ITestLogger Logger { get; }
    
    /// <summary>
    /// Navigate to a destination (URL for web, route for mobile).
    /// </summary>
    /// <param name="destination">Target destination.</param>
    void NavigateTo(string destination);
    
    /// <summary>
    /// Navigate back in history.
    /// </summary>
    void NavigateBack();
    
    /// <summary>
    /// Refresh the current page/screen.
    /// </summary>
    void Refresh();
    
    /// <summary>
    /// Capture a screenshot as byte array.
    /// </summary>
    /// <returns>Screenshot as PNG bytes.</returns>
    byte[] TakeScreenshot();
    
    /// <summary>
    /// Save a screenshot to the specified path.
    /// </summary>
    /// <param name="path">File path to save screenshot.</param>
    void SaveScreenshot(string path);
    
    /// <summary>
    /// Reset application state (clear cache, cookies, etc.).
    /// </summary>
    void ResetAppState();
}
```

### 2.2 TimeoutSettings

```csharp
public class TimeoutSettings
{
    /// <summary>
    /// Default timeout for wait operations (milliseconds).
    /// </summary>
    public int DefaultWait { get; set; } = 10000;
    
    /// <summary>
    /// Timeout for page load operations (milliseconds).
    /// </summary>
    public int PageLoad { get; set; } = 30000;
    
    /// <summary>
    /// Timeout for element finding (milliseconds).
    /// </summary>
    public int ElementFind { get; set; } = 5000;
    
    /// <summary>
    /// Delay for animation settling (milliseconds).
    /// </summary>
    public int Animation { get; set; } = 500;
    
    /// <summary>
    /// Polling interval for wait operations (milliseconds).
    /// </summary>
    public int PollingInterval { get; set; } = 100;
    
    /// <summary>
    /// Default timeout settings.
    /// </summary>
    public static TimeoutSettings Default => new();
    
    /// <summary>
    /// Fast timeout settings for quick checks.
    /// </summary>
    public static TimeoutSettings Fast => new()
    {
        DefaultWait = 5000,
        PageLoad = 15000,
        ElementFind = 2000,
        Animation = 250,
        PollingInterval = 50
    };
    
    /// <summary>
    /// Slow timeout settings for flaky environments.
    /// </summary>
    public static TimeoutSettings Slow => new()
    {
        DefaultWait = 30000,
        PageLoad = 60000,
        ElementFind = 15000,
        Animation = 1000,
        PollingInterval = 200
    };
}
```

### 2.3 ITestLogger

```csharp
public interface ITestLogger
{
    /// <summary>
    /// Log an action performed by a control.
    /// </summary>
    /// <param name="action">Action name (Click, Enter, etc.).</param>
    /// <param name="locator">Control locator.</param>
    /// <param name="value">Optional action value.</param>
    void LogAction(string action, Locator locator, string? value = null);
    
    /// <summary>
    /// Log a navigation event.
    /// </summary>
    /// <param name="destination">Navigation target.</param>
    void LogNavigation(string destination);
    
    /// <summary>
    /// Log an assertion result.
    /// </summary>
    /// <param name="assertion">Assertion description.</param>
    /// <param name="passed">Whether assertion passed.</param>
    /// <param name="message">Optional message.</param>
    void LogAssertion(string assertion, bool passed, string? message = null);
    
    /// <summary>
    /// Log a debug message.
    /// </summary>
    /// <param name="message">Debug message.</param>
    void LogDebug(string message);
    
    /// <summary>
    /// Log a warning message.
    /// </summary>
    /// <param name="message">Warning message.</param>
    void LogWarning(string message);
    
    /// <summary>
    /// Log an error message.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="exception">Optional exception.</param>
    void LogError(string message, Exception? exception = null);
}
```

### 2.4 Lifecycle Behavior

**Creation:**
- Driver is initialized in constructor
- Timeouts are configured (or defaults used)
- Logger is initialized

**Usage:**
- Context is passed to controls and pages
- Navigation, screenshots, and state management available
- Logging integrated throughout

**Disposal:**
- `Dispose()` quits and disposes driver
- Cleans up any temporary resources
- Logs context shutdown

---

## 3. Boundary

### 3.1 Navigation Failures

| Scenario | Behavior |
|----------|----------|
| `NavigateTo()` with invalid URL/route | Throws NavigationException |
| `NavigateTo()` with unreachable destination | Throws NavigationException after timeout |
| `NavigateBack()` with no history | No-op or platform-specific behavior |
| `Refresh()` on non-web platform | Platform-specific behavior |

### 3.2 Screenshot Failures

| Scenario | Behavior |
|----------|----------|
| `TakeScreenshot()` when driver not ready | Throws ScreenshotException |
| `SaveScreenshot()` to invalid path | Throws IOException |
| `SaveScreenshot()` without permissions | Throws UnauthorizedAccessException |

### 3.3 State Reset

| Scenario | Platform | Behavior |
|----------|----------|----------|
| `ResetAppState()` | MAUI | Calls `Driver.ResetApp()` or relaunches |
| `ResetAppState()` | Blazor | Clears cookies, storage, navigates to root |
| `ResetAppState()` | WPF | Platform-specific cleanup |

### 3.4 Disposal

| Scenario | Behavior |
|----------|----------|
| Double disposal | No error, idempotent |
| Use after disposal | Throws ObjectDisposedException |
| Unhandled exception during test | Dispose still called via using |

---

## 4. Acceptance Criteria

### ACC-001: Timeout Configuration

```gherkin
Given a test context with DefaultWait = 5000
When a control calls WaitExists without timeout parameter
Then it uses 5000ms timeout

Given a test context with default settings
When TimeoutSettings.Default is used
Then DefaultWait is 10000ms
And PageLoad is 30000ms
And ElementFind is 5000ms
```

### ACC-002: Navigation

```gherkin
Given a Blazor test context with BaseUrl "https://localhost:5001"
When NavigateTo("/login") is called
Then browser navigates to "https://localhost:5001/login"
And Logger.LogNavigation is called

Given a MAUI test context
When NavigateBack() is called
Then app navigates to previous screen
```

### ACC-003: Screenshots

```gherkin
Given an active test context
When TakeScreenshot() is called
Then it returns a non-empty byte array
And the bytes represent a valid PNG image

Given a screenshot byte array
When SaveScreenshot("test.png") is called
Then a file "test.png" is created
And the file contains the screenshot data
```

### ACC-004: Logging Integration

```gherkin
Given a test context with a logger
When a control performs Click action
Then Logger.LogAction("Click", locator) is called

Given a test context with a logger
When NavigateTo("/page") is called
Then Logger.LogNavigation("/page") is called
```

### ACC-005: Proper Disposal

```gherkin
Given a test context in a using block
When the using block ends
Then Dispose() is called
And the driver is quit
And the driver is disposed

Given a disposed test context
When any method is called
Then ObjectDisposedException is thrown
```

---

## 5. Assumptions

- **ASM-001:** Platform driver is available and connectable
- **ASM-002:** Screenshot capability is supported by driver
- **ASM-003:** Navigation is supported by the application type
- **ASM-004:** Logger implementation is provided (default ConsoleLogger)
- **ASM-005:** File system is accessible for screenshot saving

---

## 6. Exclusions

- **EXC-001:** Parallel test execution context management — test framework handles
- **EXC-002:** Driver capability negotiation — handled in context constructor
- **EXC-003:** Remote driver connections — platform-specific configuration
- **EXC-004:** Video recording — extension, not core functionality
- **EXC-005:** Network traffic capture — platform-specific extension

---

## 7. Implementation Patterns

### 7.1 Using Pattern

```csharp
// Recommended usage with using statement
[Test]
public void LoginTest()
{
    using var context = new MauiTestContext(options);
    var loginPage = new LoginPage(context);
    loginPage.WaitLoaded(true);
    loginPage.Login("user", "pass");
}
```

### 7.2 Fixture Pattern

```csharp
// Test fixture manages context lifecycle
public class LoginTests
{
    private IMauiTestContext _context;
    
    [SetUp]
    public void Setup()
    {
        _context = new MauiTestContext(options);
    }
    
    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }
    
    [Test]
    public void CanLogin()
    {
        var loginPage = new LoginPage(_context);
        // ...
    }
}
```

### 7.3 ConsoleLogger Implementation

```csharp
public class ConsoleLogger : ITestLogger
{
    public void LogAction(string action, Locator locator, string? value = null)
    {
        var msg = value != null
            ? $"[ACTION] {action}({locator}) = '{value}'"
            : $"[ACTION] {action}({locator})";
        Console.WriteLine(msg);
    }
    
    public void LogNavigation(string destination)
        => Console.WriteLine($"[NAV] Navigate to: {destination}");
    
    public void LogAssertion(string assertion, bool passed, string? message = null)
        => Console.WriteLine($"[{(passed ? "PASS" : "FAIL")}] {assertion} {message}");
    
    public void LogDebug(string message)
        => Console.WriteLine($"[DEBUG] {message}");
    
    public void LogWarning(string message)
        => Console.WriteLine($"[WARN] {message}");
    
    public void LogError(string message, Exception? exception = null)
        => Console.WriteLine($"[ERROR] {message} {exception?.Message}");
}
```

---

## 8. Platform Context Interfaces

Each platform extends `ITestContext` with platform-specific capabilities. See [250_009_PlatformContexts](250_009_PlatformContexts.spx.md) for full specifications.

```csharp
// Platform-specific interfaces preview
public interface IMauiTestContext : ITestContext
{
    AppiumDriver Driver { get; }
    AppiumElement FindElement(Locator locator);
    // ...
}

public interface IBlazorTestContext : ITestContext
{
    IWebDriver Driver { get; }
    string BaseUrl { get; }
    IWebElement FindElement(Locator locator);
    // ...
}

public interface IWpfTestContext : ITestContext
{
    AutomationElement FindElement(Locator locator);
    // ...
}
```

---

## Related Documents

- [IControlObject Specification](250_001_IControlObject.spx.md)
- [Platform Contexts Specification](250_009_PlatformContexts.spx.md)
- [Logging Foundation](../../200_architecture/221_Foundation/221_001_Logging.spx.md)
- [Configuration Foundation](../../200_architecture/221_Foundation/221_002_Configuration.spx.md)
- [Test Base Pattern](../../200_architecture/231_Patterns/231_006_TestBasePattern.spx.md)
