# Review: Cross-Cutting Concerns and Patterns

**Date:** January 6, 2026
**Reviewer:** Automated Analysis
**Status:** Complete

---

## Purpose

Identify cross-cutting concerns that span multiple requirements and should be addressed holistically.

---

## 1. Timeout Handling (Cross-Cutting)

### Current State

Timeouts mentioned in:

- FR-002.3: `timeoutMs` parameter on actions
- FR-004: Timeout in Wait/Check/Assert
- FR-005.2: Configurable timeouts
- FR-010.1: Timeout in error messages

### Issue

No unified timeout philosophy documented:

- When should timeout throw vs return false?
- What's the default timeout hierarchy?
- How do timeouts compose (operation vs element search)?

### Proposed: Unified Timeout Specification

```markdown
## Timeout Philosophy

### Timeout Hierarchy (lowest wins)
1. Method parameter: `button.Click(timeoutMs: 5000)`
2. Control instance: `button.DefaultTimeoutMs = 3000`
3. Page instance: `page.DefaultTimeoutMs = 10000`
4. Context: `context.DefaultTimeoutMs = 15000`
5. Configuration: `UITestConfiguration.DefaultTimeoutMs`
6. Framework default: 30000ms

### Timeout Behavior by Method Type
| Method Type | Timeout Expired | No Timeout Param |
|-------------|-----------------|------------------|
| Is* | Return null | Immediate (no timeout) |
| Wait* | Return false | Use hierarchy |
| Check* | Throw exception | Use hierarchy |
| Assert* | Throw exception | Use hierarchy |
| Action* | Throw exception | Use hierarchy |
| Get* | Return null or throw | Use hierarchy |

### Composition
Element search timeout is separate from operation timeout.
Total time = element search + operation
```

### Affected Requirements

- Update FR-002.3, FR-004, FR-005.2 to reference unified spec
- Consider creating dedicated timeout requirement (FR-014.x)

---

## 2. Logging Strategy (Cross-Cutting)

### Current State

Logging mentioned in:

- FR-002.3: "Controls must log all actions performed"
- FR-006: Logging and Diagnostics (main document)
- FR-010.4: Exception logging

### Issue

No unified logging strategy:

- What log levels exist?
- What should be logged at each level?
- How to correlate logs with test execution?
- Structured logging format?

### Proposed: Unified Logging Specification

```markdown
## Logging Strategy

### Log Levels
| Level | Usage | Example |
|-------|-------|---------|
| Debug | Element searches, waits | "Searching for 'submitBtn' using AutomationId" |
| Info | Actions, assertions | "Click: submitBtn" |
| Warning | Retries, recoveries | "Retry 2/3: Element not ready" |
| Error | Failures, exceptions | "AssertText failed: expected 'Hello', got 'Hi'" |

### Required Log Fields
- Timestamp (ISO 8601)
- Test name
- Page name (if available)
- Control ID/locator
- Action/method name
- Parameters (sanitized)
- Result (success/fail)
- Duration (ms)

### Correlation
- TestRunId: Unique per test run
- TestId: Unique per test
- StepId: Sequential within test
```

### Affected Requirements

- Expand FR-006 with logging levels and fields
- Update FR-002.3 to reference FR-006 for logging details

---

## 3. Element Search Strategy (Cross-Cutting)

### Current State

Element search mentioned in:

- FR-002.1: Locator strategies
- FR-004.1: Return null if element not found
- FR-012: Container-scoped search

### Issue

No unified search strategy documented:

- Implicit wait vs explicit wait?
- Search retry on stale element?
- Search within search (nested locators)?

### Proposed: Unified Search Specification

```markdown
## Element Search Strategy

### Search Behavior
1. Single attempt with configurable implicit wait
2. If not found → return null (for Is*/Get*) or throw (for actions)
3. If found but stale during action → retry search once

### Search Scope
| Context | Search Root |
|---------|-------------|
| Page | Application/document root |
| Container | Container element |
| Chained locator | Previous locator result |

### Caching
- Element references are NOT cached by default
- Each operation performs fresh search
- Caching can be enabled per-control for performance

### Stale Element Handling
- Stale element during Is* → return null
- Stale element during action → re-search once, then throw
```

### Affected Requirements

- Add search strategy section to FR-002.1 or FR-012
- Clarify FR-004.1 null return semantics

---

## 4. Exception Strategy (Cross-Cutting)

### Current State

Exceptions mentioned in:

- FR-010.2: Exception types
- FR-010.4: Exception logging

### Issue

Exception hierarchy and usage not fully specified:

- When to use which exception?
- Base exception properties?
- Exception chaining?

### Proposed: Unified Exception Specification

```markdown
## Exception Strategy

### Exception Hierarchy
ControlObjectException (base)
├── ControlNotFoundException — Element never found
├── ControlNotVisibleException — Found but not visible
├── ControlNotEnabledException — Found but not enabled/clickable
├── ControlTimeoutException — Condition not met within timeout
├── ControlAssertionException — Assertion failed
├── ControlReadOnlyException — Cannot modify read-only control
└── PageNotReadyException — Page not in expected state

### Base Exception Properties
- Locator: The locator used to find element
- PageName: Current page context
- TimeoutMs: Timeout that was applied
- Timestamp: When exception occurred
- ScreenshotPath: If screenshot was captured

### Exception Usage
| Situation | Exception |
|-----------|-----------|
| Element not in DOM | ControlNotFoundException |
| Element in DOM but display:none | ControlNotVisibleException |
| Element visible but disabled | ControlNotEnabledException |
| WaitVisible timed out | ControlTimeoutException |
| AssertText mismatch | ControlAssertionException |
| Enter on readonly input | ControlReadOnlyException |
```

