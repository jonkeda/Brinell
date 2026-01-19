# Requirements Document: Interface Hierarchy Consolidation

## Introduction

This specification defines the consolidated interface hierarchy for the Brinell UI Test Framework. The goal is to establish a comprehensive, consistent interface design based on the capabilities required by both MAUI (mobile/desktop via Appium) and Blazor (web via Playwright) platform implementations.

The current implementation in `srcnew/Brinell.Core/Interfaces/` provides a foundation, but gaps exist when compared to:
1. The legacy interface hierarchy in `src/Brinell.Core/ControlObject6/Interfaces/` (29 interfaces)
2. Actual control capabilities needed by MAUI and Blazor platforms
3. Missing specialized control interfaces (e.g., date pickers, expanders, navigation)

## Alignment with Product Vision

**From `product.str.spx.md`:**
- "Unified Control Interface Hierarchy" is a key feature
- "Consistent Over Identical" principle - consistent patterns across platforms, not identical APIs
- "Platform-Native Performance" - interfaces should enable, not limit, platform capabilities
- "Test Writer First" - APIs should be discoverable via IntelliSense

**From `tech.str.spx.md`:**
- Interface-based Core with TScope generic for fluent chaining
- Is/Wait/Assert pattern with nullable skip
- All Wait/Assert methods accept nullable expected values

## Scope

### In Scope

- Define complete interface hierarchy in `Brinell.Core.Interfaces`
- Consolidate patterns from legacy `src/` and current `srcnew/` implementations
- Ensure all interfaces follow the TScope generic pattern for fluent chaining
- Document which interfaces apply to which platforms
- Establish interface inheritance relationships

### Out of Scope

- Platform-specific implementations (MAUI, Blazor controls)
- Changing the core patterns (Is/Wait/Assert, TScope fluent chaining)
- Removing or modifying existing working interfaces

## Requirements

### REQ-001: Base Control Interface (IControlObject)

**User Story:** As a test writer, I want a consistent base interface for all controls, so that I can check existence, visibility, enabled state, and text content uniformly.

#### Current State

```csharp
public interface IControlObject<TScope>
{
    Locator Locator { get; }
    TScope ContainingScope { get; }
    IPageObject? Page { get; }
    
    // Exists: Is, Wait, Assert
    bool? IsExists();
    bool WaitExists(bool? expected, int? timeoutMs = null);
    TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    
    // Visible: Is, Wait, Assert
    bool? IsVisible();
    bool WaitVisible(bool? expected, int? timeoutMs = null);
    TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    
    // Enabled: Is, Wait, Assert
    bool? IsEnabled();
    bool WaitEnabled(bool? expected, int? timeoutMs = null);
    TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
    
    // Text
    string? GetText();
    bool WaitText(string? expected, int? timeoutMs = null);
    TScope AssertText(string? expected, string? message = null, int? timeoutMs = null);
    
    // Attribute
    string? GetAttribute(string name);
}
```

#### Acceptance Criteria

1. WHEN a control is created THEN it SHALL have access to its Locator, ContainingScope, and Page
2. IF expected is null on any Wait/Assert method THEN the method SHALL skip and return true/TScope
3. WHEN AssertText/AssertExists/AssertVisible/AssertEnabled is called THEN it SHALL return TScope for fluent chaining
4. WHEN IsExists/IsVisible/IsEnabled returns null THEN it indicates the element was not found

### REQ-002: Clickable Control Interface (IClickableControlObject)

**User Story:** As a test writer, I want a clickable control interface, so that I can click, double-click, right-click, and verify clickability on buttons, links, and other clickable elements.

#### Current State

```csharp
public interface IClickableControlObject<TScope> : IControlObject<TScope>
{
    bool? IsClickable();
    bool WaitClickable(bool? expected, int? timeoutMs = null);
    TScope AssertClickable(bool? expected, string? message = null, int? timeoutMs = null);
    
    TScope Click(int? timeoutMs = null);
    TScope DoubleClick(int? timeoutMs = null);
    TScope RightClick(int? timeoutMs = null);
}
```

#### Acceptance Criteria

