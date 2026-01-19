# SPEC-024: MAUI Control Objects - Tasks

**Spec ID:** 024  
**Feature:** maui-control-objects  
**Status:** Draft  
**Created:** January 19, 2026

---

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Include File path, Purpose, _Leverage, _Requirements, and _Prompt fields
- _Prompt provides AI guidance for implementing the task

---

## Phase 1: High-Priority Controls (P1)

### [ ] 1. Create Display Controls

#### [ ] 1.1 Create MauiLabelControl
- **File:** `srcnew/Brinell.Maui/Controls/Display/MauiLabelControl.cs`
- **Purpose:** Read-only text display control
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.1_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiLabelControl<TScope> inheriting from MauiControlBase<TScope>, following the existing control patterns. Include both Locator and string constructors | Restrictions: No additional methods needed - uses inherited GetText/AssertText. Follow existing naming convention Maui{Control}Control. Do NOT add any logging - base class handles it | Success: Control compiles, inherits correctly, both constructors work, can be used in PageObject pattern_

#### [ ] 1.2 Create MauiProgressBarControl
- **File:** `srcnew/Brinell.Maui/Controls/Display/MauiProgressBarControl.cs`
- **Purpose:** Progress indicator with value retrieval
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.1_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiProgressBarControl<TScope> with GetProgress(), IsIndeterminate(), WaitProgress(), AssertProgress() methods. Use GetProgressCore(element) pattern for attribute reading | Restrictions: Read Progress from element.GetAttribute("Value") or element.GetAttribute("Progress"). Use RunAssert pattern for assertions | Success: GetProgress returns 0-1 double, IsIndeterminate works, assertions use fluent chaining_

#### [ ] 1.3 Create MauiActivityIndicatorControl
- **File:** `srcnew/Brinell.Maui/Controls/Display/MauiActivityIndicatorControl.cs`
- **Purpose:** Loading spinner with running state
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.1_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiActivityIndicatorControl<TScope> with IsRunning(), WaitRunning(), AssertRunning() methods. Check IsRunning attribute from element | Restrictions: Follow WaitChecked/AssertChecked pattern from MauiToggleControlBase | Success: IsRunning returns bool?, Wait/Assert follow nullable skip pattern_

#### [ ] 1.4 Create MauiImageControl
- **File:** `srcnew/Brinell.Maui/Controls/Display/MauiImageControl.cs`
- **Purpose:** Image display with source and dimension access
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.1_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiImageControl<TScope> with IsLoaded(), GetSource(), GetWidth(), GetHeight(), AssertLoaded() methods. Read Source attribute for image path | Restrictions: GetWidth/Height may return null if not available. Use element.Size for dimensions | Success: All methods work, IsLoaded checks Source is non-empty, dimensions return int?_

---

### [ ] 2. Create Toggle Controls

#### [ ] 2.1 Create MauiCheckBoxControl
- **File:** `srcnew/Brinell.Maui/Controls/Toggle/MauiCheckBoxControl.cs`
- **Purpose:** Checkbox toggle control
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiToggleControlBase.cs`_
- _Requirements: REQ-024.2_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiCheckBoxControl<TScope> inheriting from MauiToggleControlBase<TScope>. Only needs constructors - inherits Toggle, Check, Uncheck, IsChecked, AssertChecked | Restrictions: No new methods needed. Follow exact constructor pattern from MauiButtonControl | Success: Control compiles, inherits all toggle functionality, both constructors work_

#### [ ] 2.2 Create MauiSwitchControl
- **File:** `srcnew/Brinell.Maui/Controls/Toggle/MauiSwitchControl.cs`
- **Purpose:** Switch toggle with On/Off terminology
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiToggleControlBase.cs`_
- _Requirements: REQ-024.2_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiSwitchControl<TScope> inheriting from MauiToggleControlBase<TScope>. Add alias methods: IsOn() calls IsChecked(), TurnOn() calls Check(), TurnOff() calls Uncheck() | Restrictions: Alias methods should be simple one-liners delegating to base methods | Success: Both toggle and switch terminology work, IsOn/TurnOn/TurnOff are intuitive aliases_

