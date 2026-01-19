# Requirements Document: MAUI Base Control Hierarchy

## Introduction

This specification defines the design and implementation of a comprehensive base control hierarchy for the MAUI platform in `srcnew/Brinell.Maui`. The hierarchy will leverage the interface contracts defined in `Brinell.Core.Interfaces` and provide reusable base classes that reduce code duplication across concrete control implementations.

### Current State

The `srcnew/Brinell.Maui/Controls` folder currently contains:
- `MauiControlBase<TScope>` - Base for all controls (implements IControlObject)
- `MauiContainerBase<TParent, TSelf>` - Base for containers with scoped element finding
- `MauiButtonControl<TScope>` - Implements IClickableControlObject
- `MauiEntryControl<TScope>` - Implements IEditableTextControlObject
- `MauiTabControl<TScope>` - Implements ITabControlObject
- `MauiFlyoutItemControl<TScope>` - Implements IClickableControlObject
- `MauiListControl<TScope, TItem>` - List with item factory

### Problem Statement

1. **Duplicate Code**: `MauiButtonControl`, `MauiTabControl`, and `MauiFlyoutItemControl` all implement IClickableControlObject with nearly identical Click/DoubleClick/RightClick/Hover/LongPress code.

2. **Missing Interface Support**: New interfaces from SPEC-003 (IExpandableControlObject, IFocusableControlObject, IProgressControlObject, IDateControlObject, ITimeControlObject, ISwipeableControlObject, IRefreshableControlObject) have no MAUI implementations.

3. **Inconsistent Inheritance**: Controls inherit directly from MauiControlBase instead of intermediate base classes that provide capability-specific implementations.

4. **No Composition Model**: Some controls need multiple capabilities (e.g., entry is editable AND focusable) but C# doesn't support multiple inheritance.

### Goal

Design a base control class hierarchy that:
- Provides reusable implementations for each interface capability
- Reduces code duplication in concrete controls
- Supports composition of multiple capabilities
- Follows existing patterns (RunWithElement, Is/Wait/Assert, nullable skip)

## Alignment with Product Vision

This specification directly supports the Brinell product vision:

1. **Unified Control Interface Hierarchy** - Provides the MAUI implementation layer for core interfaces
2. **Consistent Over Identical** - Each base class implements platform-specific behavior while adhering to interface contracts
3. **Test Writer First** - Reduces boilerplate for developers creating custom controls
4. **Platform-Native Performance** - Uses Appium Actions API directly for gestures

## Requirements

### REQ-001: Clickable Base Class

**User Story:** As a control developer, I want a MauiClickableControlBase class that implements IClickableControlObject, so that I don't repeat Click/DoubleClick/RightClick/Hover/LongPress code in every clickable control.

#### Acceptance Criteria

1. WHEN a control inherits from MauiClickableControlBase THEN it SHALL automatically have Click, DoubleClick, RightClick, Hover, LongPress implementations
2. WHEN Click is called THEN the control SHALL use RunWithElement pattern with logging
3. WHEN the element is not clickable (disabled or invisible) THEN CheckClickableCore SHALL wait up to timeout or throw
4. IF a derived control needs custom click behavior THEN it SHALL be able to override ClickCore, DoubleClickCore, etc.

### REQ-002: Toggle Base Class

**User Story:** As a control developer, I want a MauiToggleControlBase class that implements IToggleControlObject, so that checkboxes, switches, and radio buttons share common toggle logic.

#### Acceptance Criteria

1. WHEN a control inherits from MauiToggleControlBase THEN it SHALL have Toggle, Check, Uncheck, SetChecked implementations
2. WHEN IsChecked is called THEN the control SHALL read ToggleState or similar attribute from the element
3. WHEN Check is called on already-checked control THEN it SHALL be a no-op
4. IF platform-specific toggle behavior is needed THEN ToggleCore SHALL be overridable

### REQ-003: Range Base Class

**User Story:** As a control developer, I want a MauiRangeControlBase class that implements IRangeControlObject, so that sliders, progress bars, and steppers share range manipulation logic.

#### Acceptance Criteria

1. WHEN a control inherits from MauiRangeControlBase THEN it SHALL have GetValue, SetValue, GetMinimum, GetMaximum implementations
2. WHEN SetValue is called THEN the control SHALL set the value using appropriate Appium mechanism
3. WHEN Increment/Decrement is called THEN the control SHALL adjust value by step amount
4. WHEN AssertValue is called THEN it SHALL use tolerance-based comparison (default 0.001)

### REQ-004: Selector Base Class

**User Story:** As a control developer, I want a MauiSelectorControlBase class that implements ISelectorControlObject, so that pickers and dropdowns share selection logic.

#### Acceptance Criteria

1. WHEN a control inherits from MauiSelectorControlBase THEN it SHALL have SelectByText, SelectByIndex, GetSelectedText implementations
2. WHEN SelectByText is called THEN the control SHALL locate and select matching option
3. WHEN GetItemTexts is called THEN the control SHALL return all available options
4. IF the control is a native picker (date, time) THEN it SHALL use platform-specific selection methods

### REQ-005: Scrollable Base Class

**User Story:** As a control developer, I want a MauiScrollableControlBase class that implements IScrollableControlObject, so that scroll views, lists, and collection views share scroll logic.

#### Acceptance Criteria

