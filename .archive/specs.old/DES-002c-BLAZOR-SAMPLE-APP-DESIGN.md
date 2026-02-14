# SPEC-001c-BLAZOR-SAMPLE-APP-DESIGN

**Version:** 2.0  
**Status:** Design  
**Date:** January 2026  
**Reference:** SPEC-006

---

## Sample Blazor Application

A comprehensive test sample application demonstrating all major Blazor component types mapped to SPEC-006 interfaces.

---

## Application Structure

### App.razor
```
Router
├── MainLayout (LayoutComponentBase)
│   ├── NavMenu (Sidebar Navigation - IMenuControlObject)
│   ├── TabNavigation (ITabControlObject)
│   └── @Body (Main Content)
├── Pages
│   ├── Dashboard.razor
│   ├── UserForm.razor
│   ├── DataTable.razor
│   ├── MediaGallery.razor
│   ├── FileUpload.razor
│   ├── Validation.razor
│   ├── Navigation.razor
│   └── Advanced.razor
└── Shared
    ├── MainLayout.razor
    ├── NavMenu.razor
    ├── TabNavigation.razor
    └── Components
```

---

## SPEC-006 Interface to Blazor Component Mapping

| SPEC-006 Interface | Blazor Component(s) | Page |
|--------------------|---------------------|------|
| IControlObject | All elements | All |
| IInteractiveControlObject | button, input, a | All |
| IFocusableControlObject | input, textarea, select | UserForm |
| IClickableControlObject | button, a, NavLink | All |
| ITextControlObject | InputText, span, p | UserForm |
| IEditableTextControlObject | InputTextArea | UserForm |
| ISearchControlObject | input[type="search"] | DataTable |
| IToggleControlObject | InputCheckbox | UserForm |
| ICheckBoxControlObject | InputCheckbox (indeterminate) | UserForm |
| ISwitchControlObject | Custom toggle switch | UserForm |
| IRadioButtonControlObject | InputRadio | UserForm |
| ISelectorControlObject | InputSelect | UserForm |
| IPickerControlObject | Custom dropdown | UserForm |
| IMultiSelectorControlObject | select[multiple] | DataTable |
| IRangeControlObject | input[type="range"], Counter | UserForm |
| ISliderControlObject | input[type="range"] | UserForm |
| IStepperControlObject | Counter component | UserForm |
| IDateControlObject | InputDate | UserForm |
| ITimeControlObject | input[type="time"] | UserForm |
| IDateTimeControlObject | input[type="datetime-local"] | UserForm |
| IItemsControlObject | Virtualize, ul, table | DataTable |
| ISelectableItemsControlObject | Table with selection | DataTable |
| IMultiSelectableItemsControlObject | Multi-select list | DataTable |
| IScrollableItemsControlObject | Virtualize | DataTable |
| IGroupedItemsControlObject | Grouped table | DataTable |
| IContainerControlObject | div, section, article | All |
| IScrollableControlObject | overflow: auto div | DataTable |
| IExpanderControlObject | details/summary, Accordion | Navigation |
| IRefreshableControlObject | Refresh button pattern | DataTable |
| ISwipeableControlObject | CSS-based swipe | Advanced |
| ILabelControlObject | label, span, p, h1-h6 | All |
| IImageControlObject | img | MediaGallery |
| IProgressControlObject | progress element | Dashboard |
| IActivityIndicatorControlObject | Spinner component | Dashboard |
| IMediaControlObject | video, audio | MediaGallery |
| IWebViewControlObject | iframe | MediaGallery |
| ITabControlObject | Tab component | Dashboard |
| IMenuControlObject | NavMenu, nav | Navigation |
| IFlyoutControlObject | Dropdown/Popover | Navigation |
| IToolbarControlObject | Toolbar component | Navigation |
| IValidatableControlObject | ValidationMessage | Validation |

---

## Shared Component: MainLayout

**Purpose:** Main application layout with navigation

