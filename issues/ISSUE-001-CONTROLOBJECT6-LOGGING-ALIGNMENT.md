# ISSUE-001: ControlObject6 Logging Alignment

**Priority:** High  
**Status:** Open  
**Created:** January 5, 2026  
**Affects:** `Brinell.Maui.ControlObject6`, `Brinell.Blazor.ControlObject6`

---

## Summary

The ControlObject6 implementation uses direct `Console.WriteLine()` for logging instead of the `ITestLogger` (CSV Logger) pattern used by the original Controls implementation. This inconsistency reduces the framework's ability to produce structured test logs for CI/CD pipelines and test result analysis.

---

## Current State Analysis

### ✅ Controls Implementation (Correct Pattern)

The original Controls implementation in `Brinell.Maui.Controls` correctly uses the CSV Logger:

**File:** [src/Brinell.Maui/Infrastructure/AppiumTestContext.cs](../src/Brinell.Maui/Infrastructure/AppiumTestContext.cs)

```csharp
/// <summary>
/// Logger for CSV output. Set this to enable CSV logging.
/// </summary>
public ITestLogger? Logger { get; private set; }

/// <summary>
/// Set the CSV logger for this context.
/// </summary>
public void SetLogger(ITestLogger logger)
{
    Logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**File:** [src/Brinell.Maui/Controls/Base/ControlBase.cs](../src/Brinell.Maui/Controls/Base/ControlBase.cs)

```csharp
/// <summary>
/// Logger instance.
/// </summary>
protected ITestLogger? Logger => _context.Logger;

/// <summary>
/// Log an action being performed.
/// </summary>
protected void LogAction(string action, string? parameter = null, bool success = true)
{
    var paramStr = parameter != null ? $"(\"{parameter}\")" : "()";
    var statusStr = success ? "" : " [FAILED]";
    Log($"{action}{paramStr}{statusStr}");
    Logger?.LogAction(TestName, PageName, AutomationId, action, parameter);
}

/// <summary>
/// Log assertion success to CSV.
/// </summary>
protected void LogAssertPass(string assertType, string? actual, string? expected)
{
    Logger?.LogAssertPass(TestName, PageName, AutomationId, assertType, actual, expected);
}

/// <summary>
/// Log assertion failure, capture screenshot, and throw.
/// </summary>
protected void ThrowAssertionFailed(string assertType, string? actual, string? expected, string message)
{
    Logger.ThrowAssertionFailed(TestName, PageName, AutomationId, assertType, actual, expected, message, _context);
}
```

### ❌ ControlObject6 Implementation (Incorrect Pattern)

The ControlObject6 implementation uses direct Console output:

**File:** [src/Brinell.Maui/ControlObject6/Context/MauiTestContext.cs](../src/Brinell.Maui/ControlObject6/Context/MauiTestContext.cs)

```csharp
/// <inheritdoc />
public void Log(string? message)
{
    if (message is null) return;
    Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss.fff} - {message}");  // ❌ Direct console output
}

/// <inheritdoc />
public void LogError(string? message)
{
    if (message is null) return;
    Console.Error.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}");  // ❌ Direct console output
}
```

**File:** [src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs](../src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs)

```csharp
/// <inheritdoc />
public void Log(string? message)
{
    if (message is null) return;
    Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss.fff} - {message}");  // ❌ Direct console output
}

/// <inheritdoc />
public void LogError(string? message)
{
    if (message is null) return;
    Console.Error.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}");  // ❌ Direct console output
}
```

**File:** [src/Brinell.Maui/ControlObject6/Controls/ControlObjectBase.cs](../src/Brinell.Maui/ControlObject6/Controls/ControlObjectBase.cs)

```csharp
#region Logging

/// <summary>
/// Logs a message using the test context.
/// </summary>
protected void Log(string message)
{
    Context.Log($"[{GetType().Name}] {Locator}: {message}");  // Only logs to console, not CSV
}