#### [ ] 2.3 Create MauiRadioButtonControl
- **File:** `srcnew/Brinell.Maui/Controls/Toggle/MauiRadioButtonControl.cs`
- **Purpose:** Radio button with Select terminology
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiToggleControlBase.cs`_
- _Requirements: REQ-024.2_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiRadioButtonControl<TScope> inheriting from MauiToggleControlBase<TScope>. Add alias: IsSelected() calls IsChecked(), Select() calls Check(). No Uncheck equivalent - radio buttons can only be selected | Restrictions: Do not expose Uncheck - radio buttons cannot be deselected directly | Success: IsSelected/Select work, no Uncheck method exposed_

---

### [ ] 3. Create Text Input Controls

#### [ ] 3.1 Create MauiEditorControl
- **File:** `srcnew/Brinell.Maui/Controls/Text/MauiEditorControl.cs`
- **Purpose:** Multi-line text input
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`_
- _Requirements: REQ-024.3_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiEditorControl<TScope> implementing IEditableTextControlObject<TScope>. Copy pattern exactly from MauiEntryControl - same Enter, Clear, ClearAndEnter, Append methods | Restrictions: Editor is same as Entry but for multi-line. No special handling needed - MAUI Editor handles newlines automatically | Success: All text methods work, multi-line text preserves line breaks_

#### [ ] 3.2 Create MauiSearchBarControl
- **File:** `srcnew/Brinell.Maui/Controls/Text/MauiSearchBarControl.cs`
- **Purpose:** Search input with submit capability
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`_
- _Requirements: REQ-024.3_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiSearchBarControl<TScope> implementing IEditableTextControlObject<TScope> plus Submit() method. Submit sends Enter key to trigger search | Restrictions: Submit uses element.SendKeys("\n") or Keys.Enter. GetSearchText is alias for GetText | Success: Submit triggers search action, all text methods work_

---

### [ ] 4. Create Selection Controls

#### [ ] 4.1 Create MauiPickerControl
- **File:** `srcnew/Brinell.Maui/Controls/Selection/MauiPickerControl.cs`
- **Purpose:** Single-selection dropdown picker
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiSelectorControlBase.cs`_
- _Requirements: REQ-024.4_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiPickerControl<TScope> inheriting from MauiSelectorControlBase<TScope>. Inherits SelectByIndex, SelectByText, GetSelectedIndex, GetSelectedText. Add GetItems() to return available options | Restrictions: GetItems may need platform-specific implementation - start with returning empty list and TODO comment | Success: Selection methods work, GetItems returns list (even if empty initially)_

---

### [ ] 5. Create Range Controls

#### [ ] 5.1 Create MauiSliderControl
- **File:** `srcnew/Brinell.Maui/Controls/Range/MauiSliderControl.cs`
- **Purpose:** Slider with drag-based value setting
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiRangeControlBase.cs`_
- _Requirements: REQ-024.5_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiSliderControl<TScope> inheriting from MauiRangeControlBase<TScope>. Override SetValueCore to use drag gesture based on percentage calculation. Use element.Size.Width and calculate target X position | Restrictions: Use Selenium Actions for drag. Calculate percentage = (value - min) / (max - min). Drag from current position to target | Success: SetValue works via drag, GetValue/Min/Max work, Increment/Decrement adjust by step_

#### [ ] 5.2 Create MauiStepperControl
- **File:** `srcnew/Brinell.Maui/Controls/Range/MauiStepperControl.cs`
- **Purpose:** Stepper with increment/decrement buttons
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiRangeControlBase.cs`_
- _Requirements: REQ-024.5_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiStepperControl<TScope> inheriting from MauiRangeControlBase<TScope>. Override IncrementCore/DecrementCore to click +/- buttons within the stepper element | Restrictions: Find buttons using XPath within element scope. Stepper has two buttons - identify by position or accessibility ID | Success: Increment/Decrement click correct buttons, value changes accordingly_

---

## Phase 2: Medium-Priority Controls (P2)

### [ ] 6. Create DateTime Controls

#### [ ] 6.1 Create MauiDatePickerControl
- **File:** `srcnew/Brinell.Maui/Controls/DateTime/MauiDatePickerControl.cs`
- **Purpose:** Date selection control
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.6_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiDatePickerControl<TScope> with GetDate(), SetDate(), OpenPicker(), ClosePicker(), AssertDate(). Parse date from element text or Date attribute | Restrictions: Date parsing should handle multiple formats. SetDate may require platform-specific implementation - use TODO if complex | Success: GetDate returns DateTime?, SetDate updates value, assertions work_

