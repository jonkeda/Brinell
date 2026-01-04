# SPEC-001c-MAUI-SAMPLE-APP-DESIGN

**Version:** 2.0  
**Status:** Design  
**Date:** January 2026  
**Reference:** SPEC-006

---

## Sample MAUI Application

A comprehensive test sample application demonstrating all major MAUI control types mapped to SPEC-006 interfaces.

---

## Application Structure

### AppShell
```
Shell
├── FlyoutMenu (IFlyoutControlObject)
│   ├── Dashboard (ShellContent)
│   ├── UserForm (ShellContent)
│   ├── DataGrid (ShellContent)
│   ├── MediaGallery (ShellContent)
│   ├── Navigation Demo (ShellContent)
│   ├── Validation (ShellContent)
│   └── Advanced (ShellContent)
├── Tabs (ITabControlObject)
│   ├── Tab 1
│   ├── Tab 2
│   └── Tab 3
└── ToolbarItems (IToolbarControlObject)
```

---

## SPEC-006 Interface to MAUI Control Mapping

| SPEC-006 Interface | MAUI Control(s) | Page |
|--------------------|-----------------|------|
| IControlObject | All controls | All |
| IInteractiveControlObject | Button, Entry, etc. | All |
| IFocusableControlObject | Entry, Editor, SearchBar | UserForm |
| IClickableControlObject | Button, ImageButton | Dashboard, UserForm |
| ITextControlObject | Entry, SearchBar | UserForm |
| IEditableTextControlObject | Editor | UserForm |
| ISearchControlObject | SearchBar | DataGrid |
| IToggleControlObject | Switch, CheckBox | UserForm |
| ICheckBoxControlObject | CheckBox (with indeterminate) | UserForm |
| ISwitchControlObject | Switch | UserForm |
| IRadioButtonControlObject | RadioButton | UserForm |
| ISelectorControlObject | Picker | UserForm |
| IPickerControlObject | Picker (open/close) | UserForm |
| IMultiSelectorControlObject | CollectionView (SelectionMode=Multiple) | DataGrid |
| IRangeControlObject | Slider, Stepper | UserForm |
| ISliderControlObject | Slider | UserForm |
| IStepperControlObject | Stepper | UserForm |
| IDateControlObject | DatePicker | UserForm |
| ITimeControlObject | TimePicker | UserForm |
| IItemsControlObject | CollectionView, ListView | DataGrid |
| ISelectableItemsControlObject | CollectionView (SelectionMode=Single) | DataGrid |
| IMultiSelectableItemsControlObject | CollectionView (SelectionMode=Multiple) | DataGrid |
| IScrollableItemsControlObject | CollectionView (scrollable) | DataGrid |
| IGroupedItemsControlObject | ListView (IsGroupingEnabled) | DataGrid |
| IContainerControlObject | Frame, Border, ContentView | All |
| IScrollableControlObject | ScrollView | All |
| IExpanderControlObject | Expander | Advanced |
| IRefreshableControlObject | RefreshView | DataGrid |
| ISwipeableControlObject | SwipeView | DataGrid |
| ILabelControlObject | Label | All |
| IImageControlObject | Image, ImageButton | MediaGallery |
| IProgressControlObject | ProgressBar | Dashboard |
| IActivityIndicatorControlObject | ActivityIndicator | Dashboard |
| IMediaControlObject | MediaElement | MediaGallery |
| IWebViewControlObject | WebView | MediaGallery |
| ITabControlObject | TabbedPage | Dashboard |
| IMenuControlObject | ContextMenu | Navigation |
| IFlyoutControlObject | Shell Flyout | AppShell |
| IToolbarControlObject | ToolbarItem | Navigation |
| IValidatableControlObject | Entry with validation | Validation |

---

## Page 1: Dashboard Page

**Purpose:** Display key information, status, tabs, and progress indicators

**AutomationId Prefix:** Dashboard

