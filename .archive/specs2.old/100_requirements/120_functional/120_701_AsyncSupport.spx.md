# functional AsyncSupport
- **id**: FR-701
- **title**: Asynchronous Operation Support
- **priority**: high
- **status**: draft
- **category**: Test Execution

The framework must support asynchronous operations for platforms that require async-native interaction.

## capabilities

### AsyncNamingConvention
- **id**: FR-701.1
- **title**: Async method naming convention

Asynchronous methods must follow naming convention:
- Suffix: `*Async`
- Examples: `ClickAsync`, `EnterAsync`, `WaitVisibleAsync`

Clear distinction between sync and async APIs.

### AsyncInterfaces
- **id**: FR-701.2
- **title**: Async interface hierarchy

Async interfaces mirror sync interfaces:

| Sync Interface | Async Interface |
|----------------|-----------------|
| IControlObject | IAsyncControlObject |
| IClickableControl | IAsyncClickableControl |
| ITextControl | IAsyncTextControl |
| ISelectorControl | IAsyncSelectorControl |

Same capabilities, async method signatures.

### AsyncBaseClasses
- **id**: FR-701.3
- **title**: Async base classes

Async implementations need async base classes:
- AsyncControlBase
- AsyncClickableControlBase
- AsyncTextControlBase
- etc.

Base classes provide common async patterns.

### PlatformAsyncModel
- **id**: FR-701.4
- **title**: Platform-determined async model

Async vs sync determined by platform:

| Platform | Model | Reason |
|----------|-------|--------|
| MAUI/Appium | Sync | Appium client is sync |
| WPF/FlaUI | Sync | FlaUI is sync |
| WinForms/FlaUI | Sync | FlaUI is sync |
| Blazor/Playwright | Async | Playwright is async-native |
| HTML/Selenium | Sync | Selenium .NET is sync |
| Stride | Sync | Named pipe protocol is sync |

Test code style matches platform model.

### AsyncTestLifecycle
- **id**: FR-701.5
- **title**: Async test lifecycle

Async tests need async lifecycle:
- InitializeAsync - Async setup
- DisposeAsync - Async teardown
- Compatible with test framework async lifetime interfaces

### NoAsyncSyncMixing
- **id**: FR-701.6
- **title**: Avoid mixing sync and async

Within a test, use consistent model:
- All async or all sync
- Do not mix patterns in same test
- Test base class determines model

Mixing causes deadlocks and complexity.

### CancellationSupport
- **id**: FR-701.7
- **title**: Cancellation token support

Async operations should support cancellation:
- Optional CancellationToken parameter
- Enables test timeout at framework level
- Enables user-initiated cancellation

```
// Pseudocode
await control.ClickAsync(cancellationToken)
await control.WaitVisibleAsync(timeout, cancellationToken)
```

### AsyncExceptionHandling
- **id**: FR-701.8
- **title**: Async exception handling

Async exceptions must be properly propagated:
- Await exceptions surface correctly
- Exception stack traces preserved
- Logger handles async context

---

## relationships

- Alternative to sync operations in [FR-100 Controls](120_100_ControlObject.spx.md)
- Alternative to sync operations in [FR-101 Pages](120_101_PageObject.spx.md)
- Lifecycle in [FR-400 Test Context](120_400_TestContext.spx.md)
- Async logging in [FR-500 Logging](120_500_Logging.spx.md)

---

## constraints

- Async methods must not block synchronously
- Sync methods must not call async (deadlock risk)
- ConfigureAwait should be handled by framework
- Async operations must respect timeouts
