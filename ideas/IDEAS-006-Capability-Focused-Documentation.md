# IDEAS-006: Capability-Focused Documentation

**Created:** January 9, 2026  
**Status:** Open  
**Priority:** High  
**Related:** [IDEAS-005](IDEAS-005-Specification-Granularity.md), [IDEAS-004](IDEAS-004-Specification-Value-Analysis.md)

---

## 1. Building on IDEAS-005

IDEAS-005 proposed "thin specifications" — reducing 500-line specs to 50 lines by referencing source instead of duplicating code.

This document goes further: **Don't describe implementations at all. Describe capabilities.**

---

## 2. The Problem with Code Examples in Docs

### Current State: Full Interface Examples

From `203_001_CoreLayer.spx.md`:

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

### Problems with This Approach

| Problem | Example | Risk |
|---------|---------|------|
| **Signature drift** | Doc says `string? GetText()`, source says `string GetText(int? timeout)` | Confusion about which is correct |
| **Parameter mistakes** | Doc shows `WaitTextEquals(string? expected, int?)` but actual might differ | LLM generates wrong code |
| **Return type errors** | Doc shows `bool WaitTextEquals`, source might return `Task<bool>` | Compile errors |
| **Missing parameters** | Doc omits `timeoutMs` on some methods | Inconsistent understanding |
| **Outdated examples** | Interface evolved, doc wasn't updated | Technical debt |

### The Core Issue

> **Code examples in docs are promises we can't keep.**

The moment you write `void Enter(string text)` in a doc, you've created a contract. If the actual source is `void Enter(string? text, int? timeoutMs = null)`, you have:
- A lie in your documentation
- Potential for LLM-generated incorrect code
- Confusion for developers

---

## 3. Proposed Solution: Capability Descriptions

### Principle: Describe WHAT, Not HOW

Instead of showing interface code, describe the **capability** in natural language:

| Instead of This | Write This |
|-----------------|------------|
| `string? GetText()` | "Can retrieve text content" |
| `void Enter(string text)` | "Can enter/type text" |
| `bool WaitTextEquals(...)` | "Can wait for specific text" |
| `void AssertTextEquals(...)` | "Can assert text matches expected" |

### Example: Capability-Focused Layer Description

**Before (Code-Heavy):**
```markdown
## Core Layer Interfaces

### ITextControlObject

```csharp
public interface ITextControlObject : IControlObject
{
    string? GetText();
    bool WaitTextEquals(string? expected, int? timeoutMs = null);
    void AssertTextEquals(string? expected, string? message = null);
    void AssertTextContains(string? expected, string? message = null);
}
```
```

**After (Capability-Focused):**
```markdown
## Core Layer Capabilities

### Text Display (ITextControlObject)

Controls that display text provide:
- **Read:** Get current text content
- **Wait:** Wait for text to match expected value
- **Assert:** Verify text equals or contains expected

Extends: IControlObject

For method signatures, see source: `ITextControlObject.cs`
```

---

## 4. Capability Categories

### Proposed Capability Vocabulary

Instead of listing methods, describe capabilities:

| Capability | Meaning | Methods Implied |
|------------|---------|-----------------|
| **Read** | Get current value | `Get*()` |
| **Write** | Set/change value | `Set*()`, `Enter()`, `Clear()` |
| **Wait** | Poll until condition | `Wait*()` |
| **Assert** | Verify and throw | `Assert*()` |
| **Toggle** | Switch between states | `Toggle()`, `SetState()` |
| **Select** | Choose from options | `Select*()` |
| **Navigate** | Move within structure | `Scroll*()`, `Expand()`, `Collapse()` |

### Capability Patterns

**State capabilities:**
- Read state → Get methods
- Wait for state → Wait methods  
- Assert state → Assert methods

**Value capabilities:**
- Read value → Get methods
- Write value → Set/Enter methods
- Clear value → Clear methods

**Action capabilities:**
- Click/tap → Click methods
- Toggle → Toggle methods
- Select → Select methods

---

## 5. Revised Documentation Structure

### For Architecture Documents (200_*)

**Current Structure:**
```markdown
## Interfaces
[Full interface code with all methods]

## Examples
[Implementation code]
```

**Proposed Structure:**
```markdown
## Capabilities

### Control Object (base)
All controls can: check existence, check visibility, wait for state, assert state

### Text Display
Controls showing text can: read text, wait for text, assert text

### Text Input
Controls accepting input can: enter text, clear text, set text

### Toggle
Controls with on/off state can: read state, toggle, set state

## Implementation
See: `src/Brinell.Core/Interfaces/`
```

### For Pattern Documents (231_*)

**Current Structure:**
```markdown
## Pattern
[Abstract description]

## Implementation
[50-200 lines of concrete code]
```

