# SPEC-025: MAUI Control UI Tests - Tasks

**Spec ID:** 025  
**Feature:** maui-control-uitests  
**Status:** Draft  
**Created:** January 19, 2026

---

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Includes File, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Infrastructure Setup

### [ ] 1. Add control factory methods to MauiPageObjectBase

- **File:** `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`
- **Purpose:** Add factory methods for all new control types (Editor, SearchBar, Switch, CheckBox, RadioButton, Picker, DatePicker, TimePicker, Slider, Stepper, Image, ActivityIndicator, ProgressBar, WebView, ScrollView, etc.)
- _Leverage: Existing Button(), Entry(), Control() pattern in MauiPageObjectBase.cs_
- _Requirements: REQ-025.1 through REQ-025.11_
- _Prompt: Role: C# framework developer | Task: Add protected factory methods for each new control type following existing Button()/Entry() pattern | Restrictions: Follow exact naming convention, return strongly-typed controls, use MauiScope | Success: All 15+ new factory methods compile and return correct control types_

### [ ] 2. Create UserFormPage page object

- **File:** `testsnew/Brinell.Maui.UITests/Pages/UserFormPage.cs`
- **Purpose:** Expose controls from UserFormPage.xaml for testing (Editor, SearchBar, Switch, CheckBox, RadioButton, Picker, DatePicker, TimePicker, Slider, Stepper)
- _Leverage: MainPage.cs pattern, UserFormPage.xaml AutomationIds_
- _Requirements: REQ-025.2 through REQ-025.6_
- _Prompt: Role: Test automation developer | Task: Create page object exposing all testable controls from UserFormPage.xaml using factory methods | Restrictions: Use exact AutomationIds from XAML, follow MainPage.cs structure | Success: All controls accessible via strongly-typed properties, IsLoaded() works correctly_

### [ ] 3. Create MediaGalleryPage page object

- **File:** `testsnew/Brinell.Maui.UITests/Pages/MediaGalleryPage.cs`
- **Purpose:** Expose controls from MediaGalleryPage.xaml for testing (Image, ActivityIndicator, WebView, CollectionView)
- _Leverage: MainPage.cs pattern, MediaGalleryPage.xaml AutomationIds_
- _Requirements: REQ-025.1, REQ-025.8, REQ-025.10_
- _Prompt: Role: Test automation developer | Task: Create page object exposing media controls from MediaGalleryPage.xaml | Restrictions: Use exact AutomationIds from XAML | Success: Image, ActivityIndicator, WebView, CollectionView controls accessible_

### [ ] 4. Update AppiumFixture with new pages and navigation

- **File:** `testsnew/Brinell.Maui.UITests/AppiumFixture.cs`
- **Purpose:** Add UserFormPage and MediaGalleryPage, add navigation methods
- _Leverage: Existing ContainerDemoPage pattern in AppiumFixture.cs_
- _Requirements: All_
- _Prompt: Role: Test infrastructure developer | Task: Add new page objects and NavigateToUserForm(), NavigateToMediaGallery() methods | Restrictions: Follow existing navigation pattern, ensure pages are initialized in constructor | Success: Navigation methods work, pages accessible via properties_

---

## Phase 2: P1 Core Control Tests

### [ ] 5. Create Display control tests

#### [ ] 5.1 Create LabelControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Display/LabelControlTests.cs`
- **Purpose:** Test MauiLabelControl methods: GetText(), AssertText(), AssertTextContains(), IsVisible()
- _Leverage: ButtonControlTests.cs pattern, MainPage.TitleLabel_
- _Requirements: REQ-025.1_
- _Prompt: Role: UI test developer | Task: Create xUnit tests for Label control state and text assertions | Restrictions: Use [Collection("Appium")], [Trait("Control", "Label")], follow existing test pattern | Success: Tests verify label text retrieval and assertions_