**Components:**
- Router (App routing)
- CascadingValue<T> (Theme context)
- HeadOutlet (Document head)
- NavMenu (Navigation sidebar - IMenuControlObject)
  - NavLink (Dashboard)
  - NavLink (User Form)
  - NavLink (Data Table)
  - NavLink (Media Gallery)
  - NavLink (File Upload)
  - NavLink (Validation)
  - NavLink (Navigation)
  - NavLink (Advanced)
- PageTitle (Dynamic page title)
- @Body (Page content)

---

## Shared Component: NavMenu

**Purpose:** Navigation menu - IMenuControlObject

**AutomationId Prefix:** Nav

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| nav | NavMenuContainer | IMenuControlObject |
| NavLink (Dashboard) | NavDashboard | IClickableControlObject |
| NavLink (User Form) | NavUserForm | IClickableControlObject |
| NavLink (Data Table) | NavDataTable | IClickableControlObject |
| NavLink (Media) | NavMediaGallery | IClickableControlObject |
| NavLink (File Upload) | NavFileUpload | IClickableControlObject |
| NavLink (Validation) | NavValidation | IClickableControlObject |
| NavLink (Navigation) | NavNavigationDemo | IClickableControlObject |
| NavLink (Advanced) | NavAdvanced | IClickableControlObject |

---

## Page 1: Dashboard.razor

**Purpose:** Display key information, tabs, and progress indicators

**AutomationId Prefix:** Dashboard

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| PageTitle | - | - |
| h1 | DashboardTitle | ILabelControlObject |
| TabContainer | DashboardTabs | ITabControlObject |
| Tab (Summary) | DashboardTabSummary | - |
| Tab (Actions) | DashboardTabActions | - |
| Tab (Status) | DashboardTabStatus | - |
| p (KPI 1 Label) | DashboardKpi1Label | ILabelControlObject |
| span (KPI 1 Value) | DashboardKpi1Value | ILabelControlObject |
| p (KPI 2 Label) | DashboardKpi2Label | ILabelControlObject |
| span (KPI 2 Value) | DashboardKpi2Value | ILabelControlObject |
| p (KPI 3 Label) | DashboardKpi3Label | ILabelControlObject |
| span (KPI 3 Value) | DashboardKpi3Value | ILabelControlObject |
| progress (determinate) | DashboardProgressBar | IProgressControlObject |
| progress (indeterminate) | DashboardIndeterminateProgress | IProgressControlObject |
| Spinner component | DashboardSpinner | IActivityIndicatorControlObject |
| p (Last Updated) | DashboardLastUpdated | ILabelControlObject |
| button (Refresh) | DashboardRefreshButton | IClickableControlObject |
| button (Export) | DashboardExportButton | IClickableControlObject |
| button (Settings) | DashboardSettingsButton | IClickableControlObject |
| NavLink (Go to Form) | DashboardLinkForm | IClickableControlObject |
| NavLink (View Data) | DashboardLinkData | IClickableControlObject |
| img (Logo) | DashboardLogo | IImageControlObject |

---

## Page 2: UserForm.razor

**Purpose:** Comprehensive form with all input, toggle, selection, range, and date/time controls

