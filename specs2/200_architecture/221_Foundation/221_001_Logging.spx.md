# 221_001 Logging

## foundation Logging

- **title**: Test Logging and Diagnostics
- **package**: Brinell.Core.Logging
- **purpose**: Provide consistent logging across all test operations

---

## Description

The Logging foundation provides a unified interface for recording test operations, assertions, wait results, and errors. All logging outputs to CSV format for easy analysis and reporting.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Logging Contract

### 1.1 ITestLogger Interface

The core logging interface defines methods for all logging scenarios:

```csharp
public interface ITestLogger : IDisposable
{
    // Core log method - all others delegate to this
    void Log(
        string testName,
        string pageName,
        string controlId,
        string action,
        string? value,
        string? expectedValue,
        LogResult result,
        string? message);
    
    // Action logging (Click, EnterText, etc.)
    void LogAction(
        string testName,
        string pageName,
        string controlId,
        string action,
        string? value = null);
    
    // Assertion logging
    void LogAssertPass(
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue);
    
    void LogAssertFail(
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue,
        string? message = null);
    
    // Wait logging
    void LogWait(
        string testName,
        string pageName,
        string controlId,
        string waitType,
        bool success,
        int elapsedMs);
    
    // Navigation logging
    void LogNavigation(
        string testName,
        string sourcePage,
        string targetPage);
    
    void LogNavigation(
        string testName,
        string pageName,
        string pageId,
        string action,
        string? value = null);
    
    // Info and error logging
    void LogInfo(
        string testName,
        string pageName,
        string message);
    
    void LogError(
        string testName,
        string pageName,
        string controlId,
        string action,
        Exception ex);
    
    // Flush to disk
    void Flush();
}
```

### 1.2 LogResult Enumeration

```csharp
public enum LogResult
{
    Success,    // Operation completed successfully
    Fail,       // Operation failed (assertion/check)
    Error,      // Exception occurred
    Info,       // Informational message
    Warning     // Potential issue detected
}
```

---

## 2. CSV Output Format

### 2.1 Column Structure

All log entries use a consistent CSV format:

| Column | Description | Example |
|--------|-------------|---------|
| Timestamp | ISO 8601 timestamp | 2026-01-07T15:30:45.123 |
| TestName | Current test method name | LoginTest_ValidCredentials |
| PageName | Current page object name | LoginPage |
| ControlId | Control's AutomationId | UsernameEntry |
| Action | Operation performed | Click, EnterText, AssertText |
| Value | Actual/input value | "john.doe" |
| ExpectedValue | Expected value (assertions) | "Hello, John" |
| Result | Success, Fail, Error, Info | Success |
| Message | Additional context | null or error details |

### 2.2 CSV Output Example

```csv
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
2026-01-07T15:30:45.123;LoginTest;LoginPage;UsernameEntry;EnterText;john.doe;;Success;
2026-01-07T15:30:45.456;LoginTest;LoginPage;PasswordEntry;EnterText;****;;Success;
2026-01-07T15:30:45.789;LoginTest;LoginPage;LoginButton;Click;;;Success;
2026-01-07T15:30:48.012;LoginTest;HomePage;WelcomeLabel;AssertText;Hello, John;Hello, John;Success;
```

---

## 3. CSV Logger Implementation

### 3.1 CsvTestLogger

