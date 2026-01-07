# Review: 250_000_Foundation vs 200_Architecture

**Date:** January 7, 2026
**Status:** In Review
**Scope:** Comparison of 250_000_Foundation specifications against 200_architecture definitions

---

## Purpose

This document identifies differences between the **250_000_Foundation specifications** (detailed interface definitions) and the **200_architecture** documents (architectural patterns and cross-cutting concerns). Each difference is documented once with options to resolve by updating either the architecture or the specifications.

---

## Summary of Documents Compared

### 200_Architecture (Reference)

- [200_000_Overview.spx.md](../specs2/200_architecture/200_000_Overview.spx.md) - Layer model, interface hierarchy pattern
- [221_001_Logging.spx.md](../specs2/200_architecture/221_Foundation/221_001_Logging.spx.md) - ITestLogger interface, CSV logging
- [221_002_Configuration.spx.md](../specs2/200_architecture/221_Foundation/221_002_Configuration.spx.md) - UITestConfiguration, TimeoutSettings
- [221_003_ExceptionHandling.spx.md](../specs2/200_architecture/221_Foundation/221_003_ExceptionHandling.spx.md) - Exception hierarchy
- [221_004_Timeout.spx.md](../specs2/200_architecture/221_Foundation/221_004_Timeout.spx.md) - Timeout levels, Wait/Check/Assert patterns
- [231_001_ControlObjectPattern.spx.md](../specs2/200_architecture/231_Patterns/231_001_ControlObjectPattern.spx.md) - Control object design
- [231_004_ContainerPattern.spx.md](../specs2/200_architecture/231_Patterns/231_004_ContainerPattern.spx.md) - Container scoping pattern

### 250_000_Foundation (Under Review)

- [250_001_IControlObject.spx.md](../specs2/250_specifications/250_000_Foundation/250_001_IControlObject.spx.md)
- [250_002_IPageObject.spx.md](../specs2/250_specifications/250_000_Foundation/250_002_IPageObject.spx.md)
- [250_003_IContainerControlObject.spx.md](../specs2/250_specifications/250_000_Foundation/250_003_IContainerControlObject.spx.md)
- [250_003a_IContainerControl.spx.md](../specs2/250_specifications/250_000_Foundation/250_003a_IContainerControl.spx.md)
- [250_003b_IListContainerControlObject.spx.md](../specs2/250_specifications/250_000_Foundation/250_003b_IListContainerControlObject.spx.md)
- [250_004_TestContext.spx.md](../specs2/250_specifications/250_000_Foundation/250_004_TestContext.spx.md)
- [250_005_InterfaceHierarchy.spx.md](../specs2/250_specifications/250_000_Foundation/250_005_InterfaceHierarchy.spx.md)

---

## Differences Identified

### DIFF-001: ITestLogger Interface Signatures

**Architecture (221_001):** Defines `ITestLogger` with detailed signatures including `testName`, `pageName`, `controlId` parameters:

```csharp
void LogAction(string testName, string pageName, string controlId, string action, string? value = null);
void LogAssertPass(string testName, string pageName, string controlId, string assertType, string? actualValue, string? expectedValue);
void LogEntry(string testName, string pageName, string controlId, string action, string? value);
void LogExit(string testName, string pageName, string controlId, string action, LogResult result, int durationMs, string? message = null);
```

**Specification (250_004):** Defines simpler `ITestLogger` with `Locator` parameter instead of separate `testName/pageName/controlId`:

```csharp
void LogAction(string action, Locator locator, string? value = null);
void LogNavigation(string destination);
void LogAssertion(string assertion, bool passed, string? message = null);
```

**Impact:** Logger implementations would differ significantly. Architecture version provides more context for CSV output.

#### Resolution Options

- [ ] **Update Architecture:** Simplify ITestLogger to match specification, derive testName/pageName/controlId from context
- [X] **Update Specification:** Expand ITestLogger to match architecture with full context parameters

---

### DIFF-002: Wait/Check/Assert Method Pattern

**Architecture (221_004):** Defines three distinct patterns:

- `Wait*` methods: Return `bool`, poll until condition or timeout, **never throw**
- `Check*` methods: Poll, then **throw `CheckFailedException`** on timeout
- `Assert*` methods: **Immediate check** without polling, throw `AssertionException`

```csharp
// Wait (no throw)
bool WaitVisible(bool visible, int? timeoutMs = null);

// Check (polls, throws)
void CheckVisible(bool visible, int? timeoutMs = null, string? message = null);

// Assert (immediate, throws)
void AssertVisible(string? message = null);
```

