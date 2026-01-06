# functional TestIsolation
- **id**: FR-700
- **title**: Test Isolation
- **priority**: high
- **status**: draft
- **category**: Test Execution

The framework must ensure tests can execute independently without interference.

## capabilities

### OrderIndependence
- **id**: FR-700.1
- **title**: Order-independent tests

Tests must be executable in any order:
- No implicit dependencies between tests
- No required execution sequence
- Each test sets up own preconditions
- Each test cleans up own state

### PerTestInstances
- **id**: FR-700.2
- **title**: Per-test application instances

In per-test mode:
- Fresh application/browser instance per test
- Clean state guaranteed
- No leakage between tests
- Instance disposed after test

This is the default and recommended mode.

### SharedInstanceMode
- **id**: FR-700.3
- **title**: Shared application instance mode

For expensive startup scenarios:
- Single application instance for test fixture
- State reset between tests
- Automatic restart on reset failure

State reset includes:
- Navigate to starting page
- Clear cookies/storage (web)
- Clear entered data
- Close modal dialogs

### TestDataIsolation
- **id**: FR-700.4
- **title**: Test data isolation

Test data must be isolated:
- Each test uses own data set
- No shared mutable data
- Test fixtures provide isolated data
- Data cleanup after test

### ParallelExecution
- **id**: FR-700.5
- **title**: Parallel execution support

Framework must support parallel test execution:

| Requirement | Description |
|-------------|-------------|
| Thread-safe page objects | No shared mutable state |
| Isolated drivers | Each parallel test has own driver |
| Parallel-safe logging | No interleaved or corrupted logs |
| Per-test log files | Recommended for parallel |

### ResourceIsolation
- **id**: FR-700.6
- **title**: Resource isolation

Tests must not compete for resources:
- Separate browser profiles (web)
- Separate app data directories
- Non-overlapping ports (if applicable)
- Unique file names for outputs

### FailureIsolation
- **id**: FR-700.7
- **title**: Failure isolation

One test's failure must not affect others:
- Exception in test contained
- Resources cleaned up
- Next test gets clean state
- Parallel tests continue

### StateLeakPrevention
- **id**: FR-700.8
- **title**: State leak prevention

Prevent common state leaks:
- Static fields cleared between tests
- Singleton instances reset
- Cached data invalidated
- Background tasks terminated

---

## relationships

- Context modes in [FR-400 Test Context](120_400_TestContext.spx.md)
- Log file isolation in [FR-501 Log File Management](120_501_LogFileManagement.spx.md)
- Driver isolation via [FR-011 Driver Abstraction](120_011_DriverAbstraction.spx.md)

---

## constraints

- Test isolation must work with test framework parallelism
- Shared instance mode must handle instance death
- Resource cleanup must be exception-safe
- Parallel tests must not deadlock