```csharp
public class CsvTestLogger : ITestLogger
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private bool _headerWritten = false;
    
    public CsvTestLogger(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
            
        _writer = new StreamWriter(filePath, append: false, Encoding.UTF8);
    }
    
    public void Log(
        string testName,
        string pageName,
        string controlId,
        string action,
        string? value,
        string? expectedValue,
        LogResult result,
        string? message)
    {
        lock (_lock)
        {
            if (!_headerWritten)
            {
                _writer.WriteLine("Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message");
                _headerWritten = true;
            }
            
            var timestamp = DateTime.Now.ToString("O");
            var escapedValue = EscapeCsvValue(value);
            var escapedExpected = EscapeCsvValue(expectedValue);
            var escapedMessage = EscapeCsvValue(message);
            
            _writer.WriteLine($"{timestamp};{testName};{pageName};{controlId};{action};{escapedValue};{escapedExpected};{result};{escapedMessage}");
        }
    }
    
    // Other methods delegate to Log()...
    
    public void LogAction(string testName, string pageName, string controlId, string action, string? value = null)
        => Log(testName, pageName, controlId, action, value, null, LogResult.Success, null);
    
    public void LogAssertPass(string testName, string pageName, string controlId, string assertType, string? actualValue, string? expectedValue)
        => Log(testName, pageName, controlId, assertType, actualValue, expectedValue, LogResult.Success, null);
    
    public void LogAssertFail(string testName, string pageName, string controlId, string assertType, string? actualValue, string? expectedValue, string? message = null)
        => Log(testName, pageName, controlId, assertType, actualValue, expectedValue, LogResult.Fail, message);
    
    public void LogWait(string testName, string pageName, string controlId, string waitType, bool success, int elapsedMs)
        => Log(testName, pageName, controlId, waitType, elapsedMs.ToString(), null, success ? LogResult.Success : LogResult.Fail, null);
    
    public void LogNavigation(string testName, string sourcePage, string targetPage)
        => Log(testName, sourcePage, "", "Navigate", targetPage, null, LogResult.Info, null);
    
    public void LogNavigation(string testName, string pageName, string pageId, string action, string? value = null)
        => Log(testName, pageName, pageId, action, value, null, LogResult.Success, null);
    
    public void LogInfo(string testName, string pageName, string message)
        => Log(testName, pageName, "", "Info", null, null, LogResult.Info, message);
    
    public void LogError(string testName, string pageName, string controlId, string action, Exception ex)
        => Log(testName, pageName, controlId, action, null, null, LogResult.Error, ex.Message);
    
    public void Flush()
    {
        lock (_lock)
        {
            _writer.Flush();
        }
    }
    
    public void Dispose()
    {
        Flush();
        _writer.Dispose();
    }
    
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
```

---

## 4. Logging Levels

| Level | Use Case | Logged By |
|-------|----------|-----------|
| Success | Operation completed | LogAction, LogAssertPass |
| Fail | Condition not met | LogAssertFail, LogWait(false) |
| Error | Exception occurred | LogError |
| Info | Navigation, messages | LogNavigation, LogInfo |
| Warning | Potential issues | Log with LogResult.Warning |

### 4.1 Verbose Mode

For detailed debugging, enable verbose logging:

```csharp
// In test configuration
var config = new UITestConfiguration
{
    LogOutputPath = "logs",
    // Verbose mode logs all polling attempts
    VerboseLogging = true
};
```

---

## 5. Operation Wrapping Pattern

The framework uses a `Run` pattern to wrap operations with automatic logging. This ensures consistent entry/exit logging, duration tracking, and exception handling.

### 5.1 Run Methods in ControlBase

Generic `Run` methods accept any value type for logging.

```csharp
/// <summary>
/// Run operation without a value parameter.
/// </summary>
protected void Run(string action, Action operation)
{
    Run<object>(action, null, operation);
}

/// <summary>
/// Run operation with a typed value parameter.
/// </summary>
protected void Run<T>(string action, T? value, Action operation)
{
    var stopwatch = Stopwatch.StartNew();
    _logger?.LogEntry(_testName, _page.Name, AutomationId, action, value?.ToString());
    
    try
    {
        operation();
        stopwatch.Stop();
        _logger?.LogExit(_testName, _page.Name, AutomationId, action, 
            LogResult.Success, (int)stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger?.LogExit(_testName, _page.Name, AutomationId, action, 
            LogResult.Error, (int)stopwatch.ElapsedMilliseconds, ex.Message);
        throw;
    }
}

/// <summary>
/// Run operation that returns a value.
/// </summary>
protected TResult Run<TResult>(string action, Func<TResult> operation)
{
    return Run<object, TResult>(action, null, operation);
}

/// <summary>
/// Run operation with a typed value parameter that returns a result.
/// </summary>
protected TResult Run<TValue, TResult>(string action, TValue? value, Func<TResult> operation)
{
    var stopwatch = Stopwatch.StartNew();
    _logger?.LogEntry(_testName, _page.Name, AutomationId, action, value?.ToString());
    
    try
    {
        var result = operation();
        stopwatch.Stop();
        _logger?.LogExit(_testName, _page.Name, AutomationId, action, 
            LogResult.Success, (int)stopwatch.ElapsedMilliseconds);
        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger?.LogExit(_testName, _page.Name, AutomationId, action, 
            LogResult.Error, (int)stopwatch.ElapsedMilliseconds, ex.Message);
        throw;
    }
}
```

### 5.2 Usage in Control Methods

