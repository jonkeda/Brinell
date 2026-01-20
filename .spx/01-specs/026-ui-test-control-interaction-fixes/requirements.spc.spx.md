# Requirements: UI Test Control Interaction Fixes

**Spec ID:** 026  
**Status:** Draft  
**Created:** January 20, 2026

## Problem Statement

After implementing SPEC-025 UI tests, 66 of 222 tests (30%) fail due to control interaction issues. State queries work correctly (`IsExists()`, `IsChecked()`, `GetValue()`), but interactions fail (`Toggle()`, `SetValue()`, `SlideToPercentage()`).

## Root Causes

1. **Controls not scrolled into view** before interaction
2. **Slider SetValue uses SendKeys** which doesn't work for native sliders
3. **Toggle uses simple Click()** which doesn't always trigger state change on Windows

## Functional Requirements

### FR-1: Scroll Into View

- FR-1.1: Add `ScrollIntoView()` method to `MauiControlBase`
- FR-1.2: Automatically scroll element into view before interactions
- FR-1.3: Skip scroll if element is already visible
- FR-1.4: Use Selenium Actions `MoveToElement` for scroll

### FR-2: Slider Value Setting

- FR-2.1: Override `SetValueCore` in `MauiSliderControl` to use click-based positioning
- FR-2.2: Calculate click position based on value percentage within slider track
- FR-2.3: Use 5% padding on track edges to avoid positioning issues

### FR-3: Toggle Reliability

- FR-3.1: Add state verification after toggle operation
- FR-3.2: Retry with Actions-based click if state didn't change
- FR-3.3: Override in specific controls (Switch, CheckBox) for platform-specific handling

### FR-4: Stepper Control

- FR-4.1: Find and click child increment/decrement buttons
- FR-4.2: Override `SetValueCore` to use repeated button clicks

## Non-Functional Requirements

### NFR-1: Backward Compatibility

- Existing test code must continue to work without changes
- New scroll behavior is automatic, not requiring test changes

### NFR-2: Performance

- Scroll only when element is not already visible
- Single wait (100ms) after scroll animation

## Success Criteria

| Metric | Current | Target |
|--------|---------|--------|
| Tests Passed | 151 (68%) | 200+ (90%) |
| Tests Failed | 66 (30%) | <22 (10%) |

## Out of Scope

- Cross-platform scroll optimization (Windows-only for now)
- RangeValue UIA pattern (fallback only)
