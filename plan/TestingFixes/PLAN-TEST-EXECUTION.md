# Test Execution Plan - MAUI and Blazor UITests

**Created:** January 5, 2026  
**Status:** In Progress

---

## Overview

This plan outlines the systematic execution and fixing of all UI tests in the MAUI and Blazor sample projects. Tests will be executed one by one, with issues documented and fixed as they are discovered.

---

## Test Inventory

### MAUI UITests (21 tests)

| # | Test Class | Test Name | Status |
|---|------------|-----------|--------|
| 1 | CounterTests | Counter_InitialValue_IsZero | ⏳ Pending |
| 2 | CounterTests | Counter_Increment_IncreasesValue | ⏳ Pending |
| 3 | CounterTests | Counter_Decrement_DecreasesValue | ⏳ Pending |
| 4 | CounterTests | Counter_MultipleIncrements_ShowsCorrectValue | ⏳ Pending |
| 5 | CounterTests | Counter_Reset_SetsToZero | ⏳ Pending |
| 6 | TextInputTests | NameEntry_EnterText_ShowsValue | ⏳ Pending |
| 7 | TextInputTests | EmailEntry_EnterEmail_ShowsValue | ⏳ Pending |
| 8 | TextInputTests | GreetButton_WithName_ShowsGreeting | ⏳ Pending |
| 9 | TextInputTests | GreetButton_WithoutName_ShowsError | ⏳ Pending |
| 10 | TextInputTests | MessageEditor_EnterMultilineText_ShowsValue | ⏳ Pending |
| 11 | ToggleControlTests | NotificationSwitch_InitiallyOn_IsOn | ⏳ Pending |
| 12 | ToggleControlTests | NotificationSwitch_Toggle_TurnsOff | ⏳ Pending |
| 13 | ToggleControlTests | AgreeCheckBox_InitiallyUnchecked_IsUnchecked | ⏳ Pending |
| 14 | ToggleControlTests | AgreeCheckBox_Check_BecomesChecked | ⏳ Pending |
| 15 | ToggleControlTests | AgreeCheckBox_Toggle_ChangesState | ⏳ Pending |
| 16 | SliderTests | VolumeSlider_InitialValue_Is50 | ⏳ Pending |
| 17 | SliderTests | VolumeSlider_SetValue_UpdatesLabel | ⏳ Pending |
| 18 | SliderTests | VolumeProgress_InitialValue_IsHalf | ⏳ Pending |
| 19 | ActivityIndicatorTests | LoadingIndicator_Initially_NotRunning | ⏳ Pending |
| 20 | ActivityIndicatorTests | LoadingIndicator_ToggleButton_StartsIndicator | ⏳ Pending |
| 21 | ActivityIndicatorTests | LoadingIndicator_ToggleTwice_StopsIndicator | ⏳ Pending |

### Blazor UITests (79 tests)