**AutomationId Prefix:** Form

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| PageTitle | - | - |
| h1 | FormTitle | ILabelControlObject |
| EditForm | FormContainer | IContainerControlObject |
| label (First Name) | FormFirstNameLabel | ILabelControlObject |
| InputText (First Name) | FormFirstNameInput | ITextControlObject |
| label (Last Name) | FormLastNameLabel | ILabelControlObject |
| InputText (Last Name) | FormLastNameInput | ITextControlObject |
| label (Email) | FormEmailLabel | ILabelControlObject |
| InputText (Email) | FormEmailInput | ITextControlObject |
| label (Phone) | FormPhoneLabel | ILabelControlObject |
| InputText (Phone) | FormPhoneInput | ITextControlObject |
| label (Password) | FormPasswordLabel | ILabelControlObject |
| InputText (Password) | FormPasswordInput | ITextControlObject |
| InputText (Read-only) | FormReadOnlyInput | ITextControlObject |
| InputText (Disabled) | FormDisabledInput | ITextControlObject |
| input[type="search"] | FormSearchInput | ISearchControlObject |
| label (Bio) | FormBioLabel | ILabelControlObject |
| InputTextArea | FormBioTextarea | IEditableTextControlObject |
| label (Country) | FormCountryLabel | ILabelControlObject |
| InputSelect (Country) | FormCountrySelect | ISelectorControlObject |
| label (Department) | FormDepartmentLabel | ILabelControlObject |
| InputSelect (Department) | FormDepartmentSelect | ISelectorControlObject |
| Custom Dropdown | FormCustomPicker | IPickerControlObject |
| label (Birth Date) | FormBirthDateLabel | ILabelControlObject |
| InputDate (Birth Date) | FormBirthDateInput | IDateControlObject |
| label (Start Date) | FormStartDateLabel | ILabelControlObject |
| InputDate (Start Date) | FormStartDateInput | IDateControlObject |
| label (Contact Time) | FormContactTimeLabel | ILabelControlObject |
| input[type="time"] | FormContactTimeInput | ITimeControlObject |
| label (Reminder Time) | FormReminderTimeLabel | ILabelControlObject |
| input[type="time"] | FormReminderTimeInput | ITimeControlObject |
| label (Meeting DateTime) | FormMeetingLabel | ILabelControlObject |
| input[type="datetime-local"] | FormMeetingDateTimeInput | IDateTimeControlObject |
| label (Age) | FormAgeLabel | ILabelControlObject |
| InputNumber (Age) | FormAgeInput | IRangeControlObject |
| label (Salary) | FormSalaryLabel | ILabelControlObject |
| InputNumber (Salary) | FormSalaryInput | IRangeControlObject |
| label (Newsletter) | FormNewsletterLabel | ILabelControlObject |
| InputCheckbox (Newsletter) | FormNewsletterCheckbox | IToggleControlObject |
| label (Terms) | FormTermsLabel | ILabelControlObject |
| InputCheckbox (Terms) | FormTermsCheckbox | ICheckBoxControlObject |
| label (Privacy) | FormPrivacyLabel | ILabelControlObject |
| InputCheckbox (Privacy) | FormPrivacyCheckbox | ICheckBoxControlObject |
| InputCheckbox (Indeterminate) | FormIndeterminateCheckbox | ICheckBoxControlObject |
| Custom Toggle Switch | FormToggleSwitch | ISwitchControlObject |
| InputRadioGroup (Tier) | FormTierRadioGroup | IRadioButtonControlObject |
| InputRadio (Basic) | FormTierBasicRadio | IRadioButtonControlObject |
| InputRadio (Premium) | FormTierPremiumRadio | IRadioButtonControlObject |
| InputRadio (Enterprise) | FormTierEnterpriseRadio | IRadioButtonControlObject |
| InputRadioGroup (Contact) | FormContactRadioGroup | IRadioButtonControlObject |
| InputRadio (Email) | FormContactEmailRadio | IRadioButtonControlObject |
| InputRadio (SMS) | FormContactSmsRadio | IRadioButtonControlObject |
| InputRadio (Push) | FormContactPushRadio | IRadioButtonControlObject |
| label (Font Size) | FormFontSizeLabel | ILabelControlObject |
| input[type="range"] | FormFontSizeSlider | ISliderControlObject |
| span (Slider Value) | FormFontSizeValue | ILabelControlObject |
| label (Volume) | FormVolumeLabel | ILabelControlObject |
| input[type="range"] | FormVolumeSlider | ISliderControlObject |
| label (Quantity) | FormQuantityLabel | ILabelControlObject |
| Counter component | FormQuantityStepper | IStepperControlObject |
| span (Stepper Value) | FormQuantityValue | ILabelControlObject |
| button (Submit) | FormSubmitButton | IClickableControlObject |
| button (Clear) | FormClearButton | IClickableControlObject |
| button (Cancel) | FormCancelButton | IClickableControlObject |
| button (Save Draft) | FormSaveDraftButton | IClickableControlObject |

