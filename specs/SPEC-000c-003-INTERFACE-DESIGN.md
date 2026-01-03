# SPEC-000c-003: Comprehensive Control Interface Design

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Core Control Interfaces

### IVisualElement
```csharp
interface IVisualElement {
    +IsVisible() bool
    +WaitVisible(bool, int) bool
    +GetBounds() Rectangle
    +GetOpacity() double
    +SetOpacity(double) void
}
```

### ILocatable
```csharp
interface ILocatable {
    +GetX() double
    +GetY() double
    +GetWidth() double
    +GetHeight() double
    +GetRotation() double
}
```

### IStylable
```csharp
interface IStylable {
    +GetBackgroundColor() Color
    +SetBackgroundColor(Color) void
    +GetTextColor() Color
    +SetTextColor(Color) void
    +GetFontSize() double
    +SetFontSize(double) void
    +GetFontFamily() string
    +SetFontFamily(string) void
}
```

### IInteractive
```csharp
interface IInteractive {
    +IsEnabled() bool
    +WaitEnabled(bool, int) bool
    +CheckEnabled(bool, int) void
    +AssertEnabled(string) void
}
```

### IClickable
```csharp
interface IClickable {
    +Click() void
    +DoubleClick() void
    +RightClick() void
    +LongPress(int) void
}
```

### IGesturable
```csharp
interface IGesturable {
    +Tap() void
    +Pan(int, int) void
    +Pinch(double) void
    +Swipe(Direction) void
    +Hover() void
}
```

---

## Text Input Interfaces

### ITextInputControl
```csharp
interface ITextInputControl {
    +Enter(string) void
    +Clear() void
    +SetText(string) void
    +AppendText(string) void
    +SelectAll() void
    +SelectText(int, int) void
    +ClearSelection() void
    +GetText() string
    +GetSelectedText() string
    +GetTextLength() int
    +IsReadOnly() bool
    +GetCursorPosition() int
    +SetCursorPosition(int) void
}
```

### ITextSearchControl
```csharp
interface ITextSearchControl {
    +SearchForText(string) bool
    +ClearSearch() void
    +GetSearchResults() List~string~
}
```

### IEditableTextControl
```csharp
interface IEditableTextControl {
    +Focus() void
    +Blur() void
    +Copy() void
    +Cut() void
    +Paste() void
    +Undo() void
    +Redo() void
    +IsFocused() bool
}
```

### IValidatableTextControl
```csharp
interface IValidatableTextControl {
    +IsValid() bool
    +GetValidationError() string
    +ValidateRequired(bool) void
    +ValidatePattern(string) void
    +ValidateMinLength(int) void
    +ValidateMaxLength(int) void
}
```

---

## Selection Interfaces

### ISingleSelectControl
```csharp
interface ISingleSelectControl {
    +SelectByIndex(int) void
    +SelectByText(string) void
    +SelectByValue(string) void
    +GetSelectedIndex() int
    +GetSelectedText() string
    +GetSelectedValue() string
    +ClearSelection() void
}
```

### IMultiSelectControl
```csharp
interface IMultiSelectControl {
    +SelectByIndex(int) void
    +SelectByText(string) void
    +SelectByValue(string) void
    +UnselectByIndex(int) void
    +UnselectByText(string) void
    +UnselectByValue(string) void
    +GetSelectedIndices() List~int~
    +GetSelectedTexts() List~string~
    +GetSelectedValues() List~string~
    +GetSelectedCount() int
    +ClearSelection() void
}
```

### ISelectableControl
```csharp
interface ISelectableControl {
    +GetItems() List~string~
    +GetItemCount() int
    +HasItem(string) bool
    +GetItemAtIndex(int) string
    +IsSelected(string) bool
}
```

---

## Toggle Interfaces

### IToggleControl
```csharp
interface IToggleControl {
    +IsChecked() bool
    +WaitChecked(bool, int) bool
    +CheckChecked(bool, int) void
    +AssertChecked(string) void
    +AssertUnchecked(string) void
    +Toggle() void
    +Check() void
    +Uncheck() void
    +SetChecked(bool) void
}
```

### IRadioControl
```csharp
interface IRadioControl {
    +SelectRadio(string) void
    +GetSelectedRadio() string
    +GetRadioOptions() List~string~
    +IsRadioSelected(string) bool
}
```

### ISwitchControl
```csharp
interface ISwitchControl {
    +IsOn() bool
    +TurnOn() void
    +TurnOff() void
    +Toggle() void
    +SetOn(bool) void
    +AssertOn(string) void
    +AssertOff(string) void
}
```

---

## Range Interfaces

### IRangeInputControl
```csharp
interface IRangeInputControl {
    +GetValue() double
    +SetValue(double) void
    +GetMinimum() double
    +SetMinimum(double) void
    +GetMaximum() double
    +SetMaximum(double) void
    +Increment(double) void
    +Decrement(double) void
    +AssertValue(double, double, string) void
}
```

### ISliderControl
```csharp
interface ISliderControl {
    +GetCurrentValue() double
    +SetValue(double) void
    +DragToValue(double) void
    +GetMinValue() double
    +GetMaxValue() double
    +GetStepValue() double
}
```