```csharp
// No value parameter
public void Click(int? timeoutMs = null)
{
    Run("Click", () =>
    {
        ClickElement(FindElement());
    });
}

// String value
public void Enter(string? text, int? timeoutMs = null)
{
    Run("Enter", text, () =>
    {
        var element = FindElement();
        element.Clear();
        if (text != null)
            element.SendKeys(text);
    });
}

// Boolean value
public void SetChecked(bool isChecked, int? timeoutMs = null)
{
    Run("SetChecked", isChecked, () =>
    {
        var element = FindElement();
        if (element.Selected != isChecked)
            element.Click();
    });
}

// Numeric value (int)
public void SelectByIndex(int? index, int? timeoutMs = null)
{
    Run("SelectByIndex", index, () =>
    {
        if (index.HasValue)
            SelectItemAtIndex(index.Value);
    });
}

// Numeric value (double)
public void SetValue(double? value, int? timeoutMs = null)
{
    Run("SetValue", value, () =>
    {
        SetSliderValue(value ?? 0);
    });
}

// DateTime value
public void SetDate(DateTime? date, int? timeoutMs = null)
{
    Run("SetDate", date, () =>
    {
        SetDatePickerValue(date);
    });
}

// Return value
public string GetText(int? timeoutMs = null)
{
    return Run("GetText", () => FindElement().Text);
}

public bool IsChecked(int? timeoutMs = null)
{
    return Run("IsChecked", () => FindElement().Selected);
}

public double GetValue(int? timeoutMs = null)
{
    return Run("GetValue", () => ParseSliderValue());
}
```

### 5.3 Entry/Exit Log Methods

```csharp
public interface ITestLogger : IDisposable
{
    // Entry logging (before operation)
    void LogEntry(
        string testName,
        string pageName,
        string controlId,
        string action,
        string? value);
    
    // Exit logging (after operation)
    void LogExit(
        string testName,
        string pageName,
        string controlId,
        string action,
        LogResult result,
        int durationMs,
        string? message = null);
    
    // ... other methods
}
```

---

## 6. Assertion Wrapping Pattern

Assertions use a similar wrapping pattern with expected value support. The `RunAssert` method is generic and accepts any `IComparable?` type.

### 6.1 RunAssert Methods in ControlBase

Two overloads: one with default equality comparison, one with custom comparison function.

```csharp
/// <summary>
/// Run assertion with default equality comparison.
/// </summary>
protected void RunAssert<T>(
    string assertType, 
    T? expected, 
    Func<T?> getActual, 
    string? message = null) where T : IComparable?
{
    RunAssert(assertType, expected, getActual, 
        (actual, exp) => Equals(actual, exp), 
        message);
}

/// <summary>
/// Run assertion with custom comparison function.
/// </summary>
protected void RunAssert<T>(
    string assertType, 
    T? expected, 
    Func<T?> getActual, 
    Func<T?, T?, bool> compare,
    string? message = null) where T : IComparable?
{
    var stopwatch = Stopwatch.StartNew();
    _logger?.LogEntry(_testName, _page.Name, AutomationId, assertType, expected?.ToString());
    
    var actual = getActual();
    stopwatch.Stop();
    
    if (compare(actual, expected))
    {
        _logger?.LogAssertExit(_testName, _page.Name, AutomationId, assertType,
            actual?.ToString(), expected?.ToString(), LogResult.Success, (int)stopwatch.ElapsedMilliseconds);
    }
    else
    {
        _logger?.LogAssertExit(_testName, _page.Name, AutomationId, assertType,
            actual?.ToString(), expected?.ToString(), LogResult.Fail, (int)stopwatch.ElapsedMilliseconds, message);
        throw new AssertionException(
            message ?? $"Expected '{expected}' but got '{actual}'", 
            AutomationId, 
            assertType);
    }
}
```

### 6.2 Usage in Assertion Methods

```csharp
// String assertions - use default equality
public void AssertTextEquals(string? expected, string? message = null)
{
    RunAssert("AssertTextEquals", expected, () => GetText(), message);
}

// String assertions - use custom comparison for Contains
public void AssertTextContains(string? expected, string? message = null)
{
    RunAssert("AssertTextContains", expected,
        () => GetText(),
        (actual, exp) => exp == null || (actual?.Contains(exp) ?? false),
        message);
}

// Boolean assertions - no string conversion needed
public void AssertExists(string? message = null)
{
    RunAssert("AssertExists", true, () => IsExists(), message);
}

public void AssertVisible(string? message = null)
{
    RunAssert("AssertVisible", true, () => IsVisible(), message);
}

public void AssertEnabled(string? message = null)
{
    RunAssert("AssertEnabled", true, () => IsEnabled(), message);
}

// Numeric assertions
public void AssertValueEquals(double? expected, string? message = null)
{
    RunAssert("AssertValueEquals", expected, () => GetValue(), message);
}

// With tolerance (custom comparison)
public void AssertValueNear(double? expected, double tolerance, string? message = null)
{
    RunAssert("AssertValueNear", expected,
        () => GetValue(),
        (actual, exp) => exp == null || Math.Abs((actual ?? 0) - exp.Value) <= tolerance,
        message);
}
```

