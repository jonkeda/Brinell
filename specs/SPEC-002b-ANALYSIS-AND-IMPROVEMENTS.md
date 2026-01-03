# SPEC-002b: Enhanced Design Analysis & Improvement Proposals

**Version:** 1.0  
**Status:** For Review  
**Date:** January 2026

---

## Executive Summary

This document proposes enhancements to the control object hierarchy and interface design based on careful analysis of:
- REQ-001 (Functional Requirements)
- Current MAUI implementation (AppiumElement-based)
- Current Blazor/Playwright implementation (ILocator-based)
- Industry best practices for UI test automation

---

## 1. Current State Analysis

### 1.1 Control Hierarchy Implementation

**Current MAUI Hierarchy:**
```
ControlBase (IControlObject)
├── ContentControlBase (IContentControl)
│   ├── ButtonControl (IButton)
│   └── LabelControl (ILabel)
├── TextControlBase (ITextControl)
│   ├── EntryControl (TextInput)
│   ├── EditorControl (MultilineText)
│   └── SearchBarControl
├── ToggleControlBase (IToggleControl)
│   ├── CheckBoxControl (ICheckBox)
│   └── SwitchControl
├── SelectorControlBase (ISelectorControl)
│   ├── PickerControl
│   └── CollectionViewControl
├── RangeControlBase (IRangeControl)
│   └── SliderControl
├── ItemsControlBase (IItemsControl)
│   └── CollectionViewControl, CarouselView
└── ContainerControlBase (IContainerControl)
    ├── ScrollViewControl
    └── FrameControl
```

**Current Blazor Hierarchy:**
Similar structure using Playwright ILocator instead of AppiumElement

### 1.2 Element Location Strategy

**Current Implementation:**
- AutomationId only (data-automation-id, automation ID attribute)
- Container-scoped searching supported
- No alternative location methods

**Limitations:**
- Cannot adapt to applications without AutomationId
- Cannot use other reliable locators (name, id, XPath, CSS selector)
- No fallback strategy for dynamic or legacy applications

---

## 2. Proposed Improvements

### Improvement A: Flexible Element Location Strategy

**Issue:** Currently only AutomationId is used. Many applications use different location strategies.

**Proposal:**
1. Create `IElementLocator` interface with multiple strategies
2. Support: AutomationId, ElementId, ElementName, XPath, CSS Selector, Custom
3. Allow platform-specific selector builders
4. Provide fallback/retry logic

**Benefits:**
- Works with legacy applications
- Supports multiple platforms seamlessly
- Enables adaptation without core changes
- Allows framework extension by users

---

### Improvement B: Path-Based Element Selection

**Issue:** Selecting deeply nested elements requires manual container passing.

**Proposal:**
Create an element path language supporting:
```
Basic syntax:
  "automationId"                    - Direct element
  "@id=elementId"                   - By HTML ID
  "@name=elementName"               - By name
  "@xpath=//button[@id='ok']"       - XPath
  "@css=.dialog-button"             - CSS selector

Container nesting:
  "container/child"                 - Child within container
  "Parent/Child/GrandChild"         - Multi-level nesting
  "[0]" suffix                      - Index into collections

Full examples:
  "loginForm/usernameField"
  "dataGrid/[0]/checkbox"           - First row checkbox
  "Parent/ScrollView/Container/[2]/Label"
```

**Benefits:**
- More declarative than constructor chains
- Easier to read and maintain
- Supports dynamic element discovery
- Works across all platforms

---

### Improvement C: Universal Timeout Override

**Issue:** Currently timeoutMs only on select methods, inconsistent API.

**Proposal:**
Add optional `timeoutMs` parameter to ALL:
- `Is*` methods (immediate, ignore param)
- `Wait*` methods (use param, default to context)
- `Check*` methods (use param, default to context)
- `Assert*` methods (use param, default to context)
- `Get*` methods (use for precondition waits)
- `Set*` methods (use for precondition waits)
- Action methods (Click, Enter, etc.) (use for preconditions)

**Signature Pattern:**
```csharp
public bool WaitVisible(bool expected = true, int? timeoutMs = null)
public void CheckVisible(bool expected = true, int? timeoutMs = null)
public void AssertVisible(string? message = null, int? timeoutMs = null)
public void Click(int? timeoutMs = null)
public void Enter(string text, int? timeoutMs = null)
```

