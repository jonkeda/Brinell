# SPEC-015: Element Lookup Optimization

**Status:** Draft  
**Created:** January 18, 2026  
**Author:** Brinell Framework Team

---

## 1. Problem Statement

### 1.1 Current Behavior Analysis

A single `MauiButtonControl.Click()` call results in **excessive FindElement calls** due to repeated element lookups across nested method calls.

#### Call Trace for `Click()`

```
Click()
├── Run(nameof(Click), ...)
│   └── CheckClickable()
│       ├── WaitExists(true, timeout)           ──► Poll() calls IsExists() repeatedly
│       │   └── IsExists()                      ──► TryFindElement() [N times during polling]
│       │
│       ├── WaitEnabled(true, timeout)          ──► Poll() calls IsEnabled() repeatedly
│       │   └── IsEnabled()                     ──► TryFindElement() [N times during polling]
│       │
│       ├── TryFindElement()                    ──► [1 call]
│       │
│       ├── IsVisible()                         ──► TryFindElement() [1 call]
│       │
│       ├── element.ScrollIntoView(...)         ──► [uses element already found]
│       │
│       └── WaitEnabled(true, timeout/2)        ──► Poll() calls IsEnabled() repeatedly
│           └── IsEnabled()                     ──► TryFindElement() [N times during polling]
│
└── FindElement()                               ──► [1 final call before actual click]
```

### 1.2 Element Lookup Count Estimation

Assuming:
- Default timeout: 5000ms
- Polling interval: 250ms  
- Each Wait method polls: 5000/250 = **20 iterations**

**Per Click() operation:**

| Method | FindElement Calls |
|--------|-------------------|
| WaitExists polling | ~20 |
| WaitEnabled polling #1 | ~20 |
| TryFindElement (scroll check) | 1 |
| IsVisible (scroll check) | 1 |
| WaitEnabled polling #2 | ~10 |
| FindElement (final) | 1 |
| **TOTAL** | **~53 calls** |

Even in the best case (element immediately ready):
- WaitExists: 1 call
- WaitEnabled #1: 1 call
- TryFindElement: 1 call
- IsVisible: 1 call
- WaitEnabled #2: 1 call
- FindElement: 1 call
- **Best case: 6 calls** (should be 1)

### 1.3 Impact

1. **Performance**: Each FindElement is a WebDriver round-trip (HTTP to Appium server)
2. **Latency**: Network latency multiplied by call count
3. **Flakiness**: More round-trips = more chances for timing issues
4. **Resource usage**: Increased CPU/memory on Appium server

---

## 2. Requirements

### 2.1 Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| REQ-001 | Single FindElement call per Click() in the success path | P0 |
| REQ-002 | All state-check methods must accept a pre-found element | P0 |
| REQ-003 | Public API must remain backward compatible | P0 |
| REQ-004 | Pattern must apply to all control types | P1 |
| REQ-005 | Element is found once per operation, no stale handling needed | P1 |
| REQ-006 | Polling reuses the same element reference throughout | P2 |

### 2.2 Non-Functional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001 | Click() should make ≤3 FindElement calls in success path | 1-3 calls |
| NFR-002 | No breaking changes to public API | 100% compatible |
| NFR-003 | Code complexity increase should be minimal | ≤20% more code |

---

## 3. Design

### 3.1 Core Pattern: Protected Element-Aware Overloads

Each public method that calls `FindElement` internally will have a corresponding **protected overload** that accepts a pre-found element.

```
┌─────────────────────────────────────────────────────────────────┐
│                     PUBLIC API (unchanged)                       │
├─────────────────────────────────────────────────────────────────┤
│  Click()              IsEnabled()           WaitEnabled()        │
│  DoubleClick()        IsVisible()           WaitVisible()        │
│  CheckClickable()     IsExists()            WaitExists()         │
└──────────────────────────────┬──────────────────────────────────┘
                               │ calls
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│               PROTECTED CORE (element-aware)                     │
├─────────────────────────────────────────────────────────────────┤
│  ClickCore(element)          IsEnabledCore(element)              │
│  DoubleClickCore(element)    IsVisibleCore(element)              │
│  CheckClickableCore(element) WaitEnabledCore(element, ...)       │
│                              WaitVisibleCore(element, ...)       │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Method Signature Pattern

For each public method:

```csharp
// PUBLIC: Finds element, delegates to Core
public TScope Click(int? timeoutMs = null)
{
    var element = FindElementWithWait(timeoutMs);
    return ClickCore(element, timeoutMs);
}

