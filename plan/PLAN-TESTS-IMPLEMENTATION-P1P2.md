# PLAN-TESTS-IMPLEMENTATION-P1P2: Unit Tests for Remaining Controls

**Created:** January 4, 2026  
**Completed:** January 5, 2026  
**Status:** ✅ Complete  
**Dependencies:** PLAN-TESTS-IMPLEMENTATION (P0 complete)

---

## Overview

This plan addresses the test coverage gaps identified in REVIEW-006. It covers **43 controls** across MAUI and Blazor platforms that currently lack unit tests.

### Implementation Results
- **MAUI:** 27/27 controls tested (100%) ✅
- **Blazor:** 19/19 controls tested (100%) ✅
- **Total Tests:** 682 passing (MAUI: 399 + Blazor: 283)

### Original State
- **MAUI:** 9/40 controls tested (22.5%)
- **Blazor:** 7/19 controls tested (36.8%)
- **Total Tests:** 309 passing

### Target State ✅ EXCEEDED
- **MAUI:** 32/40 controls tested (80%) → Achieved: 100%
- **Blazor:** 19/19 controls tested (100%) → Achieved: 100%
- **Total Tests:** ~644 tests → Achieved: 682 tests

---

## Phase 1: P1 MAUI Controls (Week 1, Days 1-2)

### 1.1 RadioButtonControl Tests

**File:** `tests/Brinell.Maui.Tests.ControlObject6/Controls/RadioButtonControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| RB-001 | Constructor_WithAutomationId_SetsLocator | Constructor |
| RB-002 | Constructor_WithLocator_SetsLocator | Constructor |
| RB-003 | IsSelected_WhenSelected_ReturnsTrue | State |
| RB-004 | IsSelected_WhenNotSelected_ReturnsFalse | State |
| RB-005 | Select_SelectsRadioButton | Action |
| RB-006 | GetGroupName_ReturnsGroupName | State |
| RB-007 | GetText_ReturnsLabelText | State |
| RB-008 | AssertSelected_WhenMatches_Passes | Assertion |
| RB-009 | AssertSelected_WhenMismatch_Throws | Assertion |
| RB-010 | Click_SelectsRadioButton | Action |
| RB-011 | IsExists_WhenExists_ReturnsTrue | State |
| RB-012 | IsVisible_WhenVisible_ReturnsTrue | State |

**TestableControls.cs addition required:** `TestableRadioButtonControl`

---

### 1.2 SliderControl Tests

**File:** `tests/Brinell.Maui.Tests.ControlObject6/Controls/SliderControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| SL-001 | Constructor_WithAutomationId_SetsLocator | Constructor |
| SL-002 | Constructor_WithLocator_SetsLocator | Constructor |
| SL-003 | GetValue_ReturnsCurrentValue | State |
| SL-004 | SetValue_SetsNewValue | Action |
| SL-005 | GetMinimum_ReturnsMinValue | State |
| SL-006 | GetMaximum_ReturnsMaxValue | State |
| SL-007 | SetValue_WithNull_DoesNothing | Action |
| SL-008 | AssertValue_WhenMatches_Passes | Assertion |
| SL-009 | AssertValue_WhenMismatch_Throws | Assertion |
| SL-010 | Increment_IncreasesValue | Action |
| SL-011 | Decrement_DecreasesValue | Action |
| SL-012 | SetValue_AtMinimum_SetsMinimum | Action |
| SL-013 | SetValue_AtMaximum_SetsMaximum | Action |
| SL-014 | IsEnabled_WhenEnabled_ReturnsTrue | State |
| SL-015 | IsVisible_WhenVisible_ReturnsTrue | State |

**TestableControls.cs addition required:** `TestableSliderControl`

---

### 1.3 StepperControl Tests

