# 221_004 Timeout Management

## foundation Timeout

- **title**: Timeout Strategies and Configuration
- **package**: Brinell.Core
- **purpose**: Consistent timeout handling across all wait and polling operations

---

## Description

The Timeout foundation defines strategies for managing wait times and polling intervals. Timeouts apply at multiple levels: element operations, page loads, and test execution. All timeout values are configurable and follow a consistent resolution pattern.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Timeout Levels

### 1.1 Hierarchy

```
┌─────────────────────────────────────────────────────┐
│                TEST LEVEL                           │
│  TestTimeoutMs = 120000 (2 min)                    │
│  - Maximum total test execution time               │
│  - Prevents hung tests in CI/CD                    │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │           PAGE LEVEL                         │   │
│  │  PageLoadTimeoutMs = 30000 (30 sec)         │   │
│  │  - WaitForPage() operations                  │   │
│  │  - Full page load completion                 │   │
│  │                                              │   │
│  │  ┌─────────────────────────────────────┐    │   │
│  │  │       ELEMENT LEVEL                  │    │   │
│  │  │  DefaultTimeoutMs = 10000 (10 sec)  │    │   │
│  │  │  - WaitVisible, WaitExists          │    │   │
│  │  │  - Element find operations          │    │   │
│  │  │                                     │    │   │
│  │  │  ShortTimeoutMs = 3000 (3 sec)     │    │   │
│  │  │  - Quick state checks               │    │   │
│  │  │  - IsExists, IsVisible              │    │   │
│  │  └─────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### 1.2 Timeout Categories

| Category | Setting | Default | Use Case |
|----------|---------|---------|----------|
| **Element** | DefaultTimeoutMs | 10000ms | Wait for element state changes |
| **Quick** | ShortTimeoutMs | 3000ms | Quick checks, Is* methods |
| **Page** | PageLoadTimeoutMs | 30000ms | Full page load |
| **Test** | TestTimeoutMs | 120000ms | Total test execution |
| **Setup** | SetupTimeoutMs | 60000ms | Test fixture initialization |
| **Teardown** | TeardownTimeoutMs | 30000ms | Test cleanup |
| **Animation** | AnimationDelayMs | 500ms | UI animation completion |
| **Polling** | PollingIntervalMs | 100ms | Condition check interval |

---

## 2. Timeout Settings Class

### 2.1 TimeoutSettings

```csharp
public class TimeoutSettings
{
    /// <summary>
    /// Default timeout for element wait operations (10 seconds).
    /// </summary>
    public int DefaultWait { get; set; } = 10000;
    
    /// <summary>
    /// Short timeout for quick checks (3 seconds).
    /// </summary>
    public int ShortWait { get; set; } = 3000;
    
    /// <summary>
    /// Timeout for page load operations (30 seconds).
    /// </summary>
    public int PageLoad { get; set; } = 30000;
    
    /// <summary>
    /// Timeout for element find operations (5 seconds).
    /// </summary>
    public int ElementFind { get; set; } = 5000;
    
    /// <summary>
    /// Delay for animation completion (500ms).
    /// </summary>
    public int Animation { get; set; } = 500;
    
    /// <summary>
    /// Polling interval for wait operations (100ms).
    /// </summary>
    public int PollingInterval { get; set; } = 100;
    
    /// <summary>
    /// Default timeout settings.
    /// </summary>
    public static TimeoutSettings Default => new();
    
    /// <summary>
    /// Fast timeout settings for quick test execution.
    /// </summary>
    public static TimeoutSettings Fast => new()
    {
        DefaultWait = 5000,
        ShortWait = 1000,
        PageLoad = 15000,
        ElementFind = 2000,
        Animation = 250,
        PollingInterval = 100
    };
    
