# SPEC-006-001: Interface Definitions

**Version:** 1.1  
**Status:** Final  
**Date:** January 2026

---

## 1. Locator Interfaces

### By (Static Locator Factory)

```csharp
public static class By
{
    public static ControlLocator AutomationId(string value);
    public static ControlLocator Name(string value);
    public static ControlLocator Id(string value);
    public static ControlLocator ClassName(string value);
    public static ControlLocator XPath(string value);
    public static ControlLocator Css(string value);
    public static ControlLocator Text(string value);
    public static ControlLocator PartialText(string value);
    public static ControlLocator AccessibilityId(string value);
    public static ControlLocator TagName(string value);
    public static ControlLocator Label(string value);
    public static ControlLocator Placeholder(string value);
    public static ControlLocator Title(string value);
    public static ControlLocator Role(string value);
    public static ControlLocator TestId(string value);
    public static ControlLocator DataAttribute(string name, string value);
}
```

### ControlLocator

```csharp
public class ControlLocator
{
    public LocatorStrategy Strategy { get; }
    public string Value { get; }
    public ControlLocator? Parent { get; }
    
    public ControlLocator Then(ControlLocator child);
    public ControlLocator WithIndex(int index);
    public ControlLocator First();
    public ControlLocator Last();
    public ControlLocator Nth(int n);
    
    public static implicit operator ControlLocator(string automationId);
}

public enum LocatorStrategy
{
    AutomationId,
    Name,
    Id,
    ClassName,
    XPath,
    Css,
    Text,
    PartialText,
    AccessibilityId,
    TagName,
    Label,
    Placeholder,
    Title,
    Role,
    TestId,
    DataAttribute,
    Chained
}
```

---

## 2. Foundation Interfaces

### IControlObject

```csharp
public interface IControlObject
{
    ControlLocator Locator { get; }
    IPageObject? Page { get; }

    // Existence
    bool IsExists();
    bool WaitExists(bool? expected, int? timeoutMs = null);
    void CheckExists(bool? expected, int? timeoutMs = null);
    void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);

    // Visibility
    bool IsVisible();
    bool WaitVisible(bool? expected, int? timeoutMs = null);
    void CheckVisible(bool? expected, int? timeoutMs = null);
    void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);

    // Text
    string GetText(int? timeoutMs = null);
    void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    void AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null);
    void AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null);
    void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
    void AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null);
}
```

### IInteractiveControlObject

```csharp
public interface IInteractiveControlObject : IControlObject
{
    bool IsEnabled();
    bool WaitEnabled(bool? expected, int? timeoutMs = null);
    void CheckEnabled(bool? expected, int? timeoutMs = null);
    void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
}
```

### IFocusableControlObject

```csharp
public interface IFocusableControlObject : IInteractiveControlObject
{
    bool IsFocused();
    bool WaitFocused(bool? expected, int? timeoutMs = null);
    void CheckFocused(bool? expected, int? timeoutMs = null);
    void AssertFocused(bool? expected, string? message = null, int? timeoutMs = null);
    
    void Focus(int? timeoutMs = null);
    void Blur(int? timeoutMs = null);
}
```

---

## 3. Input Interfaces

### IClickableControlObject

```csharp
public interface IClickableControlObject : IInteractiveControlObject
{
    void Click(int? timeoutMs = null);
    void DoubleClick(int? timeoutMs = null);
    void RightClick(int? timeoutMs = null);
    void Hover(int? timeoutMs = null);
    void LongPress(int? durationMs = null, int? timeoutMs = null);
}
```

### ITextControlObject

```csharp
public interface ITextControlObject : IFocusableControlObject
{
    void Enter(string? text, int? timeoutMs = null);
    void Clear(int? timeoutMs = null);
    void ClearAndEnter(string? text, int? timeoutMs = null);
    void Append(string? text, int? timeoutMs = null);
    
    bool IsReadOnly();
    bool WaitReadOnly(bool? expected, int? timeoutMs = null);
    void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null);
    
    int GetTextLength(int? timeoutMs = null);
    void AssertTextLength(int? expected, string? message = null, int? timeoutMs = null);
}
```

### IEditableTextControlObject

```csharp
public interface IEditableTextControlObject : ITextControlObject
{
    void SelectAll(int? timeoutMs = null);
    void SelectText(int? start, int? length, int? timeoutMs = null);
    string GetSelectedText(int? timeoutMs = null);
    void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
    
    void Copy(int? timeoutMs = null);
    void Cut(int? timeoutMs = null);
    void Paste(int? timeoutMs = null);
    void Undo(int? timeoutMs = null);
    void Redo(int? timeoutMs = null);
}
```

### ISearchControlObject

```csharp
public interface ISearchControlObject : ITextControlObject
{
    void Search(int? timeoutMs = null);
    void ClearSearch(int? timeoutMs = null);
}
```

---

## 4. Toggle Interfaces