**Controls:**

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| TabbedPage | DashboardTabs | ITabControlObject |
| Tab (Summary) | DashboardTabSummary | - |
| Tab (Actions) | DashboardTabActions | - |
| Tab (Status) | DashboardTabStatus | - |
| Label | DashboardTitleLabel | ILabelControlObject |
| Image | DashboardLogoImage | IImageControlObject |
| Label | DashboardStatusLabel | ILabelControlObject |
| ProgressBar | DashboardLoadProgress | IProgressControlObject |
| ProgressBar | DashboardIndeterminateProgress | IProgressControlObject |
| ActivityIndicator | DashboardLoadingIndicator | IActivityIndicatorControlObject |
| Button | DashboardRefreshButton | IClickableControlObject |
| Button | DashboardExportButton | IClickableControlObject |
| Button | DashboardSettingsButton | IClickableControlObject |
| RefreshView | DashboardRefreshView | IRefreshableControlObject |
| CollectionView | DashboardStatusList | IItemsControlObject |
| Label | DashboardKpi1Label | ILabelControlObject |
| Label | DashboardKpi1Value | ILabelControlObject |
| Label | DashboardKpi2Label | ILabelControlObject |
| Label | DashboardKpi2Value | ILabelControlObject |
| Label | DashboardLastUpdated | ILabelControlObject |

---

## Page 2: User Form Page

**Purpose:** Comprehensive form with all input, toggle, selection, range, and date/time controls

**AutomationId Prefix:** Form

**Controls:**

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| ScrollView | FormScrollView | IScrollableControlObject |
| Frame | FormContainer | IContainerControlObject |
| Label | FormTitleLabel | ILabelControlObject |
| Entry | FormFirstNameEntry | ITextControlObject |
| Entry | FormLastNameEntry | ITextControlObject |
| Entry | FormEmailEntry | ITextControlObject |
| Entry | FormPasswordEntry | ITextControlObject |
| Entry | FormPhoneEntry | ITextControlObject |
| Entry | FormReadOnlyEntry | ITextControlObject |
| Entry | FormDisabledEntry | ITextControlObject |
| SearchBar | FormSearchBar | ISearchControlObject |
| Editor | FormBioEditor | IEditableTextControlObject |
| Picker | FormCountryPicker | IPickerControlObject |
| Picker | FormDepartmentPicker | IPickerControlObject |
| DatePicker | FormBirthDatePicker | IDateControlObject |
| DatePicker | FormStartDatePicker | IDateControlObject |
| TimePicker | FormContactTimePicker | ITimeControlObject |
| TimePicker | FormReminderTimePicker | ITimeControlObject |
| Switch | FormNewsletterSwitch | ISwitchControlObject |
| Switch | FormDisabledSwitch | ISwitchControlObject |
| CheckBox | FormTermsCheckBox | ICheckBoxControlObject |
| CheckBox | FormPrivacyCheckBox | ICheckBoxControlObject |
| CheckBox | FormIndeterminateCheckBox | ICheckBoxControlObject |
| RadioButton | FormTierBasicRadio | IRadioButtonControlObject |
| RadioButton | FormTierPremiumRadio | IRadioButtonControlObject |
| RadioButton | FormTierEnterpriseRadio | IRadioButtonControlObject |
| RadioButton | FormContactEmailRadio | IRadioButtonControlObject |
| RadioButton | FormContactSmsRadio | IRadioButtonControlObject |
| RadioButton | FormContactPushRadio | IRadioButtonControlObject |
| Slider | FormFontSizeSlider | ISliderControlObject |
| Slider | FormVolumeSlider | ISliderControlObject |
| Slider | FormBrightnessSlider | ISliderControlObject |
| Stepper | FormQuantityStepper | IStepperControlObject |
| Stepper | FormCountStepper | IStepperControlObject |
| Label | FormSliderValueLabel | ILabelControlObject |
| Label | FormStepperValueLabel | ILabelControlObject |
| Button | FormSubmitButton | IClickableControlObject |
| Button | FormClearButton | IClickableControlObject |
| Button | FormSaveDraftButton | IClickableControlObject |
| ImageButton | FormImageButton | IClickableControlObject |

---

## Page 3: Data Grid Page

**Purpose:** Collection controls, selection, grouping, scrolling, swiping, refresh

**AutomationId Prefix:** Grid

