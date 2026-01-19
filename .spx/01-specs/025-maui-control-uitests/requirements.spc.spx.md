# SPEC-025: MAUI Control UI Tests - Requirements

**Spec ID:** 025  
**Feature:** maui-control-uitests  
**Status:** Draft  
**Created:** January 19, 2026  
**Related:** SPEC-024 (MAUI Control Objects)

---

## Introduction

This specification defines comprehensive UI tests for all MAUI control objects created in SPEC-024. The tests will verify that each control object works correctly with actual MAUI applications using the Brinell test automation framework.

### Purpose

Provide test coverage for the 24 new MAUI control objects to:
- Validate control interactions work correctly with real MAUI apps
- Demonstrate usage patterns for each control type
- Serve as living documentation for test writers
- Catch regressions in control implementation

### Current State

**Existing UI Tests** in `testsnew/Brinell.Maui.UITests/Tests/`:
- ButtonControlTests.cs - Tests MauiButtonControl
- EntryControlTests.cs - Tests MauiEntryControl
- ContainerScopingTests.cs - Tests container scoping
- ListContainerTests.cs - Tests MauiListControl
- TabbedPageTests.cs - Tests MauiTabControl/MauiFlyoutItemControl
- NestedContainerTests.cs, SingleContainerTests.cs, IndexedContainerTests.cs

**Sample App Pages** available in `samples/Brinell.Samples.Maui.App/Pages/`:
- UserFormPage.xaml - Entry, Editor, SearchBar, Switch, CheckBox, RadioButton, Picker, DatePicker, TimePicker, Slider, Stepper
- MediaGalleryPage.xaml - Image, ActivityIndicator, WebView, CollectionView
- ContainerDemoPage.xaml - Containers, nested containers, lists
- NavigationDemoPage.xaml - Navigation elements

### Gap Analysis

**Controls needing UI tests** (created in SPEC-024):

| Category | Controls | Sample App Support |
|----------|----------|-------------------|
| Display | MauiLabelControl, MauiProgressBarControl, MauiActivityIndicatorControl, MauiImageControl | ✅ Available |
| Toggle | MauiCheckBoxControl, MauiSwitchControl, MauiRadioButtonControl | ✅ UserFormPage |
| Text | MauiEditorControl, MauiSearchBarControl | ✅ UserFormPage |
| Selection | MauiPickerControl | ✅ UserFormPage |
| Range | MauiSliderControl, MauiStepperControl | ✅ UserFormPage |
| DateTime | MauiDatePickerControl, MauiTimePickerControl | ✅ UserFormPage |
| Container | MauiScrollViewControl, MauiExpanderControl, MauiRefreshViewControl, MauiSwipeViewControl | ⚠️ Partial (needs Expander/Swipe views) |
| Collection | MauiListViewControl, MauiCollectionViewControl | ⚠️ Partial (MediaGallery has CollectionView) |
| Navigation | MauiMenuControl, MauiToolbarControl | ⚠️ Needs sample app update |
| Media | MauiWebViewControl, MauiMediaElementControl | ⚠️ MediaGallery has WebView |
| Buttons | MauiImageButtonControl, MauiLinkControl | ⚠️ Needs sample app update |

---

## Alignment with Product Vision

This feature supports Brinell framework goals:
- **Quality assurance** - Comprehensive test coverage ensures reliability
- **Documentation** - Tests demonstrate proper control usage
- **Regression prevention** - Automated tests catch breaking changes
- **Developer experience** - Examples guide test writers

---

## Requirements

### REQ-025.1: Display Control Tests

**User Story:** As a test writer, I want UI tests for display controls so that I can verify label, image, progress bar, and activity indicator interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiLabelControl tests run THEN the system SHALL verify GetText(), AssertText(), AssertTextContains(), IsVisible()
2. WHEN MauiProgressBarControl tests run THEN the system SHALL verify GetProgress(), AssertProgress() with tolerance
3. WHEN MauiActivityIndicatorControl tests run THEN the system SHALL verify IsRunning(), WaitRunning(), AssertRunning()
4. WHEN MauiImageControl tests run THEN the system SHALL verify IsLoaded(), GetSource(), IsVisible()

---

### REQ-025.2: Toggle Control Tests

**User Story:** As a test writer, I want UI tests for toggle controls so that I can verify checkbox, switch, and radio button interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiCheckBoxControl tests run THEN the system SHALL verify IsChecked(), Check(), Uncheck(), Toggle(), AssertChecked()
2. WHEN MauiSwitchControl tests run THEN the system SHALL verify IsOn(), TurnOn(), TurnOff(), AssertOn(), AssertOff()
3. WHEN MauiRadioButtonControl tests run THEN the system SHALL verify IsSelected(), Select(), AssertSelected()
4. WHEN toggle controls are used THEN the system SHALL return the containing scope for fluent chaining

