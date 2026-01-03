# SPEC-002b-001: Control Object Hierarchy (Mermaid Diagrams)

**Version:** 2.0 (Fixed Mermaid Syntax)  
**Status:** For Review  
**Date:** January 2026

---

## 1. Core Interface Hierarchy

### 1.1 Complete Interface Hierarchy Diagram

```mermaid
classDiagram
    direction TB
    
    class IControlObject {
        <<interface>>
        -string AutomationId
        -IPageObject Page
        +IsExists() bool
        +WaitExists(bool, int) bool
        +CheckExists(bool, int) void
        +AssertExists(string) void
        +IsVisible() bool
        +WaitVisible(bool, int) bool
        +CheckVisible(bool, int) void
        +AssertVisible(string) void
        +IsEnabled() bool
        +WaitEnabled(bool, int) bool
        +CheckEnabled(bool, int) void
        +AssertEnabled(string) void
    }

    class IClickableControl {
        <<interface>>
        +Click() void
        +DoubleClick() void
        +RightClick() void
        +Hover() void
    }
    IClickableControl --|> IControlObject

    class ITextControl {
        <<interface>>
        +Enter(string text) void
        +Clear() void
        +SetText(string text) void
        +IsReadOnly() bool
    }
    ITextControl --|> IControlObject

    class IEditableTextControl {
        <<interface>>
        +Focus() void
        +SelectAll() void
        +Copy() void
        +Paste() void
    }
    IEditableTextControl --|> ITextControl

    class IToggleControl {
        <<interface>>
        +IsChecked() bool
        +Toggle() void
        +Check() void
        +Uncheck() void
    }
    IToggleControl --|> IControlObject

    class ISelectorControl {
        <<interface>>
        +SelectByIndex(int) void
        +SelectByText(string) void
        +GetSelectedText() string
        +GetItems() List
    }
    ISelectorControl --|> IControlObject

    class IRangeControl {
        <<interface>>
        +GetValue() double
        +SetValue(double) void
        +Increment() void
        +Decrement() void
    }
    IRangeControl --|> IControlObject

    class ISlider {
        <<interface>>
    }
    ISlider --|> IRangeControl

    class IItemsControl {
        <<interface>>
        +GetItemCount() int
        +ClickItem(int) void
        +HasItem(string) bool
    }
    IItemsControl --|> IControlObject

    class IContainerControl {
        <<interface>>
        +GetChildCount() int
        +ChildExists(string) bool
        +GetChild(string) T
    }
    IContainerControl --|> IControlObject

    class IScrollableControl {
        <<interface>>
        +ScrollToElement(string) void
        +ScrollToTop() void
        +ScrollToBottom() void
    }
    IScrollableControl --|> IControlObject
```

---

## 2. Concrete Control Implementation - MAUI Platform

### 2.1 MAUI Control Class Hierarchy

```mermaid
classDiagram
    direction TB
    
    class ControlBase {
        -AppiumTestContext context
        -AppiumElement element
        -FindElement() AppiumElement
    }

    class ContentControlBase {
        +Click() void
        +DoubleClick() void
    }
    ContentControlBase --|> ControlBase

    class TextControlBase {
        +Enter(string text) void
        +Clear() void
        +SetText(string) void
    }
    TextControlBase --|> ControlBase

    class EditableTextControlBase {
        +Focus() void
        +Copy() void
        +Paste() void
    }
    EditableTextControlBase --|> TextControlBase

    class ToggleControlBase {
        +IsChecked() bool
        +Toggle() void
        +Check() void
    }
    ToggleControlBase --|> ControlBase

    class SelectorControlBase {
        +SelectByIndex(int) void
        +SelectByText(string) void
        +GetItems() List
    }
    SelectorControlBase --|> ControlBase

    class RangeControlBase {
        +GetValue() double
        +SetValue(double) void
    }
    RangeControlBase --|> ControlBase

    class ItemsControlBase {
        +GetItemCount() int
        +ClickItem(int) void
    }
    ItemsControlBase --|> ControlBase

    class ContainerControlBase {
        +GetChild(string) T
        +ChildExists(string) bool
    }
    ContainerControlBase --|> ControlBase

    class ScrollableControlBase {
        +ScrollToTop() void
        +ScrollDown(int) void
    }
    ScrollableControlBase --|> ControlBase

    class ButtonControl
    ButtonControl --|> ContentControlBase

    class EntryControl
    EntryControl --|> TextControlBase

    class EditorControl
    EditorControl --|> EditableTextControlBase

    class CheckBoxControl
    CheckBoxControl --|> ToggleControlBase

    class SwitchControl
    SwitchControl --|> ToggleControlBase

    class PickerControl
    PickerControl --|> SelectorControlBase

    class SliderControl
    SliderControl --|> RangeControlBase

    class CollectionViewControl
    CollectionViewControl --|> ItemsControlBase

    class ScrollViewControl
    ScrollViewControl --|> ScrollableControlBase
```