#### [ ] 6.2 Create MauiTimePickerControl
- **File:** `srcnew/Brinell.Maui/Controls/DateTime/MauiTimePickerControl.cs`
- **Purpose:** Time selection control
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.6_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiTimePickerControl<TScope> with GetTime(), SetTime(), OpenPicker(), ClosePicker(), AssertTime(). Parse time from element text or Time attribute | Restrictions: Use TimeSpan for time values. Similar pattern to DatePickerControl | Success: GetTime returns TimeSpan?, SetTime updates value, assertions work_

---

### [ ] 7. Add Sample App Controls for Testing

#### [ ] 7.1 Add Toggle Controls to Sample App
- **File:** `samples/Brinell.Samples.Maui.App/Pages/ControlsDemoPage.xaml`
- **Purpose:** Add CheckBox, Switch, RadioButton for UI testing
- _Leverage: Existing sample app structure_
- _Requirements: REQ-024.2_
- _Prompt: Role: MAUI UI Developer | Task: Add a ToggleControls section to ControlsDemoPage with CheckBox (AutomationId="DemoCheckBox"), Switch (AutomationId="DemoSwitch"), and RadioButtonGroup with 3 options (AutomationId="RadioOption1/2/3") | Restrictions: Use simple layout, add labels showing current state | Success: All controls visible and functional in sample app_

#### [ ] 7.2 Add Text Controls to Sample App
- **File:** `samples/Brinell.Samples.Maui.App/Pages/ControlsDemoPage.xaml`
- **Purpose:** Add Editor, SearchBar for UI testing
- _Leverage: Existing sample app structure_
- _Requirements: REQ-024.3_
- _Prompt: Role: MAUI UI Developer | Task: Add Editor (AutomationId="DemoEditor") and SearchBar (AutomationId="DemoSearchBar") to ControlsDemoPage | Restrictions: Editor should allow multi-line, SearchBar should have placeholder text | Success: Both controls functional in sample app_

#### [ ] 7.3 Add Selection/Range Controls to Sample App
- **File:** `samples/Brinell.Samples.Maui.App/Pages/ControlsDemoPage.xaml`
- **Purpose:** Add Picker, Slider, Stepper for UI testing
- _Leverage: Existing sample app structure_
- _Requirements: REQ-024.4, REQ-024.5_
- _Prompt: Role: MAUI UI Developer | Task: Add Picker with 5 items (AutomationId="DemoPicker"), Slider (AutomationId="DemoSlider", Min=0, Max=100), Stepper (AutomationId="DemoStepper") | Restrictions: Show current values in labels. Picker items: Option 1-5 | Success: All controls functional with visible state_

---

## Phase 3: Container & Collection Controls (P3)

### [ ] 8. Create Container Controls

#### [ ] 8.1 Create MauiScrollViewControl
- **File:** `srcnew/Brinell.Maui/Controls/Container/MauiScrollViewControl.cs`
- **Purpose:** Scrollable container
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiScrollableControlBase.cs`_
- _Requirements: REQ-024.8_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiScrollViewControl<TScope> inheriting from MauiScrollableControlBase<TScope>. Only needs constructors - inherits all scroll methods | Restrictions: Just constructors, no additional methods | Success: Compiles, scroll methods inherited_

#### [ ] 8.2 Create MauiExpanderControl
- **File:** `srcnew/Brinell.Maui/Controls/Container/MauiExpanderControl.cs`
- **Purpose:** Expandable container
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiExpandableControlBase.cs`_
- _Requirements: REQ-024.8_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiExpanderControl<TScope> inheriting from MauiExpandableControlBase<TScope>. Just constructors | Restrictions: Inherits Expand, Collapse, Toggle, IsExpanded, AssertExpanded | Success: Compiles, all expand methods work_

#### [ ] 8.3 Create MauiRefreshViewControl
- **File:** `srcnew/Brinell.Maui/Controls/Container/MauiRefreshViewControl.cs`
- **Purpose:** Pull-to-refresh container
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiRefreshableControlBase.cs`_
- _Requirements: REQ-024.8_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiRefreshViewControl<TScope> inheriting from MauiRefreshableControlBase<TScope>. Just constructors | Restrictions: Inherits Refresh, IsRefreshing, WaitRefreshing | Success: Compiles, refresh methods work_

#### [ ] 8.4 Create MauiSwipeViewControl
- **File:** `srcnew/Brinell.Maui/Controls/Container/MauiSwipeViewControl.cs`
- **Purpose:** Swipeable container
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiSwipeableControlBase.cs`_
- _Requirements: REQ-024.8_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiSwipeViewControl<TScope> inheriting from MauiSwipeableControlBase<TScope>. Just constructors | Restrictions: Inherits SwipeLeft, SwipeRight, etc. | Success: Compiles, swipe methods work_

