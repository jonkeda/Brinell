# Requirements Document

## Introduction

This specification defines the minimal set of interfaces and classes needed to support MAUI Button and Entry controls in the Brinell framework. It establishes the foundational patterns that will be extended for all other controls.

**Scope**: Minimal viable implementation supporting:
- Button control (click capability)
- Entry control (text input capability)
- Controls within Pages, Views (containers), and List Items
- Scoped element finding (controls search within their parent scope)

This specification is intentionally minimal to validate the architecture before implementing the full control set.

## Alignment with Product Vision

This feature directly supports Brinell's core objectives:

| Product Goal | How This Feature Addresses It |
|-------------|------------------------------|
| **Unified Control Interface Hierarchy** | Defines `IControlObject`, `IClickableControl`, `ITextControl`, `IEditableTextControl` |
| **Page Object Pattern Support** | Implements `IPageObject` with scoped element finding |
| **Container Scoping** | Views and ListItems act as element scopes for child controls |
| **Is/Wait/Assert Pattern** | All controls follow consistent state verification API |
| **Platform-Native Performance** | Uses Appium `AppiumElement` directly, no adapters |

## Requirements

### Requirement 1: Element Scope Abstraction

**User Story:** As a test writer, I want controls to search for elements within their parent scope (page, view, or list item), so that I can have multiple similar UI regions without locator conflicts.

#### Acceptance Criteria

1. WHEN a control is created with a page as scope THEN the control SHALL search from the driver root
2. WHEN a control is created with a container/view as scope THEN the control SHALL search within the container's root element only
3. WHEN a control is created with a list item as scope THEN the control SHALL search within that item's bounds only
4. IF multiple containers have child controls with the same AutomationId THEN each control SHALL find only the element within its own container

### Requirement 2: IControlObject Base Interface

**User Story:** As a test writer, I want all controls to provide consistent state checking, waiting, and assertion methods, so that I can write reliable tests with minimal boilerplate.

#### Acceptance Criteria

1. WHEN `IsExists()` is called on a control THEN it SHALL return `true` if element exists, `false` otherwise (immediate, no waiting)
2. WHEN `IsVisible()` is called on a missing element THEN it SHALL return `null` (not `false`)
3. WHEN `IsEnabled()` is called on a missing element THEN it SHALL return `null` (not `false`)
4. WHEN `WaitExists(true, timeout)` is called THEN it SHALL poll until element exists OR timeout, returning `bool`
5. WHEN `WaitExists(null, ...)` is called THEN it SHALL return `true` immediately (nullable skip pattern)
6. WHEN `AssertExists(true, message)` is called on missing element THEN it SHALL throw `AssertionException` with the custom message
7. WHEN `AssertExists(null, ...)` is called THEN it SHALL skip the assertion and return immediately
8. WHEN `GetText()` is called on missing element THEN it SHALL return `null`

### Requirement 3: IClickableControl Interface

**User Story:** As a test writer, I want to click buttons and other clickable controls with automatic waiting, so that I don't need to manually wait for elements to be ready.

#### Acceptance Criteria

1. WHEN `Click()` is called THEN the control SHALL wait for element to be clickable (visible AND enabled) before clicking
2. WHEN `Click()` is called on a disabled element THEN it SHALL NOT throw but SHALL do nothing
3. WHEN `Click()` is called on a hidden element THEN it SHALL wait up to timeout for visibility, then throw `TimeoutException`
4. WHEN `DoubleClick()` is called THEN the control SHALL perform two consecutive clicks

### Requirement 4: IEditableTextControl Interface

**User Story:** As a test writer, I want to enter text, clear text, and set text on input controls, so that I can fill out forms in my tests.

#### Acceptance Criteria

1. WHEN `Enter(text)` is called THEN the control SHALL append the text to existing content
2. WHEN `Enter(null)` is called THEN the control SHALL do nothing (nullable skip pattern)
3. WHEN `Clear()` is called THEN the control SHALL remove all text content
4. WHEN `SetText(text)` is called THEN the control SHALL clear existing text and enter the new text
5. WHEN `Enter(text)` is called on a disabled control THEN it SHALL do nothing without throwing
6. WHEN `GetText()` is called on an entry THEN it SHALL return the current value (not placeholder)

### Requirement 5: Container/View as Element Scope

**User Story:** As a test writer, I want to define views (containers) that scope element searches to their bounds, so that I can model complex UI hierarchies like forms within pages.