1. WHEN Click/DoubleClick/RightClick is called THEN it SHALL return TScope for fluent chaining
2. WHEN IsClickable is called THEN it SHALL return true if element is both visible AND enabled
3. IF element is not visible before click THEN it SHALL scroll into view first

#### Gap Analysis

Missing from current interface (from legacy):
- `Hover(int? timeoutMs = null)` - Mouse hover action
- `LongPress(int? durationMs = null, int? timeoutMs = null)` - Mobile gesture

**Recommendation:** Add Hover and LongPress methods to IClickableControlObject.

### REQ-003: Text Control Interface (ITextControlObject)

**User Story:** As a test writer, I want a text control interface for read-only text elements, so that I can verify text content with various matching strategies.

#### Current State

```csharp
public interface ITextControlObject<TScope> : IControlObject<TScope>
{
    bool WaitTextEquals(string? expected, int? timeoutMs = null);
    bool WaitTextContains(string? expected, int? timeoutMs = null);
    TScope AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
}
```

#### Acceptance Criteria

1. WHEN WaitTextEquals is called THEN it SHALL poll until text equals expected or timeout
2. WHEN WaitTextContains is called THEN it SHALL poll until text contains expected substring
3. WHEN AssertTextMatches is called with regex pattern THEN it SHALL verify text matches pattern

#### Gap Analysis

Missing from current interface (from legacy):
- `AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)` - Assert contains
- `AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null)` - Assert prefix
- `AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null)` - Assert suffix
- `AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null)` - Assert empty/not empty
- `int GetTextLength(int? timeoutMs = null)` - Get text length

**Recommendation:** Add these assertion methods for consistency with Is/Wait/Assert pattern.

### REQ-004: Editable Text Control Interface (IEditableTextControlObject)

**User Story:** As a test writer, I want an editable text control interface for text input elements, so that I can enter, clear, and verify text in input fields.

#### Current State

```csharp
public interface IEditableTextControlObject<TScope> : ITextControlObject<TScope>
{
    TScope Enter(string? text, int? timeoutMs = null);
    TScope Clear(int? timeoutMs = null);
    TScope SetText(string? text, int? timeoutMs = null);
    
    string? GetPlaceholder();
    TScope AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null);
    
    bool? IsReadOnly();
    bool WaitReadOnly(bool? expected, int? timeoutMs = null);
    TScope AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null);
}
```

#### Acceptance Criteria

1. WHEN Enter is called THEN it SHALL clear existing text and enter new text, returning TScope
2. WHEN Clear is called THEN it SHALL clear all text from the field, returning TScope
3. WHEN SetText is called THEN it SHALL set text directly (implementation-specific), returning TScope
4. IF text parameter is null THEN the method SHALL skip and return TScope

#### Gap Analysis

Missing from current interface (from legacy):
- `Append(string? text, int? timeoutMs = null)` - Append without clearing
- `int GetTextLength(int? timeoutMs = null)` - Get text length

**Recommendation:** Add Append method for appending text without clearing.

### REQ-005: Toggle Control Interface (IToggleControlObject)

**User Story:** As a test writer, I want a toggle control interface for checkboxes, switches, and radio buttons, so that I can check, uncheck, and verify toggle states.

#### Current State

```csharp
public interface IToggleControlObject<TScope> : IControlObject<TScope>
{
    bool? IsChecked();
    bool WaitChecked(bool? expected, int? timeoutMs = null);
    TScope AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    
    TScope Toggle(int? timeoutMs = null);
    TScope SetChecked(bool? value, int? timeoutMs = null);
    TScope Check(int? timeoutMs = null);
    TScope Uncheck(int? timeoutMs = null);
}
```

#### Acceptance Criteria

1. WHEN Toggle is called THEN it SHALL change the checked state to opposite, returning TScope
2. WHEN Check is called THEN it SHALL ensure element is checked (no-op if already checked)
3. WHEN Uncheck is called THEN it SHALL ensure element is unchecked (no-op if already unchecked)
4. WHEN SetChecked is called with null THEN it SHALL skip and return TScope

### REQ-006: Range Control Interface (IRangeControlObject)