---

### [ ] 9. Create Collection Controls

#### [ ] 9.1 Create MauiListViewControl
- **File:** `srcnew/Brinell.Maui/Controls/Collection/MauiListViewControl.cs`
- **Purpose:** List view with item access
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiListControl.cs`_
- _Requirements: REQ-024.7_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiListViewControl<TScope, TItem> similar to MauiListControl pattern. Add GetItemCount, GetItem(index), ClickItem, SelectItem methods | Restrictions: Use item factory pattern from MauiListControl. Index is 0-based | Success: Can enumerate items, click/select by index_

#### [ ] 9.2 Create MauiCollectionViewControl
- **File:** `srcnew/Brinell.Maui/Controls/Collection/MauiCollectionViewControl.cs`
- **Purpose:** Collection view with scrolling
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiListControl.cs`, `srcnew/Brinell.Maui/Controls/MauiScrollableControlBase.cs`_
- _Requirements: REQ-024.7_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiCollectionViewControl<TScope, TItem> combining list and scrollable functionality. Add ScrollToItem method | Restrictions: May need composition rather than inheritance for combining behaviors | Success: List operations + scrolling work together_

---

## Phase 4: Specialized Controls (P4)

### [ ] 10. Create Navigation Controls

#### [ ] 10.1 Create MauiMenuControl
- **File:** `srcnew/Brinell.Maui/Controls/Navigation/MauiMenuControl.cs`
- **Purpose:** Menu with item access
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.9_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiMenuControl<TScope> with Open, Close, IsOpen, ClickMenuItem, GetMenuItems methods | Restrictions: Menu behavior varies by platform - start simple | Success: Basic menu operations work_

#### [ ] 10.2 Create MauiToolbarControl
- **File:** `srcnew/Brinell.Maui/Controls/Navigation/MauiToolbarControl.cs`
- **Purpose:** Toolbar item access
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.9_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiToolbarControl<TScope> with GetToolbarItems, ClickToolbarItem methods | Restrictions: Toolbar items found by automation ID within toolbar scope | Success: Can find and click toolbar items_

---

### [ ] 11. Create Media Controls

#### [ ] 11.1 Create MauiWebViewControl
- **File:** `srcnew/Brinell.Maui/Controls/Media/MauiWebViewControl.cs`
- **Purpose:** WebView navigation
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.10_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiWebViewControl<TScope> with Navigate, GoBack, GoForward, Reload, GetCurrentUrl, CanGoBack, CanGoForward methods | Restrictions: WebView has Source property for URL. Navigation may require JavaScript execution | Success: Basic navigation methods work_

#### [ ] 11.2 Create MauiMediaElementControl
- **File:** `srcnew/Brinell.Maui/Controls/Media/MauiMediaElementControl.cs`
- **Purpose:** Media playback control
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`_
- _Requirements: REQ-024.10_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiMediaElementControl<TScope> with Play, Pause, Stop, Seek, GetDuration, GetPosition, GetVolume, SetVolume, IsPlaying methods | Restrictions: Media control via automation may be limited - implement what's possible | Success: Basic playback control works_

---

### [ ] 12. Create Button Variants

#### [ ] 12.1 Create MauiImageButtonControl
- **File:** `srcnew/Brinell.Maui/Controls/Buttons/MauiImageButtonControl.cs`
- **Purpose:** Image button with source access
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiClickableControlBase.cs`_
- _Requirements: REQ-024.11_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiImageButtonControl<TScope> inheriting from MauiClickableControlBase<TScope>. Add GetImageSource() method reading Source attribute | Restrictions: Simple addition to clickable base | Success: Click works, GetImageSource returns source path_

#### [ ] 12.2 Create MauiLinkControl
- **File:** `srcnew/Brinell.Maui/Controls/Buttons/MauiLinkControl.cs`
- **Purpose:** Link with URL access
- _Leverage: `srcnew/Brinell.Maui/Controls/MauiClickableControlBase.cs`_
- _Requirements: REQ-024.11_
- _Prompt: Role: C# MAUI Test Framework Developer | Task: Create MauiLinkControl<TScope> inheriting from MauiClickableControlBase<TScope>. Add GetUrl() method reading Url or NavigateUri attribute | Restrictions: Simple addition to clickable base | Success: Click works, GetUrl returns link URL_

