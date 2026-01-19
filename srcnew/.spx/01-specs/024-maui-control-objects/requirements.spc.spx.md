# SPEC-024: MAUI Control Objects - Requirements

**Spec ID:** 024  
**Feature:** maui-control-objects  
**Status:** Draft  
**Created:** January 19, 2026

---

## Introduction

This specification defines the complete set of control objects required for the Brinell MAUI test automation framework in `srcnew/Brinell.Maui`. The framework provides strongly-typed page objects and controls that enable fluent, scope-aware UI test automation.

### Purpose

Provide test writers with a comprehensive library of MAUI control objects that:
- Support all standard MAUI controls
- Enable fluent chaining with scope awareness
- Follow the Is/Wait/Assert pattern consistently
- Integrate with the container scoping system

### Current State

The following controls **exist** in `srcnew/Brinell.Maui/Controls/`:

| Base Classes | Concrete Controls |
|--------------|-------------------|
| MauiControlBase | MauiButtonControl |
| MauiClickableControlBase | MauiEntryControl |
| MauiFocusableControlBase | MauiFlyoutItemControl |
| MauiToggleControlBase | MauiTabControl |
| MauiSelectorControlBase | MauiListControl |
| MauiRangeControlBase | |
| MauiScrollableControlBase | |
| MauiSwipeableControlBase | |
| MauiRefreshableControlBase | |
| MauiExpandableControlBase | |
| MauiContainerBase | |

### Gap Analysis

Per SPEC-006-003b, the following controls are **missing**:

| Category | Missing Controls |
|----------|-----------------|
| **Display** | LabelControl, ImageControl, ProgressBarControl, ActivityIndicatorControl |
| **Toggle** | CheckBoxControl, SwitchControl, RadioButtonControl |
| **Text** | EditorControl, SearchBarControl |
| **Selection** | PickerControl, MultiSelectorControl |
| **Range** | SliderControl, StepperControl |
| **DateTime** | DatePickerControl, TimePickerControl |
| **Collection** | ListViewControl, CollectionViewControl, GroupedListViewControl |
| **Container** | ScrollViewControl, ExpanderControl, RefreshViewControl, SwipeViewControl |
| **Navigation** | TabbedPageControl, MenuControl, ToolbarControl |
| **Media** | MediaElementControl, WebViewControl |
| **Buttons** | ImageButtonControl, LinkControl |

---

## Alignment with Product Vision

This feature directly supports the Brinell framework's core mission:
- **Comprehensive coverage** - Test all MAUI UI control types
- **Consistency** - Uniform API across all controls
- **Productivity** - Strongly-typed controls reduce test writing time
- **Maintainability** - Page object pattern isolates control details from tests

---

## Requirements

### REQ-024.1: Display Controls

**User Story:** As a test writer, I want display control objects so that I can verify labels, images, and progress indicators in my MAUI app.

#### Acceptance Criteria

1. WHEN a LabelControl is created THEN the system SHALL support GetText(), AssertText(), AssertTextContains()
2. WHEN an ImageControl is created THEN the system SHALL support IsLoaded(), GetSource(), GetWidth(), GetHeight()
3. WHEN a ProgressBarControl is created THEN the system SHALL support GetProgress(), IsIndeterminate(), AssertProgress()
4. WHEN an ActivityIndicatorControl is created THEN the system SHALL support IsRunning(), WaitRunning(), AssertRunning()

---

### REQ-024.2: Toggle Controls

**User Story:** As a test writer, I want toggle control objects so that I can interact with checkboxes, switches, and radio buttons.

#### Acceptance Criteria

1. WHEN a CheckBoxControl is created THEN the system SHALL inherit from MauiToggleControlBase
2. WHEN a SwitchControl is created THEN the system SHALL support IsOn(), SetOn(), Toggle()
3. WHEN a RadioButtonControl is created THEN the system SHALL support IsSelected(), Select()
4. WHEN Toggle() is called on any toggle control THEN the system SHALL return TScope for fluent chaining

---

### REQ-024.3: Text Input Controls

**User Story:** As a test writer, I want text input controls so that I can interact with multi-line editors and search bars.

#### Acceptance Criteria

1. WHEN an EditorControl is created THEN the system SHALL support Enter(), Clear(), GetText(), and multi-line input
2. WHEN a SearchBarControl is created THEN the system SHALL support Enter(), Submit(), Clear(), GetSearchText()
3. IF EditorControl.Enter() is called with multi-line text THEN the system SHALL preserve line breaks

---

### REQ-024.4: Selection Controls

**User Story:** As a test writer, I want selection controls so that I can interact with pickers and selectors.

#### Acceptance Criteria

1. WHEN a PickerControl is created THEN the system SHALL support SelectByIndex(), SelectByText(), GetSelectedIndex(), GetSelectedText()
2. WHEN a PickerControl.GetItems() is called THEN the system SHALL return all available options
3. WHEN a MultiSelectorControl is created THEN the system SHALL support SelectMultiple(), GetSelectedItems(), ClearSelection()

---

### REQ-024.5: Range Controls

**User Story:** As a test writer, I want range controls so that I can interact with sliders and steppers.

#### Acceptance Criteria

1. WHEN a SliderControl is created THEN the system SHALL inherit from MauiRangeControlBase
2. WHEN SliderControl.SetValue() is called THEN the system SHALL support drag interaction
3. WHEN a StepperControl is created THEN the system SHALL support Increment(), Decrement(), GetValue()
4. WHEN StepperControl.GetMinimum/GetMaximum() is called THEN the system SHALL return the configured bounds

---

### REQ-024.6: DateTime Controls