---

### REQ-025.3: Text Input Control Tests

**User Story:** As a test writer, I want UI tests for text controls so that I can verify Editor and SearchBar interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiEditorControl tests run THEN the system SHALL verify Enter(), Clear(), GetText(), AssertText() for multi-line input
2. WHEN MauiSearchBarControl tests run THEN the system SHALL verify Enter(), Search(), GetText(), AssertText()
3. WHEN text is entered THEN the system SHALL support fluent chaining to verify results

---

### REQ-025.4: Selection Control Tests

**User Story:** As a test writer, I want UI tests for selection controls so that I can verify Picker interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiPickerControl tests run THEN the system SHALL verify SelectByIndex(), SelectByText(), GetSelectedIndex(), GetSelectedText()
2. WHEN MauiPickerControl tests run THEN the system SHALL verify GetTitle(), AssertTitle()
3. WHEN a selection is made THEN the system SHALL demonstrate fluent assertion chaining

---

### REQ-025.5: Range Control Tests

**User Story:** As a test writer, I want UI tests for range controls so that I can verify Slider and Stepper interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiSliderControl tests run THEN the system SHALL verify GetValue(), SetValue(), GetPercentage(), SlideToPercentage()
2. WHEN MauiSliderControl tests run THEN the system SHALL verify SlideToMinimum(), SlideToMaximum()
3. WHEN MauiStepperControl tests run THEN the system SHALL verify GetValue(), SetValue(), Increment(), Decrement(), IncrementBy(), DecrementBy()
4. WHEN MauiStepperControl tests run THEN the system SHALL verify CanIncrement(), CanDecrement()

---

### REQ-025.6: DateTime Control Tests

**User Story:** As a test writer, I want UI tests for date/time controls so that I can verify DatePicker and TimePicker interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiDatePickerControl tests run THEN the system SHALL verify GetDate(), SetDate(), AssertDate()
2. WHEN MauiDatePickerControl tests run THEN the system SHALL verify GetMinimumDate(), GetMaximumDate()
3. WHEN MauiTimePickerControl tests run THEN the system SHALL verify GetTime(), SetTime(), AssertTime() with tolerance
4. WHEN MauiTimePickerControl tests run THEN the system SHALL verify GetHours(), GetMinutes()

---

### REQ-025.7: Container Control Tests

**User Story:** As a test writer, I want UI tests for container controls so that I can verify ScrollView, Expander, RefreshView, and SwipeView interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiScrollViewControl tests run THEN the system SHALL verify ScrollUp(), ScrollDown(), ScrollToTop(), ScrollToBottom()
2. WHEN MauiExpanderControl tests run THEN the system SHALL verify IsExpanded(), Expand(), Collapse(), Toggle()
3. WHEN MauiRefreshViewControl tests run THEN the system SHALL verify IsRefreshing(), PullToRefresh()
4. WHEN MauiSwipeViewControl tests run THEN the system SHALL verify SwipeLeft(), SwipeRight()

---

### REQ-025.8: Collection Control Tests

**User Story:** As a test writer, I want UI tests for collection controls so that I can verify ListView and CollectionView interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiListViewControl tests run THEN the system SHALL verify GetItemCount(), GetItem(), item factory pattern
2. WHEN MauiCollectionViewControl tests run THEN the system SHALL verify GetItemCount(), GetItem(), GetSelectionMode()
3. WHEN collection controls are used THEN the system SHALL support scrolling to items and selecting items

---

### REQ-025.9: Navigation Control Tests

**User Story:** As a test writer, I want UI tests for navigation controls so that I can verify Menu and Toolbar interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiMenuControl tests run THEN the system SHALL verify IsOpen(), Open(), ClickMenuItem()
2. WHEN MauiToolbarControl tests run THEN the system SHALL verify GetTitle(), ClickToolbarItem(), GoBack()

---

### REQ-025.10: Media Control Tests

**User Story:** As a test writer, I want UI tests for media controls so that I can verify WebView and MediaElement interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiWebViewControl tests run THEN the system SHALL verify GetUrl(), GetPageTitle(), CanGoBack(), CanGoForward()
2. WHEN MauiWebViewControl tests run THEN the system SHALL verify AssertUrlContains()
3. WHEN MauiMediaElementControl tests run THEN the system SHALL verify IsPlaying(), IsPaused(), GetPlaybackState()