**Controls:**

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| SearchBar | GridSearchBar | ISearchControlObject |
| Button | GridRefreshButton | IClickableControlObject |
| Button | GridClearFilterButton | IClickableControlObject |
| RefreshView | GridRefreshView | IRefreshableControlObject |
| ListView | GridGroupedList | IGroupedItemsControlObject |
| CollectionView | GridSingleSelectList | ISelectableItemsControlObject |
| CollectionView | GridMultiSelectList | IMultiSelectableItemsControlObject |
| CollectionView | GridScrollableList | IScrollableItemsControlObject |
| CarouselView | GridCarousel | IItemsControlObject |
| SwipeView | GridSwipeItem1 | ISwipeableControlObject |
| SwipeView | GridSwipeItem2 | ISwipeableControlObject |
| SwipeView | GridSwipeItem3 | ISwipeableControlObject |
| Label | GridSelectedItemLabel | ILabelControlObject |
| Label | GridSelectedCountLabel | ILabelControlObject |
| Label | GridItemCountLabel | ILabelControlObject |
| Button | GridSelectAllButton | IClickableControlObject |
| Button | GridUnselectAllButton | IClickableControlObject |
| Button | GridEditButton | IClickableControlObject |
| Button | GridDeleteButton | IClickableControlObject |

---

## Page 4: Media Gallery Page

**Purpose:** Image, media, web view, and graphical controls

**AutomationId Prefix:** Media

**Controls:**

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Label | MediaTitleLabel | ILabelControlObject |
| Image | MediaLocalImage | IImageControlObject |
| Image | MediaRemoteImage | IImageControlObject |
| Image | MediaLoadingImage | IImageControlObject |
| ImageButton | MediaThumbnail1 | IClickableControlObject |
| ImageButton | MediaThumbnail2 | IClickableControlObject |
| ImageButton | MediaThumbnail3 | IClickableControlObject |
| ImageButton | MediaThumbnail4 | IClickableControlObject |
| Image | MediaFullSizeImage | IImageControlObject |
| MediaElement | MediaVideoPlayer | IMediaControlObject |
| Button | MediaPlayButton | IClickableControlObject |
| Button | MediaPauseButton | IClickableControlObject |
| Button | MediaStopButton | IClickableControlObject |
| Slider | MediaSeekSlider | ISliderControlObject |
| Slider | MediaVolumeSlider | ISliderControlObject |
| Switch | MediaMuteSwitch | ISwitchControlObject |
| Label | MediaDurationLabel | ILabelControlObject |
| Label | MediaPositionLabel | ILabelControlObject |
| WebView | MediaWebView | IWebViewControlObject |
| Button | MediaWebBackButton | IClickableControlObject |
| Button | MediaWebForwardButton | IClickableControlObject |
| Button | MediaWebReloadButton | IClickableControlObject |
| Entry | MediaWebUrlEntry | ITextControlObject |
| Label | MediaWebTitleLabel | ILabelControlObject |
| ActivityIndicator | MediaWebLoadingIndicator | IActivityIndicatorControlObject |
| GraphicsView | MediaGraphicsView | IControlObject |

---

## Page 5: Navigation Demo Page

**Purpose:** Navigation, menus, flyouts, toolbars, and expandable controls

**AutomationId Prefix:** Nav

**Controls:**

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Label | NavTitleLabel | ILabelControlObject |
| Button | NavPushPageButton | IClickableControlObject |
| Button | NavPopPageButton | IClickableControlObject |
| Button | NavModalPageButton | IClickableControlObject |
| Button | NavPopToRootButton | IClickableControlObject |
| ToolbarItem | NavToolbarSave | IToolbarControlObject |
| ToolbarItem | NavToolbarEdit | IToolbarControlObject |
| ToolbarItem | NavToolbarDelete | IToolbarControlObject |
| ToolbarItem | NavToolbarMenu | IToolbarControlObject |
| Button | NavOpenFlyoutButton | IClickableControlObject |
| Expander | NavExpander1 | IExpanderControlObject |
| Expander | NavExpander2 | IExpanderControlObject |
| Expander | NavExpander3 | IExpanderControlObject |
| Label | NavExpanderHeader1 | ILabelControlObject |
| Label | NavExpanderContent1 | ILabelControlObject |
| Button | NavExpandAllButton | IClickableControlObject |
| Button | NavCollapseAllButton | IClickableControlObject |

---

## Page 6: Validation Page

**Purpose:** Form validation, error messages, required fields

**AutomationId Prefix:** Validation