**Proposed Structure:**
```markdown
## Pattern: Control Object

### Intent
Wrap platform elements with consistent capability interface

### Capabilities Provided
- State checking (exists, visible, enabled)
- State waiting (with timeout)
- State assertion (with message)
- Text retrieval

### When to Use
- All UI controls
- When you need testable state operations

### Structure
```
Control
├── Identity (locator, scope)
├── State ops (is*, wait*, assert*)
└── Platform element (internal)
```

### Implementation
See: `src/Brinell.Core/Controls/ControlBase.cs`
```

### For Specification Documents (250_*)

**Current Structure:**
```markdown
## Interface
[Complete interface code - 50 lines]

## Behavior
[Detailed method descriptions - 200 lines]

## Boundary
[Edge cases - 50 lines]
```

**Proposed Structure:**
```markdown
## IControlObject

### Capabilities
- State: exists, visible, enabled
- Waiting: with nullable skip pattern
- Assertions: throw on failure
- Text: read content

### Rules
1. State methods return null when element missing (not false)
2. Wait methods return bool, never throw
3. Assert methods throw AssertionException
4. Null expected = skip operation

### Boundaries
| Missing element | State returns null, Wait returns false |
| Null expected | Skip operation, return immediately |
| Null timeout | Use default from context |

### Source
Interface: `IControlObject.cs`
Implementation: `ControlBase.cs`
Tests: `ControlBaseTests.cs`
```

---

## 6. The Capability Matrix

Replace detailed interface listings with a capability matrix:

```markdown
## Control Capability Matrix

| Control | State | Text | Edit | Click | Toggle | Select | Range |
|---------|-------|------|------|-------|--------|--------|-------|
| Button | ✓ | ✓ | | ✓ | | | |
| Label | ✓ | ✓ | | | | | |
| Entry | ✓ | ✓ | ✓ | ✓ | | | |
| CheckBox | ✓ | ✓ | | ✓ | ✓ | | |
| Picker | ✓ | ✓ | | ✓ | | ✓ | |
| Slider | ✓ | | | ✓ | | | ✓ |

**Capability definitions:**
- State: exists, visible, enabled + wait + assert
- Text: read text + wait + assert
- Edit: enter, clear, set text
- Click: click, double-click, long-press
- Toggle: toggle, set on/off
- Select: select by text/index
- Range: get/set numeric value
```

This replaces pages of interface definitions with a single scannable table.

---

## 7. What Stays in Docs vs Source

### In Documentation

| Content | Purpose | Example |
|---------|---------|---------|
| **Capability names** | What it can do | "Text display", "Toggle state" |
| **Rules** | Behavioral contracts | "Returns null if missing" |
| **Boundaries** | Edge cases | "Null expected = skip" |
| **Relationships** | Inheritance/composition | "Extends IControlObject" |
| **References** | Where to find details | "See: IControlObject.cs" |

### In Source Code

| Content | Purpose | Example |
|---------|---------|---------|
| **Method signatures** | Exact contract | `bool? IsVisible()` |
| **Parameters** | Exact types/names | `int? timeoutMs = null` |
| **Return types** | Exact types | `string?`, `bool?` |
| **XML docs** | Method-level behavior | `/// <returns>null if missing</returns>` |
| **Implementation** | How it works | Actual code |

### The Rule

> **Documentation describes capabilities and rules.**  
> **Source defines exact signatures and implementation.**

---

## 8. Handling Pre-Implementation Specs

### The Chicken-and-Egg Problem

Q: How do you describe capabilities before source exists?

A: **You don't need exact signatures to describe capabilities.**

**Before implementation:**
```markdown
## INewControl

### Capabilities
- Can expand/collapse sections
- Can read expanded state
- Can wait for expansion state
- Can assert expansion state

### Rules
- Follows state/wait/assert pattern
- Returns null if element missing

### Notes
- See IToggleControl for similar pattern
- Specific methods TBD during implementation
```

**After implementation:**
```markdown
## INewControl

### Capabilities
- Can expand/collapse sections
- Can read expanded state
- Can wait for expansion state
- Can assert expansion state

### Rules
- Follows state/wait/assert pattern
- Returns null if element missing

### Source
- Interface: `IExpandableControl.cs`
- Implementation: `ExpandableControlBase.cs`
```

The capability description stays the same. Only the "Source" section gets added.

---

## 9. Benefits

### For Documentation Maintainers

| Benefit | Why |
|---------|-----|
| **Smaller files** | 50 lines vs 500 lines |
| **No sync issues** | No signatures to keep updated |
| **Easier review** | Capabilities don't change often |
| **Less technical debt** | No stale code examples |

### For Developers

| Benefit | Why |
|---------|-----|
| **Clear capabilities** | Know what controls can do |
| **Source is truth** | Always look at .cs for signatures |
| **Less confusion** | No conflicting information |
| **Better IntelliSense** | IDE shows real methods |

### For LLMs

| Benefit | Why |
|---------|-----|
| **Smaller context** | Capability descriptions are compact |
| **No wrong signatures** | Can't generate wrong method names from docs |
| **Clear rules** | Numbered rules are easy to follow |
| **Source reference** | Knows where to look for implementation |

---

