# 250.001 IControlObject Specification

**Block Type:** SPC (Specification)  
**ID:** 250.001  
**Title:** IControlObject Interface Specification  
**Status:** Draft  
**Version:** 1.0  
**Level:** 0 - Foundation

---

## 1. Overview

`IControlObject` is the base interface for all controls in the Brinell framework. Every control, regardless of platform or capability, implements this interface. It provides the fundamental operations for control identification, state querying, waiting, and assertions.

### Interface Identity

- **Package:** `Brinell.Core`
- **Namespace:** `Brinell.Core.Interfaces`
- **Dependencies:** `Locator`, `IPageObject`
- **Implementors:** All control base classes and controls

---

## 2. Behavior

### 2.1 Identity Properties

The interface provides properties to identify and locate the control:

```csharp
public interface IControlObject
{
    /// <summary>
    /// The locator used to find this control in the UI tree.
    /// </summary>
    Locator Locator { get; }
    
    /// <summary>
    /// The page containing this control. May be null for orphan controls.
    /// </summary>
    IPageObject? Page { get; }
}
```

**Behavior:**
- `Locator` is set at construction and never changes
- `Page` may be null if control is created without page association
- Both properties are read-only after construction

### 2.2 State Methods

State methods query the current control state without waiting:

```csharp
/// <summary>
/// Check if the element exists in the UI tree.
/// </summary>
/// <returns>True if element exists, false otherwise.</returns>
bool IsExists();

/// <summary>
/// Check if the element is visible.
/// </summary>
/// <returns>True if visible, false if not visible, null if element not exists.</returns>
bool? IsVisible();

/// <summary>
/// Check if the element is enabled.
/// </summary>
/// <returns>True if enabled, false if disabled, null if element not exists.</returns>
bool? IsEnabled();
```

**Behavior:**
- State methods return immediately (no polling)
- `IsVisible()` returns null if element doesn't exist
- `IsEnabled()` returns null if element doesn't exist
- No exceptions thrown for missing elements

### 2.3 Wait Methods

Wait methods poll until condition is met or timeout occurs:

```csharp
/// <summary>
/// Wait until element existence matches expected value.
/// </summary>
/// <param name="expected">Expected existence. Null = skip operation.</param>
/// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
/// <returns>True if condition met, false if timeout.</returns>
bool WaitExists(bool? expected, int? timeoutMs = null);

/// <summary>
/// Wait until element visibility matches expected value.
/// </summary>
bool WaitVisible(bool? expected, int? timeoutMs = null);

/// <summary>
/// Wait until element enabled state matches expected value.
/// </summary>
bool WaitEnabled(bool? expected, int? timeoutMs = null);
```

**Behavior:**
- **Nullable Skip Pattern:** If `expected` is null, return true immediately (skip)
- If `timeoutMs` is null, use `_context.Timeouts.DefaultWait`
- Poll at reasonable interval (e.g., 100ms)
- Return true if condition met within timeout
- Return false if timeout occurs (no exception)

### 2.4 Assert Methods

Assert methods verify conditions and throw on failure:

```csharp
/// <summary>
/// Assert element existence matches expected value.
/// </summary>
/// <param name="expected">Expected existence. Null = skip operation.</param>
/// <param name="message">Custom failure message. Null = use default.</param>
/// <param name="timeoutMs">Timeout to wait before asserting. Null = use default.</param>
/// <exception cref="AssertionException">Thrown if assertion fails.</exception>
void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);

/// <summary>
/// Assert element visibility matches expected value.
/// </summary>
void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);

/// <summary>
/// Assert element enabled state matches expected value.
/// </summary>
void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
```

**Behavior:**
- **Nullable Skip Pattern:** If `expected` is null, return immediately (no assertion)
- Wait for condition (using Wait* method) before asserting
- Throw `AssertionException` with descriptive message if condition not met
- Include control locator and actual/expected values in exception message

### 2.5 Text Methods

Text methods retrieve and verify text content:

```csharp
/// <summary>
/// Get the text content of the control.
/// </summary>
/// <param name="timeoutMs">Timeout to wait for element. Null = use default.</param>
/// <returns>Text content, or null if element not found or has no text.</returns>
string? GetText(int? timeoutMs = null);

/// <summary>
/// Assert text content matches expected value exactly.
/// </summary>
/// <param name="expected">Expected text. Null = skip operation.</param>
void AssertText(string? expected, string? message = null, int? timeoutMs = null);

/// <summary>
/// Assert text content contains expected substring.
/// </summary>
/// <param name="expected">Expected substring. Null = skip operation.</param>
void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
```

**Behavior:**
- `GetText()` returns null if element doesn't exist
- `GetText()` returns empty string if element exists but has no text
- `GetText()` waits for element to exist before getting text
- **Nullable Skip Pattern:** If `expected` is null, return immediately
- Text comparison is case-sensitive by default

### 2.6 Attribute Methods

Attribute methods access element attributes:

```csharp
/// <summary>
/// Get an attribute value from the element.
/// </summary>
/// <param name="name">Attribute name.</param>
/// <returns>Attribute value, or null if not found.</returns>
string? GetAttribute(string name);
```

**Behavior:**
- Returns null if attribute doesn't exist
- Returns null if element doesn't exist
- Attribute names are platform-specific

---

## 3. Boundary

### 3.1 Element Not Found

| Scenario | Behavior |
|----------|----------|
| `IsExists()` on missing element | Returns false |
| `IsVisible()` on missing element | Returns null |
| `IsEnabled()` on missing element | Returns null |
| `GetText()` on missing element | Returns null |
| `GetAttribute()` on missing element | Returns null |
| `WaitExists(false, ...)` on missing element | Returns true immediately |

### 3.2 Timeout Behavior

| Scenario | Behavior |
|----------|----------|
| Wait completes before timeout | Returns true |
| Wait times out | Returns false (no exception) |
| Assert times out | Throws AssertionException |
| Negative timeout value | Treated as 0 (immediate check) |

### 3.3 Null Parameter Behavior

| Parameter | Value | Behavior |
|-----------|-------|----------|
| `expected` | null | Skip operation, return true/void |
| `timeoutMs` | null | Use default timeout from context |
| `message` | null | Use default exception message |

---

## 4. Acceptance Criteria

### ACC-001: State Methods Return Correct Values

```gherkin
Given a control that exists and is visible and enabled
When IsExists() is called
Then it returns true

Given a control that does not exist
When IsExists() is called
Then it returns false

Given a control that exists but is hidden
When IsVisible() is called
Then it returns false
```

### ACC-002: Wait Methods Respect Timeout

```gherkin
Given a control that becomes visible after 500ms
And a timeout of 2000ms
When WaitVisible(true, 2000) is called
Then it returns true

Given a control that never becomes visible
And a timeout of 1000ms
When WaitVisible(true, 1000) is called
Then it returns false after approximately 1000ms
```

### ACC-003: Nullable Skip Pattern Works

```gherkin
Given any control
When WaitExists(null, ...) is called
Then it returns true immediately without waiting

Given any control
When AssertVisible(null, ...) is called
Then it returns immediately without assertion
```

### ACC-004: Assert Methods Throw on Failure

```gherkin
Given a control that does not exist
When AssertExists(true, "Custom message") is called
Then it throws AssertionException
And the message contains "Custom message"
And the message contains the control locator
```

### ACC-005: Text Methods Return Correct Values

```gherkin
Given a Label control with text "Hello World"
When GetText() is called
Then it returns "Hello World"

Given an empty Label control
When GetText() is called
Then it returns empty string

Given a control that does not exist
When GetText() is called
Then it returns null
```

---

## 5. Assumptions

- **ASM-001:** Platform drivers (Appium, Selenium, FlaUI) are initialized before control use
- **ASM-002:** Control locators are valid for the target platform
- **ASM-003:** UI thread is responsive during state queries
- **ASM-004:** TimeoutSettings are configured in test context
- **ASM-005:** Logging infrastructure is available for action logging

---

## 6. Exclusions

- **EXC-001:** Control-specific capabilities (click, enter, toggle) — see capability interfaces
- **EXC-002:** Async/await versions of methods — synchronous API only
- **EXC-003:** Retry logic beyond simple polling — handled by higher-level infrastructure
- **EXC-004:** Screenshot capture — handled by ITestContext
- **EXC-005:** Platform-specific state (e.g., focus, hover) — platform-specific extensions

---

## 7. Complete Interface Definition

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Base interface for all controls in the Brinell framework.
    /// Provides identity, state querying, waiting, and assertion capabilities.
    /// </summary>
    public interface IControlObject
    {
        // Identity
        Locator Locator { get; }
        IPageObject? Page { get; }
        
        // State (immediate, no waiting)
        bool IsExists();
        bool? IsVisible();
        bool? IsEnabled();
        
        // Waiting (poll until condition or timeout)
        bool WaitExists(bool? expected, int? timeoutMs = null);
        bool WaitVisible(bool? expected, int? timeoutMs = null);
        bool WaitEnabled(bool? expected, int? timeoutMs = null);
        
        // Assertions (throw on failure)
        void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
        void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
        void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
        
        // Text
        string? GetText(int? timeoutMs = null);
        void AssertText(string? expected, string? message = null, int? timeoutMs = null);
        void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
        
        // Attributes
        string? GetAttribute(string name);
    }
}
```

---

## Related Documents

- [Interfaces Module](../../200_architecture/211_Modules/211_001_Interfaces.spx.md)
- [Base Classes Module](../../200_architecture/211_Modules/211_002_BaseClasses.spx.md)
- [IPageObject Specification](250_002_IPageObject.spx.md)
- [Control Object Pattern](../../200_architecture/231_Patterns/231_001_ControlObjectPattern.spx.md)
