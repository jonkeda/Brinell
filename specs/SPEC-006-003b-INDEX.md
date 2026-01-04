# SPEC-006-003b: Complete Control Hierarchy Design

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026

---

## Overview

This document set defines the complete control class hierarchy for MAUI and Blazor implementations. The hierarchy provides:

1. **Base classes with virtual methods** - for platform-specific overrides
2. **String constructors** - for convenient PageObject usage
3. **Logging integration** - for test debugging
4. **Consistent patterns** - across MAUI and Blazor

---

## Documents

| Document | Description | Controls |
|----------|-------------|----------|
| [SPEC-006-003b-FOUNDATION](SPEC-006-003b-FOUNDATION.md) | Core base classes | ControlObjectBase, ClickableControlBase, TextControlBase |
| [SPEC-006-003b-TOGGLE](SPEC-006-003b-TOGGLE.md) | Toggle controls | ToggleControlBase, CheckBoxControl, SwitchControl, RadioButtonControl |
| [SPEC-006-003b-SELECTION](SPEC-006-003b-SELECTION.md) | Selection controls | SelectorControlBase, PickerControl, MultiSelectorControl |
| [SPEC-006-003b-RANGE](SPEC-006-003b-RANGE.md) | Range controls | RangeControlBase, SliderControl, StepperControl |
| [SPEC-006-003b-DATETIME](SPEC-006-003b-DATETIME.md) | DateTime controls | DateControlBase, TimeControlBase, DatePickerControl, TimePickerControl |
| [SPEC-006-003b-COLLECTION](SPEC-006-003b-COLLECTION.md) | Collection controls | ItemsControlBase, SelectableItemsControlBase, ListViewControl, CollectionViewControl |
| [SPEC-006-003b-CONTAINER](SPEC-006-003b-CONTAINER.md) | Container controls | ContainerControlBase, ScrollViewControl, ExpanderControl, RefreshViewControl |
| [SPEC-006-003b-DISPLAY](SPEC-006-003b-DISPLAY.md) | Display controls | LabelControl, ImageControl, ProgressBarControl, ActivityIndicatorControl |
| [SPEC-006-003b-NAVIGATION](SPEC-006-003b-NAVIGATION.md) | Navigation controls | TabControlBase, MenuControl, FlyoutControl, ToolbarControl |
| [SPEC-006-003b-MEDIA](SPEC-006-003b-MEDIA.md) | Media controls | MediaControlBase, WebViewControl |
| [SPEC-006-003b-PAGE](SPEC-006-003b-PAGE.md) | Page objects | PageObjectBase, BusyPageBase, AsyncPageObjectBase |

---

## Design Principles

### 1. Virtual Methods for Override

All action methods (Click, Enter, Toggle, etc.) are `virtual` to allow:
- Platform-specific customization
- Control-specific behavior overrides
- Test framework extensions

### 2. String Constructor Convenience

All controls support simple string construction:
```csharp
// Simple - uses AutomationId (MAUI) or TestId (Blazor)
var button = new ButtonControl(context, "SubmitButton", page);

// Full - explicit locator
var button = new ButtonControl(context, By.XPath("//button[@type='submit']"), page);
```

### 3. Logging at Every Level

All operations log via `Log()`:
```csharp
public virtual void Click(int? timeoutMs = null)
{
    Log("Click()");  // Always log
    CheckVisible(true, timeoutMs);
    CheckEnabled(true, timeoutMs);
    FindElementRequired(timeoutMs).Click();
}
```

### 4. No Factory Pattern

Controls are instantiated with `new`:
```csharp
// PageObject pattern
public class LoginPage : PageObjectBase
{
    public ButtonControl SubmitButton => new(Context, "SubmitBtn", this);
    public EntryControl UsernameEntry => new(Context, "Username", this);
}
```

---

## Complete Hierarchy Diagram

### MAUI (Sync)