**File:** `tests/Brinell.Maui.Tests.ControlObject6/Controls/StepperControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| ST-001 | Constructor_WithAutomationId_SetsLocator | Constructor |
| ST-002 | Constructor_WithLocator_SetsLocator | Constructor |
| ST-003 | GetValue_ReturnsCurrentValue | State |
| ST-004 | Increment_IncreasesValue | Action |
| ST-005 | Decrement_DecreasesValue | Action |
| ST-006 | GetMinimum_ReturnsMinValue | State |
| ST-007 | GetMaximum_ReturnsMaxValue | State |
| ST-008 | GetIncrement_ReturnsStepValue | State |
| ST-009 | AssertValue_WhenMatches_Passes | Assertion |
| ST-010 | Increment_AtMaximum_StaysAtMax | Action |
| ST-011 | Decrement_AtMinimum_StaysAtMin | Action |
| ST-012 | IsEnabled_WhenEnabled_ReturnsTrue | State |

**TestableControls.cs addition required:** `TestableStepperControl`

---

### 1.4 DatePickerControl Tests

**File:** `tests/Brinell.Maui.Tests.ControlObject6/Controls/DatePickerControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| DP-001 | Constructor_WithAutomationId_SetsLocator | Constructor |
| DP-002 | Constructor_WithLocator_SetsLocator | Constructor |
| DP-003 | GetDate_ReturnsCurrentDate | State |
| DP-004 | SetDate_SetsNewDate | Action |
| DP-005 | SetDate_WithNull_DoesNothing | Action |
| DP-006 | GetMinimumDate_ReturnsMinDate | State |
| DP-007 | GetMaximumDate_ReturnsMaxDate | State |
| DP-008 | AssertDate_WhenMatches_Passes | Assertion |
| DP-009 | AssertDate_WhenMismatch_Throws | Assertion |
| DP-010 | GetFormat_ReturnsDateFormat | State |
| DP-011 | IsExists_WhenExists_ReturnsTrue | State |
| DP-012 | IsEnabled_WhenEnabled_ReturnsTrue | State |
| DP-013 | Click_OpensDatePicker | Action |
| DP-014 | SetDate_WithinRange_SetsDate | Action |
| DP-015 | GetText_ReturnsFormattedDate | State |

**TestableControls.cs addition required:** `TestableDatePickerControl`

---

### 1.5 TimePickerControl Tests

**File:** `tests/Brinell.Maui.Tests.ControlObject6/Controls/TimePickerControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| TP-001 | Constructor_WithAutomationId_SetsLocator | Constructor |
| TP-002 | Constructor_WithLocator_SetsLocator | Constructor |
| TP-003 | GetTime_ReturnsCurrentTime | State |
| TP-004 | SetTime_SetsNewTime | Action |
| TP-005 | SetTime_WithNull_DoesNothing | Action |
| TP-006 | GetFormat_ReturnsTimeFormat | State |
| TP-007 | AssertTime_WhenMatches_Passes | Assertion |
| TP-008 | AssertTime_WhenMismatch_Throws | Assertion |
| TP-009 | IsExists_WhenExists_ReturnsTrue | State |
| TP-010 | IsEnabled_WhenEnabled_ReturnsTrue | State |
| TP-011 | Click_OpensTimePicker | Action |
| TP-012 | GetText_ReturnsFormattedTime | State |

**TestableControls.cs addition required:** `TestableTimePickerControl`

---

### 1.6 ProgressBarControl Tests

**File:** `tests/Brinell.Maui.Tests.ControlObject6/Controls/ProgressBarControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| PB-001 | Constructor_WithAutomationId_SetsLocator | Constructor |
| PB-002 | Constructor_WithLocator_SetsLocator | Constructor |
| PB-003 | GetProgress_ReturnsCurrentProgress | State |
| PB-004 | AssertProgress_WhenMatches_Passes | Assertion |
| PB-005 | AssertProgress_WhenMismatch_Throws | Assertion |
| PB-006 | IsExists_WhenExists_ReturnsTrue | State |
| PB-007 | IsVisible_WhenVisible_ReturnsTrue | State |
| PB-008 | GetProgress_ReturnsZeroToOne | State |

**TestableControls.cs addition required:** `TestableProgressBarControl`

---

### 1.7 ImageControl Tests