### IToggleControlObject

```csharp
public interface IToggleControlObject : IInteractiveControlObject
{
    bool IsChecked();
    bool WaitChecked(bool? expected, int? timeoutMs = null);
    void CheckChecked(bool? expected, int? timeoutMs = null);
    void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    
    void Toggle(int? timeoutMs = null);
    void SetChecked(bool? value, int? timeoutMs = null);
}
```

### ICheckBoxControlObject

```csharp
public interface ICheckBoxControlObject : IToggleControlObject
{
    bool? GetState(int? timeoutMs = null);
    void AssertState(bool? expected, string? message = null, int? timeoutMs = null);
    void SetState(bool? value, int? timeoutMs = null);
    void SetIndeterminate(int? timeoutMs = null);
}
```

### ISwitchControlObject

```csharp
public interface ISwitchControlObject : IToggleControlObject
{
    void TurnOn(int? timeoutMs = null);
    void TurnOff(int? timeoutMs = null);
}
```

### IRadioButtonControlObject

```csharp
public interface IRadioButtonControlObject : IInteractiveControlObject
{
    bool IsSelected();
    bool WaitSelected(bool? expected, int? timeoutMs = null);
    void AssertSelected(bool? expected, string? message = null, int? timeoutMs = null);
    
    void Select(int? timeoutMs = null);
    string GetGroupName(int? timeoutMs = null);
}
```

---

## 5. Selection Interfaces

### ISelectorControlObject

```csharp
public interface ISelectorControlObject : IInteractiveControlObject
{
    void SelectByIndex(int? index, int? timeoutMs = null);
    void SelectByText(string? text, int? timeoutMs = null);
    void SelectByValue(string? value, int? timeoutMs = null);
    void ClearSelection(int? timeoutMs = null);
    
    int GetSelectedIndex(int? timeoutMs = null);
    void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    
    string? GetSelectedText(int? timeoutMs = null);
    void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
    
    string? GetSelectedValue(int? timeoutMs = null);
    void AssertSelectedValue(string? expected, string? message = null, int? timeoutMs = null);
    
    IReadOnlyList<string> GetItems(int? timeoutMs = null);
    int GetItemCount(int? timeoutMs = null);
    void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    
    bool HasItem(string text, int? timeoutMs = null);
    void AssertHasItem(string text, bool? expected, string? message = null, int? timeoutMs = null);
}
```

### IPickerControlObject

```csharp
public interface IPickerControlObject : ISelectorControlObject
{
    bool IsOpen();
    bool WaitOpen(bool? expected, int? timeoutMs = null);
    void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);
    
    void Open(int? timeoutMs = null);
    void Close(int? timeoutMs = null);
}
```

### IMultiSelectorControlObject

```csharp
public interface IMultiSelectorControlObject : ISelectorControlObject
{
    void SelectMultiple(IEnumerable<int>? indices, int? timeoutMs = null);
    void SelectMultiple(IEnumerable<string>? texts, int? timeoutMs = null);
    void UnselectByIndex(int? index, int? timeoutMs = null);
    void UnselectByText(string? text, int? timeoutMs = null);
    void SelectAll(int? timeoutMs = null);
    void UnselectAll(int? timeoutMs = null);
    
    IReadOnlyList<int> GetSelectedIndices(int? timeoutMs = null);
    IReadOnlyList<string> GetSelectedTexts(int? timeoutMs = null);
    
    int GetSelectedCount(int? timeoutMs = null);
    void AssertSelectedCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 6. Range Interfaces

### IRangeControlObject

```csharp
public interface IRangeControlObject : IInteractiveControlObject
{
    double GetValue(int? timeoutMs = null);
    bool WaitValue(double? expected, double? tolerance = null, int? timeoutMs = null);
    void AssertValue(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    void AssertValueInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
    
    void SetValue(double? value, int? timeoutMs = null);
    void Increment(int? timeoutMs = null);
    void Decrement(int? timeoutMs = null);
    void IncrementBy(double? amount, int? timeoutMs = null);
    void DecrementBy(double? amount, int? timeoutMs = null);
    
    double GetMinimum(int? timeoutMs = null);
    double GetMaximum(int? timeoutMs = null);
    double GetStep(int? timeoutMs = null);
}
```

### ISliderControlObject

```csharp
public interface ISliderControlObject : IRangeControlObject
{
    void DragToValue(double? value, int? timeoutMs = null);
    void DragByOffset(int? pixelOffset, int? timeoutMs = null);
    double GetThumbPosition(int? timeoutMs = null);
}
```

### IStepperControlObject

```csharp
public interface IStepperControlObject : IRangeControlObject
{
    void StepUp(int? timeoutMs = null);
    void StepDown(int? timeoutMs = null);
    void StepUpMultiple(int? count, int? timeoutMs = null);
    void StepDownMultiple(int? count, int? timeoutMs = null);
}
```

---

## 7. DateTime Interfaces

### IDateControlObject

```csharp
public interface IDateControlObject : IInteractiveControlObject
{
    DateTime GetDate(int? timeoutMs = null);
    bool WaitDate(DateTime? expected, int? timeoutMs = null);
    void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null);
    void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null);
    
    void SetDate(DateTime? date, int? timeoutMs = null);
    void SelectYear(int? year, int? timeoutMs = null);
    void SelectMonth(int? month, int? timeoutMs = null);
    void SelectDay(int? day, int? timeoutMs = null);
    
    DateTime GetMinDate(int? timeoutMs = null);
    DateTime GetMaxDate(int? timeoutMs = null);
    
    bool IsPickerOpen();
    bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    void OpenPicker(int? timeoutMs = null);
    void ClosePicker(int? timeoutMs = null);
}
```

### ITimeControlObject

```csharp
public interface ITimeControlObject : IInteractiveControlObject
{
    TimeSpan GetTime(int? timeoutMs = null);
    bool WaitTime(TimeSpan? expected, int? timeoutMs = null);
    void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    void AssertTimeInRange(TimeSpan? min, TimeSpan? max, string? message = null, int? timeoutMs = null);
    
