# 130_003 Test Execution Timeout

## quality TestExecutionTimeout

- **attribute**: Reliability
- **requirement**: Test-level execution timeouts prevent hung tests
- **priority**: high

---

## Description

This requirement ensures that long-running or hung tests are terminated gracefully, preventing CI/CD pipeline hangs and resource exhaustion.

---

## Configuration Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `TestTimeoutMs` | 120000 (2 min) | Maximum test execution time |
| `SetupTimeoutMs` | 60000 (1 min) | Maximum setup time |
| `TeardownTimeoutMs` | 30000 (30 sec) | Maximum teardown time |

---

## Timeout Behavior

When timeout is exceeded:

1. Test MUST be terminated
2. Application SHOULD be force-closed
3. Failure MUST be logged with timeout reason
4. Screenshot SHOULD be captured before termination

---

## Implementation Options

### Option 1: xUnit Timeout Attribute

```csharp
[Fact(Timeout = 120000)]
public void LongRunningTest()
{
    // Test code
}
```

### Option 2: Custom Test Wrapper

```csharp
public class UITestBase
{
    protected async Task RunWithTimeout(Func<Task> testAction)
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        try
        {
            await testAction().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            await CaptureTimeoutDiagnostics();
            throw new TestTimeoutException($"Test exceeded {TestTimeout}ms timeout");
        }
    }
}
```

### Option 3: Process-Level Watchdog

External process monitors test execution and terminates hung tests.

---

## Scope Clarification

**Test-level timeouts** (this requirement):
- Apply to entire test execution
- Prevent hung tests in CI/CD
- Force terminate if exceeded

**Element-level timeouts** (FR-005):
- Apply to individual operations (WaitFor, Check, etc.)
- Part of normal test flow
- Return failure result, don't terminate test

---

## Related

- [FR-005 Waiting and Synchronization](../120_functional/120_005_WaitingSynchronization.spx.md)
- [NFR-REL-001 Test Stability](130_001_TestStability.spx.md)
- [NFR-COMPAT-003 CI/CD Integration](130_009_CICDIntegration.spx.md)

---

## Source

REQ-002-non-functional-requirements.md § NFR-REL-003
