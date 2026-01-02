# Brinell UI Testing Framework - Core Wait and Action Rules

## Overview

These three fundamental rules establish the critical patterns for intelligent test automation. They eliminate time-based waits in favor of state-based waits and ensure test stability through pre-action validation.

---

## Rule 1: Never Wait for a Period of Time - Always Wait for Something

### Principle
Eliminate all time-based delays (`Thread.Sleep()`, `Task.Delay()`) from test code. Every wait operation must target a specific, observable condition that indicates the desired state has been reached.

### Why This Matters
- **Flaky Tests:** Time-based waits often complete before the target state is ready, or waste time after it's already ready
- **Test Execution:** Time-based waits are the primary cause of slow test suites that don't scale
- **Reliability:** Time-based waits fail unpredictably under load, network latency, or system resource constraints
- **Maintainability:** Hard-coded wait times create mysterious failures that are difficult to debug

### Implementation Pattern

**❌ WRONG - Time-Based Wait:**
```csharp
button.Click();
Thread.Sleep(500);  // "Hope the button click completes in 500ms"
var result = page.GetResult();
```

**✅ CORRECT - State-Based Wait:**
```csharp
button.Click();
page.WaitForResultVisible();  // "Wait until result is actually visible"
var result = page.GetResult();
```

### How to Implement

Every page object should provide:
- `WaitForXxx(int? timeoutMs = null)` - Wait for a specific condition
- `CheckXxx(int? timeoutMs = null)` - Wait + Assert (throws on timeout)
- `IsXxx()` - Immediate state check (no wait)

**Example - LoginPage:**
```csharp
public void WaitForLoginComplete(int? timeoutMs = null)
{
    _context.WaitFor(
        () => GetStatusMessage().Contains("Login successful", StringComparison.OrdinalIgnoreCase),
        timeoutMs,
        "login to complete");
}

public bool IsLoginSuccessful()
{
    return GetStatusMessage().Contains("Login successful");
}
```

### Framework Support
- `ITestContext.WaitFor(Func<bool> condition, int? timeoutMs, string description)`
- Default timeout: 5 seconds (configurable)
- Polling interval: 100ms (adaptive)
- Automatic logging of wait operations

---

## Rule 2: Before You Do an Action - Check Page Not Busy and Control Is Correct

### Principle
Before executing any action (Click, Type, Select), validate:
1. The page/application is not busy (no loading spinners, processing, animations)
2. The target control exists and is in the expected state

### Why This Matters
- **Race Conditions:** Actions on elements that are still loading or animating fail silently or produce unexpected results
- **Element State:** Controls may exist but be disabled, hidden, or in an invalid state for the intended action
- **Synchronization:** Prevents acting on stale or transitional UI states
- **Debugging:** Clear validation messages help identify what actually went wrong

### Implementation Pattern

**❌ WRONG - Direct Action:**
```csharp
var loginButton = page.GetLoginButton();
loginButton.Click();  // What if button is disabled? What if page is loading?
```

**✅ CORRECT - Validation Before Action:**
```csharp
page.WaitForPageReady();              // Page not busy
page.CheckLoginButtonVisible(true);   // Button exists and is visible
page.CheckLoginButtonEnabled(true);   // Button is clickable
var loginButton = page.GetLoginButton();
loginButton.Click();
```

### MAUI Implementation Reference

The MAUI sample demonstrates this pattern with:

**Page Readiness Check:**
```csharp
public void WaitForPageReady(int? timeoutMs = null)
{
    // Check 1: Page binding context is complete
    // Check 2: No loading indicators visible
    // Check 3: All expected controls are accessible
    _context.WaitFor(
        () => !IsPageBusy() && AllControlsAccessible(),
        timeoutMs,
        "page to be ready");
}

private bool IsPageBusy()
{
    // MAUI-specific: Check if any loading spinners are visible
    // Check if any async operations are in progress
    return _busyIndicator.IsVisible() || _context.IsLoading;
}
```

**Control State Validation:**
```csharp
public void WaitForLoginButtonEnabled(int? timeoutMs = null)
{
    _context.WaitFor(
        () => GetLoginButton().IsVisible() && GetLoginButton().IsEnabled(),
        timeoutMs,
        "login button to be enabled");
}
```

### Pattern Structure

```csharp
public async Task DoAction(string actionDescription)
{
    // STEP 1: Wait for page to be ready
    await WaitForPageReady();
    
    // STEP 2: Validate the control that will be acted upon
    CheckControlVisible(true);      // Control must exist and be visible
    CheckControlEnabled(true);      // Control must be enabled
    CheckControlHasCorrectState();  // Control must be in expected state
    
    // STEP 3: Perform the action
    var control = GetControl();
    control.Click();
    
    // STEP 4: Wait for result of the action
    WaitForActionComplete();
}
```

### Control State Checks to Perform

Before different actions, validate:

| Action | Checks Required |
|--------|-----------------|
| **Click** | Visible, Enabled, Not Obscured |
| **Type Text** | Visible, Enabled, Focused (or focusable), Empty/Clear |
| **Select Item** | Visible, Enabled, Has Items, Item Exists |
| **Get Value** | Visible, Ready, Populated |
| **Toggle** | Visible, Enabled, Current State Known |

---

## Rule 3: Before You Get a Value - Check Page Not Busy and Control Is Correct

### Principle
Before reading any value from a control, validate:
1. The page/application is not busy (not loading, processing, or animating)
2. The control exists and is in a valid state (visible, populated, not loading)