    void SetTime(TimeSpan? time, int? timeoutMs = null);
    void SelectHour(int? hour, int? timeoutMs = null);
    void SelectMinute(int? minute, int? timeoutMs = null);
    void SelectSecond(int? second, int? timeoutMs = null);
    
    TimeSpan GetMinTime(int? timeoutMs = null);
    TimeSpan GetMaxTime(int? timeoutMs = null);
    
    bool IsPickerOpen();
    bool WaitPickerOpen(bool? expected, int? timeoutMs = null);
    void OpenPicker(int? timeoutMs = null);
    void ClosePicker(int? timeoutMs = null);
}
```

### IDateTimeControlObject

```csharp
public interface IDateTimeControlObject : IDateControlObject, ITimeControlObject
{
    DateTime GetDateTime(int? timeoutMs = null);
    bool WaitDateTime(DateTime? expected, int? timeoutMs = null);
    void AssertDateTime(DateTime? expected, string? message = null, int? timeoutMs = null);
    
    void SetDateTime(DateTime? dateTime, int? timeoutMs = null);
}
```

---

## 8. Collection Interfaces

### IItemsControlObject

```csharp
public interface IItemsControlObject : IControlObject
{
    int GetItemCount(int? timeoutMs = null);
    bool WaitItemCount(int? expected, int? timeoutMs = null);
    void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    
    string GetItemText(int index, int? timeoutMs = null);
    void AssertItemText(int index, string? expected, string? message = null, int? timeoutMs = null);
    
    bool HasItem(string text, int? timeoutMs = null);
    void AssertHasItem(string text, bool? expected, string? message = null, int? timeoutMs = null);
    
    int GetItemIndex(string text, int? timeoutMs = null);
    IReadOnlyList<string> GetAllItemTexts(int? timeoutMs = null);
    
    void ClickItem(int? index, int? timeoutMs = null);
    void ClickItem(string? text, int? timeoutMs = null);
    void DoubleClickItem(int? index, int? timeoutMs = null);
    void RightClickItem(int? index, int? timeoutMs = null);
}
```

### ISelectableItemsControlObject

```csharp
public interface ISelectableItemsControlObject : IItemsControlObject
{
    void SelectItem(int? index, int? timeoutMs = null);
    void SelectItem(string? text, int? timeoutMs = null);
    
    int GetSelectedItemIndex(int? timeoutMs = null);
    void AssertSelectedItemIndex(int? expected, string? message = null, int? timeoutMs = null);
    
    string? GetSelectedItemText(int? timeoutMs = null);
    void AssertSelectedItemText(string? expected, string? message = null, int? timeoutMs = null);
    
    bool IsItemSelected(int index, int? timeoutMs = null);
    void AssertItemSelected(int index, bool? expected, string? message = null, int? timeoutMs = null);
}
```

### IMultiSelectableItemsControlObject

```csharp
public interface IMultiSelectableItemsControlObject : ISelectableItemsControlObject
{
    void SelectItems(IEnumerable<int>? indices, int? timeoutMs = null);
    void UnselectItem(int? index, int? timeoutMs = null);
    void SelectAllItems(int? timeoutMs = null);
    void UnselectAllItems(int? timeoutMs = null);
    
    IReadOnlyList<int> GetSelectedItemIndices(int? timeoutMs = null);
    IReadOnlyList<string> GetSelectedItemTexts(int? timeoutMs = null);
    