// PROTECTED CORE: Works with pre-found element
protected TScope ClickCore(IMauiElement element, int? timeoutMs = null)
{
    Run(nameof(Click), () =>
    {
        CheckClickableCore(element, timeoutMs);
        element.Click();
    });
    return ContainingScope;
}
```

### 3.3 Optimized Click() Flow

**BEFORE (53+ FindElement calls):**
```
Click()
  └── CheckClickable()
        ├── WaitExists() ──► 20x FindElement
        ├── WaitEnabled() ──► 20x FindElement
        ├── TryFindElement()
        ├── IsVisible() ──► FindElement
        └── WaitEnabled() ──► 10x FindElement
  └── FindElement()
```

**AFTER (1-3 FindElement calls):**
```
Click()
  └── FindElementWithWait(timeout)  ──► 1x FindElement (polls if needed)
  └── ClickCore(element)
        └── CheckClickableCore(element)
              ├── IsEnabledCore(element) ──► 0x FindElement (uses passed element)
              ├── IsVisibleCore(element) ──► 0x FindElement (uses passed element)
              └── ScrollIntoView(element)
        └── element.Click()
```

### 3.4 Detailed Class Changes

#### 3.4.1 MauiControlBase Changes

```csharp
public class MauiControlBase<TScope>
{
    #region Element Finding - Optimized
    
    /// <summary>
    /// Finds element, waiting for it to exist if timeout is specified.
    /// Single entry point for element retrieval with optional wait.
    /// </summary>
    protected IMauiElement FindElementWithWait(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            var timeout = timeoutMs.Value;
            var stopwatch = Stopwatch.StartNew();
            
            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                var element = TryFindElement();
                if (element != null)
                    return element;
                    
                Thread.Sleep(PollingIntervalMs);
            }
        }
        
        return FindElement(); // Throws if not found
    }
    
    #endregion
    
    #region State - Core Methods (Element-Aware)
    
    /// <summary>
    /// Checks if element is visible using pre-found element.
    /// No stale element handling - element is found once at operation start.
    /// </summary>
    protected bool? IsVisibleCore(IMauiElement? element)
    {
        if (element == null) return null;
        return element.Displayed;
    }
    
    /// <summary>
    /// Checks if element is enabled using pre-found element.
    /// No stale element handling - element is found once at operation start.
    /// </summary>
    protected bool? IsEnabledCore(IMauiElement? element)
    {
        if (element == null) return null;
        return element.Enabled;
    }
    
    #endregion
    
    #region State - Public Methods (Find + Delegate)
    
    /// <inheritdoc />
    public bool? IsVisible()
    {
        return IsVisibleCore(TryFindElement());
    }
    
    /// <inheritdoc />
    public bool? IsEnabled()
    {
        return IsEnabledCore(TryFindElement());
    }
    
    #endregion
    
    #region Waiting - Core Methods (Element-Aware)
    
    /// <summary>
    /// Polls enabled state using pre-found element.
    /// </summary>
    protected bool WaitEnabledCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsEnabledCore(e) == expected,
            timeoutMs);
    }
    
    /// <summary>
    /// Polls visible state using pre-found element.
    /// </summary>
    protected bool WaitVisibleCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsVisibleCore(e) == expected,
            timeoutMs);
    }
    
    /// <summary>
    /// Polls with element reference. Element is found once at operation start,
    /// no re-finding needed since we optimize for single-element operations.
    /// </summary>
    protected bool PollWithElement(
        IMauiElement element,
        Func<IMauiElement, bool> condition,
        int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            if (condition(element))
                return true;
            
            Thread.Sleep(PollingIntervalMs);
        }
        
        // Final check
        return condition(element);
    }
    
    #endregion
}
```

#### 3.4.2 MauiControlBase - Run Method Overloads for Core Pattern

The `Run` methods need overloads that support the element-finding-then-core-execution pattern while maintaining proper logging.

```csharp
public class MauiControlBase<TScope>
{
    #region Run Methods - Element-Aware Overloads
    
