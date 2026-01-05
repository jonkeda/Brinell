# PLAN-UI-TESTS-IMPLEMENTATION: Comprehensive UI Tests for ControlObject6 Framework

**Version:** 1.0  
**Created:** January 5, 2026  
**Status:** Ready for Implementation  
**Based on:** PLAN-POC-UI-TESTS-IMPLEMENTATION, SPEC-006*

---

## Overview

This plan defines comprehensive end-to-end UI tests for all MAUI and Blazor control objects defined in the SPEC-006 specification series. These tests validate real user interactions against the sample applications.

### Goals
- Achieve 100% UI test coverage for all control types
- Validate all interface methods work correctly in real scenarios
- Test gesture recognition, navigation, and complex interactions
- Ensure cross-platform behavior consistency

### Dependencies
| Component | Version | Purpose |
|-----------|---------|---------|
| xunit | 2.9.3 | Test framework |
| FluentAssertions | 6.12.0 | Assertions |
| Appium.WebDriver | 8.0.1 | MAUI automation |
| Microsoft.Playwright | 1.50.0 | Blazor automation |

---

## Current State

### POC Tests (Complete)
| Project | Tests | Status |
|---------|-------|--------|
| Brinell.Samples.Maui.UITests.ControlObject6 | 44 | ✅ Build Verified |
| Brinell.Samples.Blazor.UITests.ControlObject6 | 46 | ✅ Build Verified |

### Sample App Coverage
| App | Pages | Control Types |
|-----|-------|---------------|
| MAUI Sample | 7 pages | 27 control types |
| Blazor Sample | 9 pages | 19 control types |

---

## Phase 1: MAUI Page Objects (Days 1-2)

### 1.1 Required Page Objects

| Page Object | Source Page | Controls to Map |
|-------------|-------------|-----------------|
| MainPageObject6.cs | MainPage.xaml | Button, Entry, Editor, Switch, CheckBox, Slider, Picker, DatePicker, TimePicker, ProgressBar, ActivityIndicator |
| UserFormPageObject6.cs | UserFormPage.xaml | Entry, Editor, SearchBar, Switch, CheckBox, RadioButton, Picker, DatePicker, TimePicker, Slider, Stepper |
| AdvancedPageObject6.cs | AdvancedPage.xaml | Frame, SwipeView, Grid, FlexLayout, TapGesture, PanGesture, PinchGesture |
| NavigationPageObject6.cs | NavigationDemoPage.xaml | Shell, FlyoutItem, TabBar, NavigationPage |
| DashboardPageObject6.cs | DashboardPage.xaml | Frame, Grid, ScrollView, RefreshView |
| DataGridPageObject6.cs | DataGridPage.xaml | CollectionView, ListView, CarouselView |
| MediaPageObject6.cs | MediaGalleryPage.xaml | Image, WebView, ActivityIndicator |

### 1.2 MainPageObject6.cs Controls

```csharp
public class MainPageObject6 : PageObjectBase
{
    // Counter Section
    public LabelControl CounterLabel => new(Context, "CounterLabel", this);
    public ButtonControl IncrementButton => new(Context, "IncrementButton", this);
    public ButtonControl DecrementButton => new(Context, "DecrementButton", this);
    public ButtonControl ResetButton => new(Context, "ResetButton", this);
    
    // Text Input Section
    public EntryControl NameEntry => new(Context, "NameEntry", this);
    public EntryControl EmailEntry => new(Context, "EmailEntry", this);
    public EditorControl MessageEditor => new(Context, "MessageEditor", this);
    public LabelControl GreetingLabel => new(Context, "GreetingLabel", this);
    public ButtonControl GreetButton => new(Context, "GreetButton", this);
    
    // Toggle Section
    public SwitchControl NotificationSwitch => new(Context, "NotificationSwitch", this);
    public CheckBoxControl AgreeCheckBox => new(Context, "AgreeCheckBox", this);
    
    // Slider Section
    public SliderControl VolumeSlider => new(Context, "VolumeSlider", this);
    public LabelControl VolumeLabel => new(Context, "VolumeLabel", this);
    public ProgressBarControl VolumeProgress => new(Context, "VolumeProgress", this);
    
    // Picker Section
    public PickerControl ColorPicker => new(Context, "ColorPicker", this);
    public DatePickerControl BirthDatePicker => new(Context, "BirthDatePicker", this);
    public TimePickerControl ReminderTimePicker => new(Context, "ReminderTimePicker", this);
    
    // Activity Section
    public ActivityIndicatorControl LoadingIndicator => new(Context, "LoadingIndicator", this);
    public ButtonControl ToggleLoadingButton => new(Context, "ToggleLoadingButton", this);
}
```