    int GetSelectedItemCount(int? timeoutMs = null);
    void AssertSelectedItemCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

### IScrollableItemsControlObject

```csharp
public interface IScrollableItemsControlObject : IItemsControlObject
{
    void ScrollToItem(int? index, int? timeoutMs = null);
    void ScrollToItem(string? text, int? timeoutMs = null);
    void ScrollToTop(int? timeoutMs = null);
    void ScrollToBottom(int? timeoutMs = null);
    
    bool IsItemVisible(int index, int? timeoutMs = null);
    bool WaitItemVisible(int index, bool? expected, int? timeoutMs = null);
    void AssertItemVisible(int index, bool? expected, string? message = null, int? timeoutMs = null);
}
```

### IGroupedItemsControlObject

```csharp
public interface IGroupedItemsControlObject : IItemsControlObject
{
    int GetGroupCount(int? timeoutMs = null);
    void AssertGroupCount(int? expected, string? message = null, int? timeoutMs = null);
    
    IReadOnlyList<string> GetGroupNames(int? timeoutMs = null);
    int GetGroupItemCount(string groupName, int? timeoutMs = null);
    
    bool IsGroupExpanded(string groupName, int? timeoutMs = null);
    bool WaitGroupExpanded(string groupName, bool? expected, int? timeoutMs = null);
    void AssertGroupExpanded(string groupName, bool? expected, string? message = null, int? timeoutMs = null);
    
    void ExpandGroup(string? groupName, int? timeoutMs = null);
    void CollapseGroup(string? groupName, int? timeoutMs = null);
    void ClickItemInGroup(string? groupName, int? itemIndex, int? timeoutMs = null);
}
```

---

## 9. Container Interfaces

### IContainerControlObject&lt;T&gt;

```csharp
public interface IContainerControlObject<T> : IControlObject where T : IControlObject
{
    bool HasChild(int? timeoutMs = null);
    bool WaitHasChild(bool? expected, int? timeoutMs = null);
    void AssertHasChild(bool? expected, string? message = null, int? timeoutMs = null);
    
    T GetChild(int? timeoutMs = null);
    T? TryGetChild(int? timeoutMs = null);
}
```

### IListContainerControlObject&lt;T&gt;

```csharp
public interface IListContainerControlObject<T> : IControlObject where T : IControlObject
{
    int GetChildCount(int? timeoutMs = null);
    bool WaitChildCount(int? expected, int? timeoutMs = null);
    void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
    
    IReadOnlyList<string> GetChildNames(int? timeoutMs = null);
    
    bool ChildExists(ControlLocator locator, int? timeoutMs = null);
    bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    
    T GetChild(ControlLocator locator, int? timeoutMs = null);
    T? TryGetChild(ControlLocator locator, int? timeoutMs = null);
    T GetChildByIndex(int index, int? timeoutMs = null);
    IReadOnlyList<T> GetAllChildren(int? timeoutMs = null);
}
```

### IScrollableControlObject

```csharp
public interface IScrollableControlObject : IListContainerControlObject<IControlObject>
{
    void ScrollToElement(ControlLocator? locator, int? timeoutMs = null);
    void ScrollToTop(int? timeoutMs = null);
    void ScrollToBottom(int? timeoutMs = null);
    void ScrollUp(int? distance = null, int? timeoutMs = null);
    void ScrollDown(int? distance = null, int? timeoutMs = null);
    void ScrollLeft(int? distance = null, int? timeoutMs = null);
    void ScrollRight(int? distance = null, int? timeoutMs = null);
    
    Point GetScrollPosition(int? timeoutMs = null);
    void SetScrollPosition(Point? position, int? timeoutMs = null);
    
    double GetScrollableHeight(int? timeoutMs = null);
    double GetScrollableWidth(int? timeoutMs = null);
    double GetViewportHeight(int? timeoutMs = null);
    double GetViewportWidth(int? timeoutMs = null);
    
    double GetVerticalScrollPercent(int? timeoutMs = null);
    void AssertVerticalScrollPercent(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    
    double GetHorizontalScrollPercent(int? timeoutMs = null);
    void AssertHorizontalScrollPercent(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
}
```

### IExpanderControlObject

```csharp
public interface IExpanderControlObject : IListContainerControlObject<IControlObject>
{
    bool IsExpanded(int? timeoutMs = null);
    bool WaitExpanded(bool? expected, int? timeoutMs = null);
    void CheckExpanded(bool? expected, int? timeoutMs = null);
    void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);
    
    void Expand(int? timeoutMs = null);
    void Collapse(int? timeoutMs = null);
    void Toggle(int? timeoutMs = null);
    
    string GetHeaderText(int? timeoutMs = null);
    void AssertHeaderText(string? expected, string? message = null, int? timeoutMs = null);
}
```

### IRefreshableControlObject

```csharp
public interface IRefreshableControlObject : IControlObject
{
    bool IsRefreshing(int? timeoutMs = null);
    bool WaitRefreshing(bool? expected, int? timeoutMs = null);
    void AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null);
    
    void Refresh(int? timeoutMs = null);
    void PullToRefresh(int? timeoutMs = null);
    void CancelRefresh(int? timeoutMs = null);
}
```

### ISwipeableControlObject

```csharp
public interface ISwipeableControlObject : IControlObject
{
    void SwipeLeft(int? timeoutMs = null);
    void SwipeRight(int? timeoutMs = null);
    void SwipeUp(int? timeoutMs = null);
    void SwipeDown(int? timeoutMs = null);
    void SwipeToReveal(string? direction, int? timeoutMs = null);
    
    bool IsSwipeEnabled(int? timeoutMs = null);
    void AssertSwipeEnabled(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 10. Display Interfaces

### ILabelControlObject

```csharp
public interface ILabelControlObject : IControlObject
{
    string GetFormattedText(int? timeoutMs = null);
    