```
ControlObjectBase : IInteractiveControlObject
│   ├── Locator, Page, Context
│   ├── Log()
│   ├── FindElement(), FindElementRequired()
│   ├── IsExists(), IsVisible(), IsEnabled(), GetText()
│   ├── WaitExists(), WaitVisible(), WaitEnabled()
│   ├── CheckExists(), CheckVisible(), CheckEnabled()
│   └── AssertExists(), AssertVisible(), AssertEnabled(), AssertText()
│
├── LabelControl : ILabelControlObject
│
├── ClickableControlBase : IClickableControlObject
│   ├── virtual Click(), DoubleClick(), RightClick()
│   ├── virtual Hover(), LongPress()
│   │
│   ├── ButtonControl
│   ├── ImageButtonControl
│   ├── LinkControl
│   │
│   └── TextControlBase : ITextControlObject, IFocusableControlObject
│       ├── virtual IsFocused(), Focus(), Blur()
│       ├── virtual Enter(), Clear(), ClearAndEnter(), Append()
│       ├── virtual IsReadOnly(), GetTextLength()
│       │
│       ├── EntryControl
│       ├── EditorControl
│       └── SearchBarControl : ISearchControlObject
│
├── ToggleControlBase : IToggleControlObject
│   ├── virtual IsChecked(), Toggle(), SetChecked()
│   │
│   ├── CheckBoxControl : ICheckBoxControlObject
│   ├── SwitchControl : ISwitchControlObject
│   └── RadioButtonControl : IRadioButtonControlObject
│
├── SelectorControlBase : ISelectorControlObject
│   ├── virtual SelectByIndex(), SelectByText(), SelectByValue()
│   ├── virtual GetSelectedIndex(), GetSelectedText(), GetItems()
│   │
│   ├── PickerControl : IPickerControlObject
│   └── MultiSelectorControlBase : IMultiSelectorControlObject
│       └── CollectionViewControl (multi-select mode)
│
├── RangeControlBase : IRangeControlObject
│   ├── virtual GetValue(), SetValue(), Increment(), Decrement()
│   ├── virtual GetMinimum(), GetMaximum(), GetStep()
│   │
│   ├── SliderControl : ISliderControlObject
│   └── StepperControl : IStepperControlObject
│
├── DateControlBase : IDateControlObject
│   ├── virtual GetDate(), SetDate(), OpenPicker(), ClosePicker()
│   └── DatePickerControl
│
├── TimeControlBase : ITimeControlObject
│   ├── virtual GetTime(), SetTime(), OpenPicker(), ClosePicker()
│   └── TimePickerControl
│
├── ItemsControlBase : IItemsControlObject
│   ├── virtual GetItemCount(), GetItemText(), ClickItem()
│   │
│   ├── SelectableItemsControlBase : ISelectableItemsControlObject
│   │   ├── virtual SelectItem(), GetSelectedItemIndex()
│   │   │
│   │   └── ListViewControl
│   │
│   ├── ScrollableItemsControlBase : IScrollableItemsControlObject
│   │   ├── virtual ScrollToItem(), ScrollToTop(), ScrollToBottom()
│   │   └── CollectionViewControl
│   │
│   └── GroupedItemsControlBase : IGroupedItemsControlObject
│       ├── virtual GetGroupCount(), ExpandGroup(), CollapseGroup()
│       └── GroupedListViewControl
│
├── ContainerControlBase : IListContainerControlObject
│   ├── virtual GetChildCount(), GetChild(), GetAllChildren()
│   │
│   ├── ScrollViewControl : IScrollableControlObject
│   ├── ExpanderControl : IExpanderControlObject
│   ├── RefreshViewControl : IRefreshableControlObject
│   └── SwipeViewControl : ISwipeableControlObject
│
├── ProgressControlBase : IProgressControlObject
│   ├── virtual GetProgress(), IsIndeterminate()
│   │
│   ├── ProgressBarControl
│   └── ActivityIndicatorControl : IActivityIndicatorControlObject
│
├── ImageControl : IImageControlObject
│   └── virtual GetSource(), IsLoaded(), GetWidth(), GetHeight()
│
├── TabControlBase : ITabControlObject
│   ├── virtual GetTabCount(), SelectTab(), GetSelectedTabIndex()
│   └── TabbedPageControl
│
├── MenuControlBase : IMenuControlObject
│   ├── virtual Open(), Close(), ClickMenuItem()
│   └── FlyoutControl : IFlyoutControlObject
│
├── MediaControlBase : IMediaControlObject
│   ├── virtual Play(), Pause(), Stop(), Seek()
│   ├── virtual GetDuration(), GetPosition(), GetVolume()
│   └── MediaElementControl
│
└── WebViewControl : IWebViewControlObject
    └── virtual Navigate(), Reload(), GoBack(), GoForward()
```

### Blazor (Async)