### 1.3 UserFormPageObject6.cs Controls

```csharp
public class UserFormPageObject6 : PageObjectBase
{
    // Text Entry Controls
    public EntryControl FirstNameEntry => new(Context, "FirstNameEntry", this);
    public EntryControl LastNameEntry => new(Context, "LastNameEntry", this);
    public EntryControl EmailEntry => new(Context, "EmailEntry", this);
    public EntryControl PhoneEntry => new(Context, "PhoneEntry", this);
    public EditorControl BioEditor => new(Context, "BioEditor", this);
    public SearchBarControl UserSearchBar => new(Context, "UserSearchBar", this);
    
    // Toggle Controls
    public SwitchControl NewsletterSwitch => new(Context, "NewsletterSwitch", this);
    public CheckBoxControl TermsCheckBox => new(Context, "TermsCheckBox", this);
    public CheckBoxControl PrivacyCheckBox => new(Context, "PrivacyCheckBox", this);
    public RadioButtonControl BasicRadio => new(Context, "BasicRadio", this);
    public RadioButtonControl ProfessionalRadio => new(Context, "ProfessionalRadio", this);
    public RadioButtonControl EnterpriseRadio => new(Context, "EnterpriseRadio", this);
    
    // Selection Controls
    public PickerControl CountryPicker => new(Context, "CountryPicker", this);
    public PickerControl DepartmentPicker => new(Context, "DepartmentPicker", this);
    public DatePickerControl BirthDatePicker => new(Context, "BirthDatePicker", this);
    public TimePickerControl PreferredTimePicker => new(Context, "PreferredTimePicker", this);
    
    // Range Controls
    public SliderControl FontSizeSlider => new(Context, "FontSizeSlider", this);
    public SliderControl VolumeSlider => new(Context, "VolumeSlider", this);
    public StepperControl QuantityStepper => new(Context, "QuantityStepper", this);
    
    // Action Buttons
    public ButtonControl SubmitButton => new(Context, "SubmitButton", this);
    public ButtonControl SaveDraftButton => new(Context, "SaveDraftButton", this);
    public ButtonControl ClearButton => new(Context, "ClearButton", this);
}
```

---

## Phase 2: MAUI Test Cases (Days 3-6)

### 2.1 Foundation Control Tests

**File:** `Tests/FoundationControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| FC-001 | Label_DisplaysText_ShowsCorrectValue | LabelControl | P0 |
| FC-002 | Label_AssertText_ValidatesContent | LabelControl | P0 |
| FC-003 | Button_Click_PerformsAction | ButtonControl | P0 |
| FC-004 | Button_DoubleClick_PerformsAction | ButtonControl | P1 |
| FC-005 | Button_LongPress_PerformsAction | ButtonControl | P1 |
| FC-006 | Button_IsEnabled_ValidatesState | ButtonControl | P0 |
| FC-007 | Button_IsVisible_ValidatesState | ButtonControl | P0 |

### 2.2 Text Input Control Tests

**File:** `Tests/TextInputControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| TI-001 | Entry_Enter_SetsText | EntryControl | P0 |
| TI-002 | Entry_Clear_RemovesText | EntryControl | P0 |
| TI-003 | Entry_ClearAndEnter_ReplacesText | EntryControl | P0 |
| TI-004 | Entry_GetText_ReturnsValue | EntryControl | P0 |
| TI-005 | Entry_IsReadOnly_ValidatesState | EntryControl | P1 |
| TI-006 | Entry_Focus_ReceivesFocus | EntryControl | P1 |
| TI-007 | Editor_Enter_SetsMultilineText | EditorControl | P0 |
| TI-008 | Editor_Append_AddsText | EditorControl | P1 |
| TI-009 | Editor_GetTextLength_ReturnsLength | EditorControl | P1 |
| TI-010 | SearchBar_Enter_SetsSearchText | SearchBarControl | P0 |
| TI-011 | SearchBar_Submit_TriggersSearch | SearchBarControl | P1 |
| TI-012 | SearchBar_ClearSearch_ClearsText | SearchBarControl | P1 |

### 2.3 Toggle Control Tests