    /// <summary>
    /// Slow timeout settings for unstable environments.
    /// </summary>
    public static TimeoutSettings Slow => new()
    {
        DefaultWait = 20000,
        ShortWait = 5000,
        PageLoad = 60000,
        ElementFind = 10000,
        Animation = 1000,
        PollingInterval = 500
    };
}
```

---

## 3. Timeout Resolution

### 3.1 Resolution Priority

When resolving timeouts, values are checked in order:

1. **Method parameter** - Explicit `timeoutMs` passed to method
2. **Context timeout** - Platform context's DefaultTimeoutMs
3. **Configuration timeout** - UITestConfiguration.DefaultTimeoutMs
4. **Framework default** - Hard-coded fallback value

### 3.2 Resolution Implementation

```csharp
public int ResolveTimeout(int? methodTimeout)
{
    // 1. Method parameter takes precedence
    if (methodTimeout.HasValue)
        return methodTimeout.Value;
    
    // 2. Context timeout
    if (_context?.DefaultTimeoutMs > 0)
        return _context.DefaultTimeoutMs;
    
    // 3. Configuration timeout
    if (_config?.DefaultTimeoutMs > 0)
        return _config.DefaultTimeoutMs;
    
    // 4. Framework default
    return 10000;
}
```

### 3.3 Usage in Controls

```csharp
public bool WaitVisible(bool visible, int? timeoutMs = null)
{
    var timeout = ResolveTimeout(timeoutMs);
    var stopwatch = Stopwatch.StartNew();
    
    while (stopwatch.ElapsedMilliseconds < timeout)
    {
        if (IsVisible() == visible)
            return true;
        Thread.Sleep(_pollingInterval);
    }
    
    return false;
}
```

---

## 4. Polling Strategy

### 4.1 Fixed Interval Polling

Default strategy: check condition at fixed intervals.

```csharp
public bool Poll(Func<bool> condition, int timeoutMs, int intervalMs = 250)
{
    var stopwatch = Stopwatch.StartNew();
    
    while (stopwatch.ElapsedMilliseconds < timeoutMs)
    {
        if (condition())
            return true;
        Thread.Sleep(intervalMs);
    }
    
    return false;
}
```

### 4.2 Exponential Backoff

For resource-intensive checks, use exponential backoff:

```csharp
public bool PollWithBackoff(Func<bool> condition, int timeoutMs, int initialIntervalMs = 100, int maxIntervalMs = 1000)
{
    var stopwatch = Stopwatch.StartNew();
    var interval = initialIntervalMs;
    
    while (stopwatch.ElapsedMilliseconds < timeoutMs)
    {
        if (condition())
            return true;
            
        Thread.Sleep(interval);
        interval = Math.Min(interval * 2, maxIntervalMs);
    }
    
    return false;
}
```

---

## 5. Method Patterns

### 5.1 Wait Methods (Return bool)

Wait methods return boolean and do NOT throw on timeout:

```csharp
// Returns true if condition met, false if timeout
public bool WaitExists(bool exists, int? timeoutMs = null)
{
    var timeout = ResolveTimeout(timeoutMs);
    return Poll(() => IsExists() == exists, timeout);
}

public bool WaitVisible(bool visible, int? timeoutMs = null)
{
    var timeout = ResolveTimeout(timeoutMs);
    return Poll(() => IsVisible() == visible, timeout);
}

public bool WaitEnabled(bool enabled, int? timeoutMs = null)
{
    var timeout = ResolveTimeout(timeoutMs);
    return Poll(() => IsEnabled() == enabled, timeout);
}
```

### 5.2 Assert Methods (Wait, Then Verify)

Assert methods wait for a condition and throw AssertionException if not met within timeout:

```csharp
public void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
{
    if (expected == null) return;  // Nullable skip pattern
    
    if (!WaitExists(expected, timeoutMs))
    {
        throw new AssertionException(
            message ?? $"Element '{Locator}' existence did not become {expected}",
            Locator.Value,
            "AssertExists");
    }
}

public void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
{
    if (expected == null) return;  // Nullable skip pattern
    
    if (!WaitVisible(expected, timeoutMs))
    {
        throw new AssertionException(
            message ?? $"Element '{Locator}' visibility did not become {expected}",
            Locator.Value,
            "AssertVisible");
    }
}
```

> **Note:** Assert methods include waiting by default (unlike immediate assert patterns in some frameworks). This consolidates Wait+Check patterns into a single Assert pattern that waits before verifying.

---

## 6. Test-Level Timeout

### 6.1 Test Execution Timeout

Prevent hung tests from blocking CI/CD:

```csharp
[Fact(Timeout = 120000)]  // xUnit native timeout
public async Task LongRunningTest()
{
    // Test code
}
```

### 6.2 Custom Timeout Wrapper

```csharp
public abstract class UITestBase
{
    protected int TestTimeoutMs { get; set; } = 120000;
    
    protected async Task RunWithTimeout(Func<Task> testAction)
    {
        using var cts = new CancellationTokenSource(TestTimeoutMs);
        
        try
        {
            await testAction().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            await CaptureTimeoutDiagnostics();
            throw new UITestTimeoutException(
                $"Test exceeded {TestTimeoutMs}ms timeout",
                "Test",
                TestTimeoutMs,
                "TestExecution");
        }
    }
    
