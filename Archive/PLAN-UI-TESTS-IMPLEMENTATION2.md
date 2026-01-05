# PLAN-UI-TESTS-IMPLEMENTATION2

**Corrected UI Test Implementation Plan for SPEC-006 Controls**

**Created:** January 5, 2026  
**Status:** Planning  
**Supersedes:** PLAN-UI-TESTS-IMPLEMENTATION (failed with 122 build errors)

---

## 1. Lessons Learned from Previous Attempt

### 1.1 Critical API Mismatches Identified

| Incorrect API | Correct API | Control |
|--------------|-------------|---------|
| `SetOn(bool)` | `TurnOn()`, `TurnOff()`, `Toggle()` | SwitchControl |
| `NavigateTo()` (page object) | Not available - use FlyoutItem clicks | PageObjectBase |
| `ScrollToEnd()` | `ScrollToBottom()`, `ScrollToTop()` | ScrollViewControlBase |
| `GetScrollPositionY()` | `GetScrollPosition()` returns tuple | ScrollViewControlBase |
| `NavigateToRoute()` | Not in ShellControl | ShellControl |
| `GetCurrentRoute()` | Not in ShellControl | ShellControl |
| `CloseFlyout()` | Not in ShellControl | ShellControl |
| `SelectTabByIndex()` | Not in TabBarControl | TabBarControl |

### 1.2 Correct API Summary

#### ToggleControlBase (SwitchControl, CheckBoxControl, RadioButtonControl)
```csharp
bool IsChecked()
void Toggle(int? timeoutMs = null)
void Check(int? timeoutMs = null)
void Uncheck(int? timeoutMs = null)
void SetChecked(bool? expected, int? timeoutMs = null)
void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
bool WaitChecked(bool? expected, int? timeoutMs = null)
```

#### SliderControl (inherits RangeControlBase)
```csharp
double GetValue(int? timeoutMs = null)
void SetValue(double? value, int? timeoutMs = null)
(double min, double max) GetRange(int? timeoutMs = null)
void SlideToPercent(double? percent, int? timeoutMs = null)
void Increase(int? times = null, int? timeoutMs = null)
void Decrease(int? times = null, int? timeoutMs = null)
void AssertValue(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null)
```

#### PickerControl (inherits SelectorControlBase)
```csharp
string GetSelectedText(int? timeoutMs = null)
void SelectByText(string? text, int? timeoutMs = null)
void SelectByIndex(int? index, int? timeoutMs = null)
int GetItemCount(int? timeoutMs = null)
IReadOnlyList<string> GetItems(int? timeoutMs = null)
void Open(int? timeoutMs = null)
void Close(int? timeoutMs = null)
void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
```

#### DatePickerControl (inherits DateControlBase)
```csharp
DateTime GetDate(int? timeoutMs = null)
void SetDate(DateTime? date, int? timeoutMs = null)
void OpenPicker(int? timeoutMs = null)
void ClosePicker(int? timeoutMs = null)
DateTime GetMinDate(int? timeoutMs = null)
DateTime GetMaxDate(int? timeoutMs = null)
void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null)
void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null)
```

#### TimePickerControl (inherits TimeControlBase)
```csharp
TimeSpan GetTime(int? timeoutMs = null)
void SetTime(TimeSpan? time, int? timeoutMs = null)
void OpenPicker(int? timeoutMs = null)
void ClosePicker(int? timeoutMs = null)
TimeSpan GetMinTime(int? timeoutMs = null)
TimeSpan GetMaxTime(int? timeoutMs = null)
void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null)
```

#### ProgressBarControl (inherits ProgressControlBase)
```csharp
double GetProgress(int? timeoutMs = null)
double GetProgressPercent(int? timeoutMs = null)
(double min, double max) GetMinMax(int? timeoutMs = null)
bool IsComplete(int? timeoutMs = null)
void AssertProgress(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null)
```

#### ActivityIndicatorControl (inherits ActivityIndicatorControlBase)
```csharp
bool IsRunning(int? timeoutMs = null)
void WaitUntilStopped(int? timeoutMs = null)
void WaitUntilStarted(int? timeoutMs = null)
void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null)
```