**File:** `Tests/ToggleControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| TG-001 | Switch_Toggle_ChangesState | SwitchControl | P0 |
| TG-002 | Switch_SetChecked_SetsState | SwitchControl | P0 |
| TG-003 | Switch_IsChecked_ReturnsState | SwitchControl | P0 |
| TG-004 | CheckBox_Toggle_ChangesState | CheckBoxControl | P0 |
| TG-005 | CheckBox_Check_SetsChecked | CheckBoxControl | P0 |
| TG-006 | CheckBox_Uncheck_ClearsChecked | CheckBoxControl | P0 |
| TG-007 | RadioButton_Select_SelectsOption | RadioButtonControl | P0 |
| TG-008 | RadioButton_IsSelected_ReturnsState | RadioButtonControl | P0 |
| TG-009 | RadioButton_GroupSelection_DeselectsOthers | RadioButtonControl | P1 |
| TG-010 | RadioButton_GetGroupName_ReturnsGroup | RadioButtonControl | P1 |

### 2.4 Range Control Tests

**File:** `Tests/RangeControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| RG-001 | Slider_SetValue_SetsPosition | SliderControl | P0 |
| RG-002 | Slider_GetValue_ReturnsPosition | SliderControl | P0 |
| RG-003 | Slider_GetMinimum_ReturnsMin | SliderControl | P1 |
| RG-004 | Slider_GetMaximum_ReturnsMax | SliderControl | P1 |
| RG-005 | Slider_Increment_IncreasesValue | SliderControl | P1 |
| RG-006 | Slider_Decrement_DecreasesValue | SliderControl | P1 |
| RG-007 | Stepper_Increment_IncreasesValue | StepperControl | P0 |
| RG-008 | Stepper_Decrement_DecreasesValue | StepperControl | P0 |
| RG-009 | Stepper_GetValue_ReturnsValue | StepperControl | P0 |
| RG-010 | Stepper_AtMaximum_StopsIncrement | StepperControl | P1 |
| RG-011 | Stepper_AtMinimum_StopsDecrement | StepperControl | P1 |
| RG-012 | ProgressBar_GetProgress_ReturnsValue | ProgressBarControl | P0 |
| RG-013 | ProgressBar_AssertProgress_ValidatesValue | ProgressBarControl | P1 |

### 2.5 Selection Control Tests

**File:** `Tests/SelectionControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| SL-001 | Picker_SelectByIndex_SelectsItem | PickerControl | P0 |
| SL-002 | Picker_SelectByText_SelectsItem | PickerControl | P0 |
| SL-003 | Picker_GetSelectedIndex_ReturnsIndex | PickerControl | P0 |
| SL-004 | Picker_GetSelectedText_ReturnsText | PickerControl | P0 |
| SL-005 | Picker_GetItems_ReturnsAllItems | PickerControl | P1 |
| SL-006 | DatePicker_SetDate_SetsDate | DatePickerControl | P0 |
| SL-007 | DatePicker_GetDate_ReturnsDate | DatePickerControl | P0 |
| SL-008 | DatePicker_AssertDate_ValidatesDate | DatePickerControl | P1 |
| SL-009 | TimePicker_SetTime_SetsTime | TimePickerControl | P0 |
| SL-010 | TimePicker_GetTime_ReturnsTime | TimePickerControl | P0 |
| SL-011 | TimePicker_AssertTime_ValidatesTime | TimePickerControl | P1 |

### 2.6 Collection Control Tests

**File:** `Tests/CollectionControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| CC-001 | ListView_GetItemCount_ReturnsCount | ListViewControl | P0 |
| CC-002 | ListView_GetItemText_ReturnsText | ListViewControl | P0 |
| CC-003 | ListView_ClickItem_SelectsItem | ListViewControl | P0 |
| CC-004 | ListView_ScrollToItem_ScrollsToItem | ListViewControl | P1 |
| CC-005 | CollectionView_GetItemCount_ReturnsCount | CollectionViewControl | P0 |
| CC-006 | CollectionView_SelectItem_SelectsItem | CollectionViewControl | P0 |
| CC-007 | CollectionView_ScrollToTop_ScrollsToTop | CollectionViewControl | P1 |
| CC-008 | CollectionView_ScrollToBottom_ScrollsToBottom | CollectionViewControl | P1 |
| CC-009 | CarouselView_GetCurrentPosition_ReturnsPosition | CarouselViewControl | P1 |
| CC-010 | CarouselView_SwipeNext_AdvancesPosition | CarouselViewControl | P1 |
| CC-011 | CarouselView_SwipePrevious_GoesBack | CarouselViewControl | P1 |
| CC-012 | CarouselView_GoToPosition_NavigatesToPosition | CarouselViewControl | P1 |

### 2.7 Container Control Tests