| # | Test Class | Test Name | Status |
|---|------------|-----------|--------|
| 1 | CounterTests | Counter_InitialLoad_ShowsZeroCount | ⏳ Pending |
| 2 | CounterTests | Counter_ClickIncrement_IncreasesCount | ⏳ Pending |
| 3 | CounterTests | Counter_MultipleIncrements_CountsCorrectly | ⏳ Pending |
| 4 | CounterTests | Counter_Reset_SetsCountToZero | ⏳ Pending |
| 5 | CounterTests | Counter_IncrementAfterReset_CountsFromZero | ⏳ Pending |
| 6 | CounterTests | Counter_ButtonsAreVisible_OnLoad | ⏳ Pending |
| 7 | LoginTests | Login_WithValidCredentials_NavigatesToDashboard | ⏳ Pending |
| 8 | LoginTests | Login_WithInvalidCredentials_ShowsErrorMessage | ⏳ Pending |
| 9 | LoginTests | Login_PageLoad_ShowsAllFormElements | ⏳ Pending |
| 10 | LoginTests | Login_EmailInputHasPlaceholder | ⏳ Pending |
| 11 | LoginTests | Login_PasswordInputHasCorrectType | ⏳ Pending |
| 12 | LoginTests | Login_ShowsLoadingSpinnerDuringSubmit | ⏳ Pending |
| 13 | LoginTests | Login_ClearFields_WorksCorrectly | ⏳ Pending |
| 14 | NavigationTests | Navigation_HomePageLoad_ShowsWelcomeContent | ⏳ Pending |
| 15 | NavigationTests | Navigation_HomeToCounter_WorksCorrectly | ⏳ Pending |
| 16 | NavigationTests | Navigation_HomeToLogin_WorksCorrectly | ⏳ Pending |
| 17 | NavigationTests | Navigation_HomeToDashboard_WorksCorrectly | ⏳ Pending |
| 18 | NavigationTests | Navigation_DashboardToHome_WorksCorrectly | ⏳ Pending |
| 19 | NavigationTests | Navigation_DirectUrlToCounter_WorksCorrectly | ⏳ Pending |
| 20 | NavigationTests | Navigation_DirectUrlToLogin_WorksCorrectly | ⏳ Pending |
| 21 | NavigationTests | Navigation_AllHomeLinksVisible | ⏳ Pending |
| 22 | NavigationTests | Navigation_BackAndForth_MaintainsState | ⏳ Pending |
| 23 | NavigationTests | Navigation_BrowserBack_WorksCorrectly | ⏳ Pending |
| 24 | TableTests | Table_GetRowCount_ReturnsCorrectCount | ⏳ Pending |
| 25 | TableTests | Table_GetHeaders_ReturnsCorrectHeaders | ⏳ Pending |
| 26 | TableTests | Table_GetRowCells_ReturnsCorrectData | ⏳ Pending |
| 27 | TableTests | Table_GetCellText_ReturnsSpecificCell | ⏳ Pending |
| 28 | TableTests | Table_HasRowContaining_FindsRow | ⏳ Pending |
| 29 | TableTests | Table_FindRowContaining_ReturnsCorrectIndex | ⏳ Pending |
| 30 | TableTests | Table_GetColumnCells_ReturnsAllValuesInColumn | ⏳ Pending |
| 31 | TableTests | Table_AssertRowCount_PassesWithCorrectCount | ⏳ Pending |
| 32 | TableTests | Table_AssertCellText_PassesWithCorrectText | ⏳ Pending |
| 33 | TableTests | Table_IsVisible_WhenDisplayed | ⏳ Pending |
| 34 | FormControlsTests | FormControls_InitialLoad_DisplaysAllSections | ⏳ Pending |
| 35 | FormControlsTests | FormControls_Checkbox_CanBeChecked | ⏳ Pending |
| 36 | FormControlsTests | FormControls_Newsletter_CanBeToggled | ⏳ Pending |
| 37 | FormControlsTests | FormControls_Select_CanSelectCountry | ⏳ Pending |
| 38 | FormControlsTests | FormControls_TextArea_CanEnterText | ⏳ Pending |
| 39 | FormControlsTests | FormControls_RangeInputs_Exist | ⏳ Pending |
| 40 | FormControlsTests | FormControls_Progress_Exists | ⏳ Pending |
| 41 | FormControlsTests | FormControls_Links_AreVisible | ⏳ Pending |
| 42 | ValidationTests | Validation_InitialLoad_DisplaysForm | ⏳ Pending |
| 43 | ValidationTests | Validation_RequiredField_ShowsError_WhenEmpty | ⏳ Pending |
| 44 | ValidationTests | Validation_RequiredField_AcceptsInput | ⏳ Pending |
| 45 | ValidationTests | Validation_EmailField_ValidatesFormat | ⏳ Pending |
| 46 | ValidationTests | Validation_EmailField_AcceptsValidEmail | ⏳ Pending |
| 47 | ValidationTests | Validation_ClearButton_ClearsForm | ⏳ Pending |
| 48 | ValidationTests | Validation_SubmitButton_Exists | ⏳ Pending |
| 49 | DataTableTests | DataTable_InitialLoad_DisplaysTable | ⏳ Pending |
| 50 | DataTableTests | DataTable_Search_FiltersData | ⏳ Pending |
| 51 | DataTableTests | DataTable_ClearFilters_Works | ⏳ Pending |
| 52 | DataTableTests | DataTable_Filters_Exist | ⏳ Pending |
| 53 | DataTableTests | DataTable_Pagination_Exists | ⏳ Pending |
| 54 | DataTableTests | DataTable_BulkActions_Exist | ⏳ Pending |
| 55 | DataTableTests | DataTable_Refresh_Works | ⏳ Pending |
| 56 | UserFormTests | UserForm_InitialLoad_DisplaysForm | ⏳ Pending |
| 57 | UserFormTests | UserForm_FirstName_CanEnterText | ⏳ Pending |
| 58 | UserFormTests | UserForm_FillPersonalInfo_Works | ⏳ Pending |
| 59 | UserFormTests | UserForm_AcceptTerms_Works | ⏳ Pending |
| 60 | UserFormTests | UserForm_Selects_Exist | ⏳ Pending |
| 61 | UserFormTests | UserForm_ActionButtons_Exist | ⏳ Pending |
| 62 | UserFormTests | UserForm_Clear_ClearsForm | ⏳ Pending |
| 63 | UserFormTests | UserForm_IncrementDecrement_Works | ⏳ Pending |
| 64 | MediaGalleryTests | MediaGallery_InitialLoad_DisplaysGallery | ⏳ Pending |
| 65 | MediaGalleryTests | MediaGallery_ImageNavigation_Exists | ⏳ Pending |
| 66 | MediaGalleryTests | MediaGallery_ImageCounter_Exists | ⏳ Pending |
| 67 | MediaGalleryTests | MediaGallery_VideoTitle_Exists | ⏳ Pending |
| 68 | MediaGalleryTests | MediaGallery_VideoControls_Exist | ⏳ Pending |
| 69 | MediaGalleryTests | MediaGallery_AudioTitle_Exists | ⏳ Pending |
| 70 | MediaGalleryTests | MediaGallery_AudioControls_Exist | ⏳ Pending |
| 71 | MediaGalleryTests | MediaGallery_UploadControls_Exist | ⏳ Pending |
| 72 | MediaGalleryTests | MediaGallery_ThumbnailControls_Exist | ⏳ Pending |
| 73 | MediaGalleryTests | MediaGallery_ToggleThumbnails_Works | ⏳ Pending |
| 74 | AdvancedTests | Advanced_InitialLoad_DisplaysPage | ⏳ Pending |
| 75 | AdvancedTests | Advanced_DragDrop_ZonesExist | ⏳ Pending |
| 76 | AdvancedTests | Advanced_DragDrop_ResetButtonExists | ⏳ Pending |
| 77 | AdvancedTests | Advanced_ClipboardControls_Exist | ⏳ Pending |
| 78 | AdvancedTests | Advanced_Canvas_Exists | ⏳ Pending |
| 79 | AdvancedTests | Advanced_CanvasControls_Exist | ⏳ Pending |
| 80 | AdvancedTests | Advanced_AnimationControls_Exist | ⏳ Pending |
| 81 | AdvancedTests | Advanced_StorageControls_Exist | ⏳ Pending |
| 82 | AdvancedTests | Advanced_SaveToStorage_Works | ⏳ Pending |
| 83 | AdvancedTests | Advanced_GeolocationButton_Exists | ⏳ Pending |