1. WHEN a control inherits from MauiScrollableControlBase THEN it SHALL have ScrollToTop, ScrollToEnd, ScrollBy implementations
2. WHEN ScrollTo(locator) is called THEN the control SHALL scroll until element is visible
3. WHEN GetScrollPosition is called THEN the control SHALL return 0-100 percentage
4. WHEN scrolling THEN the control SHALL use Appium Actions API for swipe gestures

### REQ-006: Expandable Base Class

**User Story:** As a control developer, I want a MauiExpandableControlBase class that implements IExpandableControlObject, so that expanders, accordions, and tree nodes share expand/collapse logic.

#### Acceptance Criteria

1. WHEN a control inherits from MauiExpandableControlBase THEN it SHALL have Expand, Collapse, ToggleExpanded implementations
2. WHEN IsExpanded is called THEN the control SHALL read ExpandCollapseState or similar attribute
3. WHEN Expand is called on already-expanded control THEN it SHALL be a no-op
4. WHEN AssertExpanded is called THEN it SHALL follow Is/Wait/Assert pattern

### REQ-007: Focusable Base Class

**User Story:** As a control developer, I want a MauiFocusableControlBase class that implements IFocusableControlObject, so that controls can implement focus management.

#### Acceptance Criteria

1. WHEN a control inherits from MauiFocusableControlBase THEN it SHALL have Focus, Blur, IsFocused implementations
2. WHEN Focus is called THEN the control SHALL click or use SetFocus pattern
3. WHEN IsFocused is called THEN the control SHALL check HasKeyboardFocus or similar attribute
4. WHEN Blur is called THEN the control SHALL move focus away (click elsewhere or tab)

### REQ-008: Swipeable Base Class

**User Story:** As a control developer, I want a MauiSwipeableControlBase class that implements ISwipeableControlObject, so that swipe-to-delete and carousel controls share swipe gesture logic.

#### Acceptance Criteria

1. WHEN a control inherits from MauiSwipeableControlBase THEN it SHALL have SwipeLeft, SwipeRight, SwipeUp, SwipeDown implementations
2. WHEN Swipe is called THEN the control SHALL use Appium Actions API with ClickAndHold/Move/Release
3. WHEN swipe distance is needed THEN the control SHALL calculate based on element bounds
4. IF custom swipe behavior is needed THEN SwipeCore SHALL be overridable

### REQ-009: Refreshable Base Class

**User Story:** As a control developer, I want a MauiRefreshableControlBase class that implements IRefreshableControlObject, so that pull-to-refresh views share refresh logic.

#### Acceptance Criteria

1. WHEN a control inherits from MauiRefreshableControlBase THEN it SHALL have PullToRefresh, IsRefreshing implementations
2. WHEN PullToRefresh is called THEN the control SHALL perform swipe-down gesture from top
3. WHEN IsRefreshing is called THEN the control SHALL check refresh indicator state
4. WHEN WaitRefreshing(false) is called THEN it SHALL wait for refresh to complete

### REQ-010: Refactor Existing Controls

**User Story:** As a framework maintainer, I want existing controls refactored to use base classes, so that code duplication is eliminated.

#### Acceptance Criteria

1. WHEN MauiButtonControl is refactored THEN it SHALL inherit from MauiClickableControlBase
2. WHEN MauiTabControl is refactored THEN it SHALL inherit from MauiClickableControlBase
3. WHEN MauiFlyoutItemControl is refactored THEN it SHALL inherit from MauiClickableControlBase
4. WHEN MauiEntryControl is refactored THEN it SHALL keep editable text capability (consider composition)
5. WHEN refactoring is complete THEN all existing tests SHALL pass without modification

### REQ-011: Capability Composition Support

**User Story:** As a control developer, I want to compose multiple capabilities when a control needs more than one interface, so that I'm not limited by single inheritance.

#### Acceptance Criteria

1. WHEN a control needs IEditableTextControlObject + IFocusableControlObject THEN it SHALL use delegation or mixins
2. WHEN composing capabilities THEN each capability SHALL maintain its Is/Wait/Assert pattern
3. IF interfaces share method names THEN explicit interface implementation SHALL be used
4. WHEN documenting composition THEN clear patterns SHALL be provided in code comments

## Non-Functional Requirements

### Code Architecture and Modularity

- **Single Responsibility Principle**: Each base class handles exactly one capability interface
- **Modular Design**: Base classes can be used independently or composed
- **Clear Interfaces**: Base classes implement one interface contract each
- **Inheritance Depth**: Maximum 3 levels (MauiControlBase → MauiClickableControlBase → MauiButtonControl)

### Performance

- **Element Finding**: Base classes SHALL NOT add additional element lookups beyond what's needed
- **Core Method Efficiency**: Core methods (ClickCore, etc.) SHALL be efficient for repeated calls
- **Caching**: No additional caching beyond existing MauiControlBase patterns

### Maintainability

- **XML Documentation**: All public methods SHALL have XML documentation
- **Code Comments**: Complex logic (gesture calculations) SHALL have explanatory comments
- **Consistent Patterns**: All base classes SHALL use RunWithElement, Poll, Is/Wait/Assert patterns

### Testability

- **Unit Testing**: Core methods SHALL be testable without full Appium context where possible
- **Override Points**: Protected virtual methods for platform-specific behavior
- **Isolation**: Each base class SHALL be testable in isolation

---

**Document Version:** 1.0  
**Created:** January 19, 2026  
**Spec ID:** 004  
**Status:** Draft  
**Workflow:** spec_workflow/requirements
