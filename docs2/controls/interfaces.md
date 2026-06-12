# Interface Specifications

**Source of truth:** `srcnew/Brinell.Core/Interfaces/`

## Consolidation History (SPEC-003)

The interface hierarchy was consolidated from 29 legacy interfaces to the current 25. Key changes:

- **Merged:** Separate `ICheckable`/`ISwitchable` → unified `IToggleControlObject`
- **Added:** `IExpandableControlObject`, `IFocusableControlObject`, `IProgressControlObject`, `IDateControlObject`, `ITimeControlObject`, `ISwipeableControlObject`, `IRefreshableControlObject`
- **Enhanced:** `IClickableControlObject` gained `Hover()`, `LongPress()`, `DoubleClick()`, `RightClick()`
- **Enhanced:** `ITextControlObject` gained `AssertTextMatches()`, `AssertTextStartsWith()`, `AssertTextEndsWith()`, `AssertTextEmpty()`
- **Enhanced:** `IEditableTextControlObject` gained `Append()`, `SetText()`, placeholder support, read-only queries

**Status:** 16/18 implementation tasks complete (Phases 1-4 done, Phase 5 testing partially remaining).

This document lists all interface signatures. For full documentation, see the source code.

## Inheritance Hierarchy

```
IControlObject<TScope>
├── IClickableControlObject<TScope>
│   ├── IExpandableControlObject<TScope>
│   └── ITabControlObject<TScope>
├── ITextControlObject<TScope>
│   └── IEditableTextControlObject<TScope>
├── IToggleControlObject<TScope>
├── ISelectorControlObject<TScope>
├── IRangeControlObject<TScope>
├── IProgressControlObject<TScope>
├── IScrollableControlObject<TScope>
├── ISwipeableControlObject<TScope>
├── IRefreshableControlObject<TScope>
├── IFocusableControlObject<TScope>
├── IDateControlObject<TScope>
└── ITimeControlObject<TScope>

IElementScope
├── IElementScope<TElement>
│   ├── IPagedScope<TPage, TElement>
│   ├── IContainerControl<TElement>
│   ├── IPageObject<TElement>
│   └── ITestContext<TElement>
└── IPageObject

IElement<TSelf>  (self-referencing)
IDriver<TElement> : IDisposable
ITestContext : IDisposable
```

## Foundation Interfaces

### IControlObject\<TScope\>

Base for all controls. Provides the Is/Wait/Assert pattern for existence, visibility, enabled state, and text.

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsExists()` | `bool` | Element exists in DOM/tree |
| `IsVisible()` | `bool?` | Element visible (null = not found) |
| `IsEnabled()` | `bool?` | Element enabled (null = not found) |
| `WaitExists(bool?, int?)` | `bool` | Wait for existence state |
| `WaitVisible(bool?, int?)` | `bool` | Wait for visibility state |
| `WaitEnabled(bool?, int?)` | `bool` | Wait for enabled state |
| `AssertExists(bool?, string?, int?)` | `TScope` | Assert existence |
| `AssertVisible(bool?, string?, int?)` | `TScope` | Assert visibility |
| `AssertEnabled(bool?, string?, int?)` | `TScope` | Assert enabled |
| `GetText(int?)` | `string?` | Get element text |
| `WaitText(string?, int?)` | `bool` | Wait for text value |
| `AssertText(string?, string?, int?)` | `TScope` | Assert text equals |
| `AssertTextContains(string?, string?, int?)` | `TScope` | Assert text contains |
| `GetAttribute(string)` | `string?` | Get element attribute |

### IClickableControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsClickable()` | `bool?` | Can be clicked |
| `Click(int?)` | `TScope` | Click the control |
| `DoubleClick(int?)` | `TScope` | Double-click |
| `RightClick(int?)` | `TScope` | Right/context click |
| `Hover(int?)` | `TScope` | Hover over control |
| `LongPress(int?, int?)` | `TScope` | Long press (mobile) |
| `WaitClickable(bool?, int?)` | `bool` | Wait for clickable state |
| `AssertClickable(bool?, string?, int?)` | `TScope` | Assert clickable |

### IFocusableControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsFocused()` | `bool?` | Has keyboard focus |
| `Focus(int?)` | `TScope` | Set focus |
| `Blur(int?)` | `TScope` | Remove focus |
| `WaitFocused(bool?, int?)` | `bool` | Wait for focus state |
| `AssertFocused(bool?, string?, int?)` | `TScope` | Assert focus |

## Text Interfaces

### ITextControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `WaitTextEquals(string?, int?)` | `bool` | Wait for exact text match |
| `WaitTextContains(string?, int?)` | `bool` | Wait for text containing |
| `AssertTextMatches(string?, string?, int?)` | `TScope` | Assert regex match |
| `AssertTextStartsWith(string?, string?, int?)` | `TScope` | Assert starts with |
| `AssertTextEndsWith(string?, string?, int?)` | `TScope` | Assert ends with |
| `AssertTextEmpty(bool?, string?, int?)` | `TScope` | Assert empty/not empty |