**File:** `Tests/ContainerControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| CN-001 | Frame_IsVisible_ValidatesVisibility | FrameControl | P1 |
| CN-002 | Frame_GetChildCount_ReturnsCount | FrameControl | P2 |
| CN-003 | ScrollView_ScrollToEnd_ScrollsToEnd | ScrollViewControl | P1 |
| CN-004 | ScrollView_ScrollToStart_ScrollsToStart | ScrollViewControl | P1 |
| CN-005 | ScrollView_GetScrollY_ReturnsPosition | ScrollViewControl | P2 |
| CN-006 | SwipeView_SwipeLeft_OpensRightItems | SwipeViewControl | P1 |
| CN-007 | SwipeView_SwipeRight_OpensLeftItems | SwipeViewControl | P1 |
| CN-008 | SwipeView_CloseSwipe_ClosesItems | SwipeViewControl | P1 |
| CN-009 | RefreshView_PullToRefresh_TriggersRefresh | RefreshViewControl | P1 |
| CN-010 | RefreshView_IsRefreshing_ReturnsState | RefreshViewControl | P2 |
| CN-011 | Border_IsVisible_ValidatesVisibility | BorderControl | P2 |
| CN-012 | ContentView_HasContent_ValidatesContent | ContentViewControl | P2 |

### 2.8 Navigation Control Tests

**File:** `Tests/NavigationControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| NV-001 | Shell_NavigateTo_NavigatesToRoute | ShellControl | P0 |
| NV-002 | Shell_GetCurrentRoute_ReturnsRoute | ShellControl | P1 |
| NV-003 | Shell_IsFlyoutOpen_ReturnsState | ShellControl | P1 |
| NV-004 | Shell_OpenFlyout_OpensFlyout | ShellControl | P1 |
| NV-005 | Shell_CloseFlyout_ClosesFlyout | ShellControl | P1 |
| NV-006 | FlyoutItem_Select_NavigatesToPage | FlyoutItemControl | P0 |
| NV-007 | FlyoutItem_IsSelected_ReturnsState | FlyoutItemControl | P1 |
| NV-008 | TabBar_SelectTab_SelectsTab | TabBarControl | P0 |
| NV-009 | TabBar_GetSelectedTabIndex_ReturnsIndex | TabBarControl | P0 |
| NV-010 | TabBar_GetTabCount_ReturnsCount | TabBarControl | P1 |

### 2.9 Display Control Tests

**File:** `Tests/DisplayControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| DS-001 | Image_IsVisible_ValidatesVisibility | ImageControl | P0 |
| DS-002 | Image_GetSource_ReturnsSource | ImageControl | P1 |
| DS-003 | Image_IsLoaded_ReturnsLoadState | ImageControl | P1 |
| DS-004 | ActivityIndicator_IsRunning_ReturnsState | ActivityIndicatorControl | P0 |
| DS-005 | ActivityIndicator_Start_StartsAnimation | ActivityIndicatorControl | P1 |
| DS-006 | ActivityIndicator_Stop_StopsAnimation | ActivityIndicatorControl | P1 |
| DS-007 | WebView_Navigate_NavigatesToUrl | WebViewControl | P1 |
| DS-008 | WebView_GetCurrentUrl_ReturnsUrl | WebViewControl | P1 |
| DS-009 | WebView_GoBack_NavigatesBack | WebViewControl | P2 |
| DS-010 | WebView_GoForward_NavigatesForward | WebViewControl | P2 |

### 2.10 Gesture Tests

**File:** `Tests/GestureTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| GS-001 | TapGesture_SingleTap_RecognizesTap | Frame (TapGesture) | P1 |
| GS-002 | TapGesture_DoubleTap_RecognizesDoubleTap | Frame (TapGesture) | P1 |
| GS-003 | PanGesture_Pan_RecognizesPan | Frame (PanGesture) | P2 |
| GS-004 | PinchGesture_Pinch_RecognizesPinch | Frame (PinchGesture) | P2 |
| GS-005 | SwipeGesture_SwipeLeft_RecognizesSwipe | Frame (SwipeGesture) | P2 |
| GS-006 | SwipeGesture_SwipeRight_RecognizesSwipe | Frame (SwipeGesture) | P2 |

---

## Phase 3: Blazor Page Objects (Days 7-8)

### 3.1 Required Page Objects

| Page Object | Source Page | Controls to Map |
|-------------|-------------|-----------------|
| IndexPageObject6.cs | Index.razor | Label, Button, Link |
| CounterPageObject6.cs | Counter.razor | Button, Label |
| LoginPageObject6.cs | Login.razor | Input, Button, Label |
| FormControlsPageObject6.cs | FormControls.razor | CheckBox, Select, TextArea, Range, Progress, Link |
| DataTablePageObject6.cs | DataTable.razor | Table, Input, Select, Button, Pagination |
| AdvancedPageObject6.cs | Advanced.razor | Click events, Hover, Focus, DragDrop, Grid, Flex |
| DashboardPageObject6.cs | Dashboard.razor | Cards, Grid, Progress |
| NavigationPageObject6.cs | Navigation.razor | NavMenu, Link, Tabs |
| MediaPageObject6.cs | MediaGallery.razor | Image, Video, Audio, IFrame |