#endregion
```

---

## Problems with Current ControlObject6 Approach

1. **No structured CSV output** - Actions, assertions, and waits are not recorded in CSV format
2. **No test/page context** - Missing TestName, PageName in log entries
3. **No result tracking** - Cannot distinguish Pass/Fail/Error results
4. **No expected/actual values** - Assertions don't record comparison values
5. **Missing logging helpers** - No `LogAction()`, `LogAssertPass()`, `LogWait()` methods
6. **No screenshot capture integration** - Failures don't trigger screenshot capture via logger extensions
7. **CI/CD incompatibility** - Console output is not easily parseable by test frameworks

---

## Required Changes

### 1. Update `ITestContext` Interface (ControlObject6)

**File:** `src/Brinell.Core/ControlObject6/Interfaces/ITestContext.cs`

```csharp
using Brinell.Core.Logging;

public interface ITestContext
{
    // ... existing members ...

    /// <summary>
    /// The current test name for logging context.
    /// </summary>
    string TestName { get; set; }

    /// <summary>
    /// CSV Logger instance. May be null if logging is disabled.
    /// </summary>
    ITestLogger? Logger { get; }

    /// <summary>
    /// Set the CSV logger for this context.
    /// </summary>
    void SetLogger(ITestLogger logger);

    // Keep the simple Log() for backward compatibility but deprecate
    [Obsolete("Use Logger methods directly for structured logging")]
    void Log(string? message);

    [Obsolete("Use Logger.LogError() for structured error logging")]
    void LogError(string? message);
}
```

### 2. Update `MauiTestContext` (ControlObject6)

**File:** `src/Brinell.Maui/ControlObject6/Context/MauiTestContext.cs`

```csharp
using Brinell.Core.Logging;

public class MauiTestContext : ITestContext
{
    private readonly AppiumDriver _driver;
    private ITestLogger? _logger;

    /// <summary>
    /// The current test name for logging.
    /// </summary>
    public string TestName { get; set; } = "Unknown";

    /// <summary>
    /// CSV Logger instance.
    /// </summary>
    public ITestLogger? Logger => _logger;

    /// <summary>
    /// Set the CSV logger for this context.
    /// </summary>
    public void SetLogger(ITestLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Log(string? message)
    {
        if (message is null) return;
        
        // Write to CSV if logger is set
        _logger?.LogInfo(TestName, CurrentPage?.Name ?? "Global", message);
        
        // Also write to Debug output for development
        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{TestName}] {message}");
    }

    /// <inheritdoc />
    public void LogError(string? message)
    {
        if (message is null) return;
        
        // For error logging without exception, use LogInfo with ERROR prefix
        _logger?.LogInfo(TestName, CurrentPage?.Name ?? "Global", $"ERROR: {message}");
        
        System.Diagnostics.Debug.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}");
    }
    
    // ... rest of implementation
}
```

### 3. Update `BlazorTestContext` (ControlObject6)

**File:** `src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs`

Same changes as MauiTestContext above.

### 4. Update `ControlObjectBase` (MAUI ControlObject6)

**File:** `src/Brinell.Maui/ControlObject6/Controls/ControlObjectBase.cs`

```csharp
using Brinell.Core.Logging;

public abstract class ControlObjectBase : IInteractiveControlObject
{
    // ... existing members ...

    /// <summary>
    /// The page name for logging (from Page or "Global").
    /// </summary>
    protected string PageName => Page?.Name ?? "Global";

    /// <summary>
    /// The test name for logging.
    /// </summary>
    protected string TestName => Context.TestName;

    /// <summary>
    /// Logger instance.
    /// </summary>
    protected ITestLogger? Logger => Context.Logger;

    #region Logging

    /// <summary>
    /// Log a message with control context (for debug output).
    /// </summary>
    protected void Log(string message)
    {
        Context.Log($"[{GetType().Name}] {Locator}: {message}");
    }

    /// <summary>
    /// Log an action being performed to CSV.
    /// </summary>
    protected void LogAction(string action, string? parameter = null)
    {
        Log($"{action}({parameter ?? ""})");
        Logger?.LogAction(TestName, PageName, Locator.Value, action, parameter);
    }

    /// <summary>
    /// Log assertion success to CSV.
    /// </summary>
    protected void LogAssertPass(string assertType, string? actual, string? expected)
    {
        Logger?.LogAssertPass(TestName, PageName, Locator.Value, assertType, actual, expected);
    }

