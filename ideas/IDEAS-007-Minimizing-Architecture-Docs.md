# IDEAS-007: Minimizing Architecture Documents

**Created:** January 9, 2026  
**Status:** Open  
**Priority:** High  
**Related:** [IDEAS-005](IDEAS-005-Specification-Granularity.md), [IDEAS-006](IDEAS-006-Capability-Focused-Documentation.md)

---

## 1. The Problem

Architecture documents contain code examples with specific method signatures that:
- May be incorrect or outdated
- Will drift from actual source
- Cause confusion for LLMs and humans
- Create maintenance burden

**Solution:** Show structure and patterns with `...` placeholders, not exact signatures.

---

## 2. Target Documents

### 2.1 [203_001_CoreLayer.spx.md](../specs2/200_architecture/203_Layers/203_001_CoreLayer.spx.md)

**Current:** ~250 lines  
**Target:** ~150 lines

### 2.2 [211_001_Interfaces.spx.md](../specs2/200_architecture/211_Modules/211_001_Interfaces.spx.md)

**Current:** ~200 lines  
**Target:** ~120 lines

---

## 3. Proposed Changes: 203_001_CoreLayer

### 3.1 Section 5.2 — Replace Full Signatures with Placeholders

**Current:**
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

**Proposed:**
```csharp
// ✓ Good: Single responsibility with full method set
public interface ITextControlObject : IControlObject
{
    ... GetText();
    ... WaitText...(...);
    ... AssertText...(...);
}

public interface IEditableTextControlObject : ITextControlObject
{
    ... Enter(...);
    ... Clear();
    ... SetText(...);
}

// See source for exact signatures
```

### 3.2 Section 5.1 — Simplify Clickable Example

**Current:**
```csharp
// ✓ Good: Capability interface
public interface IClickableControlObject : IControlObject
{
    void Click();
    void DoubleClick();
    bool WaitClickable(bool clickable = true, int? timeoutMs = null);
    void AssertClickable(string? message = null);
}
```

**Proposed:**
```csharp
// ✓ Good: Capability interface
public interface IClickableControlObject : IControlObject
{
    ... Click();
    ... DoubleClick();
    ... WaitClickable(...);
    ... AssertClickable(...);
}
```

### 3.3 Section 5.3 — Simplify Base Class Example

**Current:**
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

**Proposed:** Keep as-is (shows structure, not signatures)

### 3.4 Sections to Remove Entirely

| Section | Reason | Replacement |
|---------|--------|-------------|
| Section 3.3 Exception Types | File list only, no value | Reference to source folder |
| Section 3.4 Configuration | File list only, no value | Reference to source folder |
| Section 3.5 Cross-Cutting Concerns | File list with no explanation | Merge into Section 3 as bullet list |

### 3.5 Sections to Condense

**Section 3 (Contents)** — Currently ~60 lines of file listings. Condense to:

```markdown
### 3. Contents

Core layer contains:

- **Interfaces/** — Control and page interfaces (IControlObject, ITextControlObject, etc.)
- **Exceptions/** — ControlNotFoundException, TimeoutException, AssertionException
- **Configuration/** — ITimeoutConfiguration, IRetryConfiguration
- **Logging/** — ITestLogger, ConsoleLogger
- **Timeout/** — TimeoutSettings, WaitHelper
- **Retry/** — RetryPolicy, RetryExecutor
- **Assertions/** — AssertionHelper

See source: `src/Brinell.Core/`
```

**Reduction:** ~60 lines → ~15 lines

---

## 4. Proposed Changes: 211_001_Interfaces

### 4.1 Section 4.2 — Replace Parameter Pattern Examples

**Current:**
```csharp
// Nullable expected: null means skip the operation entirely
bool WaitExists(bool? expected, int? timeoutMs = null);
void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
void AssertText(string? expected, string? message = null, int? timeoutMs = null);

// Timeout: null means use default from TimeoutSettings
bool WaitVisible(bool? expected, int? timeoutMs = null);

// Input values: null means skip the action
void Enter(string? text, int? timeoutMs = null);
void SetValue(double? value, int? timeoutMs = null);
void SelectByText(string? text, int? timeoutMs = null);
```

**Proposed:**
```csharp
// Nullable expected: null means skip the operation entirely
... Wait...(... expected, ... timeoutMs);
... Assert...(... expected, ... message, ... timeoutMs);

// Input values: null means skip the action
... Enter(... text, ... timeoutMs);
... SetValue(... value, ... timeoutMs);

// See source for exact signatures
```

### 4.2 Sections That Can Be Removed

| Section | Reason | Action |
|---------|--------|--------|
| Section 6 (Namespace Organization) | File list duplicates Section 3.2 table | Remove entirely |
| Section 2 (Purpose) | Generic, adds no specific value | Merge 1 line into Overview |

### 4.3 Section 5 — Already Good

The hierarchy tree is valuable and has no signatures:
```
IControlObject (base)
│
├── IClickableControlObject
│   └── Click, DoubleClick, RightClick, LongPress
```

**Keep as-is** — shows method names without signatures.

---

## 5. Summary of Reductions

### 203_001_CoreLayer.spx.md