### 3.2 FormControlsPageObject6.cs Controls

```csharp
public class FormControlsPageObject6 : AsyncPageObjectBase
{
    // Checkboxes
    public CheckBoxControl TermsCheckbox => new(Context, By.Id("terms-checkbox"), this);
    public CheckBoxControl NewsletterCheckbox => new(Context, By.Id("newsletter-checkbox"), this);
    public CheckBoxControl DisabledCheckbox => new(Context, By.Id("disabled-checkbox"), this);
    
    // Select Controls
    public SelectControl CountrySelect => new(Context, By.Id("country-select"), this);
    public SelectControl ColorsSelect => new(Context, By.Id("colors-select"), this);
    
    // Links
    public LinkControl InternalLink => new(Context, By.Id("internal-link"), this);
    public LinkControl ExternalLink => new(Context, By.Id("external-link"), this);
    public LinkControl DownloadLink => new(Context, By.Id("download-link"), this);
    
    // TextArea
    public TextAreaControl CommentsTextArea => new(Context, By.Id("comments-textarea"), this);
    
    // Range
    public RangeControl VolumeRange => new(Context, By.Id("volume-range"), this);
    public RangeControl BrightnessRange => new(Context, By.Id("brightness-range"), this);
    
    // Progress
    public ProgressControl UploadProgress => new(Context, By.Id("upload-progress"), this);
    public ButtonControl SimulateUploadBtn => new(Context, By.Id("simulate-upload-btn"), this);
}
```

### 3.3 DataTablePageObject6.cs Controls

```csharp
public class DataTablePageObject6 : AsyncPageObjectBase
{
    // Search and Filter
    public InputControl SearchInput => new(Context, By.TestId("SearchInput"), this);
    public SelectControl CategoryFilter => new(Context, By.TestId("CategoryFilter"), this);
    public SelectControl StatusFilter => new(Context, By.TestId("StatusFilter"), this);
    public ButtonControl ClearFiltersButton => new(Context, By.TestId("ClearFiltersButton"), this);
    
    // Table
    public TableControl DataTable => new(Context, By.TestId("DataTable"), this);
    
    // Pagination
    public ButtonControl PrevPageButton => new(Context, By.TestId("PrevPageButton"), this);
    public ButtonControl NextPageButton => new(Context, By.TestId("NextPageButton"), this);
    public SelectControl PageSizeSelector => new(Context, By.TestId("PageSizeSelector"), this);
    
    // Bulk Actions
    public ButtonControl SelectAllButton => new(Context, By.TestId("SelectAllButton"), this);
    public ButtonControl DeleteSelectedButton => new(Context, By.TestId("DeleteSelectedButton"), this);
    
    // Row Methods
    public CheckBoxControl GetRowCheckbox(int id) => 
        new(Context, By.TestId($"RowCheckbox_{id}"), this);
    public ButtonControl GetViewButton(int id) => 
        new(Context, By.TestId($"ViewButton_{id}"), this);
    public ButtonControl GetEditButton(int id) => 
        new(Context, By.TestId($"EditButton_{id}"), this);
    public ButtonControl GetDeleteButton(int id) => 
        new(Context, By.TestId($"DeleteButton_{id}"), this);
}
```

---

## Phase 4: Blazor Test Cases (Days 9-12)

### 4.1 Foundation Control Tests

**File:** `Tests/FoundationControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BFC-001 | Label_DisplaysText_ShowsCorrectValue | LabelControl | P0 |
| BFC-002 | Label_AssertTextAsync_ValidatesContent | LabelControl | P0 |
| BFC-003 | Button_ClickAsync_PerformsAction | ButtonControl | P0 |
| BFC-004 | Button_DoubleClickAsync_PerformsAction | ButtonControl | P1 |
| BFC-005 | Button_HoverAsync_TriggersHover | ButtonControl | P1 |
| BFC-006 | Button_IsEnabledAsync_ValidatesState | ButtonControl | P0 |
| BFC-007 | Link_ClickAsync_Navigates | LinkControl | P0 |
| BFC-008 | Link_GetHrefAsync_ReturnsHref | LinkControl | P0 |
| BFC-009 | Link_GetTargetAsync_ReturnsTarget | LinkControl | P1 |

### 4.2 Text Input Control Tests

**File:** `Tests/TextInputControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BTI-001 | Input_EnterAsync_SetsText | InputControl | P0 |
| BTI-002 | Input_ClearAsync_RemovesText | InputControl | P0 |
| BTI-003 | Input_GetTextAsync_ReturnsValue | InputControl | P0 |
| BTI-004 | Input_FocusAsync_ReceivesFocus | InputControl | P1 |
| BTI-005 | Input_BlurAsync_LosesFocus | InputControl | P1 |
| BTI-006 | Input_IsReadOnlyAsync_ValidatesState | InputControl | P1 |
| BTI-007 | TextArea_EnterAsync_SetsMultilineText | TextAreaControl | P0 |
| BTI-008 | TextArea_AppendAsync_AddsText | TextAreaControl | P1 |
| BTI-009 | TextArea_GetTextLengthAsync_ReturnsLength | TextAreaControl | P1 |