    /// <summary>
    /// Log assertion failure to CSV and throw.
    /// Uses LoggingExtensions for consistent screenshot capture.
    /// </summary>
    protected void ThrowAssertionFailed(string assertType, string? actual, string? expected, string message)
    {
        // Use extension method if available, otherwise throw directly
        if (Logger != null)
        {
            Logger.LogAssertFail(TestName, PageName, Locator.Value, assertType, actual, expected, message);
        }
        throw new AssertionException(message, Locator.Value, assertType);
    }

    /// <summary>
    /// Log wait result to CSV.
    /// </summary>
    protected void LogWait(string waitType, bool success, int elapsedMs)
    {
        Logger?.LogWait(TestName, PageName, Locator.Value, waitType, success, elapsedMs);
    }

    #endregion

    // ... rest of implementation
}
```

### 5. Update `AsyncControlObjectBase` (Blazor ControlObject6)

**File:** `src/Brinell.Blazor/ControlObject6/Controls/AsyncControlObjectBase.cs`

Same logging helper methods as MAUI ControlObjectBase.

### 6. Update IAsyncTestContext Interface

**File:** `src/Brinell.Blazor/ControlObject6/Interfaces/IAsyncTestContext.cs`

Add the same `Logger`, `TestName`, and `SetLogger()` members.

---

## Documentation Updates Required

### REQ Documents

**File:** `specs/REQ-002-non-functional-requirements.md`

Add explicit requirement:

```markdown
### NFR-8.5: Structured Logging

- Framework MUST provide CSV logging for all control actions, assertions, and waits
- Framework MUST support `ITestLogger` interface for structured log output
- Framework SHOULD support both CSV file and optional console output
- Framework SHOULD NOT write directly to Console.WriteLine in production code
- Environment variables SHOULD control log output mode:
  - `UITEST_LOG_OUTPUT`: "csv" | "console" | "both"
  - `UITEST_CONSOLE_FORMAT`: "formatted" | "csv"
```

### SPEC Documents

**File:** `specs/SPEC-006-003b-FOUNDATION.md`

Update the Logging section:

```markdown
#region Logging

/// <summary>The page name for logging.</summary>
protected string PageName => Page?.Name ?? "Global";

/// <summary>The test name for logging.</summary>
protected string TestName => Context.TestName;

/// <summary>CSV Logger instance.</summary>
protected ITestLogger? Logger => Context.Logger;

/// <summary>Log a message with control context.</summary>
protected void Log(string message)
{
    Context.Log($"[{GetType().Name}] {Locator}: {message}");
}

/// <summary>Log an action to CSV.</summary>
protected void LogAction(string action, string? parameter = null)
{
    Log($"{action}({parameter ?? ""})");
    Logger?.LogAction(TestName, PageName, Locator.Value, action, parameter);
}

/// <summary>Log assertion success to CSV.</summary>
protected void LogAssertPass(string assertType, string? actual, string? expected)
{
    Logger?.LogAssertPass(TestName, PageName, Locator.Value, assertType, actual, expected);
}

/// <summary>Log assertion failure and throw.</summary>
protected void ThrowAssertionFailed(string assertType, string? actual, string? expected, string message)
{
    Logger?.LogAssertFail(TestName, PageName, Locator.Value, assertType, actual, expected, message);
    throw new AssertionException(message, Locator.Value, assertType);
}

/// <summary>Log wait result to CSV.</summary>
protected void LogWait(string waitType, bool success, int elapsedMs)
{
    Logger?.LogWait(TestName, PageName, Locator.Value, waitType, success, elapsedMs);
}

#endregion
```

**File:** `specs/SPEC-006-003b-INDEX.md`

Update section "3. Logging at Every Level":

```markdown
### 3. Logging at Every Level

All operations log via structured CSV logging:

```csharp
public virtual void Click(int? timeoutMs = null)
{
    Log("Click()");  // Debug output
    LogAction("Click");  // CSV logging
    CheckVisible(true, timeoutMs);
    CheckEnabled(true, timeoutMs);
    FindElementRequired(timeoutMs).Click();
}

public void AssertTextEquals(string expected, int? timeoutMs = null)
{
    var actual = GetText(timeoutMs);
    if (actual != expected)
    {
        ThrowAssertionFailed("TextEquals", actual, expected, 
            $"Expected '{expected}' but got '{actual}'");
    }
    LogAssertPass("TextEquals", actual, expected);
}
```