## 10. Migration Path

### Phase 1: Stop Adding Code to Docs

New documentation should use capability descriptions, not code:

```markdown
❌ Don't write:
```csharp
public interface IFoo
{
    string GetBar();
    void SetBar(string value);
}
```

✅ Do write:
Provides read/write access to Bar property.
See: `IFoo.cs`
```

### Phase 2: Create Capability Reference

Create a single capability reference document:

```markdown
# Capability Reference

## State Capabilities
All controls have: exists, visible, enabled
- Check: Is*() methods return current state
- Wait: Wait*() methods poll until condition
- Assert: Assert*() methods throw on failure

## Text Capabilities
Controls displaying text have:
- Read: Get text content
- Wait: Wait for text match
- Assert: Verify text equals/contains

[etc.]
```

### Phase 3: Slim Existing Docs

Replace code blocks with capability descriptions + source references:

1. Identify code blocks in 200_*/231_*/250_*
2. Replace with capability description
3. Add "See: source-file.cs" reference
4. Verify doc is under 100 lines

---

## 11. Template: Capability-Focused Spec

```markdown
# [ID] [Name] Specification

**Edition:** 🟢Ⅰ Lite

## Capabilities

[List what this component can do, not how]

- Capability 1
- Capability 2
- Capability 3

## Rules

1. [Behavioral rule - testable]
2. [Behavioral rule - testable]
3. [Behavioral rule - testable]

## Boundaries

| Scenario | Behavior |
|----------|----------|
| Edge case 1 | Expected result |
| Edge case 2 | Expected result |

## Relationships

- Extends: [parent interface/class]
- Used by: [consumers]
- Related: [similar components]

## Source

- Interface: `path/to/Interface.cs`
- Implementation: `path/to/Implementation.cs`
- Tests: `path/to/Tests.cs`
```

**Target: Under 50 lines.**

---

## 12. Example Transformation

### Before: 203_001_CoreLayer (excerpt)

```markdown
## 3. Key Interfaces

### IControlObject

The base interface for all controls:

```csharp
public interface IControlObject
{
    Locator Locator { get; }
    IElementScope Scope { get; }
    IPageObject? Page { get; }
    
    bool IsExists();
    bool? IsVisible();
    bool? IsEnabled();
    
    bool WaitExists(bool? expected, int? timeoutMs = null);
    bool WaitVisible(bool? expected, int? timeoutMs = null);
    bool WaitEnabled(bool? expected, int? timeoutMs = null);
    
    void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    // ... 20 more lines
}
```

### ITextControlObject

```csharp
public interface ITextControlObject : IControlObject
{
    string? GetText();
    bool WaitTextEquals(string? expected, int? timeoutMs = null);
    // ... 10 more lines
}
```
```

### After: Capability-Focused

```markdown
## 3. Core Layer Capabilities

### Base Control (IControlObject)
All controls provide:
- **Identity:** locator, scope, page reference
- **State:** check exists/visible/enabled
- **Waiting:** poll for state with timeout
- **Assertions:** verify state or throw

### Text Display (ITextControlObject)
Extends base with:
- **Read:** get text content
- **Wait:** wait for text match
- **Assert:** verify text equals/contains

### Text Input (IEditableTextControlObject)
Extends text display with:
- **Write:** enter, clear, set text

**Source:** `src/Brinell.Core/Interfaces/`
```

**Reduction: ~60 lines → ~20 lines. No signature to get wrong.**

---

## 13. Questions to Resolve

1. **What about complex rules?** Some behaviors need more than one sentence.
   - Answer: Put complex rules in source XML docs, reference from spec.

2. **What about design rationale?** Why was it designed this way?
   - Answer: Architecture docs (200_*) explain WHY. Keep it there.

3. **What about examples?** Sometimes examples help understanding.
   - Answer: Examples go in `/docs/` or `/samples/`, not specs.

4. **What about versioning?** Capabilities might change.
   - Answer: Capability changes are rare. When they happen, update the ~10 affected lines.

---

## 14. Summary

| Aspect | IDEAS-005 | IDEAS-006 (This) |
|--------|-----------|------------------|
| Code in docs | Reference only | None (capabilities only) |
| Interface signatures | In source only | In source only |
| Method details | In source only | In source only |
| Spec content | Rules + boundaries | Capabilities + rules + boundaries |
| Risk of drift | Low | Very low |
| Maintenance | Easy | Easier |

### The Principle

> **Describe capabilities, not implementations.**  
> **Source code is the only signature authority.**  
> **If you can't write it without exact method names, it belongs in source.**

---

## Related Documents

- [IDEAS-005: Specification Granularity](IDEAS-005-Specification-Granularity.md)
- [IDEAS-004: Architecture vs Specification Value](IDEAS-004-Specification-Value-Analysis.md)
- [Questions-to-Blocks.md](../SPX/Docs/V7/Overview/Questions-to-Blocks.md)

---

**Version:** 1.0  
**Status:** Open  
**Last Review:** January 9, 2026