    /// <summary>
    /// Run operation that finds element first, then executes core logic.
    /// Logging wraps the entire operation including element finding.
    /// </summary>
    protected TScope RunWithElement(string action, int? timeoutMs, Action<IMauiElement> coreOperation)
    {
        Run(action, () =>
        {
            var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
            coreOperation(element);
        });
        return ContainingScope;
    }
    
    /// <summary>
    /// Run operation with value that finds element first, then executes core logic.
    /// </summary>
    protected TScope RunWithElement<TValue>(string action, TValue? value, int? timeoutMs, 
        Action<IMauiElement> coreOperation)
    {
        Run(action, value, () =>
        {
            var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
            coreOperation(element);
        });
        return ContainingScope;
    }
    
    /// <summary>
    /// Run operation that finds element first, then executes core logic returning a result.
    /// </summary>
    protected TResult RunWithElement<TResult>(string action, int? timeoutMs, 
        Func<IMauiElement, TResult> coreOperation)
    {
        return Run(action, () =>
        {
            var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
            return coreOperation(element);
        });
    }
    
    #endregion
}
```

#### 3.4.3 MauiButtonControl Changes

```csharp
public class MauiButtonControl<TScope>
{
    #region Click - Public API (unchanged signature)
    
    /// <inheritdoc />
    public TScope Click(int? timeoutMs = null)
    {
        // RunWithElement handles: logging entry, find element, execute core, logging exit
        return RunWithElement(nameof(Click), timeoutMs, element =>
        {
            ClickCore(element, timeoutMs);
        });
    }
    