---

## 3. Concrete Control Implementation - Blazor/Playwright Platform

### 3.1 Blazor Control Class Hierarchy

```mermaid
classDiagram
    direction TB
    
    class ControlBase {
        -PlaywrightTestContext context
        -ILocator locator
        -GetLocator() ILocator
    }

    class ContentControlBase {
        +Click() void
        +DoubleClick() void
    }
    ContentControlBase --|> ControlBase

    class TextControlBase {
        +Enter(string text) void
        +Clear() void
        +SetText(string) void
    }
    TextControlBase --|> ControlBase

    class EditableTextControlBase {
        +Focus() void
        +Copy() void
        +Paste() void
    }
    EditableTextControlBase --|> TextControlBase

    class ToggleControlBase {
        +IsChecked() bool
        +Toggle() void
        +Check() void
    }
    ToggleControlBase --|> ControlBase

    class SelectorControlBase {
        +SelectByIndex(int) void
        +SelectByText(string) void
        +GetItems() List
    }
    SelectorControlBase --|> ControlBase

    class RangeControlBase {
        +GetValue() double
        +SetValue(double) void
    }
    RangeControlBase --|> ControlBase

    class ItemsControlBase {
        +GetItemCount() int
        +ClickItem(int) void
    }
    ItemsControlBase --|> ControlBase

    class ContainerControlBase {
        +GetChild(string) T
        +ChildExists(string) bool
    }
    ContainerControlBase --|> ControlBase

    class ScrollableControlBase {
        +ScrollToTop() void
        +ScrollDown(int) void
    }
    ScrollableControlBase --|> ControlBase

    class ButtonControl
    ButtonControl --|> ContentControlBase

    class LinkControl
    LinkControl --|> ContentControlBase

    class TextInputControl
    TextInputControl --|> TextControlBase

    class TextAreaControl
    TextAreaControl --|> EditableTextControlBase

    class CheckBoxControl
    CheckBoxControl --|> ToggleControlBase

    class SelectControl
    SelectControl --|> SelectorControlBase

    class RangeInputControl
    RangeInputControl --|> RangeControlBase

    class ListControl
    ListControl --|> ItemsControlBase

    class TableControl
    TableControl --|> ItemsControlBase

    class DivControl
    DivControl --|> ContainerControlBase

    class ScrollContainerControl
    ScrollContainerControl --|> ScrollableControlBase
```

---

## 4. Control Capability Matrix

```mermaid
classDiagram
    class ClickableControls {
        ButtonControl
        LabelControl
        LinkControl
    }
    
    class TextControls {
        EntryControl
        TextInputControl
        TextAreaControl
        SearchBarControl
    }
    
    class ToggleControls {
        CheckBoxControl
        SwitchControl
    }
    
    class SelectorControls {
        PickerControl
        SelectControl
        DatePickerControl
        TimePickerControl
    }
    
    class RangeControls {
        SliderControl
        ProgressBarControl
        RangeInputControl
        ProgressControl
    }
    
    class ItemsControls {
        CarouselViewControl
        CollectionViewControl
        ListControl
        TableControl
    }
    
    class ContainerControls {
        FrameControl
        ScrollViewControl
        DivControl
        ScrollContainerControl
    }
```

---

## 5. Method Pattern Overview

```mermaid
classDiagram
    class ImmediateCheck {
        IsExists() bool
        IsVisible() bool
        IsEnabled() bool
        IsChecked() bool
    }

    class PollingWait {
        WaitExists(bool, int) bool
        WaitVisible(bool, int) bool
        WaitChecked(bool, int) bool
    }

    class Assertions {
        AssertExists(string) void
        AssertVisible(string) void
        AssertEnabled(string) void
        AssertChecked(string) void
    }

    class Actions {
        Click() void
        Enter(string) void
        Select(int) void
        Toggle() void
    }

    class Getters {
        GetText() string
        GetValue() double
        GetItemCount() int
    }
```