---

## Page 3: DataTable.razor

**Purpose:** Collection controls, selection, grouping, scrolling, refresh

**AutomationId Prefix:** Data

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| PageTitle | - | - |
| h1 | DataTitle | ILabelControlObject |
| input[type="search"] | DataSearchInput | ISearchControlObject |
| button (Filter) | DataFilterButton | IClickableControlObject |
| button (Clear Filter) | DataClearFilterButton | IClickableControlObject |
| button (Refresh) | DataRefreshButton | IRefreshableControlObject |
| InputSelect (Sort By) | DataSortSelect | ISelectorControlObject |
| button (Ascending) | DataSortAscButton | IClickableControlObject |
| button (Descending) | DataSortDescButton | IClickableControlObject |
| select[multiple] | DataMultiSelect | IMultiSelectorControlObject |
| div (scrollable) | DataScrollContainer | IScrollableControlObject |
| Virtualize | DataVirtualList | IScrollableItemsControlObject |
| table | DataTable | IItemsControlObject |
| table (selectable) | DataSelectableTable | ISelectableItemsControlObject |
| table (multi-select) | DataMultiSelectTable | IMultiSelectableItemsControlObject |
| table (grouped) | DataGroupedTable | IGroupedItemsControlObject |
| tr (Row 1) | DataRow1 | IControlObject |
| tr (Row 2) | DataRow2 | IControlObject |
| tr (Row 3) | DataRow3 | IControlObject |
| button (Edit) | DataEditButton | IClickableControlObject |
| button (Delete) | DataDeleteButton | IClickableControlObject |
| button (View Details) | DataViewButton | IClickableControlObject |
| button (Select All) | DataSelectAllButton | IClickableControlObject |
| button (Unselect All) | DataUnselectAllButton | IClickableControlObject |
| button (Previous) | DataPrevButton | IClickableControlObject |
| span (Page Info) | DataPageInfo | ILabelControlObject |
| button (Next) | DataNextButton | IClickableControlObject |
| p (Total Records) | DataTotalCount | ILabelControlObject |
| p (Selected Count) | DataSelectedCount | ILabelControlObject |

---

## Page 4: MediaGallery.razor

**Purpose:** Image, media, web view, and graphical controls

**AutomationId Prefix:** Media

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| PageTitle | - | - |
| h1 | MediaTitle | ILabelControlObject |
| img (Local Image) | MediaLocalImage | IImageControlObject |
| img (Remote Image) | MediaRemoteImage | IImageControlObject |
| img (Loading Image) | MediaLoadingImage | IImageControlObject |
| button (Thumbnail 1) | MediaThumbnail1 | IClickableControlObject |
| button (Thumbnail 2) | MediaThumbnail2 | IClickableControlObject |
| button (Thumbnail 3) | MediaThumbnail3 | IClickableControlObject |
| button (Thumbnail 4) | MediaThumbnail4 | IClickableControlObject |
| img (Full Size) | MediaFullSizeImage | IImageControlObject |
| video | MediaVideoPlayer | IMediaControlObject |
| audio | MediaAudioPlayer | IMediaControlObject |
| button (Play) | MediaPlayButton | IClickableControlObject |
| button (Pause) | MediaPauseButton | IClickableControlObject |
| button (Stop) | MediaStopButton | IClickableControlObject |
| input[type="range"] (Seek) | MediaSeekSlider | ISliderControlObject |
| input[type="range"] (Volume) | MediaVolumeSlider | ISliderControlObject |
| InputCheckbox (Mute) | MediaMuteCheckbox | IToggleControlObject |
| span (Duration) | MediaDuration | ILabelControlObject |
| span (Position) | MediaPosition | ILabelControlObject |
| iframe | MediaIframe | IWebViewControlObject |
| button (Back) | MediaWebBackButton | IClickableControlObject |
| button (Forward) | MediaWebForwardButton | IClickableControlObject |
| button (Reload) | MediaWebReloadButton | IClickableControlObject |
| InputText (URL) | MediaUrlInput | ITextControlObject |
| span (Page Title) | MediaWebTitle | ILabelControlObject |
| Spinner | MediaWebSpinner | IActivityIndicatorControlObject |