```
AsyncControlObjectBase : IAsyncControlObject
│   ├── Locator, Page, Context
│   ├── Log()
│   ├── GetLocator() -> ILocator
│   ├── IsExistsAsync(), IsVisibleAsync(), IsEnabledAsync(), GetTextAsync()
│   ├── WaitExistsAsync(), WaitVisibleAsync(), WaitEnabledAsync()
│   ├── CheckExistsAsync(), CheckVisibleAsync(), CheckEnabledAsync()
│   └── AssertExistsAsync(), AssertVisibleAsync(), AssertTextAsync()
│
├── LabelControl
│
├── AsyncClickableControlBase : IAsyncClickableControlObject
│   ├── virtual ClickAsync(), DoubleClickAsync(), RightClickAsync()
│   ├── virtual HoverAsync()
│   │
│   ├── ButtonControl
│   ├── LinkControl
│   │
│   └── AsyncTextControlBase : IAsyncTextControlObject
│       ├── virtual IsFocusedAsync(), FocusAsync(), BlurAsync()
│       ├── virtual EnterAsync(), ClearAsync(), ClearAndEnterAsync()
│       │
│       ├── InputControl
│       └── TextAreaControl
│
├── AsyncToggleControlBase : IAsyncToggleControlObject
│   ├── virtual IsCheckedAsync(), ToggleAsync(), SetCheckedAsync()
│   │
│   ├── CheckBoxControl
│   └── RadioButtonControl
│
├── AsyncSelectorControlBase : IAsyncSelectorControlObject
│   ├── virtual SelectByIndexAsync(), SelectByTextAsync()
│   ├── virtual GetSelectedIndexAsync(), GetItemsAsync()
│   │
│   └── SelectControl
│
├── AsyncRangeControlBase : IAsyncRangeControlObject
│   ├── virtual GetValueAsync(), SetValueAsync()
│   │
│   └── RangeInputControl
│
├── AsyncDateControlBase
│   └── DateInputControl
│
├── AsyncTimeControlBase
│   └── TimeInputControl
│
├── AsyncItemsControlBase
│   ├── virtual GetItemCountAsync(), ClickItemAsync()
│   │
│   ├── ListControl (<ul>, <ol>)
│   └── TableControl
│
├── AsyncContainerControlBase
│   └── DivContainerControl
│
├── ProgressControl
├── ImageControl
│
├── AsyncTabControlBase
│   └── TabControl (component-based)
│
├── NavMenuControl
│
└── AsyncMediaControlBase
    ├── VideoControl
    ├── AudioControl
    └── IFrameControl
```

---

## Interface to Base Class Mapping

| Interface | MAUI Base Class | Blazor Base Class |
|-----------|-----------------|-------------------|
| IControlObject | ControlObjectBase | AsyncControlObjectBase |
| IInteractiveControlObject | ControlObjectBase | AsyncControlObjectBase |
| IClickableControlObject | ClickableControlBase | AsyncClickableControlBase |
| ITextControlObject | TextControlBase | AsyncTextControlBase |
| IToggleControlObject | ToggleControlBase | AsyncToggleControlBase |
| ISelectorControlObject | SelectorControlBase | AsyncSelectorControlBase |
| IRangeControlObject | RangeControlBase | AsyncRangeControlBase |
| IDateControlObject | DateControlBase | AsyncDateControlBase |
| ITimeControlObject | TimeControlBase | AsyncTimeControlBase |
| IItemsControlObject | ItemsControlBase | AsyncItemsControlBase |
| IContainerControlObject | ContainerControlBase | AsyncContainerControlBase |
| IProgressControlObject | ProgressControlBase | ProgressControl |
| ITabControlObject | TabControlBase | AsyncTabControlBase |
| IMenuControlObject | MenuControlBase | NavMenuControl |
| IMediaControlObject | MediaControlBase | AsyncMediaControlBase |
| IWebViewControlObject | WebViewControl | IFrameControl |

---

## Default Locator Strategy

| Platform | Default Strategy | HTML/XAML Attribute |
|----------|-----------------|---------------------|
| MAUI | `By.AutomationId(string)` | `AutomationId="..."` |
| Blazor | `By.TestId(string)` | `data-testid="..."` |

---

## Next Steps

1. Review each category document for implementation details
2. Implement base classes per category
3. Add concrete controls as needed
4. Update POC code with new base classes