### Why This Matters
- **Stale Data:** Values read during page transitions or control updates may be incomplete or incorrect
- **Missing Data:** Control may not have been populated yet, returning null/empty
- **Visibility:** Hidden or off-screen controls may return unexpected values
- **Consistency:** Ensures values read during assertions are stable and reliable

### Implementation Pattern

**❌ WRONG - Direct Read:**
```csharp
var value = page.GetStatusMessage();  // What if page is still loading?
Assert.Equal("Success", value);        // Value might be incomplete or empty
```

**✅ CORRECT - Validation Before Read:**
```csharp
page.WaitForPageReady();              // Page not busy
page.CheckStatusMessageVisible(true); // Control is visible and populated
var value = page.GetStatusMessage();
Assert.Equal("Success", value);       // Value is guaranteed to be stable
```

### MAUI Implementation Reference

The MAUI sample demonstrates this pattern with:

**Value Readiness Check:**
```csharp
public string GetStatusMessage(int? timeoutMs = null)
{
    // STEP 1: Wait for page to be ready
    _context.WaitFor(
        () => !IsPageBusy(),
        timeoutMs,
        "page to be ready for reading");
    
    // STEP 2: Check control is in valid state for reading
    var statusControl = GetStatusControl();
    if (!statusControl.IsVisible())
        throw new InvalidOperationException("Status control not visible");
    
    // STEP 3: Wait for value to be populated (not empty/loading)
    _context.WaitFor(
        () => !string.IsNullOrEmpty(statusControl.GetText()),
        timeoutMs,
        "status message to be populated");
    
    // STEP 4: Return the value (guaranteed stable)
    return statusControl.GetText();
}
```

**Pattern for Properties:**
```csharp
private string _cachedStatus;
public string StatusMessage
{
    get
    {
        // Validate before returning cached value
        CheckPageReady();
        CheckStatusControlValid();
        
        // Read fresh value
        return GetStatusControl().GetText();
    }
}
```

### Pattern Structure

```csharp
public string GetValue()
{
    // STEP 1: Wait for page to be ready
    WaitForPageReady();
    
    // STEP 2: Validate the control that will be read
    CheckControlVisible(true);     // Control must be visible
    CheckControlPopulated(true);   // Control must have data
    CheckControlNotLoading(true);  // Control must not be loading
    
    // STEP 3: Read the value
    var control = GetControl();
    var value = control.GetText();
    
    // STEP 4: Validate value is reasonable
    if (string.IsNullOrEmpty(value))
        throw new InvalidOperationException("Control value unexpectedly empty");
    
    return value;
}
```

### Value Validation Checks

Before reading different types of values:

| Value Type | Checks Required |
|-----------|-----------------|
| **Text** | Control Visible, Not Empty, Not Loading |
| **Numeric** | Control Visible, Populated, Parseable |
| **Boolean** | Control Visible, State Determined |
| **List Items** | Control Visible, Not Empty, Loaded |
| **Selected Item** | Control Visible, Selection Valid |
| **Status** | Control Visible, Updated (not stale) |

---

## Integration Example

Here's how all three rules work together:

```csharp
[Fact]
public async Task LoginFlow_WithValidation()
{
    var page = _context.CreatePage<LoginPage>();
    
    // Rule 2: Before action - check page ready + button valid
    page.WaitForPageReady();
    page.CheckLoginButtonEnabled(true);
    
    // Perform action
    await page.EnterUsername("user@test.com");
    await page.EnterPassword("SecurePass123!");
    page.ClickLogin();
    
    // Rule 1: Wait for something (login completion), not for time
    page.WaitForLoginComplete();  // ✅ Not Thread.Sleep(3000)
    
    // Rule 3: Before getting value - check page ready + control valid
    page.WaitForPageReady();
    page.CheckStatusMessageVisible(true);
    
    // Read value safely
    var statusMessage = page.GetStatusMessage();
    Assert.Equal("Login Successful", statusMessage);
}
```

---

## Framework Support Required

The framework must provide:

1. **Page Ready Detection:**
   - `ITestContext.WaitForPageReady()`
   - `IsPageBusy()` method on page objects
   - Platform-specific busy indicators (spinners, loaders, progress bars)

2. **Control State Validation:**
   - `IControlObject.IsVisible()`
   - `IControlObject.IsEnabled()`
   - `IControlObject.IsPopulated()`
   - `IControlObject.WaitForReady(int? timeoutMs)`

3. **Intelligent Waits:**
   - `ITestContext.WaitFor(Func<bool>, int?, string)`
   - Automatic polling with configurable intervals
   - Logging of all wait operations

4. **Assertions with Waits:**
   - `IControlObject.CheckXxx(expectedState, int? timeoutMs)`
   - Throws on timeout with descriptive message

---

## Common Pitfalls to Avoid

| ❌ Pitfall | ✅ Solution |
|-----------|-----------|
| `Thread.Sleep(1000)` | Use `WaitForCondition()` |
| Clicking without checking | Use `CheckControlEnabled()` first |
| Reading without waiting | Use `WaitForPageReady()` first |
| Hard-coded timeouts | Use configurable defaults |
| Silent failures | Use Check methods that throw |
| No validation messages | Include description in Wait calls |

---

*Rules Established: January 2, 2026*  
*Critical for Test Stability and Performance*  
*Implementation Reference: MAUI Sample Application*