### Affected Requirements

- Expand FR-010.2 with hierarchy and usage table
- Ensure all exceptions logged per FR-010.4

---

## 5. Platform Abstraction Pattern (Cross-Cutting)

### Current State

Platform handling mentioned in:

- FR-001: Multi-platform support
- FR-007: Platform-specific automation
- FR-013: Async pattern for Blazor

### Issue

Pattern for platform-specific behavior not specified:

- How to handle platform differences in controls?
- How to expose platform-specific features?
- How to document platform limitations?

### Proposed: Platform Abstraction Pattern

```markdown
## Platform Abstraction Pattern

### Shared Behavior
Core interfaces define shared behavior. All platforms implement interfaces with same semantics.

### Platform-Specific Extensions
```csharp
// Platform extensions via extension methods or derived interfaces
public static class MauiControlExtensions
{
    public static void LongPress(this IClickableControlObject control, int durationMs)
    {
        ((IMauiControlObject)control).LongPress(durationMs);
    }
}
```

### Platform Capability Query

```csharp
if (context.Platform.SupportsGestures)
{
    control.Swipe(Direction.Left);
}
```

### Platform Limitations Documentation

Each platform documents limitations in implementation notes:

- Web: No hardware back button
- MAUI iOS: Limited gesture recognition
- WPF: No touch events without hardware

```

### Affected Requirements

- Add platform abstraction section to FR-001 or FR-007
- Document extension pattern for platform-specific features

---

## 6. Test Data Pattern (Cross-Cutting)

### Current State

Test data mentioned only briefly in:
- FR-009.3/4: Test data isolation

### Issue

No pattern for test data management:
- How to provide test data to page objects?
- How to generate unique test data?
- How to clean up test data?

### Proposed: Test Data Pattern

```markdown
## Test Data Pattern

### Data Provision
Page objects should not generate test data. Test methods provide data:
```csharp
var user = TestDataGenerator.CreateUser();
loginPage.Login(user.Email, user.Password);
```

### Unique Data Generation

Framework may provide helpers for unique data:

```csharp
TestData.UniqueEmail() // "test-{guid}@example.com"
TestData.UniqueString(prefix: "user")
TestData.RandomInt(min: 1, max: 100)
```

### Data Cleanup

- Test-created data should be cleaned up in test teardown
- Framework does not manage external data stores
- Consider database transaction rollback for integration tests

```

### Affected Requirements

- Expand FR-009.4 with test data patterns
- Consider referencing from FR-003 (page objects)

---

## 7. Assertion Pattern (Cross-Cutting)

### Current State

Assertions mentioned in:
- FR-004.4: Assert* methods
- FR-004.5: Prefer control assertions
- FR-011.2: No FluentAssertions

### Issue

Assertion pattern not fully specified:
- Custom assertion messages
- Multiple assertions (soft assertions?)
- Assertion helpers beyond controls

### Proposed: Unified Assertion Pattern

```markdown
## Assertion Pattern

### Method Signature
All Assert methods follow:
```csharp
void AssertX(T? expected, string? message = null, int? timeoutMs = null)
```

### Custom Messages

Messages are appended to generated message:

```csharp
button.AssertVisible(true, "Submit button should appear after form validation");
// Output: "AssertVisible failed for 'submitBtn': expected true, got false. Submit button should appear after form validation"
```

### Multiple Assertions

Framework supports Check* methods that don't throw:

```csharp
// Collect multiple failures
var errors = new List<string>();
if (!label1.WaitText("A")) errors.Add("label1");
if (!label2.WaitText("B")) errors.Add("label2");
Assert.Empty(errors); // Use test framework assertion
```

### Soft Assertions (Future)

Consider adding SoftAssert pattern for collecting failures:

```csharp
using (var soft = context.SoftAssertScope())
{
    label1.AssertText("A");
    label2.AssertText("B");
} // Throws if any assertion failed
```

```

### Affected Requirements

- Expand FR-004.4 with message handling
- Consider soft assertion as future enhancement

---

## Summary Matrix

| Cross-Cutting Concern | Primary Requirement | Supporting Requirements |
|-----------------------|--------------------|-----------------------|
| Timeout Handling | FR-005.2 | FR-002.3, FR-004, FR-010 |
| Logging Strategy | FR-006 | FR-002.3, FR-010.4 |
| Element Search | FR-002.1 | FR-004.1, FR-012 |
| Exception Strategy | FR-010 | FR-004, FR-005 |
| Platform Abstraction | FR-007 | FR-001, FR-013 |
| Test Data | FR-009.4 | FR-003 |
| Assertion Pattern | FR-004.4 | FR-011 |

---

## Recommendations

### High Priority

1. **Consolidate timeout specification** — Single source of truth for timeout behavior
2. **Expand logging specification** — Define levels, fields, correlation
3. **Clarify exception usage** — When to use which exception type

### Medium Priority

4. **Document search strategy** — Implicit wait, caching, stale handling
5. **Platform abstraction pattern** — Extensions, capabilities, limitations
6. **Assertion message handling** — Custom messages, soft assertions

### Low Priority

7. **Test data helpers** — Utility methods for unique data generation

---

## Next Steps

1. Create dedicated cross-cutting sections in existing requirements
2. Consider extracting common patterns to shared document
3. Add cross-references between related requirements
4. Update implementation to match specified patterns
```