### IProgressControl
```csharp
interface IProgressControl {
    +GetProgress() double
    +IsIndeterminate() bool
    +GetProgressText() string
}
```

---

## Date/Time Interfaces

### IDateInputControl
```csharp
interface IDateInputControl {
    +SelectDate(DateTime) void
    +GetSelectedDate() DateTime
    +GetMinDate() DateTime
    +SetMinDate(DateTime) void
    +GetMaxDate() DateTime
    +SetMaxDate(DateTime) void
    +OpenDatePicker() void
    +CloseDatePicker() void
}
```

### ITimeInputControl
```csharp
interface ITimeInputControl {
    +SelectTime(TimeSpan) void
    +GetSelectedTime() TimeSpan
    +GetMinTime() TimeSpan
    +SetMinTime(TimeSpan) void
    +GetMaxTime() TimeSpan
    +SetMaxTime(TimeSpan) void
    +OpenTimePicker() void
    +CloseTimePicker() void
}
```

### IDateRangeInputControl
```csharp
interface IDateRangeInputControl {
    +SelectDateRange(DateTime, DateTime) void
    +GetStartDate() DateTime
    +GetEndDate() DateTime
    +SetStartDate(DateTime) void
    +SetEndDate(DateTime) void
}
```

---

## Collection Interfaces

### ICollectionControl
```csharp
interface ICollectionControl {
    +GetItemCount() int
    +GetItemText(int) string
    +GetItemValue(int) string
    +GetAllItems() List~string~
    +HasItem(string) bool
    +GetItemIndex(string) int
}
```

### IClickableCollectionControl
```csharp
interface IClickableCollectionControl {
    +ClickItem(int) void
    +ClickItem(string) void
    +ClickItemByValue(string) void
    +DoubleClickItem(int) void
    +RightClickItem(int) void
}
```

### IScrollableCollectionControl
```csharp
interface IScrollableCollectionControl {
    +ScrollToItem(int) void
    +ScrollToItem(string) void
    +ScrollToTop() void
    +ScrollToBottom() void
    +ScrollUp(int) void
    +ScrollDown(int) void
}
```

### ILoadableCollectionControl
```csharp
interface ILoadableCollectionControl {
    +LoadMore() void
    +IsLoading() bool
    +WaitForLoad(int) void
    +GetLoadedItemCount() int
}
```

### IGroupedCollectionControl
```csharp
interface IGroupedCollectionControl {
    +GetGroups() List~string~
    +GetGroupItemCount(string) int
    +ExpandGroup(string) void
    +CollapseGroup(string) void
    +IsGroupExpanded(string) bool
}
```

### IFilterableCollectionControl
```csharp
interface IFilterableCollectionControl {
    +ApplyFilter(string) void
    +ClearFilter() void
    +GetFilteredItemCount() int
    +IsFiltered() bool
}
```

---

## Container Interfaces

### IContainerControl
```csharp
interface IContainerControl {
    +GetChildCount() int
    +GetChildNames() List~string~
    +ChildExists(string) bool
    +GetChild~T~(string) T
    +GetAllChildren() List~IVisualElement~
}
```

### IScrollableControl
```csharp
interface IScrollableControl {
    +ScrollToElement(string) void
    +ScrollToTop() void
    +ScrollToBottom() void
    +ScrollLeft() void
    +ScrollRight() void
    +ScrollUp(int) void
    +ScrollDown(int) void
    +ScrollLeft(int) void
    +ScrollRight(int) void
    +GetScrollPosition() Point
    +SetScrollPosition(Point) void
}
```

### IExpandableControl
```csharp
interface IExpandableControl {
    +IsExpanded() bool
    +Expand() void
    +Collapse() void
    +Toggle() void
    +GetExpandedHeight() double
    +GetCollapsedHeight() double
}
```

### IRefreshableControl
```csharp
interface IRefreshableControl {
    +IsRefreshing() bool
    +Refresh() void
    +WaitForRefresh(int) void
    +CancelRefresh() void
}
```

### ISwipeableControl
```csharp
interface ISwipeableControl {
    +SwipeLeft() void
    +SwipeRight() void
    +SwipeUp() void
    +SwipeDown() void
    +IsSwipeEnabled() bool
}
```

---

## Display Interfaces

### ILabelControl
```csharp
interface ILabelControl {
    +GetText() string
    +SetText(string) void
    +GetTextFormatted() FormattedString
    +SetTextFormatted(FormattedString) void
    +IsBold() bool
    +IsItalic() bool
    +GetLineHeight() double
    +GetCharacterSpacing() double
}
```

### IImageControl
```csharp
interface IImageControl {
    +GetImageSource() string
    +SetImageSource(string) void
    +GetImageWidth() double
    +GetImageHeight() double
    +GetAspectRatio() double
    +IsImageLoaded() bool
    +WaitForImageLoad(int) void
}
```