#### [ ] 5.2 Create ProgressBarControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Display/ProgressBarControlTests.cs`
- **Purpose:** Test MauiProgressBarControl methods: GetProgress(), IsIndeterminate(), AssertProgress()
- _Leverage: MainPage.VolumeProgress_
- _Requirements: REQ-025.1_
- _Prompt: Role: UI test developer | Task: Create tests for ProgressBar value retrieval and assertions with tolerance | Restrictions: Test tolerance-based assertions | Success: Progress value and assertion tests pass_

#### [ ] 5.3 Create ActivityIndicatorControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Display/ActivityIndicatorControlTests.cs`
- **Purpose:** Test MauiActivityIndicatorControl methods: IsRunning(), WaitRunning(), AssertRunning()
- _Leverage: MediaGalleryPage.WebLoadingIndicator_
- _Requirements: REQ-025.1_
- _Prompt: Role: UI test developer | Task: Create tests for ActivityIndicator running state | Restrictions: Test state changes, use Wait methods | Success: Running state detection and wait methods verified_

#### [ ] 5.4 Create ImageControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Display/ImageControlTests.cs`
- **Purpose:** Test MauiImageControl methods: IsLoaded(), GetSource(), IsVisible()
- _Leverage: MediaGalleryPage.MainImage_
- _Requirements: REQ-025.1_
- _Prompt: Role: UI test developer | Task: Create tests for Image loading and source | Restrictions: Verify image is displayed | Success: Image load detection and source retrieval work_

### [ ] 6. Create Toggle control tests

#### [ ] 6.1 Create CheckBoxControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Toggle/CheckBoxControlTests.cs`
- **Purpose:** Test MauiCheckBoxControl: IsChecked(), Check(), Uncheck(), Toggle(), AssertChecked()
- _Leverage: UserFormPage.TermsCheckBox, PrivacyCheckBox_
- _Requirements: REQ-025.2_
- _Prompt: Role: UI test developer | Task: Create tests for CheckBox toggle operations | Restrictions: Test Check/Uncheck/Toggle independently, verify state after each | Success: All toggle operations work, assertions pass_

#### [ ] 6.2 Create SwitchControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Toggle/SwitchControlTests.cs`
- **Purpose:** Test MauiSwitchControl: IsOn(), TurnOn(), TurnOff(), AssertOn(), AssertOff()
- _Leverage: UserFormPage.NewsletterSwitch_
- _Requirements: REQ-025.2_
- _Prompt: Role: UI test developer | Task: Create tests for Switch on/off operations with alias methods | Restrictions: Test TurnOn/TurnOff separately | Success: Switch state changes verified_

#### [ ] 6.3 Create RadioButtonControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Toggle/RadioButtonControlTests.cs`
- **Purpose:** Test MauiRadioButtonControl: IsSelected(), Select(), AssertSelected()
- _Leverage: UserFormPage.BasicRadio, ProfessionalRadio, EnterpriseRadio_
- _Requirements: REQ-025.2_
- _Prompt: Role: UI test developer | Task: Create tests for RadioButton selection in group | Restrictions: Test mutual exclusion in radio group | Success: Radio selection works, only one selected at a time_

### [ ] 7. Create Text control tests

#### [ ] 7.1 Create EditorControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Text/EditorControlTests.cs`
- **Purpose:** Test MauiEditorControl: Enter(), Clear(), GetText(), AssertText() for multi-line
- _Leverage: UserFormPage.BioEditor_
- _Requirements: REQ-025.3_
- _Prompt: Role: UI test developer | Task: Create tests for multi-line text entry | Restrictions: Test newlines in text, verify multi-line content | Success: Multi-line text entry and retrieval work_

#### [ ] 7.2 Create SearchBarControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Text/SearchBarControlTests.cs`
- **Purpose:** Test MauiSearchBarControl: Enter(), Search(), GetText(), AssertText()
- _Leverage: UserFormPage.UserSearchBar_
- _Requirements: REQ-025.3_
- _Prompt: Role: UI test developer | Task: Create tests for SearchBar text entry and search submit | Restrictions: Test Search() triggers search action | Success: Text entry and search submission verified_