---

## Page 5: FileUpload.razor

**Purpose:** File input and upload controls

**AutomationId Prefix:** Upload

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| PageTitle | - | - |
| h1 | UploadTitle | ILabelControlObject |
| InputFile | UploadFileInput | IControlObject |
| InputTextArea (Description) | UploadDescriptionInput | IEditableTextControlObject |
| InputSelect (Category) | UploadCategorySelect | ISelectorControlObject |
| button (Upload) | UploadSubmitButton | IClickableControlObject |
| button (Clear) | UploadClearButton | IClickableControlObject |
| progress | UploadProgressBar | IProgressControlObject |
| span (Progress %) | UploadProgressValue | ILabelControlObject |
| p (Success Message) | UploadSuccessMessage | ILabelControlObject |
| p (File Name) | UploadFileName | ILabelControlObject |
| p (File Size) | UploadFileSize | ILabelControlObject |
| table | UploadFileList | IItemsControlObject |
| button (Download) | UploadDownloadButton | IClickableControlObject |
| button (Delete) | UploadDeleteButton | IClickableControlObject |
| button (Preview) | UploadPreviewButton | IClickableControlObject |

---

## Page 6: Validation.razor

**Purpose:** Form validation, error messages, required fields - IValidatableControlObject

**AutomationId Prefix:** Validate

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| PageTitle | - | - |
| h1 | ValidateTitle | ILabelControlObject |
| EditForm | ValidateForm | IContainerControlObject |
| DataAnnotationsValidator | - | - |
| ValidationSummary | ValidateSummary | ILabelControlObject |
| label (Required) | ValidateRequiredLabel | ILabelControlObject |
| InputText (Required) | ValidateRequiredInput | IValidatableControlObject |
| ValidationMessage | ValidateRequiredError | ILabelControlObject |
| label (Email) | ValidateEmailLabel | ILabelControlObject |
| InputText (Email) | ValidateEmailInput | IValidatableControlObject |
| ValidationMessage | ValidateEmailError | ILabelControlObject |
| label (Phone) | ValidatePhoneLabel | ILabelControlObject |
| InputText (Phone) | ValidatePhoneInput | IValidatableControlObject |
| ValidationMessage | ValidatePhoneError | ILabelControlObject |
| label (Min Length) | ValidateMinLengthLabel | ILabelControlObject |
| InputText (Min Length) | ValidateMinLengthInput | IValidatableControlObject |
| ValidationMessage | ValidateMinLengthError | ILabelControlObject |
| label (Max Length) | ValidateMaxLengthLabel | ILabelControlObject |
| InputText (Max Length) | ValidateMaxLengthInput | IValidatableControlObject |
| ValidationMessage | ValidateMaxLengthError | ILabelControlObject |
| label (Range) | ValidateRangeLabel | ILabelControlObject |
| InputNumber (Range) | ValidateRangeInput | IValidatableControlObject |
| ValidationMessage | ValidateRangeError | ILabelControlObject |
| label (Regex) | ValidateRegexLabel | ILabelControlObject |
| InputText (Regex) | ValidateRegexInput | IValidatableControlObject |
| ValidationMessage | ValidateRegexError | ILabelControlObject |
| label (Compare) | ValidateCompareLabel | ILabelControlObject |
| InputText (Compare) | ValidateCompareInput | IValidatableControlObject |
| ValidationMessage | ValidateCompareError | ILabelControlObject |
| button (Submit) | ValidateSubmitButton | IClickableControlObject |
| button (Clear) | ValidateClearButton | IClickableControlObject |
| p (Success) | ValidateSuccessMessage | ILabelControlObject |
| p (Error Count) | ValidateErrorCount | ILabelControlObject |

---