**Controls:**

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Label | ValidationTitleLabel | ILabelControlObject |
| Entry | ValidationRequiredEntry | IValidatableControlObject |
| Entry | ValidationEmailEntry | IValidatableControlObject |
| Entry | ValidationPhoneEntry | IValidatableControlObject |
| Entry | ValidationMinLengthEntry | IValidatableControlObject |
| Entry | ValidationMaxLengthEntry | IValidatableControlObject |
| Entry | ValidationRangeEntry | IValidatableControlObject |
| Entry | ValidationRegexEntry | IValidatableControlObject |
| Label | ValidationRequiredError | ILabelControlObject |
| Label | ValidationEmailError | ILabelControlObject |
| Label | ValidationPhoneError | ILabelControlObject |
| Label | ValidationMinLengthError | ILabelControlObject |
| Label | ValidationMaxLengthError | ILabelControlObject |
| Label | ValidationRangeError | ILabelControlObject |
| Label | ValidationRegexError | ILabelControlObject |
| Label | ValidationSummary | ILabelControlObject |
| Button | ValidationSubmitButton | IClickableControlObject |
| Button | ValidationClearButton | IClickableControlObject |
| Label | ValidationSuccessLabel | ILabelControlObject |
| Label | ValidationErrorCountLabel | ILabelControlObject |

---

## Page 7: Advanced Controls Page

**Purpose:** Gestures, borders, advanced interactions

**AutomationId Prefix:** Advanced

**Controls:**

| Control | AutomationId | SPEC-006 Interface |
|---------|--------------|-------------------|
| Label | AdvancedTitleLabel | ILabelControlObject |
| Frame | AdvancedTapFrame | IControlObject |
| Frame | AdvancedPanFrame | IControlObject |
| Frame | AdvancedPinchFrame | IControlObject |
| Frame | AdvancedPointerFrame | IControlObject |
| Frame | AdvancedDropFrame | IControlObject |
| Label | AdvancedTapLabel | ILabelControlObject |
| Label | AdvancedTapCountLabel | ILabelControlObject |
| Label | AdvancedPanLabel | ILabelControlObject |
| Label | AdvancedPinchLabel | ILabelControlObject |
| Image | AdvancedPinchImage | IImageControlObject |
| SwipeView | AdvancedSwipeView | ISwipeableControlObject |
| Border | AdvancedBorder1 | IContainerControlObject |
| Border | AdvancedBorder2 | IContainerControlObject |
| ContentView | AdvancedContentView | IContainerControlObject |

---

## Control Count Summary by SPEC-006 Interface

| SPEC-006 Interface | Count | Controls |
|--------------------|-------|----------|
| ILabelControlObject | 35+ | Label |
| IClickableControlObject | 25+ | Button, ImageButton |
| ITextControlObject | 12+ | Entry |
| IEditableTextControlObject | 2+ | Editor |
| ISearchControlObject | 2+ | SearchBar |
| ISwitchControlObject | 4+ | Switch |
| ICheckBoxControlObject | 3+ | CheckBox |
| IRadioButtonControlObject | 6+ | RadioButton |
| IPickerControlObject | 4+ | Picker |
| ISliderControlObject | 5+ | Slider |
| IStepperControlObject | 2+ | Stepper |
| IDateControlObject | 2+ | DatePicker |
| ITimeControlObject | 2+ | TimePicker |
| IItemsControlObject | 5+ | CollectionView, ListView, CarouselView |
| ISelectableItemsControlObject | 2+ | CollectionView (Single) |
| IMultiSelectableItemsControlObject | 2+ | CollectionView (Multiple) |
| IScrollableItemsControlObject | 2+ | CollectionView |
| IGroupedItemsControlObject | 1+ | ListView (grouped) |
| IScrollableControlObject | 3+ | ScrollView |
| IContainerControlObject | 8+ | Frame, Border, ContentView |
| IExpanderControlObject | 3+ | Expander |
| IRefreshableControlObject | 2+ | RefreshView |
| ISwipeableControlObject | 4+ | SwipeView |
| IImageControlObject | 6+ | Image |
| IProgressControlObject | 2+ | ProgressBar |
| IActivityIndicatorControlObject | 2+ | ActivityIndicator |
| IMediaControlObject | 1+ | MediaElement |
| IWebViewControlObject | 1+ | WebView |
| ITabControlObject | 1+ | TabbedPage |
| IToolbarControlObject | 4+ | ToolbarItem |
| IValidatableControlObject | 7+ | Entry with validation |
| **TOTAL** | **150+** | All SPEC-006 interfaces |

---

**Last Updated:** January 4, 2026