    bool IsBold(int? timeoutMs = null);
    void AssertBold(bool? expected, string? message = null, int? timeoutMs = null);
    
    bool IsItalic(int? timeoutMs = null);
    void AssertItalic(bool? expected, string? message = null, int? timeoutMs = null);
    
    double GetFontSize(int? timeoutMs = null);
    void AssertFontSize(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    
    string GetTextColor(int? timeoutMs = null);
    void AssertTextColor(string? expected, string? message = null, int? timeoutMs = null);
    
    int GetLineCount(int? timeoutMs = null);
    void AssertLineCount(int? expected, string? message = null, int? timeoutMs = null);
    
    bool IsTruncated(int? timeoutMs = null);
    void AssertTruncated(bool? expected, string? message = null, int? timeoutMs = null);
}
```

### IImageControlObject

```csharp
public interface IImageControlObject : IControlObject
{
    string GetSource(int? timeoutMs = null);
    void AssertSource(string? expected, string? message = null, int? timeoutMs = null);
    
    bool IsLoaded(int? timeoutMs = null);
    bool WaitLoaded(bool? expected, int? timeoutMs = null);
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
    
    double GetWidth(int? timeoutMs = null);
    void AssertWidth(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    
    double GetHeight(int? timeoutMs = null);
    void AssertHeight(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    
    double GetAspectRatio(int? timeoutMs = null);
    void AssertAspectRatio(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
}
```

### IProgressControlObject

```csharp
public interface IProgressControlObject : IControlObject
{
    double GetProgress(int? timeoutMs = null);
    bool WaitProgress(double? expected, double? tolerance = null, int? timeoutMs = null);
    void AssertProgress(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    void AssertProgressInRange(double? min, double? max, string? message = null, int? timeoutMs = null);
    
    bool IsIndeterminate(int? timeoutMs = null);
    void AssertIndeterminate(bool? expected, string? message = null, int? timeoutMs = null);
    
    double GetMinimum(int? timeoutMs = null);
    double GetMaximum(int? timeoutMs = null);
}
```

### IActivityIndicatorControlObject

```csharp
public interface IActivityIndicatorControlObject : IControlObject
{
    bool IsRunning(int? timeoutMs = null);
    bool WaitRunning(bool? expected, int? timeoutMs = null);
    void AssertRunning(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 11. Media Interfaces

### IMediaControlObject

```csharp
public interface IMediaControlObject : IControlObject
{
    void Play(int? timeoutMs = null);
    void Pause(int? timeoutMs = null);
    void Stop(int? timeoutMs = null);
    void Seek(TimeSpan? position, int? timeoutMs = null);
    
    bool IsPlaying(int? timeoutMs = null);
    bool WaitPlaying(bool? expected, int? timeoutMs = null);
    void AssertPlaying(bool? expected, string? message = null, int? timeoutMs = null);
    
    bool IsPaused(int? timeoutMs = null);
    bool IsStopped(int? timeoutMs = null);
    
    TimeSpan GetDuration(int? timeoutMs = null);
    void AssertDuration(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    
    TimeSpan GetPosition(int? timeoutMs = null);
    bool WaitPosition(TimeSpan? expected, int? timeoutMs = null);
    void AssertPosition(TimeSpan? expected, TimeSpan? tolerance = null, string? message = null, int? timeoutMs = null);
    
    double GetVolume(int? timeoutMs = null);
    void SetVolume(double? volume, int? timeoutMs = null);
    void AssertVolume(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    
    bool IsMuted(int? timeoutMs = null);
    void Mute(int? timeoutMs = null);
    void Unmute(int? timeoutMs = null);
    void AssertMuted(bool? expected, string? message = null, int? timeoutMs = null);
    
    string GetSource(int? timeoutMs = null);
    void AssertSource(string? expected, string? message = null, int? timeoutMs = null);
}
```

### IWebViewControlObject

```csharp
public interface IWebViewControlObject : IControlObject
{
    void Navigate(string? url, int? timeoutMs = null);
    void Reload(int? timeoutMs = null);
    void Stop(int? timeoutMs = null);
    void GoBack(int? timeoutMs = null);
    void GoForward(int? timeoutMs = null);
    
    string GetUrl(int? timeoutMs = null);
    bool WaitUrl(string? expected, int? timeoutMs = null);
    void AssertUrl(string? expected, string? message = null, int? timeoutMs = null);
    void AssertUrlContains(string? expected, string? message = null, int? timeoutMs = null);
    
    string GetTitle(int? timeoutMs = null);
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
    
    bool IsLoading(int? timeoutMs = null);
    bool WaitLoading(bool? expected, int? timeoutMs = null);
    void AssertLoading(bool? expected, string? message = null, int? timeoutMs = null);
    
    bool CanGoBack(int? timeoutMs = null);
    void AssertCanGoBack(bool? expected, string? message = null, int? timeoutMs = null);
    
    bool CanGoForward(int? timeoutMs = null);
    void AssertCanGoForward(bool? expected, string? message = null, int? timeoutMs = null);
    
    string EvaluateJavaScript(string? script, int? timeoutMs = null);
}
```

---

## 12. Navigation Interfaces

### ITabControlObject

```csharp
public interface ITabControlObject : IControlObject
{
    int GetTabCount(int? timeoutMs = null);
    void AssertTabCount(int? expected, string? message = null, int? timeoutMs = null);
    
    IReadOnlyList<string> GetTabNames(int? timeoutMs = null);
    
    int GetSelectedTabIndex(int? timeoutMs = null);
    bool WaitSelectedTabIndex(int? expected, int? timeoutMs = null);
    void AssertSelectedTabIndex(int? expected, string? message = null, int? timeoutMs = null);
    
    string GetSelectedTabName(int? timeoutMs = null);
    void AssertSelectedTabName(string? expected, string? message = null, int? timeoutMs = null);
    
    void SelectTab(int? index, int? timeoutMs = null);
    void SelectTab(string? name, int? timeoutMs = null);
    
    bool IsTabEnabled(int index, int? timeoutMs = null);
    void AssertTabEnabled(int index, bool? expected, string? message = null, int? timeoutMs = null);
    
    bool IsTabVisible(int index, int? timeoutMs = null);
    void AssertTabVisible(int index, bool? expected, string? message = null, int? timeoutMs = null);
}
```

### IMenuControlObject

```csharp
public interface IMenuControlObject : IControlObject
{
    bool IsOpen(int? timeoutMs = null);
    bool WaitOpen(bool? expected, int? timeoutMs = null);
    void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);
    
    void Open(int? timeoutMs = null);
    void Close(int? timeoutMs = null);
    
    IReadOnlyList<string> GetMenuItems(int? timeoutMs = null);
    int GetMenuItemCount(int? timeoutMs = null);
    void AssertMenuItemCount(int? expected, string? message = null, int? timeoutMs = null);
    
    void ClickMenuItem(string? text, int? timeoutMs = null);
    void ClickMenuItem(int? index, int? timeoutMs = null);
    
    bool IsMenuItemEnabled(string text, int? timeoutMs = null);
    void AssertMenuItemEnabled(string text, bool? expected, string? message = null, int? timeoutMs = null);
    
    void ExpandMenuItem(string? text, int? timeoutMs = null);
    IReadOnlyList<string> GetSubmenuItems(string parentText, int? timeoutMs = null);
}
```

### IFlyoutControlObject

```csharp
public interface IFlyoutControlObject : IControlObject
{
    bool IsOpen(int? timeoutMs = null);
    bool WaitOpen(bool? expected, int? timeoutMs = null);
    void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);
    
    void Open(int? timeoutMs = null);
    void Close(int? timeoutMs = null);
    void Toggle(int? timeoutMs = null);
    
    IReadOnlyList<string> GetFlyoutItems(int? timeoutMs = null);
    void SelectFlyoutItem(string? text, int? timeoutMs = null);
    void SelectFlyoutItem(int? index, int? timeoutMs = null);
}
```

### IToolbarControlObject

```csharp
public interface IToolbarControlObject : IControlObject
{
    IReadOnlyList<string> GetToolbarItems(int? timeoutMs = null);
    int GetToolbarItemCount(int? timeoutMs = null);
    void AssertToolbarItemCount(int? expected, string? message = null, int? timeoutMs = null);
    
    void ClickToolbarItem(string? text, int? timeoutMs = null);
    void ClickToolbarItem(int? index, int? timeoutMs = null);
    
    bool IsToolbarItemEnabled(string text, int? timeoutMs = null);
    void AssertToolbarItemEnabled(string text, bool? expected, string? message = null, int? timeoutMs = null);
    
    bool IsToolbarItemVisible(string text, int? timeoutMs = null);
    void AssertToolbarItemVisible(string text, bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 13. Validation Interface

### IValidatableControlObject

```csharp
public interface IValidatableControlObject : IControlObject
{
    bool IsValid(int? timeoutMs = null);
    bool WaitValid(bool? expected, int? timeoutMs = null);
    void AssertValid(bool? expected, string? message = null, int? timeoutMs = null);
    
    IReadOnlyList<string> GetValidationErrors(int? timeoutMs = null);
    int GetValidationErrorCount(int? timeoutMs = null);
    void AssertValidationErrorCount(int? expected, string? message = null, int? timeoutMs = null);
    
    bool HasValidationError(string errorMessage, int? timeoutMs = null);
    void AssertHasValidationError(string errorMessage, bool? expected, string? message = null, int? timeoutMs = null);
    
    void Validate(int? timeoutMs = null);
    void ClearValidation(int? timeoutMs = null);
    
    bool IsRequired(int? timeoutMs = null);
    void AssertRequired(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 14. Page Interface

### IPageObject

```csharp
public interface IPageObject
{
    string Name { get; }
    
    bool IsLoaded(int? timeoutMs = null);
    bool WaitLoaded(bool? expected, int? timeoutMs = null);
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
    
    string GetTitle(int? timeoutMs = null);
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
    
    T GetControl<T>(ControlLocator locator, int? timeoutMs = null) where T : IControlObject;
    T? TryGetControl<T>(ControlLocator locator, int? timeoutMs = null) where T : IControlObject;
    
    bool ControlExists(ControlLocator locator, int? timeoutMs = null);
    bool WaitControlExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    void AssertControlExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    
    void TakeScreenshot(string? filename, int? timeoutMs = null);
    void ScrollToControl(ControlLocator? locator, int? timeoutMs = null);
}
```

### IBusyPageObject

Extends `IPageObject` with busy/loading state tracking for pages that display loading indicators during asynchronous operations.

```csharp
public interface IBusyPageObject : IPageObject
{
    /// <summary>
    /// Gets the locator for the busy indicator element.
    /// Override to specify a custom busy indicator.
    /// </summary>
    ControlLocator? BusyIndicatorLocator { get; }
    
    /// <summary>
    /// Returns true if the page is currently showing a busy/loading state.
    /// Default implementation checks if BusyIndicatorLocator element is visible.
    /// </summary>
    bool IsBusy(int? timeoutMs = null);
    
    /// <summary>
    /// Returns true if the page is not busy (ready for interaction).
    /// </summary>
    bool IsNotBusy(int? timeoutMs = null);
    
    /// <summary>
    /// Waits for the busy state to match the expected value.
    /// </summary>
    bool WaitBusy(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Waits for the page to become not busy (loading complete).
    /// </summary>
    bool WaitNotBusy(int? timeoutMs = null);
    
    /// <summary>
    /// Throws if busy state doesn't match expected within timeout.
    /// </summary>
    void CheckBusy(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Throws if page is still busy after timeout.
    /// </summary>
    void CheckNotBusy(int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the busy state matches expected.
    /// </summary>
    void AssertBusy(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the page is not busy.
    /// </summary>
    void AssertNotBusy(string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Returns true when page is loaded AND not busy.
    /// Override of IsLoaded that includes busy state check.
    /// </summary>
    bool IsReady(int? timeoutMs = null);
    
    /// <summary>
    /// Waits for page to be ready (loaded and not busy).
    /// </summary>
    bool WaitReady(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Throws if page is not ready within timeout.
    /// </summary>
    void CheckReady(int? timeoutMs = null);
    
    /// <summary>
    /// Asserts the page is ready (loaded and not busy).
    /// </summary>
    void AssertReady(string? message = null, int? timeoutMs = null);
}
```

**Usage Examples:**

```csharp
// Simple busy indicator by element
public class DashboardPage : BusyPageBase
{
    public override ControlLocator? BusyIndicatorLocator => 
        By.AutomationId("LoadingSpinner");
}

// Custom busy logic
public class DataGridPage : BusyPageBase
{
    public override bool IsBusy(int? timeoutMs = null)
    {
        var grid = GetControl<IControlObject>("DataGrid");
        return grid.GetText().Contains("Loading...");
    }
}

// In tests
var dashboard = new DashboardPage(context);
dashboard.WaitNotBusy();  // Wait for loading to complete
dashboard.AssertReady();  // Assert page is loaded and not busy
```

---

## 15. Context Interface

### ITestContext

```csharp
public interface ITestContext
{
    int DefaultTimeoutMs { get; set; }
    int DefaultPollingIntervalMs { get; set; }
    
    IPageObject CurrentPage { get; }
    void NavigateTo(string? route, int? timeoutMs = null);
    void NavigateTo<TPage>(int? timeoutMs = null) where TPage : IPageObject;
    
    void TakeScreenshot(string? filename);
    void Log(string? message);
    void LogError(string? message);
    
    T CreateControl<T>(ControlLocator locator) where T : IControlObject;
}
```

---

## 16. Async Interfaces (Blazor)

### IAsyncControlObject

```csharp
public interface IAsyncControlObject
{
    ControlLocator Locator { get; }
    
    Task<bool> IsExistsAsync(int? timeoutMs = null);
    Task<bool> WaitExistsAsync(bool? expected, int? timeoutMs = null);
    Task AssertExistsAsync(bool? expected, string? message = null, int? timeoutMs = null);
    
    Task<bool> IsVisibleAsync(int? timeoutMs = null);
    Task<bool> WaitVisibleAsync(bool? expected, int? timeoutMs = null);
    Task AssertVisibleAsync(bool? expected, string? message = null, int? timeoutMs = null);
    
    Task<string> GetTextAsync(int? timeoutMs = null);
    Task AssertTextAsync(string? expected, string? message = null, int? timeoutMs = null);
}
```

### IAsyncClickableControlObject

```csharp
public interface IAsyncClickableControlObject : IAsyncControlObject
{
    Task<bool> IsEnabledAsync(int? timeoutMs = null);
    Task<bool> WaitEnabledAsync(bool? expected, int? timeoutMs = null);
    Task AssertEnabledAsync(bool? expected, string? message = null, int? timeoutMs = null);
    
    Task ClickAsync(int? timeoutMs = null);
    Task DoubleClickAsync(int? timeoutMs = null);
    Task RightClickAsync(int? timeoutMs = null);
    Task HoverAsync(int? timeoutMs = null);
}
```

### IAsyncTextControlObject

```csharp
public interface IAsyncTextControlObject : IAsyncControlObject
{
    Task EnterAsync(string? text, int? timeoutMs = null);
    Task ClearAsync(int? timeoutMs = null);
    Task ClearAndEnterAsync(string? text, int? timeoutMs = null);
    
    Task<bool> IsReadOnlyAsync(int? timeoutMs = null);
    Task AssertReadOnlyAsync(bool? expected, string? message = null, int? timeoutMs = null);
}
```

### IAsyncSelectorControlObject

```csharp
public interface IAsyncSelectorControlObject : IAsyncControlObject
{
    Task SelectByIndexAsync(int? index, int? timeoutMs = null);
    Task SelectByTextAsync(string? text, int? timeoutMs = null);
    
    Task<int> GetSelectedIndexAsync(int? timeoutMs = null);
    Task AssertSelectedIndexAsync(int? expected, string? message = null, int? timeoutMs = null);
    
    Task<string?> GetSelectedTextAsync(int? timeoutMs = null);
    Task AssertSelectedTextAsync(string? expected, string? message = null, int? timeoutMs = null);
    
    Task<IReadOnlyList<string>> GetItemsAsync(int? timeoutMs = null);
    Task<int> GetItemCountAsync(int? timeoutMs = null);
    Task AssertItemCountAsync(int? expected, string? message = null, int? timeoutMs = null);
}
```

### IAsyncToggleControlObject

```csharp
public interface IAsyncToggleControlObject : IAsyncControlObject
{
    Task<bool> IsCheckedAsync(int? timeoutMs = null);
    Task<bool> WaitCheckedAsync(bool? expected, int? timeoutMs = null);
    Task AssertCheckedAsync(bool? expected, string? message = null, int? timeoutMs = null);
    
    Task ToggleAsync(int? timeoutMs = null);
    Task SetCheckedAsync(bool? value, int? timeoutMs = null);
}
```

### IAsyncRangeControlObject

```csharp
public interface IAsyncRangeControlObject : IAsyncControlObject
{
    Task<double> GetValueAsync(int? timeoutMs = null);
    Task<bool> WaitValueAsync(double? expected, double? tolerance = null, int? timeoutMs = null);
    Task AssertValueAsync(double? expected, double? tolerance = null, string? message = null, int? timeoutMs = null);
    
    Task SetValueAsync(double? value, int? timeoutMs = null);
    Task IncrementAsync(int? timeoutMs = null);
    Task DecrementAsync(int? timeoutMs = null);
}
```

---

## 17. Exception Types

```csharp
public class ControlObjectException : Exception
{
    public ControlLocator Locator { get; }
    public string ControlType { get; }
    public int? TimeoutMs { get; }
}

public class ControlNotFoundException : ControlObjectException { }
public class ControlNotVisibleException : ControlObjectException { }
public class ControlNotEnabledException : ControlObjectException { }

public class ControlTimeoutException : ControlObjectException
{
    public string ExpectedState { get; }
    public string ActualState { get; }
}

public class ControlAssertionException : ControlObjectException
{
    public object? Expected { get; }
    public object? Actual { get; }
    public string AssertionType { get; }
}

public class ControlReadOnlyException : ControlObjectException { }

public class ControlValueOutOfRangeException : ControlObjectException
{
    public object? Value { get; }
    public object? Minimum { get; }
    public object? Maximum { get; }
}

public class LocatorNotFoundException : ControlObjectException
{
    public LocatorStrategy Strategy { get; }
}
```

---

**Next:** [SPEC-006-002: Class Definitions](SPEC-006-002-CLASSES.md)
