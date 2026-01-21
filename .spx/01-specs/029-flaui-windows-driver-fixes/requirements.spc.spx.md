# Requirements Document: FlaUI Windows Driver Fixes

## Introduction

This specification addresses the known FlaUI driver issues discovered during Windows MAUI UI test automation validation. The FlaUI driver provides native Windows UI Automation support for MAUI desktop apps, but several controls require platform-specific implementations to work correctly.

**Current Test Status:** 152/232 tests passing (65.5%)

The issues fall into four categories:
1. **Slider/Stepper RangeValue Pattern** - Slider manipulation via keyboard doesn't work reliably
2. **Picker ComboBox Expansion** - Picker item enumeration returns 0 items
3. **SearchBar Text Retrieval** - GetText() returns null after entering text
4. **Editor Clear Operation** - Clear() doesn't remove text

## Alignment with Product Vision

This feature supports Brinell's goal of providing a unified, cross-platform UI test automation framework. By fixing these FlaUI-specific issues, we ensure that Windows desktop MAUI app testing achieves parity with Android/iOS testing via Appium.

## Requirements

### Requirement 1: Slider RangeValue Pattern Support

**User Story:** As a test developer, I want to set slider values directly using the UI Automation RangeValue pattern, so that slider manipulation is reliable and performant on Windows.

#### Background Research

From Microsoft documentation and FlaUI GitHub issues:
- The `RangeValuePattern` provides `SetValue(double)` method for direct value manipulation
- MAUI Slider on Windows uses WinUI Slider which implements `IRangeValueProvider`
- FlaUI exposes this via `_element.Patterns.RangeValue.Pattern.SetValue(value)`

#### Acceptance Criteria

1. WHEN FlaUIMauiElement is used on a Slider control THEN the system SHALL check for RangeValue pattern support
2. IF RangeValue pattern is supported THEN the system SHALL use `Patterns.RangeValue.Pattern.SetValue()` for value setting
3. WHEN setting a slider value THEN the system SHALL clamp the value to the control's Minimum/Maximum range
4. IF RangeValue pattern is not supported THEN the system SHALL fall back to keyboard-based approach
5. WHEN getting slider value THEN the system SHALL read from `Patterns.RangeValue.Pattern.Value`
6. WHEN getting slider minimum/maximum THEN the system SHALL read from `Patterns.RangeValue.Pattern.Minimum/Maximum`

### Requirement 2: Picker/ComboBox Item Enumeration

**User Story:** As a test developer, I want to enumerate Picker items on Windows, so that I can verify dropdown contents and select items by text or index.

#### Background Research