**User Story:** As a test writer, I want a range control interface for sliders and progress bars, so that I can get/set values and verify value ranges.

#### Current State

```csharp
public interface IRangeControlObject<TScope> : IControlObject<TScope>
{
    double? GetValue();
    double? GetMinimum();
    double? GetMaximum();
    
    bool WaitValue(double? expected, int? timeoutMs = null);
    TScope AssertValue(double? expected, string? message = null, int? timeoutMs = null);
    TScope AssertValueInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
    
    TScope SetValue(double? value, int? timeoutMs = null);
    TScope Increment(int? timeoutMs = null);
    TScope Decrement(int? timeoutMs = null);
}
```

#### Acceptance Criteria

1. WHEN GetValue is called THEN it SHALL return the current value or null if element not found
2. WHEN SetValue is called THEN it SHALL set the value and return TScope
3. WHEN AssertValueInRange is called THEN it SHALL verify value is between min and max

### REQ-007: Selector Control Interface (ISelectorControlObject)

**User Story:** As a test writer, I want a selector control interface for dropdowns, combo boxes, and list boxes, so that I can select items and verify selections.

#### Current State

```csharp
public interface ISelectorControlObject<TScope> : IControlObject<TScope>
{
    TScope SelectByText(string? text, int? timeoutMs = null);
    TScope SelectByIndex(int? index, int? timeoutMs = null);
    TScope SelectByValue(string? value, int? timeoutMs = null);
    
    string? GetSelectedText();
    int? GetSelectedIndex();
    
    bool WaitSelectedText(string? expected, int? timeoutMs = null);
    bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    
    TScope AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
    TScope AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    
    IReadOnlyList<string> GetItemTexts();
    int GetItemCount();
}
```

#### Acceptance Criteria

1. WHEN SelectByText is called THEN it SHALL select item matching text, returning TScope
2. WHEN SelectByIndex is called with null THEN it SHALL skip and return TScope
3. WHEN GetItemTexts is called THEN it SHALL return all item texts in order

### REQ-008: Scrollable Control Interface (IScrollableControlObject)

**User Story:** As a test writer, I want a scrollable control interface, so that I can scroll content and verify scroll positions.

#### Current State

```csharp
public interface IScrollableControlObject<TScope> : IControlObject<TScope>
{
    TScope ScrollToTop(int? timeoutMs = null);
    TScope ScrollToEnd(int? timeoutMs = null);
    TScope ScrollBy(int deltaX, int deltaY, int? timeoutMs = null);
    TScope ScrollTo(double scrollPosition, int? timeoutMs = null);
    
    double? GetScrollPosition();
    bool WaitScrollPosition(double? expected, int? timeoutMs = null);
    TScope AssertScrollPosition(double? expected, string? message = null, int? timeoutMs = null);
}
```

#### Acceptance Criteria

1. WHEN ScrollToTop is called THEN it SHALL scroll to beginning, returning TScope
2. WHEN ScrollToEnd is called THEN it SHALL scroll to end, returning TScope
3. WHEN ScrollBy is called THEN it SHALL scroll by relative amounts, returning TScope

### REQ-009: Tab Control Interface (ITabControlObject)

**User Story:** As a test writer, I want a tab control interface, so that I can select tabs and verify tab selection state.

#### Current State (in Abstractions/Controls/)

```csharp
public interface ITabControlObject<TScope> : IClickableControlObject<TScope>
{
    string Title { get; }
    
    bool? IsSelected();
    bool WaitSelected(bool? expected, int? timeoutMs = null);
    TScope AssertSelected(bool? expected, string? message = null, int? timeoutMs = null);
}
```

#### Acceptance Criteria

1. WHEN Click is called on a tab THEN it SHALL select the tab, returning TScope
2. WHEN IsSelected is called THEN it SHALL return the tab's selected state
3. WHEN AssertSelected is called THEN it SHALL verify selection state, returning TScope

### REQ-010: Additional Interfaces (From Legacy)

**User Story:** As a test writer, I want specialized interfaces for common control types, so that I can use the most appropriate API for each control.