### [ ] 8. Create Range control tests

#### [ ] 8.1 Create SliderControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Range/SliderControlTests.cs`
- **Purpose:** Test MauiSliderControl: GetValue(), SetValue(), GetPercentage(), SlideToPercentage(), SlideToMinimum(), SlideToMaximum()
- _Leverage: UserFormPage.FontSizeSlider, VolumeSlider_
- _Requirements: REQ-025.5_
- _Prompt: Role: UI test developer | Task: Create tests for slider value manipulation | Restrictions: Test percentage and absolute value methods | Success: Slider position changes verified_

#### [ ] 8.2 Create StepperControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Range/StepperControlTests.cs`
- **Purpose:** Test MauiStepperControl: GetValue(), Increment(), Decrement(), IncrementBy(), DecrementBy(), CanIncrement(), CanDecrement()
- _Leverage: UserFormPage.QuantityStepper_
- _Requirements: REQ-025.5_
- _Prompt: Role: UI test developer | Task: Create tests for stepper increment/decrement | Restrictions: Test boundary conditions (min/max) | Success: Stepper value changes and limits verified_

---

## Phase 3: P2 Selection & DateTime Tests

### [ ] 9. Create Selection control tests

#### [ ] 9.1 Create PickerControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Selection/PickerControlTests.cs`
- **Purpose:** Test MauiPickerControl: SelectByIndex(), SelectByText(), GetSelectedIndex(), GetSelectedText(), GetTitle()
- _Leverage: UserFormPage.CountryPicker, DepartmentPicker_
- _Requirements: REQ-025.4_
- _Prompt: Role: UI test developer | Task: Create tests for Picker selection by index and text | Restrictions: Test both selection methods, verify selected value | Success: Picker selection and retrieval verified_

### [ ] 10. Create DateTime control tests

#### [ ] 10.1 Create DatePickerControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/DateTime/DatePickerControlTests.cs`
- **Purpose:** Test MauiDatePickerControl: GetDate(), SetDate(), AssertDate(), GetMinimumDate(), GetMaximumDate()
- _Leverage: UserFormPage.BirthDatePicker_
- _Requirements: REQ-025.6_
- _Prompt: Role: UI test developer | Task: Create tests for DatePicker date selection | Restrictions: Test date format handling, min/max constraints | Success: Date selection and assertions work_

#### [ ] 10.2 Create TimePickerControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/DateTime/TimePickerControlTests.cs`
- **Purpose:** Test MauiTimePickerControl: GetTime(), SetTime(), AssertTime(), GetHours(), GetMinutes()
- _Leverage: UserFormPage.PreferredTimePicker_
- _Requirements: REQ-025.6_
- _Prompt: Role: UI test developer | Task: Create tests for TimePicker time selection with tolerance | Restrictions: Test time tolerance assertions | Success: Time selection and retrieval verified_

---

## Phase 4: P3 Container & Collection Tests

### [ ] 11. Create Container control tests

#### [ ] 11.1 Create ScrollViewControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Container/ScrollViewControlTests.cs`
- **Purpose:** Test MauiScrollViewControl: ScrollUp(), ScrollDown(), ScrollToTop(), ScrollToBottom()
- _Leverage: UserFormPage scroll container or ContainerDemoPage_
- _Requirements: REQ-025.7_
- _Prompt: Role: UI test developer | Task: Create tests for ScrollView scroll operations | Restrictions: Verify scroll position changes | Success: Scroll operations move content_

#### [ ] 11.2 Create ExpanderControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Container/ExpanderControlTests.cs`
- **Purpose:** Test MauiExpanderControl: IsExpanded(), Expand(), Collapse(), Toggle()
- _Leverage: Sample app needs Expander control added_
- _Requirements: REQ-025.7, REQ-025.12_
- _Prompt: Role: UI test developer | Task: Create tests for Expander expand/collapse | Restrictions: May need sample app update | Success: Expander state changes verified_