---

### REQ-025.11: Button Variant Control Tests

**User Story:** As a test writer, I want UI tests for button variants so that I can verify ImageButton and Link interactions work correctly.

#### Acceptance Criteria

1. WHEN MauiImageButtonControl tests run THEN the system SHALL verify Click(), GetSource(), IsPressed()
2. WHEN MauiLinkControl tests run THEN the system SHALL verify Click(), GetLinkText(), GetUrl(), AssertLinkTextEquals()

---

### REQ-025.12: Sample App Updates

**User Story:** As a test writer, I need the sample app to include all control types so that UI tests can exercise every control object.

#### Acceptance Criteria

1. IF sample app lacks Expander control THEN the system SHALL add an Expander demo section
2. IF sample app lacks SwipeView control THEN the system SHALL add a SwipeView demo section
3. IF sample app lacks ImageButton/Link controls THEN the system SHALL add button variant demos
4. IF sample app lacks RefreshView THEN the system SHALL add a pull-to-refresh demo

---

## Non-Functional Requirements

### Test Organization

- **File per control type**: Each control object SHALL have a dedicated test file
- **Trait annotations**: Tests SHALL use `[Trait("Control", "ControlName")]` for filtering
- **Timeout handling**: Tests SHALL specify appropriate timeouts using `TestConstants`
- **Parallel safety**: Tests SHALL be safe to run in parallel within control groups

### Test Quality

- **Fluent patterns**: Tests SHALL demonstrate fluent chaining patterns
- **State isolation**: Tests SHALL reset to known state before assertions
- **Clear naming**: Test methods SHALL follow `ControlName_Method_ExpectedResult` pattern
- **Documentation**: Tests SHALL include XML documentation explaining the test purpose

### Sample App Quality

- **AutomationId coverage**: All testable elements SHALL have unique AutomationId attributes
- **ViewModel binding**: Controls SHALL be bound to view models for state verification
- **Platform compatibility**: Controls SHALL work on Windows (primary) with Android consideration

### Performance

- **Test execution time**: Individual tests SHALL complete within 30 seconds
- **Parallel execution**: Test collections SHALL support parallel execution
- **Resource cleanup**: Tests SHALL properly dispose of resources

---

## Scope

### In Scope

- UI tests for all 24 MAUI control objects from SPEC-024
- Sample app updates to support missing control types
- Page object updates to expose new controls
- Test documentation and usage examples

### Out of Scope

- Unit tests for control objects (separate test project)
- Blazor/WPF/WinForms control tests
- Performance benchmarking tests
- Visual regression tests
- Mobile-specific tests (Android/iOS focus)

---

## Test File Structure

```
testsnew/Brinell.Maui.UITests/
├── Tests/
│   ├── Display/
│   │   ├── LabelControlTests.cs
│   │   ├── ProgressBarControlTests.cs
│   │   ├── ActivityIndicatorControlTests.cs
│   │   └── ImageControlTests.cs
│   ├── Toggle/
│   │   ├── CheckBoxControlTests.cs
│   │   ├── SwitchControlTests.cs
│   │   └── RadioButtonControlTests.cs
│   ├── Text/
│   │   ├── EditorControlTests.cs
│   │   └── SearchBarControlTests.cs
│   ├── Selection/
│   │   └── PickerControlTests.cs
│   ├── Range/
│   │   ├── SliderControlTests.cs
│   │   └── StepperControlTests.cs
│   ├── DateTime/
│   │   ├── DatePickerControlTests.cs
│   │   └── TimePickerControlTests.cs
│   ├── Container/
│   │   ├── ScrollViewControlTests.cs
│   │   ├── ExpanderControlTests.cs
│   │   ├── RefreshViewControlTests.cs
│   │   └── SwipeViewControlTests.cs
│   ├── Collection/
│   │   ├── ListViewControlTests.cs
│   │   └── CollectionViewControlTests.cs
│   ├── Navigation/
│   │   ├── MenuControlTests.cs
│   │   └── ToolbarControlTests.cs
│   ├── Media/
│   │   ├── WebViewControlTests.cs
│   │   └── MediaElementControlTests.cs
│   └── Buttons/
│       ├── ImageButtonControlTests.cs
│       └── LinkControlTests.cs
├── Pages/
│   ├── UserFormPage.cs (existing, add new controls)
│   ├── MediaGalleryPage.cs (existing, update)
│   └── ControlShowcasePage.cs (new)
```

---

## Dependencies

- SPEC-024: MAUI Control Objects (completed)
- Brinell.Samples.Maui.App sample application
- Appium server running for UI automation
- Windows platform for development/testing