**File:** `tests/Brinell.Maui.Tests.ControlObject6/Controls/ImageControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| IM-001 | Constructor_WithAutomationId_SetsLocator | Constructor |
| IM-002 | Constructor_WithLocator_SetsLocator | Constructor |
| IM-003 | GetSource_ReturnsImageSource | State |
| IM-004 | IsExists_WhenExists_ReturnsTrue | State |
| IM-005 | IsVisible_WhenVisible_ReturnsTrue | State |
| IM-006 | GetAspect_ReturnsAspectRatio | State |
| IM-007 | AssertSource_WhenMatches_Passes | Assertion |
| IM-008 | AssertVisible_WhenVisible_Passes | Assertion |
| IM-009 | Click_WhenClickable_PerformsClick | Action |
| IM-010 | IsLoaded_WhenLoaded_ReturnsTrue | State |

**TestableControls.cs addition required:** `TestableImageControl`

---

### 1.8 ScrollViewControl Tests

**File:** `tests/Brinell.Maui.Tests.ControlObject6/Controls/ScrollViewControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| SV-001 | Constructor_WithAutomationId_SetsLocator | Constructor |
| SV-002 | Constructor_WithLocator_SetsLocator | Constructor |
| SV-003 | ScrollTo_ScrollsToPosition | Action |
| SV-004 | ScrollToEnd_ScrollsToEnd | Action |
| SV-005 | ScrollToStart_ScrollsToStart | Action |
| SV-006 | GetScrollX_ReturnsHorizontalPosition | State |
| SV-007 | GetScrollY_ReturnsVerticalPosition | State |
| SV-008 | IsExists_WhenExists_ReturnsTrue | State |
| SV-009 | IsVisible_WhenVisible_ReturnsTrue | State |
| SV-010 | GetContentSize_ReturnsContentDimensions | State |

**TestableControls.cs addition required:** `TestableScrollViewControl`

---

## Phase 2: P1 Blazor Controls (Week 1, Days 2-3)

### 2.1 LinkControl Tests

**File:** `tests/Brinell.Blazor.Tests.ControlObject6/Controls/LinkControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| LK-001 | Constructor_WithTestId_SetsLocator | Constructor |
| LK-002 | Constructor_WithLocator_SetsLocator | Constructor |
| LK-003 | ClickAsync_NavigatesToHref | Action |
| LK-004 | GetTextAsync_ReturnsLinkText | State |
| LK-005 | GetHrefAsync_ReturnsHref | State |
| LK-006 | IsExistsAsync_WhenExists_ReturnsTrue | State |
| LK-007 | IsVisibleAsync_WhenVisible_ReturnsTrue | State |
| LK-008 | GetTargetAsync_ReturnsTarget | State |
| LK-009 | AssertHrefAsync_WhenMatches_Passes | Assertion |
| LK-010 | IsEnabledAsync_WhenEnabled_ReturnsTrue | State |

---

### 2.2 RadioButtonControl Tests (Blazor)

**File:** `tests/Brinell.Blazor.Tests.ControlObject6/Controls/RadioButtonControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| BRB-001 | Constructor_WithTestId_SetsLocator | Constructor |
| BRB-002 | Constructor_WithLocator_SetsLocator | Constructor |
| BRB-003 | IsCheckedAsync_WhenChecked_ReturnsTrue | State |
| BRB-004 | IsCheckedAsync_WhenNotChecked_ReturnsFalse | State |
| BRB-005 | CheckAsync_SelectsRadioButton | Action |
| BRB-006 | ClickAsync_SelectsRadioButton | Action |
| BRB-007 | GetValueAsync_ReturnsValue | State |
| BRB-008 | GetNameAsync_ReturnsGroupName | State |
| BRB-009 | AssertCheckedAsync_WhenMatches_Passes | Assertion |
| BRB-010 | IsExistsAsync_WhenExists_ReturnsTrue | State |
| BRB-011 | IsVisibleAsync_WhenVisible_ReturnsTrue | State |
| BRB-012 | IsEnabledAsync_WhenEnabled_ReturnsTrue | State |

---

### 2.3 RangeControl Tests (Blazor)