### 4.3 Toggle Control Tests

**File:** `Tests/ToggleControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BTG-001 | CheckBox_ToggleAsync_ChangesState | CheckBoxControl | P0 |
| BTG-002 | CheckBox_CheckAsync_SetsChecked | CheckBoxControl | P0 |
| BTG-003 | CheckBox_UncheckAsync_ClearsChecked | CheckBoxControl | P0 |
| BTG-004 | CheckBox_IsCheckedAsync_ReturnsState | CheckBoxControl | P0 |
| BTG-005 | CheckBox_Disabled_CannotToggle | CheckBoxControl | P1 |
| BTG-006 | RadioButton_SelectAsync_SelectsOption | RadioButtonControl | P0 |
| BTG-007 | RadioButton_IsCheckedAsync_ReturnsState | RadioButtonControl | P0 |
| BTG-008 | RadioButton_GroupSelection_DeselectsOthers | RadioButtonControl | P1 |

### 4.4 Selection Control Tests

**File:** `Tests/SelectionControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BSL-001 | Select_SelectByIndexAsync_SelectsItem | SelectControl | P0 |
| BSL-002 | Select_SelectByTextAsync_SelectsItem | SelectControl | P0 |
| BSL-003 | Select_GetSelectedIndexAsync_ReturnsIndex | SelectControl | P0 |
| BSL-004 | Select_GetSelectedTextAsync_ReturnsText | SelectControl | P0 |
| BSL-005 | Select_GetItemsAsync_ReturnsAllItems | SelectControl | P1 |
| BSL-006 | DateInput_SetValueAsync_SetsDate | DateInputControl | P0 |
| BSL-007 | DateInput_GetValueAsync_ReturnsDate | DateInputControl | P0 |
| BSL-008 | TimeInput_SetValueAsync_SetsTime | TimeInputControl | P0 |
| BSL-009 | TimeInput_GetValueAsync_ReturnsTime | TimeInputControl | P0 |

### 4.5 Range Control Tests

**File:** `Tests/RangeControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BRG-001 | Range_SetValueAsync_SetsPosition | RangeControl | P0 |
| BRG-002 | Range_GetValueAsync_ReturnsPosition | RangeControl | P0 |
| BRG-003 | Range_GetMinAsync_ReturnsMin | RangeControl | P1 |
| BRG-004 | Range_GetMaxAsync_ReturnsMax | RangeControl | P1 |
| BRG-005 | Range_GetStepAsync_ReturnsStep | RangeControl | P2 |
| BRG-006 | Progress_GetValueAsync_ReturnsValue | ProgressControl | P0 |
| BRG-007 | Progress_GetMaxAsync_ReturnsMax | ProgressControl | P1 |
| BRG-008 | Progress_GetPercentageAsync_ReturnsPercentage | ProgressControl | P1 |

### 4.6 Collection Control Tests

**File:** `Tests/CollectionControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BCC-001 | List_GetItemCountAsync_ReturnsCount | ListControl | P0 |
| BCC-002 | List_GetItemTextAsync_ReturnsText | ListControl | P0 |
| BCC-003 | List_ClickItemAsync_SelectsItem | ListControl | P0 |
| BCC-004 | Table_GetRowCountAsync_ReturnsCount | TableControl | P0 |
| BCC-005 | Table_GetCellTextAsync_ReturnsCellText | TableControl | P0 |
| BCC-006 | Table_ClickRowAsync_SelectsRow | TableControl | P0 |
| BCC-007 | Table_SortByColumn_SortsData | TableControl | P1 |
| BCC-008 | Table_Pagination_NavigatesPages | TableControl | P1 |
| BCC-009 | Table_Filter_FiltersData | TableControl | P1 |

### 4.7 Media Control Tests