### 6.3 Assertion Log Exit Method

```csharp
public interface ITestLogger : IDisposable
{
    // Assertion exit logging (includes actual and expected)
    void LogAssertExit(
        string testName,
        string pageName,
        string controlId,
        string assertType,
        string? actualValue,
        string? expectedValue,
        LogResult result,
        int durationMs,
        string? message = null);
    
    // ... other methods
}
```

---

## 7. Log Output with Entry/Exit

### 7.1 Extended CSV Format

```csv
Timestamp;Direction;TestName;PageName;ControlId;Action;Value;Expected;Result;DurationMs;Message
2026-01-07T15:30:45.100;→;LoginTest;LoginPage;UsernameEntry;Enter;john.doe;;;;
2026-01-07T15:30:45.150;←;LoginTest;LoginPage;UsernameEntry;Enter;;;Success;50;
2026-01-07T15:30:45.200;→;LoginTest;LoginPage;PasswordEntry;Enter;****;;;;
2026-01-07T15:30:45.250;←;LoginTest;LoginPage;PasswordEntry;Enter;;;Success;50;
2026-01-07T15:30:45.300;→;LoginTest;LoginPage;LoginButton;Click;;;;;
2026-01-07T15:30:45.400;←;LoginTest;LoginPage;LoginButton;Click;;;Success;100;
2026-01-07T15:30:48.000;→;LoginTest;HomePage;WelcomeLabel;AssertTextEquals;;Hello, John;;;
2026-01-07T15:30:48.050;←;LoginTest;HomePage;WelcomeLabel;AssertTextEquals;Hello, John;Hello, John;Success;50;
```

### 7.2 Direction Indicators

| Direction | Symbol | Meaning |
|-----------|--------|---------|
| Entry | → | Operation starting |
| Exit | ← | Operation completed |

---

## 8. Usage Patterns

### 8.1 Test Base Integration

```csharp
public abstract class UITestBase : IDisposable
{
    protected ITestLogger Logger { get; }
    protected string TestName { get; }
    
    protected UITestBase(string testName)
    {
        TestName = testName;
        var logPath = Path.Combine("logs", $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        Logger = new CsvTestLogger(logPath);
    }
    
    public void Dispose()
    {
        Logger.Dispose();
    }
}
```

### 8.2 Control Base with Logger

```csharp
public abstract class ControlBase : IControlObject
{
    protected readonly ITestLogger? _logger;
    protected readonly string _testName;
    protected readonly IPageObject _page;
    
    protected ControlBase(IPageObject page, Locator locator, ITestLogger? logger = null, string? testName = null)
    {
        _page = page;
        _logger = logger;
        _testName = testName ?? "UnknownTest";
        // ...
    }
    
    // Run and RunAssert methods available to all controls
}
```

---

## 9. Log Analysis

### 9.1 Filtering Failed Operations

```powershell
# PowerShell: Find all failures
Import-Csv -Delimiter ';' -Path 'logs/test.csv' | Where-Object { $_.Result -eq 'Fail' -or $_.Result -eq 'Error' }
```

### 9.2 Test Timeline

```powershell
# PowerShell: Get operation timeline for a test
Import-Csv -Delimiter ';' -Path 'logs/test.csv' | 
    Select-Object Timestamp, Direction, PageName, ControlId, Action, Result, DurationMs |
    Format-Table
```

---

## 10. Validation Rules

The Logging foundation is valid when:

- [ ] ITestLogger interface defines LogEntry and LogExit methods
- [ ] ControlBase provides Run() and RunAssert() convenience methods
- [ ] Run() handles try/catch and logs entry/exit automatically
- [ ] RunAssert() includes expected value (nullable) in logging
- [ ] CSV output includes Direction column (→ or ←)
- [ ] Duration is captured and logged on exit
- [ ] Timestamps are ISO 8601 format
- [ ] Thread-safe logging (lock on write)
- [ ] Flush and Dispose properly implemented

---

## Related Documents

- [221_002 Configuration](221_002_Configuration.spx.md)
- [133_003 Debugging Support](../../100_requirements/133_usability/133_003_DebuggingSupport.spx.md)
- [FR-500 Logging](../../100_requirements/120_functional/120_500_Logging.spx.md)
- [211_004 PageContext](../211_Modules/211_004_PageContext.spx.md)