**Specification (250_001):** Only defines Wait and Assert, no Check methods. Assert methods include timeout and wait before asserting:

```csharp
// Wait (matches architecture)
bool WaitVisible(bool? expected, int? timeoutMs = null);

// Assert (waits then asserts - different from architecture!)
void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
```

**Impact:** Specification Assert methods have different behavior (they wait) than architecture Assert methods (immediate). Missing Check methods entirely.

#### Resolution Options

- [X] **Update Architecture:** Remove Check methods, make Assert methods include waiting (simpler API)
- [ ] **Update Specification:** Add Check methods, make Assert immediate without timeout parameter

---

### DIFF-003: Nullable Skip Pattern for Assert Methods

**Architecture (231_001):** Assert methods take non-nullable expected values:

```csharp
void AssertExists(string? message = null);
void AssertVisible(string? message = null);
void AssertTextEquals(string? expected, string? message = null);
```

**Specification (250_001):** Assert methods take nullable expected with skip behavior:

```csharp
void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
void AssertText(string? expected, string? message = null, int? timeoutMs = null);
```

**Impact:** Specification allows `AssertExists(null)` which skips the assertion. Architecture expects non-nullable booleans.

#### Resolution Options

- [X] **Update Architecture:** Add nullable skip pattern documentation to Assert methods
- [ ] **Update Specification:** Remove nullable expected from boolean Assert methods (keep for text/value)

---

### DIFF-004: State Method Return Types

**Architecture (231_001):** State methods return nullable bool to indicate element not found:

```csharp
bool? IsExists(int? timeoutMs = null);  // null if timeout/not found
bool? IsVisible(int? timeoutMs = null);
bool? IsEnabled(int? timeoutMs = null);
```

**Specification (250_001):** `IsExists()` returns non-nullable `bool`:

```csharp
bool IsExists();       // false if not found (no null)
bool? IsVisible();     // null if element doesn't exist
bool? IsEnabled();     // null if element doesn't exist
```

**Impact:** `IsExists()` can never return null in specification (false = not exists), but architecture allows null.

#### Resolution Options

- [X] **Update Architecture:** Change `IsExists()` to return `bool` (not nullable) - makes semantic sense
- [ ] **Update Specification:** Change `IsExists()` to return `bool?` for consistency with other state methods

---

### DIFF-005: Exception Type for Assert Failures

**Architecture (221_003):** Distinguishes between `AssertionException` (immediate checks) and `CheckFailedException` (polling checks):

```csharp
// Assert* throws AssertionException
public class AssertionException : Exception { ... }

// Check* throws CheckFailedException  
public class CheckFailedException : Exception { ... }
```

**Specification (250_001):** Only references `AssertionException`, no `CheckFailedException`:

```csharp
/// <exception cref="AssertionException">Thrown if assertion fails.</exception>
void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
```

**Impact:** Specification is missing `CheckFailedException` since it has no Check methods.

#### Resolution Options

- [X] **Update Architecture:** Consolidate to single `AssertionException` if Check methods are removed
- [ ] **Update Specification:** Add CheckFailedException and Check methods to match architecture

---

### DIFF-006: Container Interface Hierarchy

**Architecture (231_004):** Defines single `IContainerControl` interface extending `IControlObject`:

```csharp
public interface IContainerControl : IControlObject
{
    Locator ScopedLocator(string automationId);
    object? FindChild(Locator locator);
    IReadOnlyList<object> FindChildren(Locator locator);
}
```

**Specification (250_003*):** Defines three container interfaces with generic types:

```csharp
// Typed single child
public interface IContainerControlObject<T> : IControlObject where T : IControlObject
{
    T Child { get; }
}

// Dynamic scoped finding
public interface IContainerControl : IControlObject
{
    // ... more methods
}

// Typed list of children
public interface IListContainerControlObject<T> : IControlObject where T : IControlObject
{
    IReadOnlyList<T> Children { get; }
    T this[int index] { get; }
}
```

**Impact:** Architecture shows simpler pattern. Specification has evolved to generic typed containers for compile-time safety.

#### Resolution Options

- [X] **Update Architecture:** Add generic container interfaces to 231_004 to match specification evolution
- [ ] **Update Specification:** Simplify back to non-generic pattern (lose type safety)

---

### DIFF-007: TimeoutSettings Property Names

**Architecture (221_004):** Uses camelCase property names:

```csharp
public class TimeoutSettings
{
    public int DefaultWait { get; set; } = 10000;
    public int ShortWait { get; set; } = 3000;
    public int PageLoad { get; set; } = 30000;
    public int ElementFind { get; set; } = 5000;
    public int Animation { get; set; } = 500;
    public int PollingInterval { get; set; } = 250;
}
```

**Specification (250_004):** Matches architecture but with slightly different defaults:

```csharp
public class TimeoutSettings
{
    public int DefaultWait { get; set; } = 10000;  // Same
    public int PageLoad { get; set; } = 30000;     // Same
    public int ElementFind { get; set; } = 5000;   // Same
    public int Animation { get; set; } = 500;      // Same
    public int PollingInterval { get; set; } = 100; // Different! (arch=250)
}
```

**Impact:** Default `PollingInterval` differs (architecture: 250ms, specification: 100ms).

#### Resolution Options

- [X] **Update Architecture:** Change PollingInterval default to 100ms (faster polling)
- [ ] **Update Specification:** Change PollingInterval default to 250ms (match architecture)

---

### DIFF-008: IControlObject Identity Property

**Architecture (231_001):** Uses `AutomationId` string property:

```csharp
public interface IControlObject
{
    string AutomationId { get; }
    IPageObject? Page { get; }
}
```

**Specification (250_001):** Uses `Locator` object property:

```csharp
public interface IControlObject
{
    Locator Locator { get; }
    IPageObject? Page { get; }
}
```

**Impact:** Specification is more flexible (Locator supports multiple strategies). Architecture is simpler but assumes AutomationId strategy.

#### Resolution Options

- [X] **Update Architecture:** Change `AutomationId` to `Locator` property for flexibility
- [ ] **Update Specification:** Add `AutomationId` convenience property (keep Locator)

---

### DIFF-009: GetText Return Type on Missing Element

**Architecture:** Not explicitly documented what `GetText()` returns when element doesn't exist.

**Specification (250_001):** Explicitly documents nullable return:

```csharp
/// <returns>Text content, or null if element not found or has no text.</returns>
string? GetText(int? timeoutMs = null);
```

- Returns `null` if element doesn't exist
- Returns empty string `""` if element exists but has no text

**Impact:** Specification is more explicit. Architecture should clarify this behavior.

#### Resolution Options

- [ ] **Update Architecture:** Add GetText return value documentation to match specification
- [X] **No Change Needed:** Specification provides the detail that architecture lacks (complementary)

---

### DIFF-010: Method Timeout Parameter Position

**Architecture (231_001, 221_004):** Timeout is typically last or only parameter:

```csharp
bool WaitVisible(bool visible, int? timeoutMs = null);
void Click(int? timeoutMs = null);
```

**Specification (250_001):** Assert methods have timeout after message:

```csharp
void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
```

**Impact:** Parameter order consistency. Architecture doesn't show assert methods with all three parameters.

#### Resolution Options

- [X] **Update Architecture:** Show Assert method signatures with full parameter list
- [ ] **Update Specification:** Reorder to `timeoutMs` before `message` for consistency

---

### DIFF-011: Run/RunAssert Wrapper Pattern in Interfaces

**Architecture (221_001):** Defines `Run()` and `RunAssert()` protected methods in `ControlBase`:

```csharp
protected void Run(string action, Action operation);
protected void Run<T>(string action, T? value, Action operation);
protected void RunAssert<T>(string assertType, T? expected, Func<T?> getActual, string? message = null);
```

**Specification (250_001-005):** Does not document the Run/RunAssert wrapper pattern. Interface definitions focus on public API.

**Impact:** Architecture describes implementation patterns that specifications don't reference. This is appropriate separation of concerns.

#### Resolution Options

- [ ] **Update Architecture:** Keep as-is (implementation guidance belongs in architecture)
- [X] **Update Specification:** Add reference to architecture Run pattern in implementation notes

---

### DIFF-012: Page Object Control Existence Methods

**Architecture (200_000 Section 4):** Shows `IPageObject` has reference to controls but no control existence checking.

**Specification (250_002 v1.0):** Removed section 2.4 (ControlExists methods) per earlier review. Only page-level methods remain.

```csharp
// Current specification - no ControlExists methods
public interface IPageObject
{
    string Name { get; }
    bool IsLoaded(int? timeoutMs = null);
    // ... no ControlExists, WaitControlExists, etc.
}
```

**Impact:** Aligned. Both architecture and specification agree that IPageObject doesn't have ControlExists methods.