## Page 7: Navigation.razor

**Purpose:** Navigation, menus, flyouts, toolbars, and expandable controls

**AutomationId Prefix:** Nav

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| PageTitle | - | - |
| h1 | NavDemoTitle | ILabelControlObject |
| nav (Menu) | NavMenuDemo | IMenuControlObject |
| button (Menu Item 1) | NavMenuItem1 | IClickableControlObject |
| button (Menu Item 2) | NavMenuItem2 | IClickableControlObject |
| button (Menu Item 3) | NavMenuItem3 | IClickableControlObject |
| Toolbar | NavToolbar | IToolbarControlObject |
| button (Save) | NavToolbarSave | IClickableControlObject |
| button (Edit) | NavToolbarEdit | IClickableControlObject |
| button (Delete) | NavToolbarDelete | IClickableControlObject |
| button (Menu) | NavToolbarMenu | IClickableControlObject |
| Dropdown/Popover | NavFlyout | IFlyoutControlObject |
| button (Open Flyout) | NavOpenFlyoutButton | IClickableControlObject |
| details (Expander 1) | NavExpander1 | IExpanderControlObject |
| summary (Header 1) | NavExpanderHeader1 | ILabelControlObject |
| div (Content 1) | NavExpanderContent1 | IContainerControlObject |
| details (Expander 2) | NavExpander2 | IExpanderControlObject |
| summary (Header 2) | NavExpanderHeader2 | ILabelControlObject |
| div (Content 2) | NavExpanderContent2 | IContainerControlObject |
| details (Expander 3) | NavExpander3 | IExpanderControlObject |
| summary (Header 3) | NavExpanderHeader3 | ILabelControlObject |
| div (Content 3) | NavExpanderContent3 | IContainerControlObject |
| Accordion | NavAccordion | IExpanderControlObject |
| button (Expand All) | NavExpandAllButton | IClickableControlObject |
| button (Collapse All) | NavCollapseAllButton | IClickableControlObject |
| TabContainer | NavTabs | ITabControlObject |
| Tab (Tab 1) | NavTab1 | IClickableControlObject |
| Tab (Tab 2) | NavTab2 | IClickableControlObject |
| Tab (Tab 3) | NavTab3 | IClickableControlObject |

---

## Page 8: Advanced.razor

**Purpose:** Advanced features, gestures, error handling

**AutomationId Prefix:** Advanced

**Components:**

| Component | AutomationId | SPEC-006 Interface |
|-----------|--------------|-------------------|
| PageTitle | - | - |
| h1 | AdvancedTitle | ILabelControlObject |
| ErrorBoundary | AdvancedErrorBoundary | IControlObject |
| button (Trigger Error) | AdvancedTriggerErrorButton | IClickableControlObject |
| p (Error Message) | AdvancedErrorMessage | ILabelControlObject |
| FocusOnNavigate | AdvancedFocusSection | IFocusableControlObject |
| EditForm | AdvancedForm | IContainerControlObject |
| InputText | AdvancedFormInput | ITextControlObject |
| button (Submit) | AdvancedFormSubmitButton | IClickableControlObject |
| p (Submit Message) | AdvancedSubmitMessage | ILabelControlObject |
| Virtualize | AdvancedVirtualList | IScrollableItemsControlObject |
| DynamicComponent | AdvancedDynamicComponent | IControlObject |
| InputSelect (Component) | AdvancedComponentSelect | ISelectorControlObject |
| button (Load Component) | AdvancedLoadButton | IClickableControlObject |
| CascadingValue | AdvancedCascading | IControlObject |
| div (Swipeable) | AdvancedSwipeContainer | ISwipeableControlObject |
| button (Swipe Left) | AdvancedSwipeLeftAction | IClickableControlObject |
| button (Swipe Right) | AdvancedSwipeRightAction | IClickableControlObject |

---

## Component Count Summary by SPEC-006 Interface