    private async Task CaptureTimeoutDiagnostics()
    {
        // Capture screenshot
        // Dump element tree
        // Log current state
    }
}
```

---

## 7. Platform-Specific Considerations

### 7.1 MAUI/Mobile

Mobile platforms often need longer timeouts due to:
- Device startup time
- Network latency
- Animation complexity

```csharp
var mauiTimeouts = new TimeoutSettings
{
    DefaultWait = 15000,   // 15 seconds
    PageLoad = 45000,      // 45 seconds
    Animation = 1000       // 1 second
};
```

### 7.2 Blazor/Web

Web platforms may have varying performance:

```csharp
var blazorTimeouts = new TimeoutSettings
{
    DefaultWait = 10000,
    PageLoad = 30000,
    Animation = 500
};
```

### 7.3 WPF/Desktop

Desktop apps are typically faster:

```csharp
var wpfTimeouts = TimeoutSettings.Fast;  // Use fast profile
```

---

## 8. Best Practices

### 8.1 Avoid Arbitrary Waits

```csharp
// ❌ BAD: Arbitrary sleep
Thread.Sleep(2000);
button.Click();

// ✅ GOOD: Wait for specific condition
button.AssertVisible(true);
button.Click();
```

### 8.2 Use Appropriate Timeout Level

```csharp
// ❌ BAD: Using test timeout for element wait
button.WaitExists(true, 120000);

// ✅ GOOD: Use element-level timeout
button.WaitExists(true);  // Uses DefaultWait (10s)

// ✅ GOOD: Override when needed
button.WaitExists(true, 20000);  // Slower element
```

### 8.3 Wait for Something, Not After

```csharp
// ❌ BAD: Wait after action
button.Click();
Thread.Sleep(3000);  // Wait for something to happen

// ✅ GOOD: Wait for expected result
button.Click();
nextPage.WaitForPage();  // Wait for page to load
```

### 8.4 Chain Waits, Don't Nest

```csharp
// ❌ BAD: Nested timeout logic
if (!button.WaitVisible(true, 5000))
{
    if (!button.WaitVisible(true, 10000))
    {
        throw new Exception("Button not visible");
    }
}

// ✅ GOOD: Single assert with appropriate timeout
button.AssertVisible(true, timeoutMs: 10000);
```

---

## 9. Diagnostic Output

### 9.1 Timeout Logging

When timeouts occur, log diagnostic information:

```csharp
public bool WaitVisible(bool visible, int? timeoutMs = null)
{
    var timeout = ResolveTimeout(timeoutMs);
    var stopwatch = Stopwatch.StartNew();
    
    _logger?.LogInfo(_testName, _page.Name, 
        $"Waiting for '{AutomationId}' visibility={visible}, timeout={timeout}ms");
    
    while (stopwatch.ElapsedMilliseconds < timeout)
    {
        var current = IsVisible();
        if (current == visible)
        {
            _logger?.LogWait(_testName, _page.Name, AutomationId, 
                "WaitVisible", true, (int)stopwatch.ElapsedMilliseconds);
            return true;
        }
        Thread.Sleep(_pollingInterval);
    }
    
    _logger?.LogWait(_testName, _page.Name, AutomationId, 
        "WaitVisible", false, (int)stopwatch.ElapsedMilliseconds);
    return false;
}
```

---

## 10. Validation Rules

The Timeout foundation is valid when:

- [ ] Timeout values follow hierarchy (Test > Page > Element)
- [ ] Resolution priority is Method > Context > Config > Default
- [ ] Wait* methods return bool (never throw on timeout)
- [ ] Assert* methods wait then throw AssertionException on timeout
- [ ] Polling interval is configurable (default 100ms)
- [ ] Platform-specific timeouts can override defaults
- [ ] Test-level timeouts prevent hung tests
- [ ] Timeout diagnostics are logged

---

## Related Documents

- [221_002 Configuration](221_002_Configuration.spx.md)
- [221_003 ExceptionHandling](221_003_ExceptionHandling.spx.md)
- [130_003 Test Execution Timeout](../../100_requirements/130_quality/130_003_TestExecutionTimeout.spx.md)
- [211_001 Interfaces](../211_Modules/211_001_Interfaces.spx.md)