#### Missing Interfaces (from legacy `src/Brinell.Core/ControlObject6/Interfaces/`)

| Interface | Purpose | Priority |
|-----------|---------|----------|
| IExpandableControlObject | Expanders, accordions, tree nodes | High |
| IFocusableControlObject | Controls that can receive focus | Medium |
| IItemsControlObject | Lists, trees, data grids | High |
| IDateControlObject | Date pickers | Medium |
| ITimeControlObject | Time pickers | Medium |
| IProgressControlObject | Progress bars, loading indicators | Medium |
| INavigationPageControlObject | Page navigation (back, forward) | High |
| IWebViewControlObject | Embedded web content | Low |
| IImageControlObject | Image display controls | Low |
| IMediaControlObject | Audio/video players | Low |
| ISwipeableControlObject | Swipeable content (mobile) | Medium |
| IRefreshableControlObject | Pull-to-refresh containers | Medium |

#### Acceptance Criteria

1. WHEN an interface is added THEN it SHALL follow the TScope pattern for fluent chaining
2. WHEN an interface is added THEN it SHALL follow Is/Wait/Assert naming pattern
3. WHEN an interface is platform-specific THEN it SHALL be documented as such

## Non-Functional Requirements

### Code Architecture and Modularity

- **Single Responsibility Principle**: Each interface file should contain one interface
- **Interface Segregation**: Keep interfaces focused; prefer composition over large interfaces
- **Location**: All interfaces SHALL be in `Brinell.Core.Interfaces` namespace
- **File Naming**: Interface files SHALL be named `I{ControlType}ControlObject.cs`

### Documentation

- All interfaces SHALL have XML documentation comments
- Each method SHALL have clear parameter and return documentation
- Nullable skip pattern SHALL be documented on each applicable method

### Inheritance Hierarchy

The recommended hierarchy (based on analysis):

```
IControlObject<TScope> (base)
├── IClickableControlObject<TScope>
│   ├── ITabControlObject<TScope>
│   └── IExpandableControlObject<TScope>
├── ITextControlObject<TScope>
│   └── IEditableTextControlObject<TScope>
├── IToggleControlObject<TScope>
├── IRangeControlObject<TScope>
│   └── IProgressControlObject<TScope> (read-only range)
├── ISelectorControlObject<TScope>
│   └── IItemsControlObject<TScope>
├── IScrollableControlObject<TScope>
├── IFocusableControlObject<TScope>
├── IDateControlObject<TScope>
├── ITimeControlObject<TScope>
└── INavigationPageControlObject<TScope>

IContainerControl<TElement> : IElementScope<TElement>
└── (for scoped element finding)
```

### Consistency

- All action methods (Click, Enter, Select, etc.) SHALL return TScope
- All Is* methods SHALL return bool? (null if element not found)
- All Wait* methods SHALL return bool (success indicator)
- All Assert* methods SHALL return TScope for fluent chaining
- All Wait/Assert methods SHALL accept nullable expected with skip behavior

### Platform Mapping

Document which interfaces are applicable to each platform:

| Interface | MAUI | Blazor | WPF | HTML |
|-----------|------|--------|-----|------|
| IControlObject | ✓ | ✓ | ✓ | ✓ |
| IClickableControlObject | ✓ | ✓ | ✓ | ✓ |
| ITextControlObject | ✓ | ✓ | ✓ | ✓ |
| IEditableTextControlObject | ✓ | ✓ | ✓ | ✓ |
| IToggleControlObject | ✓ | ✓ | ✓ | ✓ |
| IRangeControlObject | ✓ | ✓ | ✓ | ✓ |
| ISelectorControlObject | ✓ | ✓ | ✓ | ✓ |
| IScrollableControlObject | ✓ | ✓ | ✓ | ✓ |
| ITabControlObject | ✓ | ✓ | ✓ | ✓ |
| ISwipeableControlObject | ✓ | - | - | - |
| IRefreshableControlObject | ✓ | - | - | - |

---

**Document Version:** 1.0  
**Created:** January 19, 2026  
**Spec ID:** 003  
**Status:** Draft  
**Workflow:** spec_workflow/requirements