### IEditableTextControlObject\<TScope\> : ITextControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `Enter(string?, int?)` | `TScope` | Type text |
| `Clear(int?)` | `TScope` | Clear text |
| `SetText(string?, int?)` | `TScope` | Set text directly |
| `Append(string?, int?)` | `TScope` | Append text |
| `GetPlaceholder()` | `string?` | Get placeholder text |
| `WaitPlaceholder(string?, int?)` | `bool` | Wait for placeholder |
| `AssertPlaceholder(string?, string?, int?)` | `TScope` | Assert placeholder |
| `IsReadOnly()` | `bool?` | Is read-only |
| `WaitReadOnly(bool?, int?)` | `bool` | Wait for read-only |
| `AssertReadOnly(bool?, string?, int?)` | `TScope` | Assert read-only |

## Selection Interfaces

### ISelectorControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `SelectByText(string?, int?)` | `TScope` | Select item by text |
| `SelectByIndex(int?, int?)` | `TScope` | Select item by index |
| `SelectByValue(string?, int?)` | `TScope` | Select item by value |
| `GetSelectedText(int?)` | `string?` | Get selected item text |
| `GetSelectedIndex(int?)` | `int?` | Get selected index |
| `GetItemTexts(int?)` | `IReadOnlyList<string>?` | Get all item texts |
| `GetItemCount(int?)` | `int?` | Get item count |
| `WaitSelectedText(string?, int?)` | `bool` | Wait for selection |
| `WaitSelectedIndex(int?, int?)` | `bool` | Wait for index |
| `WaitItemCount(int?, int?)` | `bool` | Wait for count |
| `AssertSelectedText(string?, string?, int?)` | `TScope` | Assert selection |
| `AssertSelectedIndex(int?, string?, int?)` | `TScope` | Assert index |
| `AssertItemCount(int?, string?, int?)` | `TScope` | Assert count |

### IToggleControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsChecked()` | `bool?` | Current checked state |
| `Toggle(int?)` | `TScope` | Toggle state |
| `SetChecked(bool?, int?)` | `TScope` | Set to specific state |
| `Check(int?)` | `TScope` | Check (set true) |
| `Uncheck(int?)` | `TScope` | Uncheck (set false) |
| `WaitChecked(bool?, int?)` | `bool` | Wait for state |
| `AssertChecked(bool?, string?, int?)` | `TScope` | Assert state |

### ITabControlObject\<TScope\> : IClickableControlObject\<TScope\>

| Property/Method | Returns | Purpose |
|--------|---------|---------|
| `Title` | `string` | Tab title |
| `IsSelected()` | `bool?` | Tab selected state |
| `WaitSelected(bool?, int?)` | `bool` | Wait for selection |
| `AssertSelected(bool?, string?, int?)` | `TScope` | Assert selection |

### IExpandableControlObject\<TScope\> : IClickableControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsExpanded()` | `bool?` | Expanded state |
| `Expand(int?)` | `TScope` | Expand |
| `Collapse(int?)` | `TScope` | Collapse |
| `ToggleExpanded(int?)` | `TScope` | Toggle expand/collapse |
| `WaitExpanded(bool?, int?)` | `bool` | Wait for state |
| `AssertExpanded(bool?, string?, int?)` | `TScope` | Assert state |

## Range Interfaces

### IRangeControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `GetValue(int?)` | `double?` | Current value |
| `GetMinimum(int?)` | `double?` | Minimum value |
| `GetMaximum(int?)` | `double?` | Maximum value |
| `GetStep(int?)` | `double?` | Step increment |
| `SetValue(double?, int?)` | `TScope` | Set value |
| `Increment(int?)` | `TScope` | Increment by step |
| `Decrement(int?)` | `TScope` | Decrement by step |
| `WaitValue(double?, double, int?)` | `bool` | Wait (with tolerance) |
| `AssertValue(double?, double, string?, int?)` | `TScope` | Assert (with tolerance) |

### IProgressControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsIndeterminate()` | `bool?` | Is indeterminate mode |
| `GetProgress()` | `double?` | Current progress |
| `WaitProgress(double?, int?)` | `bool` | Wait for progress |
| `WaitComplete(int?)` | `bool` | Wait for completion |
| `AssertProgress(double?, string?, int?)` | `TScope` | Assert progress |
| `AssertComplete(string?, int?)` | `TScope` | Assert complete |

## DateTime Interfaces

### IDateControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `GetDate()` | `DateTime?` | Current date |
| `SetDate(DateTime?, int?)` | `TScope` | Set date |
| `WaitDate(DateTime?, int?)` | `bool` | Wait for date |
| `AssertDate(DateTime?, string?, int?)` | `TScope` | Assert date |

### ITimeControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `GetTime()` | `TimeSpan?` | Current time |
| `SetTime(TimeSpan?, int?)` | `TScope` | Set time |
| `WaitTime(TimeSpan?, int?)` | `bool` | Wait for time |
| `AssertTime(TimeSpan?, string?, int?)` | `TScope` | Assert time |

## Scrolling Interfaces

### IScrollableControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `ScrollToTop(int?)` | `TScope` | Scroll to top |
| `ScrollToEnd(int?)` | `TScope` | Scroll to bottom |
| `ScrollBy(int, int, int?)` | `TScope` | Scroll by offset |
| `ScrollTo(Locator, int?)` | `TScope` | Scroll to element |
| `SetScrollPosition(double, int?)` | `TScope` | Set position (0-1) |
| `GetScrollPosition(int?)` | `double?` | Get current position |
| `CanScrollDown(int?)` | `bool?` | More content below |
| `CanScrollUp(int?)` | `bool?` | More content above |
| `WaitScrollPosition(double?, double, int?)` | `bool` | Wait for position |
| `AssertScrollPosition(double?, double, string?, int?)` | `TScope` | Assert position |

### ISwipeableControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `SwipeLeft/Right/Up/Down(int?)` | `TScope` | Directional swipe |
| `Swipe(int, int, int, int, int?)` | `TScope` | Custom swipe |

### IRefreshableControlObject\<TScope\> : IControlObject\<TScope\>

| Method | Returns | Purpose |
|--------|---------|---------|
| `IsRefreshing()` | `bool?` | Currently refreshing |
| `PullToRefresh(int?)` | `TScope` | Trigger refresh |
| `WaitRefreshing(bool?, int?)` | `bool` | Wait for state |
| `AssertRefreshing(bool?, string?, int?)` | `TScope` | Assert state |

## Infrastructure Interfaces

### IElement\<TSelf\>

Self-referencing element abstraction. Properties: `Visible`, `Enabled`, `Selected`, `Text`, `TagName`, `Location`, `Size`, `Rect`. Actions: `Click()`, `SendKeys()`, `Clear()`, `DoubleClick()`, `RightClick()`, `Hover()`, `LongPress()`, `ScrollIntoView()`, `Swipe()`. Finding: `FindElement()`, `FindElements()`, `TryFindElement()`.

### IDriver\<TElement\> : IDisposable

UI automation driver. Finding: `FindElement()`, `FindElements()`, `TryFindElement()`. Session: `Close()`, `Quit()`. Screenshot: `GetScreenshot()`.

### IPageObject / IPageObject\<TElement\>

Page lifecycle: `IsLoaded()`, `WaitLoaded()`, `AssertLoaded()`. Title: `GetTitle()`, `WaitTitle()`, `AssertTitle()`. Screenshot: `TakeScreenshot()`. Property `Name`.

### ITestContext / ITestContext\<TElement\>

Test execution context. Navigation: `NavigateTo()`, `NavigateBack()`, `Refresh()`. Screenshots: `TakeScreenshot()`, `SaveScreenshot()`. State: `ResetAppState()`. Properties: `Timeouts`, `Logger`.

### IContainerControl\<TElement\>

Container with scoped element finding. Property: `ContainerRoot`. Inherits element finding from `IElementScope<TElement>`.

### IRangePatternElement

Windows UI Automation `RangeValuePattern` support. Methods: `SetRangeValue()`, `GetRangeValue()`, `GetRangeMinimum()`, `GetRangeMaximum()`, `GetRangeSmallChange()`.

## Exceptions

| Exception | Base | Key Properties |
|-----------|------|----------------|
| `BrinellException` | `Exception` | Root exception |
| `AssertionException` | `BrinellException` | `Expected`, `Actual`, `ControlLocator` |
| `ElementNotFoundException` | `BrinellException` | `LocatorString`, `LocatorInfo` |
| `WaitTimeoutException` | `BrinellException` | `TimeoutMs`, `Condition` |
| `LocatorNotSupportedException` | `BrinellException` | `Strategy`, `DriverName` |
| `PageLoadException` | `Exception` | Page load failure |

## Locator System

| Strategy | Typical Platform |
|----------|-----------------|
| `AutomationId` | MAUI, WPF |
| `AccessibilityId` | MAUI (mobile) |
| `Id` | General |
| `Name` | General |
| `XPath` | All |
| `Css` | Blazor/HTML |
| `Text` | General |
| `ClassName` | General |
| `TagName` | HTML |
| `DataTestId` | Blazor |
| `DataAutomationId` | Blazor |
| `LinkText` | Blazor |
| `PartialLinkText` | Blazor |
| `ControlType` | WPF |