#### [ ] 11.3 Create RefreshViewControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Container/RefreshViewControlTests.cs`
- **Purpose:** Test MauiRefreshViewControl: IsRefreshing(), PullToRefresh()
- _Leverage: Sample app needs RefreshView added_
- _Requirements: REQ-025.7, REQ-025.12_
- _Prompt: Role: UI test developer | Task: Create tests for pull-to-refresh gesture | Restrictions: May need sample app update | Success: Refresh gesture triggers refresh_

#### [ ] 11.4 Create SwipeViewControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Container/SwipeViewControlTests.cs`
- **Purpose:** Test MauiSwipeViewControl: SwipeLeft(), SwipeRight()
- _Leverage: Sample app needs SwipeView added_
- _Requirements: REQ-025.7, REQ-025.12_
- _Prompt: Role: UI test developer | Task: Create tests for swipe gestures | Restrictions: May need sample app update | Success: Swipe gestures reveal content_

### [ ] 12. Create Collection control tests

#### [ ] 12.1 Create ListViewControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Collection/ListViewControlTests.cs`
- **Purpose:** Test MauiListViewControl: GetItemCount(), GetItem(), IsPullToRefreshEnabled()
- _Leverage: Existing ListContainerTests.cs pattern_
- _Requirements: REQ-025.8_
- _Prompt: Role: UI test developer | Task: Create tests for ListView item access | Restrictions: Follow item factory pattern | Success: Item count and access verified_

#### [ ] 12.2 Create CollectionViewControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Collection/CollectionViewControlTests.cs`
- **Purpose:** Test MauiCollectionViewControl: GetItemCount(), GetItem(), GetSelectionMode(), IsMultiSelectEnabled()
- _Leverage: MediaGalleryPage.ThumbnailCollection_
- _Requirements: REQ-025.8_
- _Prompt: Role: UI test developer | Task: Create tests for CollectionView item access and selection mode | Restrictions: Test different selection modes if possible | Success: Collection operations verified_

---

## Phase 5: P4 Specialized Tests

### [ ] 13. Create Navigation control tests

#### [ ] 13.1 Create MenuControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Navigation/MenuControlTests.cs`
- **Purpose:** Test MauiMenuControl: IsOpen(), Open(), ClickMenuItem()
- _Leverage: Sample app may need menu added_
- _Requirements: REQ-025.9_
- _Prompt: Role: UI test developer | Task: Create tests for menu open and item click | Restrictions: May need sample app update | Success: Menu interaction verified_

#### [ ] 13.2 Create ToolbarControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Navigation/ToolbarControlTests.cs`
- **Purpose:** Test MauiToolbarControl: GetTitle(), ClickToolbarItem()
- _Leverage: AppShell toolbar items_
- _Requirements: REQ-025.9_
- _Prompt: Role: UI test developer | Task: Create tests for toolbar item interaction | Restrictions: Use existing navigation structure | Success: Toolbar actions verified_

### [ ] 14. Create Media control tests

#### [ ] 14.1 Create WebViewControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Media/WebViewControlTests.cs`
- **Purpose:** Test MauiWebViewControl: GetUrl(), GetPageTitle(), CanGoBack(), CanGoForward(), AssertUrlContains()
- _Leverage: MediaGalleryPage.ContentWebView_
- _Requirements: REQ-025.10_
- _Prompt: Role: UI test developer | Task: Create tests for WebView URL and navigation state | Restrictions: Test actual web content loading | Success: URL retrieval and assertions work_

#### [ ] 14.2 Create MediaElementControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Media/MediaElementControlTests.cs`
- **Purpose:** Test MauiMediaElementControl: IsPlaying(), IsPaused(), GetPlaybackState()
- _Leverage: MediaGalleryPage simulated player_
- _Requirements: REQ-025.10_
- _Prompt: Role: UI test developer | Task: Create tests for media playback state | Restrictions: May test simulated player UI | Success: Playback state detection works_

### [ ] 15. Create Button variant tests