#### ScrollViewControl (inherits ScrollViewControlBase)
```csharp
(double horizontal, double vertical) GetScrollPosition(int? timeoutMs = null)
bool CanScrollHorizontally(int? timeoutMs = null)
bool CanScrollVertically(int? timeoutMs = null)
void ScrollUp(double? amount = null, int? timeoutMs = null)
void ScrollDown(double? amount = null, int? timeoutMs = null)
void ScrollLeft(double? amount = null, int? timeoutMs = null)
void ScrollRight(double? amount = null, int? timeoutMs = null)
void ScrollToTop(int? timeoutMs = null)
void ScrollToBottom(int? timeoutMs = null)
void ScrollToElement(IControlObject? control, int? timeoutMs = null)
```

#### CollectionViewControl / ListViewControl (inherits ItemsControlBase)
```csharp
int GetItemCount(int? timeoutMs = null)
string GetItemText(int index, int? timeoutMs = null)
void ClickItem(int? index, int? timeoutMs = null)
void ClickItem(string? text, int? timeoutMs = null)
IReadOnlyList<string> GetAllItemTexts(int? timeoutMs = null)
bool HasItem(string text, int? timeoutMs = null)
int GetItemIndex(string text, int? timeoutMs = null)
void SelectItem(int? index, int? timeoutMs = null)  // CollectionView
int GetSelectedItemIndex(int? timeoutMs = null)      // CollectionView
void ScrollToItem(int? index, int? timeoutMs = null) // ListView
void ScrollToTop(int? timeoutMs = null)              // ListView
void ScrollToBottom(int? timeoutMs = null)           // ListView
```

### 1.3 Test Framework Rules

1. **Use xUnit Assert** - NOT FluentAssertions
2. **Use ControlObject Assert methods** - `AssertVisible()`, `AssertExists()`, `AssertChecked()`, etc.
3. **Navigation via Shell** - Click FlyoutItems to navigate, not route methods
4. **Scroll via ScrollView** - Use `MainScrollView.ScrollToElement("AutomationId")` pattern
5. **Wait methods** - Use built-in wait methods before assertions

---

## 2. Implementation Scope

### 2.1 Sample App Pages Available

| Page | AutomationId | Controls Available |
|------|-------------|-------------------|
| Main | `MainPage` | Button, Entry, Editor, Label, Switch, CheckBox, Slider, ProgressBar, Picker, DatePicker, TimePicker, ActivityIndicator, ScrollView |
| Dashboard | `DashboardPage` | Summary labels, buttons, refresh controls |
| UserForm | `UserFormPage` | Form inputs, validation |
| DataGrid | `DataGridPage` | CollectionView, RefreshView, SearchBar, SwipeView, CarouselView |
| MediaGallery | `MediaGalleryPage` | Image, WebView, MediaElement |
| Navigation | `NavigationDemoPage` | Navigation controls |
| Validation | `ValidationPage` | Form validation |
| Advanced | `AdvancedPage` | Gesture areas, Grid, FlexLayout, SwipeView |

### 2.2 Test Priority

| Priority | Controls | Test Count |
|----------|----------|------------|
| **P0-Critical** | Switch, CheckBox, Slider, Picker, ProgressBar, Entry, Button | 20 |
| **P1-High** | DatePicker, TimePicker, ActivityIndicator, ScrollView | 12 |
| **P2-Medium** | CollectionView, ListView, RefreshView, SwipeView | 12 |
| **P3-Lower** | Gesture tests, Container tests | 8 |

**Total: ~52 tests** (focused on working controls with verified APIs)

---

## 3. Implementation Plan

### Phase 1: Project Setup (Completed Previously)
- [x] Create `Brinell.Samples.Maui.UITests.ControlObject6` project
- [x] Add project references
- [x] Create `MauiTestBase6` base class

### Phase 2: Fix Page Object (MainPageObject6)

Create a minimal, working page object using actual MAUI sample app controls:

```csharp
// samples/Brinell.Samples.Maui.UITests.ControlObject6/Pages/MainPageObject6.cs
public class MainPageObject6 : PageObjectBase
{
    public override string Name => "Main";
    protected override ControlLocator PageLocator => ControlLocator.AutomationId("TitleLabel");

    public MainPageObject6(MauiTestContext context) : base(context) { }

    // Labels
    public LabelControl TitleLabel => new(Context, "TitleLabel", this);
    public LabelControl CounterLabel => new(Context, "CounterLabel", this);
    public LabelControl VolumeLabel => new(Context, "VolumeLabel", this);
    public LabelControl GreetingLabel => new(Context, "GreetingLabel", this);
    public LabelControl SelectedColorLabel => new(Context, "SelectedColorLabel", this);

    // Buttons
    public ButtonControl IncrementButton => new(Context, "IncrementButton", this);
    public ButtonControl DecrementButton => new(Context, "DecrementButton", this);
    public ButtonControl ResetButton => new(Context, "ResetButton", this);
    public ButtonControl GreetButton => new(Context, "GreetButton", this);
    public ButtonControl ToggleLoadingButton => new(Context, "ToggleLoadingButton", this);

    // Text Input
    public EntryControl NameEntry => new(Context, "NameEntry", this);
    public EntryControl EmailEntry => new(Context, "EmailEntry", this);
    public EditorControl MessageEditor => new(Context, "MessageEditor", this);

    // Toggle Controls
    public SwitchControl NotificationSwitch => new(Context, "NotificationSwitch", this);
    public CheckBoxControl AgreeCheckBox => new(Context, "AgreeCheckBox", this);

    // Range Controls
    public SliderControl VolumeSlider => new(Context, "VolumeSlider", this);
    public ProgressBarControl VolumeProgress => new(Context, "VolumeProgress", this);

    // Selection Controls
    public PickerControl ColorPicker => new(Context, "ColorPicker", this);
    public DatePickerControl BirthDatePicker => new(Context, "BirthDatePicker", this);
    public TimePickerControl ReminderTimePicker => new(Context, "ReminderTimePicker", this);

    // Activity
    public ActivityIndicatorControl LoadingIndicator => new(Context, "LoadingIndicator", this);

    // Scrolling
    public ScrollViewControl MainScrollView => new(Context, "MainScrollView", this);
}
```

### Phase 3: Test Files Implementation

#### 3.1 ToggleControlTests6.cs (P0)
```csharp
[Fact] public void Switch_InitiallyOn_IsCheckedTrue()
[Fact] public void Switch_Toggle_ChangesState()
[Fact] public void Switch_TurnOff_BecomesUnchecked()
[Fact] public void Switch_TurnOn_BecomesChecked()
[Fact] public void CheckBox_InitiallyUnchecked_IsCheckedFalse()
[Fact] public void CheckBox_Check_BecomesChecked()
[Fact] public void CheckBox_Uncheck_BecomesUnchecked()
[Fact] public void CheckBox_Toggle_ChangesState()
```

#### 3.2 SliderControlTests6.cs (P0)
```csharp
[Fact] public void Slider_GetValue_ReturnsCurrentValue()
[Fact] public void Slider_SetValue_UpdatesSlider()
[Fact] public void Slider_GetRange_ReturnsMinMax()
[Fact] public void Slider_SlideToPercent_SetsCorrectValue()
[Fact] public void Slider_Increase_IncreasesValue()
[Fact] public void Slider_Decrease_DecreasesValue()
```

#### 3.3 PickerControlTests6.cs (P0)
```csharp
[Fact] public void Picker_GetSelectedText_ReturnsCurrentSelection()
[Fact] public void Picker_SelectByText_ChangesSelection()
[Fact] public void Picker_SelectByIndex_ChangesSelection()
[Fact] public void Picker_GetItems_ReturnsList()
[Fact] public void Picker_GetItemCount_ReturnsCorrectCount()
```

#### 3.4 ProgressControlTests6.cs (P0)
```csharp
[Fact] public void ProgressBar_GetProgress_ReturnsValue()
[Fact] public void ProgressBar_GetProgressPercent_ReturnsPercentage()
[Fact] public void ProgressBar_AssertProgress_ValidatesValue()
```

#### 3.5 DateTimeControlTests6.cs (P1)
```csharp
[Fact] public void DatePicker_GetDate_ReturnsCurrentDate()
[Fact] public void DatePicker_SetDate_ChangesDate()
[Fact] public void DatePicker_GetMinMaxDate_ReturnsRange()
[Fact] public void TimePicker_GetTime_ReturnsCurrentTime()
[Fact] public void TimePicker_SetTime_ChangesTime()
```