    /// <inheritdoc />
    public TScope DoubleClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(DoubleClick), timeoutMs, element =>
        {
            DoubleClickCore(element, timeoutMs);
        });
    }
    
    /// <inheritdoc />
    public TScope RightClick(int? timeoutMs = null)
    {
        return RunWithElement(nameof(RightClick), timeoutMs, element =>
        {
            RightClickCore(element, timeoutMs);
        });
    }
    
    #endregion
    
    #region Click - Core Methods (Element-Aware, No Logging)
    
    /// <summary>
    /// Performs click on pre-found element. No logging - caller handles logging.
    /// </summary>
    protected void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckClickableCore(element, timeoutMs);
        element.Click();
    }
    
    /// <summary>
    /// Performs double-click on pre-found element. No logging - caller handles logging.
    /// </summary>
    protected void DoubleClickCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckClickableCore(element, timeoutMs);
        element.Click();
        element.Click();
    }
    
    /// <summary>
    /// Performs right-click on pre-found element. No logging - caller handles logging.
    /// </summary>
    protected void RightClickCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckClickableCore(element, timeoutMs);
        
        var unwrappedElement = element.UnwrapElement();
        var unwrappedDriver = Context.Driver.UnwrapDriver();
        
        var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
        actions.ContextClick(unwrappedElement).Perform();
    }
    
    /// <summary>
    /// Verifies element is clickable using pre-found element. No logging.
    /// </summary>
    protected void CheckClickableCore(IMauiElement element, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        
        // Check enabled state (element already exists, so skip WaitExists)
        if (IsEnabledCore(element) != true)
        {
            if (!WaitEnabledCore(element, true, timeout))
            {
                throw new TimeoutException(
                    $"Element was not enabled within {timeout}ms. Locator: {Locator}");
            }
        }
        
        // Check visibility, scroll if needed
        if (IsVisibleCore(element) != true)
        {
            element.ScrollIntoView(Context.Driver);
        }
    }
    
    /// <summary>
    /// Public CheckClickable - finds element and delegates to Core.
    /// </summary>
    public void CheckClickable(int? timeoutMs = null)
    {
        var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
        CheckClickableCore(element, timeoutMs);
    }
    
    #endregion
    
    #region IsClickable - Core Methods
    
    /// <summary>
    /// Checks clickable state using pre-found element.
    /// </summary>
    protected bool? IsClickableCore(IMauiElement? element)
    {
        var isVisible = IsVisibleCore(element);
        var isEnabled = IsEnabledCore(element);
        
        if (isVisible == null || isEnabled == null)
            return null;
        
        return isVisible.Value && isEnabled.Value;
    }
    
    /// <inheritdoc />
    public bool? IsClickable()
    {
        return IsClickableCore(TryFindElement());
    }
    
    #endregion
    
    #region WaitClickable - Core Methods
    
    /// <summary>
    /// Waits for clickable state using pre-found element.
    /// </summary>
    protected bool WaitClickableCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(
            element,
            e => IsClickableCore(e) == expected,
            timeoutMs);
    }
    
    /// <inheritdoc />
    public bool WaitClickable(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        var element = TryFindElement();
        if (element == null)
        {
            // If element doesn't exist and we expect clickable=false, that's a match
            return expected.Value == false;
        }
        
        return WaitClickableCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }
    
    #endregion
}
```

### 3.5 Method Mapping

| Public Method | Run Wrapper | Core Method | Notes |
|---------------|-------------|-------------|-------|
| `IsExists()` | N/A | N/A | Already optimal (single call) |
| `IsVisible()` | N/A | `IsVisibleCore(element)` | No logging for state checks |
| `IsEnabled()` | N/A | `IsEnabledCore(element)` | No logging for state checks |
| `IsClickable()` | N/A | `IsClickableCore(element)` | No logging for state checks |
| `WaitExists()` | N/A | N/A | Must find element to check existence |
| `WaitVisible()` | N/A | `WaitVisibleCore(element, ...)` | No logging for waits |
| `WaitEnabled()` | N/A | `WaitEnabledCore(element, ...)` | No logging for waits |
| `WaitClickable()` | N/A | `WaitClickableCore(element, ...)` | No logging for waits |
| `Click()` | `RunWithElement()` | `ClickCore(element)` | Logged operation |
| `DoubleClick()` | `RunWithElement()` | `DoubleClickCore(element)` | Logged operation |
| `RightClick()` | `RunWithElement()` | `RightClickCore(element)` | Logged operation |
| `CheckClickable()` | N/A | `CheckClickableCore(element)` | Internal validation |
| `GetText()` | N/A | `GetTextCore(element)` | No logging for getters |
| `SendKeys()` | `RunWithElement()` | `SendKeysCore(element, keys)` | Logged operation |
| `Enter()` | `RunWithElement()` | `EnterCore(element, text)` | Logged operation |
| `Toggle()` | `RunWithElement()` | `ToggleCore(element)` | Logged operation |

### 3.6 Logging Pattern

**Key Principle:** Only **action methods** (Click, Enter, Toggle, etc.) are logged. State checks and waits are not logged individually.

```
┌─────────────────────────────────────────────────────────────────┐
│                    LOGGED OPERATIONS                             │
│  (Use RunWithElement - wraps find + core + logging)              │
├─────────────────────────────────────────────────────────────────┤
│  Click()    DoubleClick()    RightClick()    SendKeys()          │
│  Enter()    Clear()          Toggle()        SetValue()          │
│  Select()   Scroll()                                             │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                  NOT LOGGED (Internal Use)                       │
│  (Core methods - no Run wrapper)                                 │
├─────────────────────────────────────────────────────────────────┤
│  IsVisibleCore()     IsEnabledCore()     IsClickableCore()       │
│  WaitVisibleCore()   WaitEnabledCore()   WaitClickableCore()     │
│  CheckClickableCore() GetTextCore()                              │
└─────────────────────────────────────────────────────────────────┘
```

**Log output for Click():**
```
[ENTRY] Test.ButtonTest | MainPage | IncrementBtn | Click
[EXIT]  Test.ButtonTest | MainPage | IncrementBtn | Click | Success | 45ms
```

**Internal call flow (not logged individually):**
```
Click()
  └── RunWithElement(nameof(Click), ...)   ← Log ENTRY
        └── FindElementWithWait()          ← Not logged
        └── ClickCore(element)             ← Not logged
              └── CheckClickableCore()     ← Not logged
                    └── IsEnabledCore()    ← Not logged
                    └── IsVisibleCore()    ← Not logged
              └── element.Click()          ← Not logged
        ← Log EXIT