**File:** `tests/Brinell.Blazor.Tests.ControlObject6/Controls/RangeControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| BRG-001 | Constructor_WithTestId_SetsLocator | Constructor |
| BRG-002 | Constructor_WithLocator_SetsLocator | Constructor |
| BRG-003 | GetValueAsync_ReturnsCurrentValue | State |
| BRG-004 | SetValueAsync_SetsNewValue | Action |
| BRG-005 | GetMinAsync_ReturnsMinValue | State |
| BRG-006 | GetMaxAsync_ReturnsMaxValue | State |
| BRG-007 | GetStepAsync_ReturnsStepValue | State |
| BRG-008 | SetValueAsync_WithNull_DoesNothing | Action |
| BRG-009 | AssertValueAsync_WhenMatches_Passes | Assertion |
| BRG-010 | IsExistsAsync_WhenExists_ReturnsTrue | State |
| BRG-011 | IsVisibleAsync_WhenVisible_ReturnsTrue | State |
| BRG-012 | IsEnabledAsync_WhenEnabled_ReturnsTrue | State |

---

### 2.4 DateInputControl Tests

**File:** `tests/Brinell.Blazor.Tests.ControlObject6/Controls/DateInputControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| BDI-001 | Constructor_WithTestId_SetsLocator | Constructor |
| BDI-002 | Constructor_WithLocator_SetsLocator | Constructor |
| BDI-003 | GetValueAsync_ReturnsCurrentDate | State |
| BDI-004 | SetValueAsync_SetsNewDate | Action |
| BDI-005 | SetValueAsync_WithNull_DoesNothing | Action |
| BDI-006 | GetMinAsync_ReturnsMinDate | State |
| BDI-007 | GetMaxAsync_ReturnsMaxDate | State |
| BDI-008 | AssertValueAsync_WhenMatches_Passes | Assertion |
| BDI-009 | IsExistsAsync_WhenExists_ReturnsTrue | State |
| BDI-010 | IsEnabledAsync_WhenEnabled_ReturnsTrue | State |

---

### 2.5 TimeInputControl Tests

**File:** `tests/Brinell.Blazor.Tests.ControlObject6/Controls/TimeInputControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| BTI-001 | Constructor_WithTestId_SetsLocator | Constructor |
| BTI-002 | Constructor_WithLocator_SetsLocator | Constructor |
| BTI-003 | GetValueAsync_ReturnsCurrentTime | State |
| BTI-004 | SetValueAsync_SetsNewTime | Action |
| BTI-005 | SetValueAsync_WithNull_DoesNothing | Action |
| BTI-006 | GetMinAsync_ReturnsMinTime | State |
| BTI-007 | GetMaxAsync_ReturnsMaxTime | State |
| BTI-008 | AssertValueAsync_WhenMatches_Passes | Assertion |
| BTI-009 | IsExistsAsync_WhenExists_ReturnsTrue | State |
| BTI-010 | IsEnabledAsync_WhenEnabled_ReturnsTrue | State |

---

### 2.6 ProgressControl Tests

**File:** `tests/Brinell.Blazor.Tests.ControlObject6/Controls/ProgressControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| BPR-001 | Constructor_WithTestId_SetsLocator | Constructor |
| BPR-002 | Constructor_WithLocator_SetsLocator | Constructor |
| BPR-003 | GetValueAsync_ReturnsCurrentProgress | State |
| BPR-004 | GetMaxAsync_ReturnsMaxValue | State |
| BPR-005 | AssertValueAsync_WhenMatches_Passes | Assertion |
| BPR-006 | IsExistsAsync_WhenExists_ReturnsTrue | State |
| BPR-007 | IsVisibleAsync_WhenVisible_ReturnsTrue | State |
| BPR-008 | GetPercentageAsync_ReturnsPercentage | State |

---

### 2.7 ImageControl Tests (Blazor)

**File:** `tests/Brinell.Blazor.Tests.ControlObject6/Controls/ImageControlTests.cs`

| Test ID | Test Name | Category |
|---------|-----------|----------|
| BIM-001 | Constructor_WithTestId_SetsLocator | Constructor |
| BIM-002 | Constructor_WithLocator_SetsLocator | Constructor |
| BIM-003 | GetSrcAsync_ReturnsSource | State |
| BIM-004 | GetAltAsync_ReturnsAltText | State |
| BIM-005 | IsExistsAsync_WhenExists_ReturnsTrue | State |
| BIM-006 | IsVisibleAsync_WhenVisible_ReturnsTrue | State |
| BIM-007 | AssertSrcAsync_WhenMatches_Passes | Assertion |
| BIM-008 | ClickAsync_WhenClickable_PerformsClick | Action |