---

## Execution Strategy

### Phase 1: MAUI UITests
1. Start Appium server
2. Build and launch MAUI sample app
3. Execute tests one by one by test class:
   - CounterTests (5 tests)
   - TextInputTests (5 tests)
   - ToggleControlTests (5 tests)
   - SliderTests (3 tests)
   - ActivityIndicatorTests (3 tests)

### Phase 2: Blazor UITests
1. Start Blazor sample app (ensure port 5180 is available)
2. Execute tests one by one by test class:
   - CounterTests (6 tests)
   - LoginTests (7 tests)
   - NavigationTests (10 tests)
   - TableTests (10 tests)
   - FormControlsTests (8 tests)
   - ValidationTests (7 tests)
   - DataTableTests (7 tests)
   - UserFormTests (8 tests)
   - MediaGalleryTests (10 tests)
   - AdvancedTests (10 tests)

---

## Issue Documentation

Complex issues will be documented in separate files:
- `ISSUE-XXX-<description>.md` - Individual issue files

---

## Progress Log

| Date | Action | Result |
|------|--------|--------|
| 2026-01-05 | Plan created | 100 tests identified |

---

## Summary Statistics

| Category | Count |
|----------|-------|
| Total MAUI Tests | 21 |
| Total Blazor Tests | 79+ |
| **Total Tests** | **100+** |
| Passed | 0 |
| Failed | 0 |
| Skipped | 0 |
| Fixed | 0 |
