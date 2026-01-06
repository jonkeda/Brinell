# functional ControlObject
- **id**: FR-100
- **title**: Control Object Model
- **priority**: high
- **status**: draft
- **category**: Object Model

The framework Core must define control object interfaces for UI element interactions. Control objects encapsulate element location, state verification, and actions.

**Architecture:**
- **Core defines interfaces only** (IControlObject, IClickableControl, ITextControl, etc.)
- **Technology packages provide concrete implementations** that implement these interfaces
- Each technology has its own class hierarchy optimized for that platform

## capabilities

### ControlDefinition
- **id**: FR-100.1
- **title**: Control object definition

A control object represents a single UI element and provides:
- Element location via locator
- State queries (existence, visibility, enabled state)
- Actions appropriate to control type (click, enter text, select)
- Assertions for verification

Controls are created within a page or container scope.

### ControlTypes
- **id**: FR-100.2
- **title**: Supported control types

The framework must support these fundamental control types:

| Type | Description | Primary Actions |
|------|-------------|-----------------|
| Clickable | Buttons, links, icons | Click, double-click |
| Text Input | Text fields, text areas | Enter, clear, get text |
| Toggle | Checkboxes, switches, radio buttons | Toggle, set state, get state |
| Selector | Dropdowns, lists, combo boxes | Select item, get selected |
| Range | Sliders, progress bars | Set value, get value |
| Container | Panels, groups, cards | Scope child elements |
| Collection | Lists, grids, tables | Enumerate items, select items |

Additional control types may be defined for specific platforms.

### ControlStateQueries
- **id**: FR-100.3
- **title**: Control state query methods

Controls must provide state query methods:

| Method Pattern | Return | Behavior |
|----------------|--------|----------|
| IsExists | Boolean or null | Immediate check, no waiting |
| IsVisible | Boolean or null | Immediate check, no waiting |
| IsEnabled | Boolean or null | Immediate check, no waiting |
| IsClickable | Boolean or null | Visible AND enabled |
| GetText | Text or null | Get current text content |
| GetValue | Value or null | Get current value |
| GetAttribute | Value or null | Get specific attribute |

**Null return semantics:**
- Non-null value = element exists and state was determined
- Null = element does not exist (not found in UI tree)

### ControlActions
- **id**: FR-100.4
- **title**: Control action methods

Controls must provide action methods appropriate to their type:

**Common actions:**
- Click - Perform click/tap
- DoubleClick - Perform double click/tap
- Focus - Set focus to element

**Text input actions:**
- Enter - Input text (append or replace based on control)
- Clear - Clear current content
- SetText - Clear then enter

**Selection actions:**
- Select - Select item by text or index
- Deselect - Remove selection

**Toggle actions:**
- Toggle - Change state
- SetChecked - Set to checked/on
- SetUnchecked - Set to unchecked/off

**Range actions:**
- SetValue - Set numeric value
- Increment - Increase value
- Decrement - Decrease value

### ActionPreconditions
- **id**: FR-100.5
- **title**: Action precondition verification

Before performing any action, controls must:
1. Wait for element to exist (with timeout)
2. Wait for element to be in actionable state
3. Fail fast with clear error if preconditions not met

Actionable state varies by action:
- Click requires: exists, visible, enabled
- Enter requires: exists, visible, enabled, accepts input
- GetText requires: exists

### NullableParameters
- **id**: FR-100.6
- **title**: Nullable parameter handling

Action methods that accept values must handle null parameters:
- Null parameter = no action performed
- Method returns immediately without error
- No logging occurs for null parameter calls

This enables conditional operations without explicit null checks:
```
// Pseudocode - if optionalValue is null, nothing happens
textField.Enter(optionalValue)
```

### TimeoutOverride
- **id**: FR-100.7
- **title**: Per-method timeout override

All action and wait methods must accept an optional timeout parameter:
- When provided, overrides default timeout for that operation
- When omitted, uses configured default timeout
- Timeout specified in milliseconds

---

## relationships

- Controls are created within [FR-101 Page Object](120_101_PageObject.spx.md) or [FR-102 Container](120_102_ContainerObject.spx.md) scope
- Controls implement interfaces defined in [FR-103 Interface Hierarchy](120_103_InterfaceHierarchy.spx.md)
- Control state methods follow patterns in [FR-300 State Verification](120_300_StateVerification.spx.md)
- Timeout handling follows [FR-402 Timeout Handling](120_402_TimeoutHandling.spx.md)

---

## constraints

- Controls must not expose underlying automation elements directly
- Control instances are lightweight; element lookup occurs on demand
- Controls must be reusable after element is replaced in UI (re-rendered)