| Section | Current | Proposed | Change |
|---------|---------|----------|--------|
| Overview | 25 | 25 | Keep |
| Purpose | 15 | 15 | Keep |
| Contents | 60 | 15 | Condense file lists |
| Design Rules | 40 | 40 | Keep |
| Interface Design | 60 | 35 | Use `...` placeholders |
| Namespace Structure | 15 | 15 | Keep |
| Package Dependencies | 10 | 10 | Keep |
| Validation Rules | 15 | 15 | Keep |
| **Total** | **~250** | **~170** | **-32%** |

### 211_001_Interfaces.spx.md

| Section | Current | Proposed | Change |
|---------|---------|----------|--------|
| Overview | 20 | 20 | Keep |
| Purpose | 15 | 0 | Merge into Overview |
| Interface Categories | 60 | 60 | Keep (already references specs) |
| Interface Design Patterns | 50 | 30 | Use `...` placeholders |
| Interface Hierarchy | 25 | 25 | Keep |
| Namespace Organization | 15 | 0 | Remove (duplicate) |
| Validation Rules | 15 | 15 | Keep |
| **Total** | **~200** | **~150** | **-25%** |

---

## 6. The Pattern: `...` Placeholders

### When to Use `...`

Use `...` when showing:
- Method signatures where parameters may change
- Return types where exact type may evolve
- Any code that will be duplicated in source

### When NOT to Use `...`

Keep exact names for:
- Interface names (e.g., `ITextControlObject`)
- Method names (e.g., `GetText`, `Enter`, `Clear`)
- Inheritance relationships (e.g., `extends IControlObject`)
- Class names (e.g., `ControlBase`, `TextControlBase`)

### Example Pattern

```csharp
// Shows: structure, inheritance, method names
// Hides: parameters, return types, exact signatures
public interface ITextControlObject : IControlObject
{
    ... GetText();           // Method exists
    ... WaitText...(...);    // Wait pattern exists
    ... AssertText...(...);  // Assert pattern exists
}
```

---

## 7. Additional Removals Across All Architecture Docs

### 7.1 Remove File Listings

**Current pattern (bad):**
```markdown
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
```

**Proposed pattern (good):**
```markdown
### Exception Types

Exception classes: `ControlNotFoundException`, `TimeoutException`, `AssertionException`, etc.

See: `src/Brinell.Core/Exceptions/`
```

### 7.2 Remove Repeated Information

Several architecture docs repeat the same information:
- Interface hierarchy appears in 202_004, 211_001, 200_INDEX
- Package structure appears in 203_001, 211_001, 200_000_Overview

**Rule:** Define once, reference elsewhere.

### 7.3 Remove "Purpose" Sections That Are Obvious

Many docs have a "Purpose" section that restates the overview:

**Example (redundant):**
```markdown
## 1. Overview
The Interfaces module contains all control interfaces...

## 2. Purpose
The Interfaces module provides:
1. Contracts — Define what each control capability provides
2. Abstraction — Hide platform-specific implementation details
...
```

**Proposed:** Merge into Overview or remove.

---

## 8. Implementation Plan

### Phase 1: Update 203_001_CoreLayer.spx.md

1. Replace code examples with `...` placeholders (Section 5)
2. Condense Contents section (Section 3)
3. Keep Design Rules, Validation as-is

### Phase 2: Update 211_001_Interfaces.spx.md

1. Replace code examples with `...` placeholders (Section 4.2)
2. Remove Section 6 (Namespace Organization)
3. Merge Section 2 (Purpose) into Section 1

### Phase 3: Apply Pattern to Other Docs

Apply same pattern to:
- 231_001_ControlObjectPattern.spx.md
- 211_002_BaseClasses.spx.md
- Other architecture docs with code examples

---

## 9. Before/After Example

### Before (203_001 Section 5.2)

```markdown
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
```

### After

```markdown
### 5.2 Single Responsibility

Each interface defines **one capability** with state, wait, and assert methods:

```csharp
// ✓ Good: Single responsibility with full method set
public interface ITextControlObject : IControlObject
{
    ... GetText();
    ... WaitText...(...);
    ... AssertText...(...);
}

public interface IEditableTextControlObject : ITextControlObject
{
    ... Enter(...);
    ... Clear();
    ... SetText(...);
}

// See source for exact signatures
```
```

**Same structure. No signature drift. Still understandable.**

---

## 10. Benefits

| Benefit | Description |
|---------|-------------|
| **No drift** | `...` can't be wrong |
| **Smaller files** | ~25-30% reduction |
| **Clearer intent** | Shows pattern, not implementation |
| **Easier maintenance** | No signatures to update |
| **LLM-friendly** | Less noise, same signal |

---

## 11. Risks

| Risk | Mitigation |
|------|------------|
| Too abstract | Keep method names visible |
| Loss of examples | Source is the example |
| Harder to understand | Hierarchy tree shows structure |

---

## Related Documents

- [IDEAS-005: Specification Granularity](IDEAS-005-Specification-Granularity.md)
- [203_001_CoreLayer.spx.md](../specs2/200_architecture/203_Layers/203_001_CoreLayer.spx.md)
- [211_001_Interfaces.spx.md](../specs2/200_architecture/211_Modules/211_001_Interfaces.spx.md)

---

**Version:** 1.0  
**Status:** Open  
**Last Review:** January 9, 2026