**File:** `Tests/MediaControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BMD-001 | Image_IsVisibleAsync_ValidatesVisibility | ImageControl | P0 |
| BMD-002 | Image_GetSrcAsync_ReturnsSource | ImageControl | P0 |
| BMD-003 | Image_GetAltAsync_ReturnsAltText | ImageControl | P1 |
| BMD-004 | Video_PlayAsync_StartsPlayback | VideoControl | P1 |
| BMD-005 | Video_PauseAsync_PausesPlayback | VideoControl | P1 |
| BMD-006 | Video_GetDurationAsync_ReturnsDuration | VideoControl | P2 |
| BMD-007 | Audio_PlayAsync_StartsPlayback | AudioControl | P1 |
| BMD-008 | Audio_PauseAsync_PausesPlayback | AudioControl | P1 |
| BMD-009 | IFrame_GetSrcAsync_ReturnsSource | IFrameControl | P1 |
| BMD-010 | IFrame_NavigateToAsync_ChangesSource | IFrameControl | P2 |

### 4.8 Navigation Control Tests

**File:** `Tests/NavigationControlTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BNV-001 | NavMenu_IsOpenAsync_ReturnsState | NavMenuControl | P0 |
| BNV-002 | NavMenu_ToggleAsync_TogglesMenu | NavMenuControl | P0 |
| BNV-003 | NavMenu_GetItemCountAsync_ReturnsCount | NavMenuControl | P1 |
| BNV-004 | NavMenu_ClickItemAsync_NavigatesToPage | NavMenuControl | P0 |
| BNV-005 | Tab_SelectTabAsync_SelectsTab | TabControl | P0 |
| BNV-006 | Tab_GetSelectedTabIndexAsync_ReturnsIndex | TabControl | P0 |
| BNV-007 | Tab_GetTabCountAsync_ReturnsCount | TabControl | P1 |

### 4.9 Advanced Interaction Tests

**File:** `Tests/AdvancedInteractionTests6.cs`

| Test ID | Test Name | Controls | Priority |
|---------|-----------|----------|----------|
| BAI-001 | ClickArea_SingleClick_IncrementsCount | Click event | P0 |
| BAI-002 | ClickArea_DoubleClick_IncrementsCount | DblClick event | P1 |
| BAI-003 | ContextMenu_RightClick_ShowsMenu | Context menu | P1 |
| BAI-004 | HoverArea_MouseEnter_ChangesBackground | Hover event | P1 |
| BAI-005 | HoverArea_MouseLeave_ResetsBackground | Hover event | P1 |
| BAI-006 | KeyboardInput_KeyDown_CapturesKey | Keyboard event | P1 |
| BAI-007 | FocusInput_Focus_UpdatesStatus | Focus event | P1 |
| BAI-008 | FocusInput_Blur_UpdatesStatus | Focus event | P1 |
| BAI-009 | DragDrop_DragItem_InitiatesDrag | DragStart event | P2 |
| BAI-010 | DragDrop_DropItem_CompletesDropAsync | Drop event | P2 |
| BAI-011 | Tooltip_Hover_ShowsTooltip | Title attribute | P2 |
| BAI-012 | Popover_Click_ShowsPopover | Popover | P2 |

---

## Phase 5: Integration & Validation (Days 13-14)

### 5.1 Cross-Page Navigation Tests

**File:** `Tests/CrossPageNavigationTests6.cs`

| Test ID | Test Name | Priority |
|---------|-----------|----------|
| XN-001 | MAUI_NavigateToAllPages_NoErrors | P0 |
| XN-002 | MAUI_BackNavigation_WorksCorrectly | P1 |
| XN-003 | Blazor_NavigateToAllPages_NoErrors | P0 |
| XN-004 | Blazor_BackNavigation_WorksCorrectly | P1 |

### 5.2 End-to-End Workflow Tests

**File:** `Tests/WorkflowTests6.cs`

| Test ID | Test Name | Priority |
|---------|-----------|----------|
| WF-001 | MAUI_UserFormSubmission_CompletesSuccessfully | P0 |
| WF-002 | MAUI_CounterOperations_AllButtonsWork | P0 |
| WF-003 | Blazor_LoginFlow_AuthenticatesUser | P0 |
| WF-004 | Blazor_DataTableCRUD_AllOperationsWork | P0 |

### 5.3 Error Handling Tests

**File:** `Tests/ErrorHandlingTests6.cs`

| Test ID | Test Name | Priority |
|---------|-----------|----------|
| EH-001 | MAUI_MissingElement_ThrowsTimeoutException | P1 |
| EH-002 | MAUI_DisabledButton_ThrowsOnClick | P1 |
| EH-003 | Blazor_MissingElement_ThrowsTimeoutException | P1 |
| EH-004 | Blazor_DisabledButton_ThrowsOnClick | P1 |

---

## Test Count Summary

### MAUI UI Tests

