# functional WaitingSynchronization
- **id**: FR-301
- **title**: Waiting and Synchronization
- **priority**: high
- **status**: draft
- **category**: State and Verification

The framework must provide mechanisms to handle asynchronous UI updates and ensure test synchronization with application state.

## capabilities

### AutoWait
- **id**: FR-301.1
- **title**: Automatic waiting for element readiness

Action methods must automatically wait for element readiness:
- Wait for element to exist
- Wait for element to be visible (for visible actions)
- Wait for element to be enabled (for interactive actions)
- No manual waits needed before standard actions

### PollingMechanism
- **id**: FR-301.2
- **title**: Polling mechanism for state changes

Wait operations must use polling:
- Repeatedly check condition at intervals
- Continue until condition met or timeout
- Configurable polling interval
- Default polling interval: 100ms
- Maximum polling interval: 500ms

### TimeoutConfiguration
- **id**: FR-301.3
- **title**: Timeout configuration

Timeouts must be configurable at multiple levels (lowest wins):
1. Method parameter (highest priority)
2. Control instance setting
3. Page instance setting
4. Test context setting
5. Configuration file
6. Framework default (lowest priority)

See [FR-402 Timeout Handling](120_402_TimeoutHandling.spx.md) for details.

### CustomConditionWait
- **id**: FR-301.4
- **title**: Custom condition waiting

The framework must support waiting for custom conditions:
- Wait for arbitrary boolean condition
- Condition evaluated on each poll
- Timeout applies to entire wait

```
// Pseudocode
WaitFor(() => control.GetText() == "Complete", timeout)
WaitFor(() => page.IsBusy == false, timeout)
```

### BusyStateTracking
- **id**: FR-301.5
- **title**: Page busy state tracking

Pages may define busy state indicators:

| Method | Description |
|--------|-------------|
| IsBusy | Returns true if page shows busy indicator |
| WaitForNotBusy | Wait until busy state clears |

Busy indicator is page-specific:
- Loading spinner visible
- Progress bar active
- Overlay displayed
- Network activity pending

### WaitForNotAfter
- **id**: FR-301.6
- **title**: Wait FOR conditions, not AFTER events

Tests must wait for conditions, not arbitrary time periods:

**Correct patterns:**
```
// Wait for specific condition
WaitFor(page.IsReady)
WaitFor(button.IsEnabled)
WaitFor(text.GetText() == "Complete")
```

**Incorrect patterns:**
```
// Do not use arbitrary delays
Sleep(5000)  // WRONG
Delay(TimeSpan.FromSeconds(5))  // WRONG
```

### SynchronousModel
- **id**: FR-301.7
- **title**: Synchronous operation model

Standard framework operations are synchronous:
- Actions block until complete
- Waits block until condition met or timeout
- State queries return immediately
- Tests read top-to-bottom without async complexity

For platforms requiring async operations, see [FR-701 Async Support](120_701_AsyncSupport.spx.md).

### FailFastOnTimeout
- **id**: FR-301.8
- **title**: Fail fast when timeout expires

When a timeout expires:
- Check* methods throw exception immediately
- Assert* methods throw exception immediately
- Action methods throw exception immediately
- Wait* methods return false (do not throw)

No silent failures - explicit result for all operations.

---

## relationships

- Used by [FR-100 Control](120_100_ControlObject.spx.md) actions
- Used by [FR-101 Page](120_101_PageObject.spx.md) readiness
- Timeout hierarchy in [FR-402 Timeout Handling](120_402_TimeoutHandling.spx.md)
- Async alternative in [FR-701 Async Support](120_701_AsyncSupport.spx.md)

---

## constraints

- Polling must not consume excessive CPU
- Timeouts must be respected exactly (no indefinite waits)
- Busy state definition must be overridable per page
- Custom conditions must not have side effects