#### [ ] 15.1 Create ImageButtonControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Buttons/ImageButtonControlTests.cs`
- **Purpose:** Test MauiImageButtonControl: Click(), GetSource(), IsPressed()
- _Leverage: Sample app needs ImageButton added_
- _Requirements: REQ-025.11, REQ-025.12_
- _Prompt: Role: UI test developer | Task: Create tests for ImageButton click and image source | Restrictions: May need sample app update | Success: ImageButton click and source verified_

#### [ ] 15.2 Create LinkControlTests

- **File:** `testsnew/Brinell.Maui.UITests/Tests/Buttons/LinkControlTests.cs`
- **Purpose:** Test MauiLinkControl: Click(), GetLinkText(), GetUrl(), AssertLinkTextEquals()
- _Leverage: Sample app needs link/hyperlink added_
- _Requirements: REQ-025.11, REQ-025.12_
- _Prompt: Role: UI test developer | Task: Create tests for link click and URL | Restrictions: May need sample app update | Success: Link text and URL verified_

---

## Phase 6: Sample App Updates (If Needed)

### [ ] 16. Add missing controls to sample app

#### [ ] 16.1 Create ControlShowcasePage

- **File:** `samples/Brinell.Samples.Maui.App/Pages/ControlShowcasePage.xaml`
- **Purpose:** Add page with Expander, SwipeView, RefreshView, ImageButton, Link controls
- _Leverage: Existing page XAML patterns_
- _Requirements: REQ-025.12_
- _Prompt: Role: MAUI developer | Task: Create showcase page with missing control types | Restrictions: Add proper AutomationIds to all controls | Success: All missing controls present with AutomationIds_

#### [ ] 16.2 Add ControlShowcasePage to navigation

- **File:** `samples/Brinell.Samples.Maui.App/AppShell.xaml`
- **Purpose:** Add tab for ControlShowcasePage
- _Leverage: Existing tab navigation pattern_
- _Requirements: REQ-025.12_
- _Prompt: Role: MAUI developer | Task: Add new page to shell navigation | Restrictions: Follow existing tab pattern | Success: New page accessible via tab_

#### [ ] 16.3 Create ControlShowcasePage page object

- **File:** `testsnew/Brinell.Maui.UITests/Pages/ControlShowcasePage.cs`
- **Purpose:** Expose new controls for testing
- _Leverage: Existing page object pattern_
- _Requirements: REQ-025.12_
- _Prompt: Role: Test automation developer | Task: Create page object for ControlShowcasePage | Restrictions: Use factory methods for controls | Success: All showcase controls accessible_

---

## Phase 7: Verification

### [ ] 17. Build and verify all tests compile

- **Command:** `dotnet build testsnew/Brinell.Maui.UITests/`
- **Purpose:** Ensure all test files compile without errors
- _Requirements: All_
- _Prompt: Role: Build engineer | Task: Verify solution builds with all new tests | Restrictions: Fix any compilation errors | Success: Build succeeds with 0 errors_

### [ ] 18. Run test subset to verify infrastructure

- **Command:** `dotnet test testsnew/Brinell.Maui.UITests/ --filter "Control=Label"`
- **Purpose:** Verify test infrastructure works before full run
- _Requirements: All_
- _Prompt: Role: QA engineer | Task: Run subset of tests to verify Appium connection and page navigation | Restrictions: Ensure Appium server running, app deployed | Success: Sample tests pass_

---

## Summary

| Phase | Tasks | Est. Time |
|-------|-------|-----------|
| 1. Infrastructure | 4 tasks | 2 hours |
| 2. P1 Core Tests | 9 tasks | 4 hours |
| 3. P2 Selection/DateTime | 3 tasks | 1.5 hours |
| 4. P3 Container/Collection | 6 tasks | 3 hours |
| 5. P4 Specialized | 6 tasks | 3 hours |
| 6. Sample App Updates | 3 tasks | 2 hours |
| 7. Verification | 2 tasks | 1 hour |
| **Total** | **33 tasks** | **~16.5 hours** |
