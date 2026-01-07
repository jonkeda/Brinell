# 203.001 Core Layer

**Block Type:** LYR (Layer)
**ID:** 203.001
**Title:** Core Layer Definition
**Status:** Draft
**Version:** 1.0

---

## 1. Overview

The Core layer is the innermost layer of the Brinell architecture. It contains **abstractions and cross-cutting concerns** — interfaces, contracts, exception types, configuration definitions, and platform-agnostic utilities. It has **zero dependencies** on external packages.

### Layer Identity

- **Package:** `Brinell.Core`
- **Namespace Root:** `Brinell.Core`
- **Dependencies:** None (pure abstractions + cross-cutting concerns)
- **Dependents:** All platform packages, all test projects

---

## 2. Purpose

The Core layer serves as the **stable contract** that all platform implementations must follow. By defining interfaces in Core:

1. **Platform Independence** — Test code can be written against interfaces, not implementations
2. **Swappable Implementations** — Different platforms implement the same contracts
3. **Compile-Time Safety** — Interface changes break compilation, catching issues early
4. **Documentation** — Interfaces serve as living documentation of capabilities

---

## 3. Contents

### 3.1 Control Interfaces

All control object interfaces are defined in Core:

```
Brinell.Core/
├── Interfaces/
│   ├── IControlObject.cs           # Base for all controls
│   ├── IClickableControlObject.cs  # Click capability
│   ├── ITextControlObject.cs       # Text display
│   ├── IEditableTextControlObject.cs
│   ├── IToggleControlObject.cs
│   ├── ISelectorControlObject.cs
│   ├── IRangeControlObject.cs
│   ├── IContainerControlObject.cs
│   ├── IItemsControlObject.cs
│   └── ... (complete set)
```

**Note:** The complete interface catalog is defined in specifications.

### 3.2 Page Interfaces

```
Brinell.Core/
├── IPageObject.cs                      # Base page interface
└── ITestContext.cs                     # Test execution context
```

### 3.3 Exception Types

```
Brinell.Core/
├── Exceptions/
│   ├── ControlNotFoundException.cs
│   ├── ControlNotVisibleException.cs
│   ├── ControlNotEnabledException.cs
│   ├── TimeoutException.cs
│   └── AssertionException.cs
```

### 3.4 Configuration

```
Brinell.Core/
├── Configuration/
│   ├── ITimeoutConfiguration.cs
│   ├── IRetryConfiguration.cs
│   └── ILoggingConfiguration.cs
```

### 3.5 Cross-Cutting Concerns

Core contains platform-agnostic implementations for cross-cutting concerns:

```
Brinell.Core/
├── Logging/
│   ├── ITestLogger.cs              # Logging contract
│   ├── LogLevel.cs                 # Log level enum
│   └── ConsoleLogger.cs            # Default implementation
├── Timeout/
│   ├── TimeoutSettings.cs          # Default timeout values
│   └── WaitHelper.cs               # Platform-agnostic wait logic
├── Retry/
│   ├── RetryPolicy.cs              # Retry configuration
│   └── RetryExecutor.cs            # Retry execution logic
└── Assertions/
    └── AssertionHelper.cs          # Common assertion logic
```

**Key:** Cross-cutting implementations are **technology-agnostic** — they use only .NET types and Core abstractions.

---

## 4. Design Rules

### 4.1 No Technology-Specific Code

Core must not contain any technology-specific implementation code:

**Allowed:**

- Interface definitions
- Abstract base classes (if needed for contract)
- Exception classes (simple data containers)
- Enums and constants
- Extension methods on interfaces
- Cross-cutting utilities (logging, retry, timeout) using only .NET types

**NOT Allowed:**

- Any code that references automation libraries (Appium, Selenium, Playwright)
- Any code that uses platform-specific types
- Any concrete control implementations

### 4.2 No External Dependencies

Core's only dependency must be the .NET runtime. It cannot reference:

- ❌ Appium libraries
- ❌ Selenium libraries
- ❌ Playwright libraries
- ❌ Platform-specific SDKs
- ❌ Third-party packages

### 4.3 Stable Contracts

Once an interface is published:

- Methods can be **added** (with default implementations if needed)
- Methods cannot be **removed** without major version bump
- Method signatures cannot be **changed** without major version bump

---

## 5. Interface Design Principles

### 5.1 Capability-Based

Interfaces represent **capabilities**, not control types:

```csharp
// ✓ Good: Capability interface
public interface IClickableControlObject : IControlObject
{
    void Click();
    void DoubleClick();
    bool WaitClickable(bool clickable = true, int? timeoutMs = null);
    void AssertClickable(string? message = null);
}

// ✗ Bad: Control-type interface
public interface IButtonControlObject  // Don't do this
```

### 5.2 Single Responsibility

Each interface defines **one capability** with state, wait, and assert methods:

```csharp
// ✓ Good: Single responsibility with full method set
public interface ITextControlObject : IControlObject
{
    string? GetText();
    bool WaitTextEquals(string? expected, int? timeoutMs = null);
    void AssertTextEquals(string? expected, string? message = null);
    void AssertTextContains(string? expected, string? message = null);
}

public interface IEditableTextControlObject : ITextControlObject
{
    void Enter(string text);
    void Clear();
    void SetText(string text);
}
```

### 5.3 Inheritance and Base Classes

Use inheritance and base classes where possible to maximize code reuse:

```csharp
// Base class hierarchy provides common functionality
public abstract class ControlBase : IControlObject { ... }
public abstract class TextControlBase : ControlBase, ITextControlObject { ... }
public abstract class EditableTextControlBase : TextControlBase, IEditableTextControlObject { ... }

// Concrete controls extend appropriate base class
public class EntryControl : EditableTextControlBase, IClickableControlObject
{
    // Only platform-specific code here
}
```

### 5.4 Multiple Interface Implementation

Controls implement additional interfaces beyond their base class to express all capabilities:

```csharp
// Entry has editable text (from base) AND click capability (additional interface)
public class EntryControl : EditableTextControlBase, IClickableControlObject

// CheckBox has toggle (from base) AND click capability
public class CheckBoxControl : ToggleControlBase, IClickableControlObject
```

---

## 6. Namespace Structure

```
Brinell.Core
├── Brinell.Core.Interfaces       # All control and page interfaces
├── Brinell.Core.Exceptions       # Exception types
├── Brinell.Core.Configuration    # Configuration contracts
├── Brinell.Core.Logging          # Logging contracts and implementations
├── Brinell.Core.Timeout          # Timeout and wait utilities
├── Brinell.Core.Retry            # Retry policies and execution
└── Brinell.Core.Assertions       # Common assertion logic
```

---

## 7. Package Dependencies

```
Brinell.Core
└── (no dependencies)
```

**Target Framework:** .NET Standard 2.0 (maximum compatibility)

---

## 8. Validation Rules

The Core layer is valid when:

- [ ] No references to automation libraries
- [ ] No references to platform-specific code
- [ ] No technology-specific implementation code
- [ ] Cross-cutting implementations use only .NET types
- [ ] Interface method signatures include nullable types and timeout parameters
- [ ] Compiles against .NET Standard 2.0

---

## Related Documents

- [ADR-001 Clean Architecture](../202_Decisions/202_001_CleanArchitecture.spx.md)
- [ADR-002 Interface-First](../202_Decisions/202_002_InterfaceFirst.spx.md)
- [Platform Layer](203_002_PlatformLayer.spx.md)
- [FR-103 Interface Hierarchy](../../100_requirements/120_functional/120_103_InterfaceHierarchy.spx.md)