**CSV Output Format:**
```
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
2026-01-05 10:30:45.123;LoginTest;LoginPage;SubmitButton;Click;;Ok;
2026-01-05 10:30:45.234;LoginTest;LoginPage;Username;Assert.TextEquals;admin;admin;Ok;
```
```

---

## Implementation Checklist

### Core Changes
- [ ] Update `ITestContext` (ControlObject6) to include `Logger`, `TestName`, `SetLogger()`
- [ ] Update `IAsyncTestContext` (ControlObject6) to include `Logger`, `TestName`, `SetLogger()`
- [ ] Update `MauiTestContext` to implement ITestLogger integration
- [ ] Update `BlazorTestContext` to implement ITestLogger integration

### Base Class Changes
- [ ] Add `LogAction()`, `LogAssertPass()`, `LogWait()`, `ThrowAssertionFailed()` to `ControlObjectBase` (MAUI)
- [ ] Add same methods to `AsyncControlObjectBase` (Blazor)
- [ ] Add same methods to `PageObjectBase` (MAUI)
- [ ] Add same methods to `AsyncPageObjectBase` (Blazor)

### Documentation Updates
- [ ] Update `REQ-002-non-functional-requirements.md` with NFR-8.5
- [ ] Update `SPEC-006-003b-FOUNDATION.md` logging section
- [ ] Update `SPEC-006-003b-INDEX.md` logging examples
- [ ] Update `SPEC-006-003b-PAGE.md` logging section

### Tests
- [ ] Add unit tests for CSV logging integration
- [ ] Verify log output in existing sample tests
- [ ] Ensure backward compatibility with console-only mode

---

## Migration Path

For existing code using ControlObject6:

1. **No breaking changes** - The `Log()` method signature remains the same
2. **Opt-in CSV logging** - Set logger in test setup: `context.SetLogger(CsvTestLogger.CreateDefault())`
3. **Environment control** - Use `UITEST_LOG_OUTPUT=both` for development

---

## Related Files

| Category | Files |
|----------|-------|
| Correct Implementation | `src/Brinell.Maui/Controls/Base/ControlBase.cs` |
| Correct Implementation | `src/Brinell.Maui/Infrastructure/AppiumTestContext.cs` |
| Needs Update | `src/Brinell.Maui/ControlObject6/Context/MauiTestContext.cs` |
| Needs Update | `src/Brinell.Maui/ControlObject6/Controls/ControlObjectBase.cs` |
| Needs Update | `src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs` |
| Needs Update | `src/Brinell.Blazor/ControlObject6/Controls/AsyncControlObjectBase.cs` |
| Core Logger | `src/Brinell.Core/Logging/ITestLogger.cs` |
| Core Logger | `src/Brinell.Core/Logging/CsvTestLogger.cs` |
| Core Extensions | `src/Brinell.Core/Logging/LoggingExtensions.cs` |

---

## CSV Logger Reference

The existing `ITestLogger` interface provides:

| Method | Purpose |
|--------|---------|
| `LogAction()` | Record control actions (Click, Enter, etc.) |
| `LogAssertPass()` | Record successful assertions |
| `LogAssertFail()` | Record failed assertions |
| `LogWait()` | Record wait operation results |
| `LogNavigation()` | Record page navigation |
| `LogInfo()` | Record informational messages |
| `LogError()` | Record errors with exceptions |
| `Flush()` | Flush buffered entries to disk |

The `CsvTestLogger` supports:
- **Environment variables** for output control
- **Formatted console output** for readability
- **CSV file output** for CI/CD parsing
- **Auto-flush** every 10 entries

---

## Acceptance Criteria

1. ✅ ControlObject6 MAUI context supports `ITestLogger`
2. ✅ ControlObject6 Blazor context supports `ITestLogger`
3. ✅ All control actions are logged to CSV when logger is set
4. ✅ All assertions record expected/actual values
5. ✅ Wait operations log elapsed time and success/failure
6. ✅ No direct `Console.WriteLine()` in production code paths
7. ✅ Backward compatible - works without logger set
8. ✅ Documentation updated (REQ + SPEC)