```

---

## 4. Implementation Plan

### Phase 1: Core Infrastructure (MauiControlBase)

1. Add `FindElementWithWait()` method
2. Add `PollWithElement()` method
3. Add `IsVisibleCore()`, `IsEnabledCore()` methods
4. Add `WaitVisibleCore()`, `WaitEnabledCore()` methods
5. Refactor existing public methods to use Core methods

### Phase 2: Button Control Optimization

1. Add `ClickCore()`, `DoubleClickCore()`, `RightClickCore()` methods
2. Add `CheckClickableCore()`, `IsClickableCore()` methods
3. Refactor `Click()`, `DoubleClick()`, `RightClick()` to use Core methods
4. Update `CheckClickable()` to use `CheckClickableCore()`

### Phase 3: Apply Pattern to Other Controls

1. `MauiTextControl` - `GetTextCore()`, `EnterCore()`
2. `MauiToggleControl` - `ToggleCore()`, `IsToggledCore()`
3. `MauiSliderControl` - `SetValueCore()`, `GetValueCore()`
4. `MauiComboBoxControl` - `SelectCore()`, `GetSelectedCore()`

### Phase 4: Testing & Validation

1. Add unit tests for Core methods
2. Add performance benchmarks
3. Run existing UI tests to verify backward compatibility
4. Measure FindElement call reduction

---

## 5. Migration Guide

### For Framework Maintainers

When adding new control methods:

```csharp
// 1. Always create Core method first
protected TResult MethodNameCore(IMauiElement element, TArg arg)
{
    // Implementation using element directly
}

// 2. Public method finds element and delegates
public TResult MethodName(TArg arg, int? timeoutMs = null)
{
    var element = FindElementWithWait(timeoutMs);
    return MethodNameCore(element, arg);
}
```

### For Test Authors

**No changes required.** Public API remains identical.

---

## 6. Performance Expectations

| Scenario | Before | After | Improvement |
|----------|--------|-------|-------------|
| Click() - element ready | ~6 calls | 1 call | 6x |
| Click() - element needs wait | ~53 calls | 1-3 calls | 17-53x |
| DoubleClick() | ~106 calls | 1-3 calls | 35-106x |
| 10 button clicks in test | ~530 calls | ~10-30 calls | 17-53x |

---

## 7. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Stale element during operation | Low | Operations complete quickly; element found at start is valid for operation duration |
| Increased code complexity | Low | Clear naming convention, documentation |
| Breaking existing behavior | High | Extensive test coverage, backward-compatible API |
| Element state changes between find and use | Low | Element state checked immediately after finding |

---

## 8. Success Criteria

1. ✅ `Click()` makes ≤3 `FindElement` calls in normal operation
2. ✅ All existing UI tests pass without modification
3. ✅ No public API changes
4. ✅ Pattern documented and applied consistently
5. ✅ Measurable performance improvement in test suite execution time

---

## 9. References

- [MauiButtonControl.cs](../srcnew/Brinell.Maui/Controls/MauiButtonControl.cs)
- [MauiControlBase.cs](../srcnew/Brinell.Maui/Controls/MauiControlBase.cs)
- [SPEC-002: Interface Contracts](./SPEC-002-interface-contracts.md)

---

**Revision History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-18 | Brinell Team | Initial draft |
