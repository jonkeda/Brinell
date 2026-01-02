# 12. Standardized Logging

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d12_StandardizedLogging_CodeExamples.md](21d12_StandardizedLogging_CodeExamples.md)  
**Previous:** [Cloud Provider Support](21d11_CloudProviderSupport.md)  
**Related:** [21g1 Logging Refactoring](21g1_LoggingRefactoring.md) | [21g3 Console Logging for Agents](21g3_ConsoleLoggingForAgents.md)

---

## 12.1 Overview

The framework uses a standardized CSV logging format for consistent test execution tracking, debugging, and reporting. The logging system supports multiple output targets (file, console, or both) for different use cases.

---

## 12.2 CSV Format Specification

### 12.2.1 Format

```
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
```

### 12.2.2 Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `Timestamp` | `datetime` | Event timestamp | `2024-12-23 10:30:45.123` |
| `TestName` | `string` | Test method name | `Login_ValidCredentials_Succeeds` |
| `PageName` | `string` | Page object name | `LoginPage` |
| `ControlId` | `string?` | Control automation ID | `txtUsername` |
| `Action` | `string` | Action performed | `Enter`, `Click`, `Assert.Text` |
| `Value` | `string?` | Actual/input value | `admin` |
| `ExpectedValue` | `string?` | Expected value (assertions) | `Welcome, admin!` |
| `Result` | `LogResult` | Action outcome | `Ok`, `Fail`, `Error`, `Info` |
| `Message` | `string?` | Additional info/error | `elapsed=500ms` |

### 12.2.3 LogResult Values

| Value | Description | Use Case |
|-------|-------------|----------|
| `Ok` | Action succeeded | Actions, assertions that pass |
| `Fail` | Action failed | Assertion failures, wait timeouts |
| `Error` | Exception occurred | Exceptions before being thrown |
| `Info` | Informational | Navigation, debug messages |
| `Skip` | Skipped | Conditional skips |

---

## 12.3 Action Types

### 12.3.1 Control Actions

| Action | Description | Value | ExpectedValue |
|--------|-------------|-------|---------------|
| `Click` | Click on element | - | - |
| `DoubleClick` | Double-click | - | - |
| `RightClick` | Context menu click | - | - |
| `Enter` | Text input | Text entered | - |
| `ClearAndEnter` | Clear and type | Text entered | - |
| `Select` | Select item | Item selected | - |
| `Toggle` | Toggle state | - | - |
| `Check` | Set checked | `true`/`false` | - |
| `Uncheck` | Set unchecked | `true`/`false` | - |

### 12.3.2 Wait Actions (with elapsed time)

| Action | Description | Message |
|--------|-------------|---------|
| `Wait.Visible` | Wait for visibility | `elapsed=500ms` |
| `Wait.Enabled` | Wait for enabled | `elapsed=200ms` |
| `Wait.Ready` | Page ready | `elapsed=1500ms` |
| `Wait.Displayed` | Page displayed | `elapsed=800ms` |
| `Wait.NotBusy` | Wait for not busy | `elapsed=2000ms` |

### 12.3.3 Assert Actions

| Action | Description | Value | ExpectedValue |
|--------|-------------|-------|---------------|
| `Assert.Visible` | Assert visibility | `true`/`false` | `true`/`false` |
| `Assert.Enabled` | Assert enabled | `true`/`false` | `true`/`false` |
| `Assert.Text` | Assert text content | Actual text | Expected text |
| `Assert.Checked` | Assert checked state | `true`/`false` | `true`/`false` |
| `Assert.Displayed` | Assert page displayed | `true`/`false` | `true` |
| `Assert.SelectedText` | Assert selection | Actual | Expected |
| `Assert.Value` | Assert numeric value | Actual | Expected |

### 12.3.4 Check Actions (preconditions)

| Action | Description | Message |
|--------|-------------|---------|
| `Check.Visible` | Verify visible before action | Error message if fails |
| `Check.Enabled` | Verify enabled before action | Error message if fails |
| `Check.Enter` | Verify can enter text | Error message if fails |
| `Check.Toggle` | Verify can toggle | Error message if fails |

### 12.3.5 Navigation Actions

| Action | Description | Value |
|--------|-------------|-------|
| `Navigate` | Navigate to page | Target page |
| `CheckDisplayed` | Verify page displayed | - |
| `CheckReady` | Verify page ready | - |

### 12.3.6 System Actions

| Action | Description | Value |
|--------|-------------|-------|
| `Launch` | Application launched | App path |
| `Close` | Application closed | - |
| `Screenshot` | Screenshot taken | File path |
| `Cleanup` | Test cleanup | - |
| `Info` | Informational | Message |

---

## 12.4 Output Configuration

### 12.4.1 LogOutput Modes

| Mode | File | Console | Use Case |
|------|------|---------|----------|
| `CsvOnly` | ✅ | ❌ | CI/CD pipelines (default) |
| `ConsoleOnly` | ❌ | ✅ | Quick debugging, no disk clutter |
| `Both` | ✅ | ✅ | **AI agent runs** - real-time visibility |

### 12.4.2 ConsoleFormat Options

| Format | Output | Use Case |
|--------|--------|----------|
| `Formatted` | `[10:15:01] LoginPage.btnSubmit Click Ok` | Human/AI readable (default) |
| `Csv` | `2024-12-23 10:15:01;TestLogin;LoginPage;...` | Copy-paste to analyze |

### 12.4.3 Environment Variables

| Variable | Values | Default | Description |
|----------|--------|---------|-------------|
| `UITEST_LOG_OUTPUT` | `csv`, `console`, `both` | `csv` | Where to write logs |
| `UITEST_CONSOLE_FORMAT` | `formatted`, `csv` | `formatted` | Console output format |
| `LOG_OUTPUT_PATH` | Directory path | `./logs` | CSV file directory |