### IMediaControl
```csharp
interface IMediaControl {
    +Play() void
    +Pause() void
    +Stop() void
    +GetDuration() TimeSpan
    +GetPosition() TimeSpan
    +SetPosition(TimeSpan) void
    +IsPlaying() bool
}
```

### IWebViewControl
```csharp
interface IWebViewControl {
    +Navigate(string) void
    +GetUrl() string
    +GoBack() void
    +GoForward() void
    +Reload() void
    +Stop() void
    +EvaluateJavaScript(string) string
}
```

### IShapeControl
```csharp
interface IShapeControl {
    +GetStrokeColor() Color
    +SetStrokeColor(Color) void
    +GetStrokeWidth() double
    +SetStrokeWidth(double) void
    +GetFillColor() Color
    +SetFillColor(Color) void
}
```

---

## File Interfaces

### IFileInputControl
```csharp
interface IFileInputControl {
    +SelectFile() void
    +SelectMultipleFiles() void
    +GetSelectedFile() string
    +GetSelectedFiles() List~string~
    +GetFileSize(string) long
    +GetFileType(string) string
    +ClearFileSelection() void
}
```

---

## State Interfaces

### ICheckableControl
```csharp
interface ICheckableControl {
    +IsChecked() bool
    +SetChecked(bool) void
    +Check() void
    +Uncheck() void
    +GetState() CheckState
}
```

### IIndeterminateControl
```csharp
interface IIndeterminateControl {
    +IsIndeterminate() bool
    +SetIndeterminate(bool) void
}
```

### IEnabledControl
```csharp
interface IEnabledControl {
    +IsEnabled() bool
    +SetEnabled(bool) void
    +Enable() void
    +Disable() void
}
```

### IFocusableControl
```csharp
interface IFocusableControl {
    +Focus() void
    +Blur() void
    +IsFocused() bool
    +SetFocused(bool) void
}
```

### ILoadingControl
```csharp
interface ILoadingControl {
    +IsLoading() bool
    +StartLoading() void
    +StopLoading() void
    +SetLoading(bool) void
}
```

---

## Validation Interfaces

### IValidatableControl
```csharp
interface IValidatableControl {
    +IsValid() bool
    +GetValidationErrors() List~string~
    +HasValidationError(string) bool
    +GetValidationState() ValidationState
    +SetValidationState(ValidationState) void
}
```

### IRequiredFieldControl
```csharp
interface IRequiredFieldControl {
    +IsRequired() bool
    +SetRequired(bool) void
    +GetRequiredMessage() string
    +SetRequiredMessage(string) void
}
```

### IErrorControl
```csharp
interface IErrorControl {
    +GetError() string
    +SetError(string) void
    +HasError() bool
    +ClearError() void
    +ShowError() void
    +HideError() void
}
```

---

## Navigation Interfaces

### INavigableControl
```csharp
interface INavigableControl {
    +Navigate(string) void
    +CanGoBack() bool
    +GoBack() void
    +CanGoForward() bool
    +GoForward() void
    +GetCurrentRoute() string
}
```

### IMenuControl
```csharp
interface IMenuControl {
    +OpenMenu() void
    +CloseMenu() void
    +IsMenuOpen() bool
    +GetMenuItems() List~string~
    +ClickMenuItem(string) void
}
```

---

## Behavior Interfaces

### ITooltipControl
```csharp
interface ITooltipControl {
    +SetTooltip(string) void
    +GetTooltip() string
    +ShowTooltip() void
    +HideTooltip() void
}
```

### IContextMenuControl
```csharp
interface IContextMenuControl {
    +ShowContextMenu() void
    +HideContextMenu() void
    +IsContextMenuOpen() bool
}
```

### IDraggableControl
```csharp
interface IDraggableControl {
    +IsDraggable() bool
    +SetDraggable(bool) void
    +Drag(int, int) void
}
```

### IDropTargetControl
```csharp
interface IDropTargetControl {
    +IsDropTarget() bool
    +SetDropTarget(bool) void
    +CanDropItem(string) bool
}
```

---

## Generic Controls

### GenericControl
```csharp
class GenericControl {
    +IsVisible() bool
    +WaitVisible(bool, int) bool
    +IsEnabled() bool
    +WaitEnabled(bool, int) bool
    +GetOpacity() double
    +SetOpacity(double) void
    +GetBackgroundColor() Color
    +SetBackgroundColor(Color) void
    +GetBounds() Rectangle
}
```

### GenericTextInputControl
```csharp
class GenericTextInputControl {
    +Enter(string) void
    +Clear() void
    +GetText() string
    +IsReadOnly() bool
    +Focus() void
    +Blur() void
}
```

### GenericSelectableControl
```csharp
class GenericSelectableControl {
    +SelectByIndex(int) void
    +SelectByText(string) void
    +GetSelectedIndex() int
    +GetSelectedText() string
    +GetItems() List~string~
    +GetItemCount() int
}
```

### GenericToggleControl
```csharp
class GenericToggleControl {
    +IsChecked() bool
    +Toggle() void
    +Check() void
    +Uncheck() void
    +SetChecked(bool) void
}
```

---

**Last Updated:** January 3, 2026