| Category | P0 | P1 | P2 | Total |
|----------|----|----|----|----|
| Foundation | 5 | 2 | 0 | 7 |
| Text Input | 4 | 8 | 0 | 12 |
| Toggle | 4 | 6 | 0 | 10 |
| Range | 4 | 7 | 2 | 13 |
| Selection | 5 | 6 | 0 | 11 |
| Collection | 4 | 8 | 0 | 12 |
| Container | 0 | 8 | 4 | 12 |
| Navigation | 4 | 6 | 0 | 10 |
| Display | 2 | 6 | 2 | 10 |
| Gestures | 0 | 2 | 4 | 6 |
| **MAUI Total** | **32** | **59** | **12** | **103** |

### Blazor UI Tests

| Category | P0 | P1 | P2 | Total |
|----------|----|----|----|----|
| Foundation | 5 | 4 | 0 | 9 |
| Text Input | 3 | 6 | 0 | 9 |
| Toggle | 4 | 4 | 0 | 8 |
| Range | 3 | 4 | 1 | 8 |
| Selection | 5 | 4 | 0 | 9 |
| Collection | 4 | 5 | 0 | 9 |
| Media | 2 | 5 | 3 | 10 |
| Navigation | 3 | 4 | 0 | 7 |
| Advanced | 2 | 6 | 4 | 12 |
| **Blazor Total** | **31** | **42** | **8** | **81** |

### Combined Total

| Priority | MAUI | Blazor | Total |
|----------|------|--------|-------|
| P0 (Critical) | 32 | 31 | **63** |
| P1 (High) | 59 | 42 | **101** |
| P2 (Medium) | 12 | 8 | **20** |
| **Total** | **103** | **81** | **184** |

---

## Implementation Schedule

### Week 1

| Day | Tasks | Est. Tests |
|-----|-------|------------|
| Day 1 | MAUI Page Objects (Main, UserForm) | 0 |
| Day 2 | MAUI Page Objects (Advanced, Navigation, Others) | 0 |
| Day 3 | MAUI Foundation, Text Input Tests | 19 |
| Day 4 | MAUI Toggle, Range Tests | 23 |
| Day 5 | MAUI Selection, Collection Tests | 23 |
| Day 6 | MAUI Container, Navigation, Display Tests | 32 |

### Week 2

| Day | Tasks | Est. Tests |
|-----|-------|------------|
| Day 7 | Blazor Page Objects (Form, Data, Advanced) | 0 |
| Day 8 | Blazor Page Objects (Navigation, Media) | 0 |
| Day 9 | Blazor Foundation, Text Input Tests | 18 |
| Day 10 | Blazor Toggle, Range, Selection Tests | 25 |
| Day 11 | Blazor Collection, Media Tests | 19 |
| Day 12 | Blazor Navigation, Advanced Tests | 19 |
| Day 13 | Cross-Page Navigation Tests | 4 |
| Day 14 | Workflow & Error Handling Tests | 8 |

---

## Running Tests

### MAUI Tests

```powershell
# Start Appium server
appium --allow-insecure chromedriver_autodownload

# Start MAUI app (new terminal)
& 'samples\Brinell.Samples.Maui.App\bin\Debug\net10.0-windows10.0.19041.0\win-x64\Brinell.Samples.Maui.App.exe'

# Run tests
cd samples/Brinell.Samples.Maui.UITests.ControlObject6
dotnet test --logger "console;verbosity=detailed"
```

### Blazor Tests

```powershell
# Start Blazor app (new terminal)
cd samples/Brinell.Samples.Blazor.App
dotnet run --urls "http://localhost:5180"

# Wait for app to start
Start-Sleep 10

# Run tests
cd samples/Brinell.Samples.Blazor.UITests.ControlObject6
dotnet test --logger "console;verbosity=detailed"
```

---

## Success Criteria

- [ ] All 184 UI test cases implemented
- [ ] All P0 tests pass consistently (63 tests)
- [ ] 95%+ P1 tests pass consistently (96+ of 101 tests)
- [ ] 80%+ P2 tests pass consistently (16+ of 20 tests)
- [ ] MAUI tests run with Appium on Windows
- [ ] Blazor tests run with Playwright headless
- [ ] Screenshot capture works for failures
- [ ] Documentation complete

---

## Notes

### MAUI-Specific Considerations
- WinAppDriver required for Windows testing
- Appium server must be running before tests
- Use `AutomationId` for element location
- Some gestures may require platform-specific implementation

### Blazor-Specific Considerations
- Playwright is async-first - all tests use `async Task` pattern
- SignalR updates need wait time for state changes
- Use `data-testid` or `id` attributes for reliable selectors
- Headless mode for CI environments

### Test Isolation
- Each test should be independent
- Reset state between tests where possible
- Use fresh page load for critical tests
- Avoid test order dependencies

---

*Plan created: January 5, 2026*
*Based on: SPEC-006-INDEX, SPEC-006-003b-INDEX, Sample Applications*
