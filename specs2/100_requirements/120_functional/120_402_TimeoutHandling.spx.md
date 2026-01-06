# functional TimeoutHandling
- **id**: FR-402
- **title**: Timeout Handling
- **priority**: high
- **status**: draft
- **category**: Execution Context

The framework must provide unified timeout handling across all operations with configurable timeouts at multiple levels.

## capabilities

### TimeoutHierarchy
- **id**: FR-402.1
- **title**: Timeout hierarchy

Timeouts are resolved from multiple levels (lowest value wins):

| Level | Description | Example |
|-------|-------------|---------|
| 1. Method parameter | Per-call override | Click(timeoutMs: 5000) |
| 2. Control instance | Per-control setting | control.Timeout = 5000 |
| 3. Page instance | Per-page setting | page.DefaultTimeout = 10000 |
| 4. Context | Per-context setting | context.DefaultTimeout = 15000 |
| 5. Configuration | File/environment | "DefaultTimeout": 30000 |
| 6. Framework default | Built-in value | 30000ms |

Resolution: Use lowest level that has a value set.

### TimeoutByMethodType
- **id**: FR-402.2
- **title**: Timeout behavior by method type

Different method types handle timeout differently:

| Method Type | On Timeout |
|-------------|------------|
| Is* | Return null (no timeout applies, immediate) |
| Get* | Return null (no timeout applies, immediate) |
| Wait* | Return false |
| Check* | Throw TimeoutException |
| Assert* | Throw AssertionException |
| Action* (Click, Enter) | Throw TimeoutException |

### ElementSearchTimeout
- **id**: FR-402.3
- **title**: Element search timeout

Element search (finding element in UI tree) has separate timeout:
- Default: 5000ms
- Applies before operation timeout
- Total time = search time + operation time

Element search timeout configured separately from operation timeout.

### TimeoutSpecification
- **id**: FR-402.4
- **title**: Timeout specification format

Timeouts must be specified in milliseconds:
- Integer value
- Nullable for "use default"
- Must be positive (or null)

Examples:
```
method(timeoutMs: 5000)     // 5 seconds
method(timeoutMs: null)     // Use default
method()                    // Use default
```

### ZeroTimeout
- **id**: FR-402.5
- **title**: Zero timeout behavior

Zero timeout means "no waiting":
- Single immediate check
- No polling
- Fail immediately if condition not met
- Useful for "is it there right now?" checks

### TimeoutExceptionContent
- **id**: FR-402.6
- **title**: Timeout exception content

When timeout exception is thrown, include:
- Operation that timed out
- Timeout value used
- Element locator
- Last known element state
- Time elapsed
- Suggested actions

---

## relationships

- Part of [FR-401 Configuration](120_401_Configuration.spx.md) system
- Applied by [FR-301 Waiting](120_301_WaitingSynchronization.spx.md) operations
- Exception types from [FR-600 Exception Strategy](120_600_ExceptionStrategy.spx.md)

---

## constraints

- Timeout must never be exceeded significantly (small overhead acceptable)
- Timeout of 0 must not poll
- Negative timeout must throw validation error
- Timeout hierarchy must be deterministic