#### Acceptance Criteria

1. WHEN a container is created THEN it SHALL locate its root element via its locator
2. WHEN a child control searches for an element THEN it SHALL search within the container's root element only
3. WHEN containers are nested (view within view) THEN searches SHALL be scoped to the innermost container
4. WHEN the container root element doesn't exist THEN `TryFindElement()` SHALL return `null`
5. WHEN the container root is stale (after UI refresh) THEN it SHALL be re-acquired on next access

### Requirement 6: Page as Element Scope

**User Story:** As a test writer, I want pages to act as the top-level scope for controls, so that I can organize my tests using the Page Object pattern.

#### Acceptance Criteria

1. WHEN a page is created THEN it SHALL NOT search for any specific element (pages search from driver root)
2. WHEN a control is created with a page as scope THEN element searches SHALL start from driver root
3. WHEN `IsLoaded()` is called THEN the page SHALL return `true` if key identifying controls exist
4. WHEN a page is created THEN it SHALL wait for the page to be loaded (configurable)

### Requirement 7: List Item as Element Scope

**User Story:** As a test writer, I want to interact with items in lists (CollectionView, ListView) where each item contains child controls, so that I can test list-based UIs.

#### Acceptance Criteria

1. WHEN a list item is accessed by index THEN it SHALL locate the item element at that position
2. WHEN a child control is created within a list item THEN searches SHALL be scoped to that item
3. WHEN `Index` property is accessed THEN it SHALL return the 0-based position of the item
4. WHEN the list item also implements IClickableControl THEN clicking SHALL select the item

### Requirement 8: MAUI Button Control

**User Story:** As a test writer, I want a ButtonControl that works with MAUI Button elements, so that I can click buttons in my MAUI application tests.

#### Acceptance Criteria

1. WHEN `MauiButtonControl` is instantiated with a scope and locator THEN it SHALL implement `IClickableControl`
2. WHEN `Click()` is called THEN it SHALL use `element.Click()` via Appium
3. WHEN `GetText()` is called THEN it SHALL return `element.Text`

### Requirement 9: MAUI Entry Control

**User Story:** As a test writer, I want an EntryControl that works with MAUI Entry elements, so that I can enter text in text fields in my MAUI application tests.

#### Acceptance Criteria

1. WHEN `MauiEntryControl` is instantiated with a scope and locator THEN it SHALL implement `IEditableTextControl`
2. WHEN `Enter(text)` is called THEN it SHALL use `element.SendKeys(text)` via Appium
3. WHEN `Clear()` is called THEN it SHALL use `element.Clear()` via Appium
4. WHEN `GetPlaceholder()` is called THEN it SHALL return the placeholder/hint attribute

## Non-Functional Requirements

### Code Architecture and Modularity

- **Single Responsibility Principle**: Each interface defines one capability (clickable, text, editable)
- **Modular Design**: Controls are composable via multiple interface implementation
- **Dependency Management**: MAUI package depends only on Brinell.Core and Appium
- **Clear Interfaces**: `IElementScope<TElement>` provides the scoping contract

### Performance

- **Element Lookup**: Controls SHALL NOT cache elements by default (elements can become stale)
- **Scope Resolution**: Container root MAY be cached until explicitly invalidated
- **Polling Interval**: Wait methods SHALL use configurable polling interval (default 100ms)

### Security

- No security requirements for this foundational specification.

### Reliability

- **Null Safety**: All state methods return `null` for missing elements (not `false`)
- **Timeout Handling**: All wait/check methods SHALL respect configured timeouts
- **Exception Context**: Assertion failures SHALL include locator, expected/actual values

### Usability

- **IntelliSense**: All interfaces SHALL have XML documentation
- **Discoverability**: Method naming follows Is/Wait/Assert pattern consistently
- **Nullable Skip**: Null parameters skip operations (enables conditional test steps)

## Out of Scope

The following are explicitly NOT part of this specification:

1. **Other Controls**: CheckBox, Picker, Slider, etc. (future specifications)
2. **Gestures**: Swipe, long-press, pinch (future specification)
3. **Visual Assertions**: Screenshot comparison (separate feature)
4. **Parallel Execution**: Thread-safety is assumed but not validated here
5. **Other Platforms**: Blazor, WPF implementations (separate specifications)
6. **List Control Enumeration**: Full `MauiListControl<T>` with iteration (this spec covers item scoping only)