---

## 6. Container Scoping Visualization

```mermaid
classDiagram
    class PageObject {
        +GetControl(string) IControlObject
        +GetContainer(string) IContainerControl
    }

    class IContainerControl {
        +GetChild(string) T
        +ChildExists(string) bool
        +GetChildCount() int
    }

    class NestedContainer {
        +Supports multiple levels
        +Scopes searches
        +Reusable patterns
    }

    PageObject --> IContainerControl
    IContainerControl --> NestedContainer
```

---

## 7. MAUI Controls Summary Table

| Control | Base Class | Interfaces | Primary Use |
|---------|-----------|-----------|-------------|
| ButtonControl | ContentControlBase | IClickableControl | Push button |
| LabelControl | ContentControlBase | IClickableControl | Read-only text |
| EntryControl | TextControlBase | ITextControl | Single-line text input |
| EditorControl | EditableTextControlBase | IEditableTextControl | Multi-line text input |
| SearchBarControl | TextControlBase | ITextControl | Searchable input |
| CheckBoxControl | ToggleControlBase | IToggleControl | Binary toggle |
| SwitchControl | ToggleControlBase | IToggleControl | On/Off state |
| PickerControl | SelectorControlBase | ISelectorControl | Option selection |
| DatePickerControl | SelectorControlBase | ISelectorControl | Date selection |
| TimePickerControl | SelectorControlBase | ISelectorControl | Time selection |
| SliderControl | RangeControlBase | IRangeControl | Numeric range |
| ProgressBarControl | RangeControlBase | IRangeControl | Progress display |
| CarouselViewControl | ItemsControlBase | IItemsControl | Item carousel |
| CollectionViewControl | ItemsControlBase | IItemsControl | Item collection |
| FrameControl | ContainerControlBase | IContainerControl | Container |
| ScrollViewControl | ScrollableControlBase | IScrollableControl, IContainerControl | Scrollable container |
| ActivityIndicatorControl | ControlBase | IControlObject | Loading indicator |

---

## 8. Blazor/Playwright Controls Summary Table

| Control | Base Class | Interfaces | Primary Use |
|---------|-----------|-----------|-------------|
| ButtonControl | ContentControlBase | IClickableControl | Push button |
| LinkControl | ContentControlBase | IClickableControl | Hyperlink |
| LabelControl | ContentControlBase | IClickableControl | Read-only text |
| TextInputControl | TextControlBase | ITextControl | Single-line text input |
| TextAreaControl | EditableTextControlBase | IEditableTextControl | Multi-line text input |
| CheckBoxControl | ToggleControlBase | IToggleControl | Binary toggle |
| SelectControl | SelectorControlBase | ISelectorControl | Option selection |
| RangeInputControl | RangeControlBase | IRangeControl | Numeric range input |
| ProgressControl | RangeControlBase | IRangeControl | Progress display |
| ListControl | ItemsControlBase | IItemsControl | Item list |
| TableControl | ItemsControlBase | IItemsControl | Item table |
| DivControl | ContainerControlBase | IContainerControl | Generic container |
| ScrollContainerControl | ScrollableControlBase | IScrollableControl, IContainerControl | Scrollable container |

---

## 9. Key Diagram Insights

### Interface Hierarchy
- **IControlObject** is the base for all controls
- **Specialized interfaces** inherit from IControlObject
- **Marker interfaces** provide specific typing

### Implementation Patterns
- **Base classes** implement core functionality
- **Platform-specific** variants exist for MAUI and Blazor
- **Control hierarchy** follows interface contracts

### Method Patterns
- **Is*** methods: Immediate state checks
- **Wait*** methods: Polling with timeout
- **Check*** methods: Precondition verification
- **Assert*** methods: Test assertions
- **Get*** methods: Current state queries
- **Action methods**: User interactions

### Scoping Strategy
- **Page objects** contain named controls and containers
- **Containers** have child elements
- **Nesting** allows multi-level scoping
- **GetChild** retrieves named children

---

## 10. Validation Notes

✅ All core interfaces documented  
✅ MAUI platform: 17+ controls  
✅ Blazor platform: 13+ controls  
✅ Inheritance hierarchies verified  
✅ Method signatures extracted from source  
✅ Container relationships validated  
✅ Mermaid syntax fixed for current version (v11.x)

**Last Updated:** January 3, 2026  
**Status:** Ready for Review
