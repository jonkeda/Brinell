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

The context interface hierarchy provides both non-generic (for backward compatibility) and generic (for type-safe element access) versions.

```csharp
/// <summary>
/// Base test context - manages environment, not element finding.
/// </summary>
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
    byte[] TakeScreenshot();
    
    /// <summary>
    /// Save a screenshot to the specified path.
    /// </summary>
    void SaveScreenshot(string path);
    
    /// <summary>
    /// Reset application state (clear cache, cookies, etc.).
    /// </summary>
    void ResetAppState();
}
```

### 2.2 Generic Test Context Interface

The generic version provides typed element finding from the driver root:

```csharp
/// <summary>
/// Generic test context providing typed element finding.
/// TElement is the platform's native element type.
/// </summary>
public interface ITestContext<TElement> : ITestContext, IElementScope<TElement>
{
    // Inherits from ITestContext:
    // - Timeouts, Logger, Navigation, Screenshots, ResetAppState
    
    // Inherits from IElementScope<TElement>:
    // - TElement? TryFindElement(Locator locator);
    // - TElement FindElement(Locator locator);
    // - IReadOnlyList<TElement> FindElements(Locator locator);
}
```

### 2.3 IElementScope Interface

Element scope defines the element finding contract for pages and containers:

```csharp
/// <summary>
/// Non-generic element scope (for polymorphic access).
/// </summary>
public interface IElementScope
{
    /// <summary>
    /// Default locator strategy for this scope.
    /// </summary>
    LocatorStrategy DefaultLocatorStrategy { get; }
}

/// <summary>
/// Generic element scope with typed element finding.
/// </summary>
public interface IElementScope<TElement> : IElementScope
{
    /// <summary>
    /// Try to find a single element. Returns null if not found.
    /// </summary>
    TElement? TryFindElement(Locator locator);
    
    /// <summary>
    /// Find a single element. Throws if not found.
    /// </summary>
    TElement FindElement(Locator locator);
    
    /// <summary>
    /// Find all matching elements.
    /// </summary>
    IReadOnlyList<TElement> FindElements(Locator locator);
}
```

### 2.4 TimeoutSettings

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

### 2.5 ITestLogger

```csharp
public interface ITestLogger
{
    /// <summary>
    /// Log an informational message with test context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="message">Informational message.</param>
    void LogInfo(string testName, string? pageName, string message);
    
    /// <summary>
    /// Log an action performed by a control with full context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="controlId">Control AutomationId or locator string.</param>
    /// <param name="action">Action name (Click, Enter, etc.).</param>
    /// <param name="value">Optional action value.</param>
    void LogAction(string testName, string? pageName, string controlId, string action, string? value = null);
    
    /// <summary>
    /// Log an assertion result with full context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="controlId">Control AutomationId or locator string.</param>
    /// <param name="assertion">Assertion type (AssertText, AssertVisible, etc.).</param>
    /// <param name="expected">Expected value.</param>
    /// <param name="actual">Actual value.</param>
    /// <param name="passed">Whether assertion passed.</param>
    void LogAssert(string testName, string? pageName, string controlId, string assertion, 
                  object? expected, object? actual, bool passed);
    
    /// <summary>
    /// Log a wait operation result with full context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="controlId">Control AutomationId or locator string.</param>
    /// <param name="waitType">Wait operation type (WaitExists, WaitVisible, etc.).</param>
    /// <param name="succeeded">Whether wait completed successfully.</param>
    /// <param name="elapsedMs">Time elapsed waiting.</param>
    void LogWait(string testName, string? pageName, string controlId, string waitType, 
                 bool succeeded, int elapsedMs);
    
    /// <summary>
    /// Log an error with full context.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="pageName">Name of the current page.</param>
    /// <param name="controlId">Control AutomationId or locator string.</param>
    /// <param name="action">Action that caused the error.</param>
    /// <param name="exception">The exception that occurred.</param>
    void LogError(string testName, string? pageName, string? controlId, string action, Exception exception);
    
    /// <summary>
    /// Log a navigation event.
    /// </summary>
    /// <param name="testName">Name of the current test.</param>
    /// <param name="destination">Navigation target.</param>
    void LogNavigation(string testName, string destination);
    
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
}
```