### 12.4.4 Usage Examples

```powershell
# Normal CI/CD (default - file only)
dotnet test

# AI Agent run (both outputs)
$env:UITEST_LOG_OUTPUT = "both"
dotnet test --filter "TestLogin"

# Quick debugging (console only)
$env:UITEST_LOG_OUTPUT = "console"
dotnet test --filter "TestLogin"

# Raw CSV to console
$env:UITEST_LOG_OUTPUT = "console"
$env:UITEST_CONSOLE_FORMAT = "csv"
dotnet test
```

---

## 12.5 ITestLogger Interface

```csharp
public interface ITestLogger : IDisposable
{
    // Core log method
    void Log(string testName, string pageName, string controlId, string action,
             string? value, string? expectedValue, LogResult result, string? message);
    
    // Action logging (Result = Ok)
    void LogAction(string testName, string pageName, string controlId, string action, string? value = null);
    
    // Assertion logging
    void LogAssertPass(string testName, string pageName, string controlId, string assertType,
                       string? actualValue, string? expectedValue);
    void LogAssertFail(string testName, string pageName, string controlId, string assertType,
                       string? actualValue, string? expectedValue, string? message = null);
    
    // Wait logging (with elapsed time)
    void LogWait(string testName, string pageName, string controlId, string waitType,
                 bool success, int elapsedMs);
    
    // Navigation logging
    void LogNavigation(string testName, string sourcePage, string targetPage);
    void LogNavigation(string testName, string pageName, string pageId, string action, string? value = null);
    
    // Info and error logging
    void LogInfo(string testName, string pageName, string message);
    void LogError(string testName, string pageName, string controlId, string action, Exception ex);
    
    void Flush();
}
```

---

## 12.6 Log-and-Throw Pattern

All exceptions are logged to CSV **before** being thrown, ensuring complete traceability:

```csharp
// Extension methods in LoggingExtensions.cs
public static class LoggingExtensions
{
    // Logs failure then throws CheckFailedException
    public static CheckFailedException ThrowCheckFailed(this ITestLogger? logger, ...);
    
    // Logs failure then throws AssertionException  
    public static AssertionException ThrowAssertionFailed(this ITestLogger? logger, ...);
    
    // Logs failure then throws PageNotReadyException
    public static PageNotReadyException ThrowPageNotReady(this ITestLogger? logger, ...);
    
    // Logs failure then throws PageNotDisplayedException
    public static PageNotDisplayedException ThrowPageNotDisplayed(this ITestLogger? logger, ...);
}
```

**Usage in controls:**
```csharp
// Instead of bare throw:
if (!IsVisible())
    throw new CheckFailedException("Not visible", AutomationId, "Click");

// Use log-and-throw:
if (!IsVisible())
    ThrowCheckFailed("Click", $"Element '{AutomationId}' not visible.");
```

---

## 12.7 Console Output Example

When `UITEST_LOG_OUTPUT=both`:

```
[10:15:01.123] LoginPage.txtUsername    Enter              admin        Ok
[10:15:01.456] LoginPage.txtPassword    Enter              ****         Ok
[10:15:01.789] LoginPage.btnSubmit      Click                           Ok
[10:15:02.234] LoginPage                Wait.Ready                      Ok    elapsed=445ms
[10:15:02.456] LoginPage                Navigate           HomePage     Info
[10:15:03.789] HomePage.MainWindow      Wait.Displayed                  Fail  elapsed=1333ms
[10:15:03.790] HomePage                 Assert.Displayed   false        Fail  Page not displayed
```

Color coding:
- 🟢 Green: `Ok` results
- 🔴 Red: `Fail` and `Error` results
- 🔵 Cyan: `Info` results

---

## 12.8 File Management

### 12.8.1 File Naming

```
logs/
├── uitest_20241223_140523.csv         # Session-specific
├── uitest_TestLogin_20241223.csv      # Test-specific
└── uitest_20241223.csv                # Daily aggregate
```

### 12.8.2 Sample Output

```csv
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
2024-12-23 14:05:23.123;TestLogin;LoginPage;txtUsername;Enter;admin;;Ok;
2024-12-23 14:05:23.456;TestLogin;LoginPage;txtPassword;Enter;****;;Ok;
2024-12-23 14:05:23.789;TestLogin;LoginPage;btnSubmit;Click;;;Ok;
2024-12-23 14:05:24.234;TestLogin;LoginPage;;Wait.Ready;;;Ok;elapsed=445ms
2024-12-23 14:05:24.456;TestLogin;LoginPage;;Navigate;HomePage;;Info;
2024-12-23 14:05:25.789;TestLogin;HomePage;;Assert.Displayed;true;true;Ok;
```

---

## 12.9 Best Practices

### 12.9.1 DO

- ✅ Use `UITEST_LOG_OUTPUT=both` for AI agent test runs
- ✅ Always log before throwing exceptions (use ThrowXxx methods)
- ✅ Include elapsed time in wait operations
- ✅ Use consistent action naming (`Assert.Visible`, `Wait.Ready`)
- ✅ Log actions AFTER success, not before
- ✅ Mask sensitive data (passwords)

### 12.9.2 DON'T

- ❌ Log passwords/secrets in clear text
- ❌ Throw exceptions without logging first
- ❌ Log actions before they complete
- ❌ Use inconsistent action name formats
- ❌ Skip console logging in interactive/agent scenarios

---

*Next: [Application UITest Projects](21d13_ApplicationUITestProjects.md)*