From FlaUI GitHub issues (#579, #609):
- MAUI Picker on Windows renders as a ComboBox with `ExpandCollapsePattern`
- Items are only visible in the UI Automation tree after the ComboBox is expanded
- UIA3 may behave differently than UIA2 for ComboBox items
- Solution: Expand ComboBox, enumerate items, then collapse

#### Acceptance Criteria

1. WHEN enumerating Picker items THEN the system SHALL expand the ComboBox first
2. WHEN the ComboBox is expanded THEN the system SHALL enumerate child ListItem elements
3. AFTER enumeration THEN the system SHALL collapse the ComboBox if it was originally collapsed
4. WHEN selecting by text THEN the system SHALL expand, find item, click item (which auto-collapses)
5. WHEN selecting by index THEN the system SHALL expand, find item at index, click item
6. IF the ComboBox has no ExpandCollapse pattern THEN the system SHALL attempt direct descendant search
7. WHEN getting selected text THEN the system SHALL read the ComboBox's current value or Name property

### Requirement 3: SearchBar Text Retrieval

**User Story:** As a test developer, I want to read entered text from SearchBar controls on Windows, so that I can verify search input values.

#### Background Research

From Microsoft documentation:
- MAUI SearchBar on Windows uses WinUI AutoSuggestBox control
- AutoSuggestBox has nested structure: main control contains a TextBox for text entry
- The text value may be in a nested TextBox element, not the parent AutoSuggestBox
- Need to find the inner TextBox or use the Value pattern on the correct element

#### Acceptance Criteria

1. WHEN getting text from a SearchBar THEN the system SHALL first try the Value pattern on the element
2. IF Value pattern returns null THEN the system SHALL search for nested TextBox descendants
3. WHEN a nested TextBox is found THEN the system SHALL read text from the TextBox's Value pattern
4. IF no nested TextBox found THEN the system SHALL try the Name property as fallback
5. WHEN entering text in SearchBar THEN the system SHALL ensure focus on the inner TextBox
6. WHEN clearing SearchBar THEN the system SHALL target the inner TextBox for the clear operation

### Requirement 4: Editor Clear Operation

**User Story:** As a test developer, I want the Clear() operation to work reliably on Editor controls on Windows, so that I can reset text fields during testing.

#### Background Research

- MAUI Editor on Windows may use WinUI TextBox which supports Value pattern
- The `SetValue("")` should work if the control is not read-only
- If Value pattern fails, need keyboard-based approach: Ctrl+A, Delete
- Some controls may require focus before Clear works

#### Acceptance Criteria

1. WHEN clearing an Editor THEN the system SHALL first ensure the element has focus
2. IF Value pattern is supported AND element is not read-only THEN the system SHALL use `SetValue("")`
3. IF Value pattern fails THEN the system SHALL use keyboard-based clear: Ctrl+A followed by Delete
4. AFTER clear operation THEN the system SHALL verify the text is empty
5. IF clear fails THEN the system SHALL throw an informative exception with troubleshooting guidance

### Requirement 5: Stepper Increment/Decrement

**User Story:** As a test developer, I want Stepper increment/decrement operations to work on Windows, so that I can test numeric input controls.

#### Background Research

- MAUI Stepper on Windows may use a custom control with +/- buttons
- The RangeValue pattern may be supported for the main control
- Alternative: Find and click the increment/decrement button children
- Button elements typically have "+", "-", "Increase", "Decrease" in Name or AutomationId

#### Acceptance Criteria

1. WHEN incrementing a Stepper THEN the system SHALL first try RangeValue pattern with current value + step
2. IF RangeValue is not supported THEN the system SHALL find and click the increment button
3. WHEN finding increment button THEN the system SHALL search for buttons with "Increase" or "+" identifiers
4. WHEN decrementing a Stepper THEN the system SHALL first try RangeValue pattern with current value - step
5. IF RangeValue is not supported THEN the system SHALL find and click the decrement button
6. WHEN finding decrement button THEN the system SHALL search for buttons with "Decrease" or "-" identifiers
7. WHEN setting Stepper value directly THEN the system SHALL use RangeValue.SetValue if available

## Non-Functional Requirements

### Code Architecture and Modularity

- **Single Responsibility**: Each fix should be isolated to the appropriate control class
- **Platform Abstraction**: Use `IMauiElement` interface - implementations should be in FlaUI-specific code
- **Fallback Pattern**: All operations should have primary (pattern-based) and fallback (keyboard-based) approaches
- **Defensive Programming**: All pattern access should use safe methods (`IsSupported`, `ValueOrDefault`)

### Performance

- Pattern-based operations SHALL complete within 100ms (UI Automation is fast)
- ComboBox expand/collapse SHALL not add more than 500ms to item enumeration
- Text retrieval SHALL not require multiple automation tree walks

### Reliability

- All operations SHALL handle `PropertyNotSupportedException` gracefully
- All operations SHALL handle stale element references with retry logic
- Operations SHALL timeout with clear error messages after configurable timeout

### Test Coverage

- Each fix SHALL include unit tests in the test project
- Tests SHALL verify both primary and fallback code paths
- Tests SHALL run on Windows 10 and Windows 11

### Documentation

- Each fix SHALL update the `WINDOWS-TEST-RESULTS.md` document
- Code SHALL include XML documentation for public methods
- Known limitations SHALL be documented in code comments

## Scope

### In Scope

- FlaUIMauiElement improvements for RangeValue, ExpandCollapse, Value patterns
- MauiSliderControl, MauiStepperControl FlaUI-specific overrides
- MauiPickerControl FlaUI-specific ComboBox handling
- MauiSearchBarControl nested TextBox handling
- MauiEditorControl clear operation improvements
- Test validation for each fixed control

### Out of Scope

- Appium driver changes (Android/iOS)
- New control implementations
- DatePicker/TimePicker fixes (separate spec)
- Container scoping fixes (separate spec)
- Performance optimization beyond basic requirements

## Dependencies

- FlaUI.Core and FlaUI.UIA3 NuGet packages
- Windows 10.0.19041.0 or later SDK
- .NET 8.0/9.0/10.0 with Windows desktop support

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| RangeValue not supported on some controls | Medium | High | Fallback to keyboard approach |
| ComboBox items still empty after expand | Low | High | Try UIA2 or direct click approach |
| SearchBar structure varies by MAUI version | Medium | Medium | Version-specific code paths |
| Clear fails on read-only controls | Low | Low | Skip clear or throw informative error |

## Success Criteria

- Test pass rate increases from 65.5% to 85%+
- All Slider tests pass (13/19 → 19/19)
- All Selection tests pass (3/8 → 8/8)
- All Text tests pass (8/14 → 14/14)
- No regression in currently passing tests