### 2.6 Lifecycle Behavior

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
    public void LogInfo(string testName, string? pageName, string message)
        => Console.WriteLine($"[INFO] [{testName}] [{pageName ?? ""}] {message}");
    
    public void LogAction(string testName, string? pageName, string controlId, string action, string? value = null)
    {
        var msg = value != null
            ? $"[ACTION] [{testName}] [{pageName ?? ""}] {controlId}.{action}('{value}')"
            : $"[ACTION] [{testName}] [{pageName ?? ""}] {controlId}.{action}()";
        Console.WriteLine(msg);
    }
    
    public void LogAssert(string testName, string? pageName, string controlId, string assertion, 
                         object? expected, object? actual, bool passed)
        => Console.WriteLine($"[{(passed ? "PASS" : "FAIL")}] [{testName}] [{pageName ?? ""}] {controlId}.{assertion}: expected={expected}, actual={actual}");
    
    public void LogWait(string testName, string? pageName, string controlId, string waitType, 
                       bool succeeded, int elapsedMs)
        => Console.WriteLine($"[WAIT] [{testName}] [{pageName ?? ""}] {controlId}.{waitType} {(succeeded ? "succeeded" : "timed out")} in {elapsedMs}ms");
    
    public void LogError(string testName, string? pageName, string? controlId, string action, Exception exception)
        => Console.WriteLine($"[ERROR] [{testName}] [{pageName ?? ""}] [{controlId ?? ""}] {action}: {exception.Message}");
    
    public void LogNavigation(string testName, string destination)
        => Console.WriteLine($"[NAV] [{testName}] Navigate to: {destination}");
    
    public void LogDebug(string message)
        => Console.WriteLine($"[DEBUG] {message}");
    
    public void LogWarning(string message)
        => Console.WriteLine($"[WARN] {message}");
}
```

---

## 8. Platform Context Interfaces

Each platform extends `ITestContext<TElement>` with platform-specific element types and capabilities. See [250_009_PlatformContexts](250_009_PlatformContexts.spx.md) for full specifications.

### 8.1 Interface Hierarchy

```
ITestContext (base - no element finding)
    │
    └── ITestContext<TElement> : IElementScope<TElement>
            │
            ├── IMauiTestContext : ITestContext<AppiumElement>
            │
            ├── IBlazorTestContext : ITestContext<IWebElement>
            │
            └── IWpfTestContext : ITestContext<AutomationElement>
```

### 8.2 Platform-Specific Interfaces

```csharp
/// <summary>
/// MAUI test context with AppiumElement finding.
/// </summary>
public interface IMauiTestContext : ITestContext<AppiumElement>, IMauiElementScope
{
    /// <summary>
    /// Access to the underlying Appium driver.
    /// </summary>
    AppiumDriver Driver { get; }
    
    // Inherits from ITestContext<AppiumElement>:
    // - AppiumElement? TryFindElement(Locator locator);
    // - AppiumElement FindElement(Locator locator);
    // - IReadOnlyList<AppiumElement> FindElements(Locator locator);
    
    // Override default locator strategy
    LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
}

/// <summary>
/// Blazor/Web test context with IWebElement finding.
/// </summary>
public interface IBlazorTestContext : ITestContext<IWebElement>, IBlazorElementScope
{
    /// <summary>
    /// Access to the underlying Selenium WebDriver.
    /// </summary>
    IWebDriver Driver { get; }
    
    /// <summary>
    /// Base URL for the web application.
    /// </summary>
    string BaseUrl { get; }
    
    // Override default locator strategy
    LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.DataTestId;
}

/// <summary>
/// WPF test context with AutomationElement finding.
/// </summary>
public interface IWpfTestContext : ITestContext<AutomationElement>
{
    // Inherits from ITestContext<AutomationElement>:
    // - AutomationElement? TryFindElement(Locator locator);
    // - AutomationElement FindElement(Locator locator);
    // - IReadOnlyList<AutomationElement> FindElements(Locator locator);
    
    LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
}
```

### 8.3 Platform Element Scope Interfaces

Platform element scope interfaces narrow the generic TElement type:

```csharp
/// <summary>
/// MAUI element scope - typed to AppiumElement.
/// </summary>
public interface IMauiElementScope : IElementScope<AppiumElement>
{
    /// <summary>
    /// Access to the context for advanced operations.
    /// </summary>
    IMauiTestContext Context { get; }
}

/// <summary>
/// Blazor element scope - typed to IWebElement.
/// </summary>
public interface IBlazorElementScope : IElementScope<IWebElement>
{
    /// <summary>
    /// Access to the context for advanced operations.
    /// </summary>
    IBlazorTestContext Context { get; }
}
```

---

## Related Documents

- [IControlObject Specification](250_001_IControlObject.spx.md)
- [Platform Contexts Specification](250_009_PlatformContexts.spx.md)
- [Logging Foundation](../../200_architecture/221_Foundation/221_001_Logging.spx.md)
- [Configuration Foundation](../../200_architecture/221_Foundation/221_002_Configuration.spx.md)
- [Test Base Pattern](../../200_architecture/231_Patterns/231_006_TestBasePattern.spx.md)