#### Resolution Options

- [ ] **No Change Needed:** Architecture and specification are aligned on this point

---

### DIFF-013: IBusyPageObject Interface

**Architecture (231_005):** Defines busy page pattern (if exists).

**Specification (250_005):** Defines `IBusyPageObject` interface:

```csharp
public interface IBusyPageObject : IPageObject
{
    bool? IsBusy(int? timeoutMs = null);
    bool WaitForNotBusy(int? timeoutMs = null);
    void AssertNotBusy(string? message = null, int? timeoutMs = null);
}
```

**Impact:** Need to verify architecture has corresponding pattern documentation.

#### Resolution Options

- [ ] **Update Architecture:** Ensure 231_005_BusyPagePattern.spx.md defines IBusyPageObject interface
- [X] **Update Specification:** Align IBusyPageObject with whatever architecture defines

---

### DIFF-014: IContainerControl Dynamic vs Static Child Finding

**Architecture (231_004):** Shows `FindChild(Locator)` returning `object?`:

```csharp
object? FindChild(Locator locator);
IReadOnlyList<object> FindChildren(Locator locator);
```

**Specification (250_003a):** Uses generic methods returning typed controls:

```csharp
T FindControl<T>(Locator locator, int? timeoutMs = null) where T : IControlObject;
IReadOnlyList<T> FindControls<T>(Locator locator, int? timeoutMs = null) where T : IControlObject;
```

**Impact:** Specification provides stronger typing for control finding within containers.

#### Resolution Options

- [X] **Update Architecture:** Update 231_004 to use generic FindControl`<T>` pattern
- [ ] **Update Specification:** Add non-generic FindChild for compatibility (in addition to generic)

---

## Summary Table

| ID       | Topic                     | Architecture        | Specification   | Recommended Action  |
| -------- | ------------------------- | ------------------- | --------------- | ------------------- |
| DIFF-001 | ITestLogger signatures    | Full context params | Locator-based   | Review needed       |
| DIFF-002 | Wait/Check/Assert pattern | 3 patterns          | 2 patterns      | Decide on Check     |
| DIFF-003 | Nullable skip pattern     | Not documented      | Documented      | Update arch         |
| DIFF-004 | IsExists return type      | `bool?`           | `bool`        | Update arch         |
| DIFF-005 | CheckFailedException      | Defined             | Missing         | Depends on DIFF-002 |
| DIFF-006 | Container generics        | Simple              | Generic `<T>` | Update arch         |
| DIFF-007 | PollingInterval default   | 250ms               | 100ms           | Standardize         |
| DIFF-008 | Identity property         | AutomationId        | Locator         | Update arch         |
| DIFF-009 | GetText null behavior     | Undocumented        | Documented      | Update arch         |
| DIFF-010 | Timeout param position    | Various             | After message   | Standardize         |
| DIFF-011 | Run/RunAssert pattern     | Documented          | Not referenced  | Keep separate       |
| DIFF-012 | Page ControlExists        | None                | None            | Aligned ✅          |
| DIFF-013 | IBusyPageObject           | Check pattern file  | Defined         | Verify alignment    |
| DIFF-014 | Container FindChild       | `object?`         | `T` generic   | Update arch         |

---

## Recommended Resolution Priority

### High Priority (API Contract)

1. **DIFF-002** - Wait/Check/Assert pattern decision
2. **DIFF-004** - IsExists return type standardization
3. **DIFF-008** - Identity property (Locator vs AutomationId)

### Medium Priority (Type Safety)

4. **DIFF-006** - Container interface generics
5. **DIFF-014** - Generic FindControl`<T>`

### Low Priority (Documentation)

6. **DIFF-001** - Logger signatures
7. **DIFF-003** - Nullable skip pattern docs
8. **DIFF-007** - PollingInterval default
9. **DIFF-009** - GetText null behavior docs
10. **DIFF-010** - Parameter ordering

---

## Action Required

For each difference, select one checkbox and implement the change:

```
Example:
### DIFF-001: ITestLogger Interface Signatures
- [x] **Update Architecture:** Simplify ITestLogger to match specification
- [ ] **Update Specification:** Expand ITestLogger to match architecture
```

**Next Steps:**

1. Review each difference with stakeholders
2. Select resolution option for each
3. Create implementation tasks
4. Update documents accordingly

---

**Created:** January 7, 2026
**Author:** GitHub Copilot
**Review Status:** Pending stakeholder review