---

## Phase 3: P2 MAUI Controls (Week 2)

### 3.1 Container Controls

| Control | Tests | Priority |
|---------|-------|----------|
| ExpanderControl | EX-001 to EX-012 | P2 |
| RefreshViewControl | RV-001 to RV-010 | P2 |
| FrameControl | FR-001 to FR-008 | P2 |
| BorderControl | BO-001 to BO-008 | P2 |

### 3.2 Navigation Controls

| Control | Tests | Priority |
|---------|-------|----------|
| NavigationPageControl | NP-001 to NP-015 | P2 |
| TabbedPageControl | TP-001 to TP-015 | P2 |
| TabBarControl | TB-001 to TB-012 | P2 |
| ToolbarControl | TL-001 to TL-010 | P2 |

### 3.3 Activity/Progress Controls

| Control | Tests | Priority |
|---------|-------|----------|
| ActivityIndicatorControl | AI-001 to AI-008 | P2 |

---

## Phase 4: P2 Blazor Controls (Week 2)

### 4.1 Navigation Controls

| Control | Tests | Priority |
|---------|-------|----------|
| NavMenuControl | NM-001 to NM-015 | P2 |
| TabControl | TC-001 to TC-015 | P2 |

### 4.2 Media Controls

| Control | Tests | Priority |
|---------|-------|----------|
| VideoControl | VC-001 to VC-012 | P2 |
| AudioControl | AC-001 to AC-012 | P2 |
| IFrameControl | IF-001 to IF-010 | P2 |

---

## Implementation Order

### Week 1 (P1 Controls)

| Day | Tasks | Est. Tests |
|-----|-------|-----------|
| Day 1 | MAUI: RadioButton, Slider, Stepper | ~39 |
| Day 2 | MAUI: DatePicker, TimePicker, ProgressBar, Image, ScrollView | ~55 |
| Day 3 | Blazor: Link, RadioButton, Range, DateInput, TimeInput, Progress, Image | ~72 |

### Week 2 (P2 Controls)

| Day | Tasks | Est. Tests |
|-----|-------|-----------|
| Day 4 | MAUI: Expander, RefreshView, Frame, Border | ~38 |
| Day 5 | MAUI: Navigation controls | ~52 |
| Day 6 | Blazor: NavMenu, Tab, Media controls | ~54 |

---

## TestableControls.cs Extensions Required

### New MAUI Testable Classes Needed:

```csharp
// Phase 1
public class TestableRadioButtonControl : TestableToggleControlBase
public class TestableSliderControl : TestableRangeControlBase
public class TestableStepperControl : TestableRangeControlBase
public class TestableDatePickerControl : TestableDateControlBase
public class TestableTimePickerControl : TestableTimeControlBase
public class TestableProgressBarControl : TestableProgressControlBase
public class TestableImageControl : TestableControlBase
public class TestableScrollViewControl : TestableContainerControlBase

// Phase 2
public class TestableExpanderControl : TestableContainerControlBase
public class TestableRefreshViewControl : TestableContainerControlBase
public class TestableFrameControl : TestableContainerControlBase
public class TestableBorderControl : TestableContainerControlBase
public class TestableNavigationPageControl : TestableControlBase
public class TestableTabbedPageControl : TestableControlBase
public class TestableTabBarControl : TestableControlBase
public class TestableToolbarControl : TestableControlBase
public class TestableActivityIndicatorControl : TestableControlBase
```

---

## Success Criteria

1. **Phase 1 Complete:** All P1 tests passing (~145 new tests)
2. **Phase 2 Complete:** All P2 tests passing (~140 new tests)
3. **Coverage Target:** 80%+ of concrete controls have unit tests
4. **Build Status:** Zero errors, zero warnings
5. **Test Stability:** All tests pass consistently

---

## Dependencies

- ✅ TestableControls.cs infrastructure (complete)
- ✅ MockAppiumFactory (complete)
- ✅ MockPlaywrightFactory (complete)
- ⏳ New testable control base classes (to be added)

---

*Plan created: January 4, 2026*