**Benefits:**
- Consistent API across all methods
- Fine-grained timing control
- Faster test execution when appropriate
- Better error diagnostics

---

### Improvement D: Logical Precondition Verification

**Issue:** Currently each action manually checks preconditions, varying logic.

**Proposal:**
Define precondition templates for method categories:

| Method Category | Preconditions | Notes |
|-----------------|---------------|-------|
| **Click actions** | Exists → Visible → Enabled | Standard for clickable |
| **Text Input** | Exists → Visible → Enabled → ReadOnly | Read-only check before enter |
| **Text Read** | Exists → Visible | No enabled check |
| **Toggle** | Exists → Visible → Enabled | Same as click |
| **Selection** | Exists → Visible → Enabled | Same as click |
| **Range** | Exists → Visible → Enabled | Same as click |
| **Get Value** | Exists → Visible | No enabled check |
| **Scroll** | Exists → Visible | No enabled check |

**Implementation Pattern:**
```csharp
// Base class provides reusable methods
protected void AssertPrecondition(string method, int? timeoutMs = null)
{
    if (method.In("Click", "DoubleClick", "RightClick", "Toggle", "Check", "Uncheck"))
    {
        CheckExists(timeoutMs: timeoutMs);
        CheckVisible(timeoutMs: timeoutMs);
        CheckEnabled(timeoutMs: timeoutMs);
    }
    else if (method.In("Enter", "Clear", "SetText", "Append"))
    {
        CheckExists(timeoutMs: timeoutMs);
        CheckVisible(timeoutMs: timeoutMs);
        CheckEnabled(timeoutMs: timeoutMs);
        if (IsReadOnly())
            throw new InvalidOperationException($"Cannot enter text into read-only {AutomationId}");
    }
    // ... other categories
}
```

**Benefits:**
- Consistent precondition logic across controls
- Self-documenting requirement for each action
- Easier to maintain and extend
- Clear error messages

---

## 3. Proposed Document Structure

### Document Organization

```
SPEC-002b-001: Control Hierarchy (Mermaid Diagrams)
  - IControlObject and base hierarchy
  - Concrete control implementations (MAUI)
  - Concrete control implementations (Blazor)
  - Capability matrix

SPEC-002b-002: All Core Interfaces Catalog
  - Complete interface listing
  - Method signatures (no code)
  - Hierarchy relationships
  - Capability requirements

SPEC-002b-003: MAUI Control Objects Reference
  - MAUI-specific controls
  - Method specializations
  - Platform considerations
  - Gesture support

SPEC-002b-004: Blazor/Playwright Control Objects Reference
  - Blazor-specific controls
  - CSS/XPath considerations
  - Async patterns
  - Browser compatibility

SPEC-002b-005: Element Location & Path Language
  - Flexible locator strategies
  - Path syntax specification
  - Container scoping
  - Fallback mechanisms

SPEC-002b-006: Precondition Verification Framework
  - Precondition matrix
  - Check/Wait/Assert prerequisites
  - Timeout propagation
  - Error handling
```

---

## 4. Questions for Clarification

### Q1: Element Location Priority
**Question:** When multiple location methods are available (AutomationId, Id, Name), which should have priority?

**Options:**
- a) AutomationId first (current practice)
- b) Platform-specific order (Blazor favors data-testid, MAUI favors AutomationId)
- c) User-specified order via locator chain
- d) Smart detection (try each in sequence, use first successful)

---

### Q2: Path Language Complexity
**Question:** How complex should the path language be?

**Options:**
- a) Simple: Just container/child nesting with AutomationId
- b) Medium: Add index support and alternative locators (@id, @xpath)
- c) Full: Include filters, predicates, and XPath-like expressions
- d) User-defined: Let each platform implement its own syntax

---

### Q3: Container vs Path Approach
**Question:** Should we keep container parameter AND add path language, or replace?

**Options:**
- a) Keep both (backwards compatible, flexible)
- b) Replace container param with path language
- c) Use path language internally, expose container param as convenience
- d) Deprecate container param in favor of path language

---

### Q4: Timeout Cascading
**Question:** For nested timeouts, how should they work?