| SPEC-006 Interface | Count | Components |
|--------------------|-------|----------|
| ILabelControlObject | 45+ | h1-h6, p, span, label |
| IClickableControlObject | 40+ | button, a, NavLink |
| ITextControlObject | 12+ | InputText |
| IEditableTextControlObject | 3+ | InputTextArea |
| ISearchControlObject | 2+ | input[type="search"] |
| IToggleControlObject | 3+ | InputCheckbox |
| ICheckBoxControlObject | 3+ | InputCheckbox (indeterminate) |
| ISwitchControlObject | 2+ | Custom toggle |
| IRadioButtonControlObject | 6+ | InputRadio |
| ISelectorControlObject | 6+ | InputSelect |
| IPickerControlObject | 2+ | Custom dropdown |
| IMultiSelectorControlObject | 2+ | select[multiple] |
| ISliderControlObject | 4+ | input[type="range"] |
| IStepperControlObject | 2+ | Counter component |
| IDateControlObject | 2+ | InputDate |
| ITimeControlObject | 2+ | input[type="time"] |
| IDateTimeControlObject | 1+ | input[type="datetime-local"] |
| IItemsControlObject | 5+ | table, ul, Virtualize |
| ISelectableItemsControlObject | 2+ | Table with selection |
| IMultiSelectableItemsControlObject | 2+ | Multi-select table |
| IScrollableItemsControlObject | 3+ | Virtualize |
| IGroupedItemsControlObject | 1+ | Grouped table |
| IScrollableControlObject | 3+ | overflow:auto div |
| IContainerControlObject | 10+ | div, section, article |
| IExpanderControlObject | 4+ | details/summary, Accordion |
| IRefreshableControlObject | 2+ | Refresh button pattern |
| ISwipeableControlObject | 2+ | CSS-based swipe |
| IImageControlObject | 6+ | img |
| IProgressControlObject | 3+ | progress |
| IActivityIndicatorControlObject | 3+ | Spinner |
| IMediaControlObject | 2+ | video, audio |
| IWebViewControlObject | 1+ | iframe |
| ITabControlObject | 2+ | Tab component |
| IMenuControlObject | 2+ | nav, NavMenu |
| IFlyoutControlObject | 1+ | Dropdown/Popover |
| IToolbarControlObject | 1+ | Toolbar |
| IValidatableControlObject | 8+ | ValidationMessage |
| **TOTAL** | **200+** | All SPEC-006 interfaces |

---

## Test Scenarios

### Form Input Testing
- Text input (InputText)
- Multi-line text (InputTextArea)
- Search input (input[type="search"])
- Number input (InputNumber)
- Date input (InputDate)
- Time input (input[type="time"])
- DateTime input (input[type="datetime-local"])
- File input (InputFile)
- Read-only and disabled states
- Focus management

### Selection Testing
- Single select (InputSelect)
- Multiple select (select[multiple])
- Radio button selection (InputRadio)
- Radio group selection (InputRadioGroup)
- Custom picker/dropdown
- Clear selection

### Toggle Testing
- Checkbox On/Off
- Checkbox indeterminate state
- Custom toggle switch
- Radio button selection

### Range Testing
- Slider value adjustment
- Stepper increment/decrement
- Progress bar value display
- Counter component

### Collection Testing
- Table rendering
- Virtualized list scrolling
- Single item selection
- Multi-item selection
- Grouped items
- Pagination

### Navigation Testing
- NavLink navigation
- Tab navigation
- Menu interaction
- Toolbar actions
- Flyout/popover open/close
- Expander expand/collapse
- Accordion behavior

### Validation Testing
- Required field validation
- Email format validation
- Phone format validation
- Min/max length validation
- Range validation
- Regex pattern validation
- Compare validation
- ValidationSummary display
- ValidationMessage display
- Error count

### Media Testing
- Image loading
- Video playback
- Audio playback
- Media controls (play/pause/stop)
- Seek/volume sliders
- iframe content

### Advanced Testing
- Error boundary
- Dynamic components
- Virtualization
- Cascading values
- Swipe gestures

---

**Last Updated:** January 4, 2026