---

## Phase 5: Testing & Integration

### [ ] 13. Create Unit Tests

#### [ ] 13.1 Create Display Control Tests
- **File:** `testsnew/Brinell.Maui.UITests/Tests/Controls/DisplayControlTests.cs`
- **Purpose:** Test LabelControl, ImageControl, ProgressBarControl, ActivityIndicatorControl
- _Leverage: Existing test patterns in testsnew_
- _Requirements: REQ-024.1_
- _Prompt: Role: C# Test Engineer | Task: Create DisplayControlTests testing all P1 display controls against sample app. Test GetText, IsLoaded, GetProgress, IsRunning methods | Restrictions: Use existing test fixtures and page objects. Navigate to controls demo page first | Success: All display control tests pass_

#### [ ] 13.2 Create Toggle Control Tests
- **File:** `testsnew/Brinell.Maui.UITests/Tests/Controls/ToggleControlTests.cs`
- **Purpose:** Test CheckBoxControl, SwitchControl, RadioButtonControl
- _Leverage: Existing test patterns in testsnew_
- _Requirements: REQ-024.2_
- _Prompt: Role: C# Test Engineer | Task: Create ToggleControlTests testing all toggle controls. Test Toggle, Check/Uncheck, IsChecked, and alias methods (IsOn, Select) | Restrictions: Test state changes and assertions | Success: All toggle control tests pass_

#### [ ] 13.3 Create Text Control Tests
- **File:** `testsnew/Brinell.Maui.UITests/Tests/Controls/TextControlTests.cs`
- **Purpose:** Test EditorControl, SearchBarControl
- _Leverage: Existing test patterns in testsnew_
- _Requirements: REQ-024.3_
- _Prompt: Role: C# Test Engineer | Task: Create TextControlTests testing Editor and SearchBar. Test Enter, Clear, multi-line text, Submit | Restrictions: Verify text persistence and submission | Success: All text control tests pass_

#### [ ] 13.4 Create Selection/Range Control Tests
- **File:** `testsnew/Brinell.Maui.UITests/Tests/Controls/SelectionRangeControlTests.cs`
- **Purpose:** Test PickerControl, SliderControl, StepperControl
- _Leverage: Existing test patterns in testsnew_
- _Requirements: REQ-024.4, REQ-024.5_
- _Prompt: Role: C# Test Engineer | Task: Create tests for Picker, Slider, Stepper. Test selection, value getting/setting, increment/decrement | Restrictions: Verify value changes and ranges | Success: All selection/range tests pass_

---

### [ ] 14. Update Interfaces (if needed)

#### [ ] 14.1 Add Missing Control Interfaces to Brinell.Core
- **File:** `srcnew/Brinell.Core/Abstractions/Controls/` (multiple files)
- **Purpose:** Add any missing interfaces (IImageControlObject, IProgressControlObject, etc.)
- _Leverage: Existing interface patterns in Brinell.Core_
- _Requirements: All_
- _Prompt: Role: C# Interface Designer | Task: Review and add any missing control interfaces to Brinell.Core. Follow existing interface patterns with TScope generic parameter | Restrictions: Only add if interface doesn't exist. Follow naming convention I{Control}ControlObject<TScope> | Success: All controls can implement their corresponding interface_

---

### [ ] 15. Documentation

#### [ ] 15.1 Update Control Documentation
- **File:** `docs/16-interface-usage-guide.md`
- **Purpose:** Document new control usage
- _Leverage: Existing documentation structure_
- _Requirements: All_
- _Prompt: Role: Technical Writer | Task: Add documentation for new controls with usage examples in PageObject pattern | Restrictions: Follow existing doc format, include code samples | Success: All new controls documented with examples_

---

## Summary

| Phase | Tasks | Controls | Priority |
|-------|-------|----------|----------|
| P1 | 1-5 | 7 controls | High |
| P2 | 6-7 | 2 controls + sample app | Medium |
| P3 | 8-9 | 6 controls | Lower |
| P4 | 10-12 | 6 controls | Specialized |
| P5 | 13-15 | Testing & docs | Integration |

**Total: 15 task groups, ~29 controls**