**Options:**
- a) timeoutMs applies to each precondition independently
- b) timeoutMs is total time budget across all preconditions
- c) Split between preconditions (e.g., 50% for Exists, 50% for Visible)
- d) Configurable per-control-type

---

### Q5: IsReadOnly Implementation
**Question:** Some platforms don't have reliable IsReadOnly detection. How to handle?

**Options:**
- a) Always return false if cannot determine (optimistic)
- b) Throw NotSupportedException if cannot determine
- c) Cache platform capabilities at startup
- d) Allow override configuration

---

### Q6: Get vs Is Methods
**Question:** For value retrieval (GetText, GetValue), should we add Wait variants?

**Options:**
- a) No, keep Get* as immediate only
- b) Add WaitForTextEquals, WaitForValueEquals separately
- c) Add optional timeoutMs to Get* methods
- d) Create separate GetWithWait* methods

---

### Q7: Container Types
**Question:** Should IContainer and IListContainer be separate interfaces or variations of IContainerControl?

**Options:**
- a) Single IContainerControl with optional item-specific methods
- b) IContainer and IListContainer as separate specialized interfaces
- c) IContainerControl with GetItemCount, GetItems; IListContainer adds Click/ClickItem
- d) Trait-based (IItemsProviderTrait, IChildNavigationTrait)

---

### Q8: Async Support
**Question:** Should Blazor async patterns (ButtonControlAsync, TextControlAsync) be in main spec?

**Options:**
- a) No, keep sync-only in SPEC-002b, async variants in platform-specific docs
- b) Yes, include full async interface definitions
- c) Define both sync and async with platform choosing
- d) Use single interface with async implementation

---

## 5. Improvement Task Recommendations

### Priority 1 - High Value, Achievable

- [ ] **TASK-001:** Add timeoutMs parameter to all public methods (Improvement C)
- [ ] **TASK-002:** Create element locator abstraction (Improvement A, step 1)
- [ ] **TASK-003:** Define precondition matrix (Improvement D)
- [ ] **TASK-004:** Add Check/Wait/Assert for value methods (e.g., GetText variants)

### Priority 2 - Strategic, Requires Design

- [ ] **TASK-005:** Implement path language for element selection (Improvement B)
- [ ] **TASK-006:** Create IElementLocator interface and builders
- [ ] **TASK-007:** Add fallback/retry logic for element location
- [ ] **TASK-008:** Design platform-specific locator strategies

### Priority 3 - Foundational, Pre-Implementation

- [ ] **TASK-009:** Clarify element location priority (Q1)
- [ ] **TASK-010:** Clarify container vs path approach (Q3)
- [ ] **TASK-011:** Clarify IsReadOnly handling (Q5)
- [ ] **TASK-012:** Clarify Get vs Is methods approach (Q6)

### Priority 4 - Nice to Have, Future

- [ ] **TASK-013:** Advanced filtering for elements in containers
- [ ] **TASK-014:** Visual element selection tool
- [ ] **TASK-015:** Element validation before interaction
- [ ] **TASK-016:** Performance optimization for large element trees

---

## 6. Recommended Next Steps

1. **Review & Answer Questions** - Address Q1-Q8 with team
2. **Refine Improvements** - Update A-D based on answers
3. **Create SPEC-002b Documents** - Build mermaid diagrams
4. **Prioritize Tasks** - Assign to roadmap
5. **Create RFCs** - For complex changes (Path Language, Locator Strategy)

---

## 7. Implementation Roadmap (Proposed)

### Phase 1 (Q1 2026) - Foundation
- Implement timeoutMs consistently (TASK-001)
- Create precondition framework (TASK-003)
- Add Check/Wait/Assert for values (TASK-004)

### Phase 2 (Q2 2026) - Flexibility
- Implement IElementLocator abstraction (TASK-006)
- Add alternative location methods (TASK-002)
- Create platform-specific locator builders (TASK-008)

### Phase 3 (Q3 2026) - Navigation
- Design path language (TASK-005)
- Implement path language parser
- Integrate with element finding

### Phase 4 (Q4 2026) - Polish
- Performance optimization (TASK-016)
- Advanced filtering (TASK-013)
- Visual tooling (TASK-014)

---

*This document is for discussion. Please review and provide feedback on questions and improvement proposals.*