**User Story:** As a test writer, I want date/time controls so that I can interact with date and time pickers.

#### Acceptance Criteria

1. WHEN a DatePickerControl is created THEN the system SHALL support GetDate(), SetDate(), OpenPicker(), ClosePicker()
2. WHEN a TimePickerControl is created THEN the system SHALL support GetTime(), SetTime(), OpenPicker(), ClosePicker()
3. IF DatePickerControl.SetDate() is called THEN the system SHALL accept DateTime or DateOnly values
4. IF TimePickerControl.SetTime() is called THEN the system SHALL accept TimeSpan or TimeOnly values

---

### REQ-024.7: Collection Controls

**User Story:** As a test writer, I want collection controls so that I can interact with list views and collection views.

#### Acceptance Criteria

1. WHEN a ListViewControl is created THEN the system SHALL support GetItemCount(), GetItemText(), ClickItem(), SelectItem()
2. WHEN a CollectionViewControl is created THEN the system SHALL support scrolling and item selection
3. WHEN a GroupedListViewControl is created THEN the system SHALL support GetGroupCount(), ExpandGroup(), CollapseGroup()
4. IF a collection control uses virtualization THEN the system SHALL support ScrollToItem() for off-screen items

---

### REQ-024.8: Container Controls

**User Story:** As a test writer, I want container controls so that I can interact with scrollable, expandable, and refreshable containers.

#### Acceptance Criteria

1. WHEN a ScrollViewControl is created THEN the system SHALL inherit from MauiScrollableControlBase
2. WHEN an ExpanderControl is created THEN the system SHALL inherit from MauiExpandableControlBase
3. WHEN a RefreshViewControl is created THEN the system SHALL inherit from MauiRefreshableControlBase
4. WHEN a SwipeViewControl is created THEN the system SHALL inherit from MauiSwipeableControlBase
5. WHEN any container control is used THEN the system SHALL support scoped element finding within that container

---

### REQ-024.9: Navigation Controls

**User Story:** As a test writer, I want navigation controls so that I can interact with tabbed pages, menus, and toolbars.

#### Acceptance Criteria

1. WHEN a TabbedPageControl is created THEN the system SHALL support GetTabCount(), SelectTab(), GetSelectedTabIndex()
2. WHEN a MenuControl is created THEN the system SHALL support Open(), Close(), ClickMenuItem()
3. WHEN a ToolbarControl is created THEN the system SHALL support GetToolbarItems(), ClickToolbarItem()

---

### REQ-024.10: Media Controls

**User Story:** As a test writer, I want media controls so that I can interact with media players and web views.

#### Acceptance Criteria

1. WHEN a MediaElementControl is created THEN the system SHALL support Play(), Pause(), Stop(), Seek()
2. WHEN a WebViewControl is created THEN the system SHALL support Navigate(), GoBack(), GoForward(), Reload()
3. WHEN WebViewControl.Navigate() is called THEN the system SHALL support waiting for page load

---

### REQ-024.11: Button Variants

**User Story:** As a test writer, I want button variant controls so that I can interact with image buttons and links.

#### Acceptance Criteria

1. WHEN an ImageButtonControl is created THEN the system SHALL inherit from MauiClickableControlBase
2. WHEN an ImageButtonControl is created THEN the system SHALL support GetImageSource()
3. WHEN a LinkControl is created THEN the system SHALL inherit from MauiClickableControlBase
4. WHEN a LinkControl is created THEN the system SHALL support GetUrl()

---

## Non-Functional Requirements

### NFR-024.1: Code Architecture and Modularity

- **Single Responsibility**: Each control class handles one MAUI control type
- **Inheritance Hierarchy**: Controls inherit from appropriate base classes
- **Interface Compliance**: All controls implement required interfaces from Brinell.Core
- **Consistent Naming**: Follow `Maui{ControlType}Control<TScope>` naming pattern

### NFR-024.2: Performance

- **Element Finding**: Use cached element references where possible
- **Polling**: Use framework's Poll() method with configurable timeouts
- **No Thread.Sleep**: Never use arbitrary waits - always poll for conditions

### NFR-024.3: Logging

- All control operations SHALL use the Run() helper for consistent logging
- All assertions SHALL use RunAssert() for consistent assertion logging

### NFR-024.4: Fluent Chaining

- All action methods SHALL return `TScope` for fluent chaining
- All assertion methods SHALL return `TScope` for fluent chaining
- Query methods (Is*, Get*) SHALL return appropriate value types

### NFR-024.5: Scope Awareness

- All controls SHALL work within container scopes
- All controls SHALL find elements relative to their containing scope
- All controls SHALL support the IMauiScope<TScope> pattern

---

## Scope

### In Scope

- All controls listed in SPEC-006-003b for MAUI
- Base classes with virtual methods for customization
- String constructor convenience for AutomationId
- Locator constructor for flexible element finding

### Out of Scope

- Blazor controls (separate specification)
- Community Toolkit controls (already in Brinell.Maui.CommunityToolkit)
- Custom control creation API
- Platform-specific iOS/Android implementations

---

## Dependencies

- SPEC-006: ControlObject Framework interfaces
- SPEC-015: Scope-aware fluent chaining (implemented)
- Brinell.Core: Interface definitions
- Brinell.Maui: Base classes and context

---

## References

- [SPEC-006-INDEX](../../specs/SPEC-006-INDEX.md) - Interface definitions
- [SPEC-006-003b-INDEX](../../specs/SPEC-006-003b-INDEX.md) - Complete control hierarchy
- [SPEC-006-003-HIERARCHY-MAUI](../../specs/SPEC-006-003-HIERARCHY-MAUI.md) - MAUI hierarchy details