#### 3.6 ActivityIndicatorTests6.cs (P1)
```csharp
[Fact] public void ActivityIndicator_IsRunning_ReturnsFalseInitially()
[Fact] public void ActivityIndicator_Toggle_ChangesRunningState()
[Fact] public void ActivityIndicator_WaitUntilStopped_WaitsForStop()
```

#### 3.7 ScrollViewTests6.cs (P1)
```csharp
[Fact] public void ScrollView_CanScrollVertically_ReturnsTrue()
[Fact] public void ScrollView_ScrollDown_ChangesPosition()
[Fact] public void ScrollView_ScrollToTop_ReachesTop()
[Fact] public void ScrollView_ScrollToBottom_ReachesBottom()
```

#### 3.8 CollectionViewTests6.cs (P2)
```csharp
[Fact] public void CollectionView_GetItemCount_ReturnsCount()
[Fact] public void CollectionView_GetItemText_ReturnsText()
[Fact] public void CollectionView_ClickItem_SelectsItem()
[Fact] public void CollectionView_SelectItem_ChangesSelection()
[Fact] public void CollectionView_GetSelectedItemIndex_ReturnsIndex()
```

---

## 4. File Structure

```
samples/Brinell.Samples.Maui.UITests.ControlObject6/
├── Brinell.Samples.Maui.UITests.ControlObject6.csproj
├── MauiTestBase6.cs                    ← Uses ControlObject6 MauiTestContext
├── xunit.runner.json
├── Pages/
│   ├── MainPageObject6.cs              ← Main page with all MainPage controls
│   └── DataGridPageObject6.cs          ← For CollectionView tests
└── Tests/
    ├── ToggleControlTests6.cs          ← 8 tests
    ├── SliderControlTests6.cs          ← 6 tests
    ├── PickerControlTests6.cs          ← 5 tests
    ├── ProgressControlTests6.cs        ← 3 tests
    ├── DateTimeControlTests6.cs        ← 5 tests
    ├── ActivityIndicatorTests6.cs      ← 3 tests
    ├── ScrollViewTests6.cs             ← 4 tests
    └── CollectionViewTests6.cs         ← 5 tests (DataGrid page)
```

**Total: 39 tests** (verified APIs only)

---

## 5. Implementation Order

| Step | Task | Tests |
|------|------|-------|
| 1 | Fix MauiTestBase6 - use ControlObject6 namespace | - |
| 2 | Create MainPageObject6 with verified controls | - |
| 3 | ToggleControlTests6 - Switch + CheckBox | 8 |
| 4 | SliderControlTests6 - Slider range operations | 6 |
| 5 | PickerControlTests6 - Picker selection | 5 |
| 6 | ProgressControlTests6 - ProgressBar value | 3 |
| 7 | DateTimeControlTests6 - DatePicker + TimePicker | 5 |
| 8 | ActivityIndicatorTests6 - Running state | 3 |
| 9 | ScrollViewTests6 - Scrolling operations | 4 |
| 10 | Create DataGridPageObject6 | - |
| 11 | CollectionViewTests6 - Collection operations | 5 |
| 12 | Build and verify no errors | - |

---

## 6. Success Criteria

1. **Zero build errors** - All tests compile
2. **All API calls valid** - Using verified control methods only
3. **No FluentAssertions** - Using xUnit Assert and control Assert methods
4. **Page navigation works** - Using FlyoutItem clicks via Shell
5. **39+ tests created** - Covering all verified control types

---

## 7. Notes

### Navigation Pattern
Since `NavigateTo()` is not available on pages, tests that need different pages should:
1. Start on MainPage (default)
2. For DataGrid page, click `FlyoutDataGrid` item

### Assertion Pattern
```csharp
// Preferred: Use control Assert methods
_page.NotificationSwitch.AssertChecked(true);
_page.VolumeSlider.AssertValue(50, tolerance: 1);

// Alternative: Use xUnit Assert
Assert.True(_page.NotificationSwitch.IsChecked());
Assert.Equal(50, _page.VolumeSlider.GetValue(), 1);
```

### Wait Pattern
```csharp
// Wait before checking state
_page.NotificationSwitch.WaitChecked(expected: false, timeoutMs: 2000);
_page.NotificationSwitch.AssertChecked(false);
```

---

**Ready for Implementation**
