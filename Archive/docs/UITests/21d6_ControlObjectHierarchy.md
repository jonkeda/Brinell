# 6. ControlObject Hierarchy

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d6_ControlObjectHierarchy_CodeExamples.md](21d6_ControlObjectHierarchy_CodeExamples.md)  
**Previous:** [Multi-Platform Support](21d5_MultiPlatformSupport.md)  
**Version:** 3.0 (Updated December 2025)

---

## 6.1 Overview

The ControlObject hierarchy is **platform-specific**, not shared. Each platform (WPF, MAUI, HTML) implements its own base class hierarchy using native driver access.

**Key Principles (v3):**
- Core defines **interfaces only** (`IControlObject`, `ITextControl`, `IToggleControl`, etc.)
- Each platform implements base classes in its own project
- Base classes use native drivers directly (FlaUI, Appium, Selenium)
- No shared base classes in Core project

---

## 6.2 Core Interfaces

Core defines the contract that all platforms must implement:

```
IControlObject                          # Base for all controls
│   Is/Wait/Check/Assert for existence, visibility, enabled, clickable
│
├── ITextControl                        # Text input capability
│   │   EnterText(), Clear(), GetText(), SetText()
│   │
├── IToggleControl                      # Toggle/checkbox capability
│   │   Toggle(), SetChecked(), IsChecked()
│   │
├── ISelectorControl                    # Selection capability
│   │   SelectItem(), GetSelectedItem()
│   │
├── IRangeControl                       # Range/slider capability
│   │   GetValue(), SetValue(), GetMinimum(), GetMaximum()
│   │
├── IItemsControl                       # Collection capability
│   │   GetItemCount(), GetItems(), GetItem()
│   │
└── IContentControl                     # Clickable content
        Click(), DoubleClick()
```

---

## 6.3 Platform-Specific Base Class Hierarchies

Each platform defines its own inheritance hierarchy that implements Core interfaces.

### 6.3.1 WPF Base Classes (FlaUI)

```
ControlBase : IControlObject
│   Uses FlaUI AutomationElement directly
│   Is/Wait/Check/Assert methods - ALL VIRTUAL
│
├── ContentControlBase : IContentControl
│   │   Click(), DoubleClick()
│   │
│   ├── ButtonControl
│   ├── LinkControl
│   └── ImageButtonControl
│
├── TextControlBase : ITextControl
│   │   EnterText(), Clear(), GetText()
│   │
│   ├── TextBoxControl
│   └── RichTextBoxControl
│
├── ToggleControlBase : IToggleControl
│   │   Toggle(), SetChecked(), IsChecked()
│   │
│   └── CheckBoxControl
│
├── SelectorControlBase : ISelectorControl
│   │   SelectItem(), GetSelectedItem()
│   │
│   ├── ComboBoxControl
│   └── ListBoxControl
│
├── RangeControlBase : IRangeControl
│   │   GetValue(), SetValue()
│   │
│   └── SliderControl
│
└── ItemsControlBase : IItemsControl
        GetItemCount(), GetItems()
```

### 6.3.2 MAUI Base Classes (Appium)

```
ControlBase : IControlObject
│   Uses Appium WindowsElement/AndroidElement/IOSElement directly
│   Is/Wait/Check/Assert methods - ALL VIRTUAL
│
├── ContentControlBase : IContentControl
│   │   Click(), Tap(), LongPress()
│   │
│   ├── ButtonControl
│   └── ImageButtonControl
│
├── TextControlBase : ITextControl
│   │   EnterText(), Clear(), GetText()
│   │
│   └── EntryControl
│
├── ToggleControlBase : IToggleControl
│   │   Toggle(), SetChecked()
│   │
│   ├── CheckBoxControl
│   └── SwitchControl
│
├── SelectorControlBase : ISelectorControl
│   │   SelectItem(), GetSelectedItem()
│   │
│   └── PickerControl
│
└── ItemsControlBase : IItemsControl
        GetItemCount(), GetItems()
        
        ├── CollectionViewControl
        └── ListViewControl
```

### 6.3.3 HTML Base Classes (Selenium)

```
ControlBase : IControlObject
│   Uses Selenium IWebElement directly
│   Is/Wait/Check/Assert methods - ALL VIRTUAL
│
├── ContentControlBase : IContentControl
│   │   Click(), DoubleClick()
│   │
│   ├── ButtonControl
│   ├── LinkControl
│   └── ImageControl
│
├── TextControlBase : ITextControl
│   │   EnterText(), Clear(), GetText()
│   │
│   ├── InputControl
│   └── TextAreaControl
│
├── ToggleControlBase : IToggleControl
│   │   Toggle(), SetChecked()
│   │
│   └── CheckboxControl
│
├── SelectorControlBase : ISelectorControl
│   │   SelectItem(), GetSelectedItem()
│   │
│   └── SelectControl
│
└── ItemsControlBase : IItemsControl
        GetItemCount(), GetItems()
        
        ├── UnorderedListControl
        └── TableControl
```

---

## 6.4 Virtual Methods Design

### 6.4.1 Why Virtual?

All base class methods are `virtual` for:
- **Platform-specific overrides** - Different automation behaviors
- **Test customization** - Extended logging, retries
- **Consistent extensibility** - No sealed methods blocking inheritance

### 6.4.2 Method Categories (on ControlBase)

| Category | Methods | All Virtual |
|----------|---------|-------------|
| State Checks | `IsExists()`, `IsVisible()`, `IsEnabled()`, `IsClickable()` | ✅ |
| Waits | `WaitExists()`, `WaitVisible()`, `WaitEnabled()`, `WaitClickable()` | ✅ |
| Assertions | `CheckExists()`, `CheckVisible()`, `CheckEnabled()`, `CheckClickable()` | ✅ |
| Value Assertions | `AssertExists()`, `AssertVisible()`, `AssertText()`, etc. | ✅ |

---

## 6.5 IControlObject Methods (Implemented by ControlBase)

### 6.4.1 Existence Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `IsExists()` | `bool` | Immediate check if element exists |
| `WaitExists(expected, timeout)` | `bool` | Wait for existence state |
| `CheckExists(expected, timeout)` | `void` | Wait + throw on failure |
| `AssertExists(expected, timeout)` | `void` | Semantic assertion with logging |

### 6.4.2 Visibility Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `IsVisible()` | `bool` | Immediate visibility check |
| `WaitVisible(expected, timeout)` | `bool` | Wait for visibility state |
| `CheckVisible(expected, timeout)` | `void` | Wait + throw on failure |
| `AssertVisible(expected, timeout)` | `void` | Semantic assertion with logging |

### 6.4.3 Enabled Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `IsEnabled()` | `bool` | Immediate enabled check |
| `WaitEnabled(expected, timeout)` | `bool` | Wait for enabled state |
| `CheckEnabled(expected, timeout)` | `void` | Wait + throw on failure |
| `AssertEnabled(expected, timeout)` | `void` | Semantic assertion with logging |

### 6.4.4 Clickable Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `IsClickable()` | `bool` | `IsVisible() && IsEnabled()` |
| `WaitClickable(timeout)` | `bool` | Wait for clickable state |
| `CheckClickable(timeout)` | `void` | Wait + throw on failure |
| `AssertClickable(timeout)` | `void` | Semantic assertion with logging |

---

## 6.6 ITextControl Methods (Implemented by TextControlBase)

**CRITICAL:** All action methods call `CheckEnabled()` first.

| Method | Description |
|--------|-------------|
| `EnterText(text)` | Calls `CheckEnabled()`, clears, enters text |
| `Clear()` | Calls `CheckEnabled()`, clears content |
| `SetText(text)` | Sets text value directly |
| `GetText()` | Gets current input value |

---

## 6.7 IToggleControl Methods (Implemented by ToggleControlBase)

**CRITICAL:** All action methods call `CheckClickable()` first.

| Method | Returns | Description |
|--------|---------|-------------|
| `IsChecked()` | `bool` | Get current toggle state |
| `Toggle()` | `void` | Calls `CheckClickable()`, toggles |
| `SetChecked(value)` | `void` | Sets to specific state (checks current first) |
| `WaitChecked(expected, timeout)` | `bool` | Wait for toggle state |
| `AssertChecked(expected, timeout)` | `void` | Assert toggle state |

---

## 6.8 IContentControl Methods (Implemented by ContentControlBase)

**CRITICAL:** All action methods call `CheckClickable()` first.

| Method | Description |
|--------|-------------|
| `Click()` | Calls `CheckClickable()`, then clicks |
| `DoubleClick()` | Calls `CheckClickable()`, then double-clicks |

---

## 6.9 ISelectorControl Methods (Implemented by SelectorControlBase)

| Method | Returns | Description |
|--------|---------|-------------|
| `SelectItem(index)` | `void` | Selects item by index |
| `SelectItem(text)` | `void` | Selects item by text |
| `GetSelectedItem()` | `string` | Gets selected item text |
| `GetSelectedIndex()` | `int` | Gets selected item index |

---

## 6.10 IItemsControl Methods (Implemented by ItemsControlBase)

| Method | Returns | Description |
|--------|---------|-------------|
| `GetItemCount()` | `int` | Number of items |
| `GetItems()` | `IReadOnlyList<string>` | All item texts |
| `GetItem(index)` | `string` | Item text at index |
| `WaitItemCount(expected, timeout)` | `bool` | Wait for item count |
| `AssertItemCount(expected, timeout)` | `void` | Assert item count |
| `AssertItemExists(text, timeout)` | `void` | Assert item with text exists |

---

## 6.11 IRangeControl Methods (Implemented by RangeControlBase)

| Method | Returns | Description |
|--------|---------|-------------|
| `GetValue()` | `double` | Current value |
| `SetValue(value)` | `void` | Set value |
| `GetMinimum()` | `double` | Minimum value |
| `GetMaximum()` | `double` | Maximum value |

---

## 6.12 Control Mapping by Platform

| WPF Control | MAUI Control | HTML Element | Core Interface |
|-------------|--------------|--------------|----------------|
| `Button` | `Button` | `<button>` | `IContentControl` |
| `TextBlock` | `Label` | `<span>`, `<p>` | `IControlObject` |
| `TextBox` | `Entry` | `<input type="text">` | `ITextControl` |
| `CheckBox` | `CheckBox` | `<input type="checkbox">` | `IToggleControl` |
| `ToggleButton` | `Switch` | `<input type="checkbox">` | `IToggleControl` |
| `ListBox` | `CollectionView` | `<ul>`, `<ol>` | `IItemsControl` |
| `ComboBox` | `Picker` | `<select>` | `ISelectorControl` |
| `Slider` | `Slider` | `<input type="range">` | `IRangeControl` |
| `ProgressBar` | `ProgressBar` | `<progress>` | `IRangeControl` |
| `Hyperlink` | N/A | `<a>` | `IContentControl` |

---

## 6.13 Always Check Before Action Pattern

Every action method in platform base classes follows this pattern:

```
Action Method (e.g., Click() in ContentControlBase)
    │
    ├── 1. Check precondition (CheckClickable/CheckEnabled)
    │       ├── Wait for condition
    │       └── Throw AssertionException if not met
    │
    ├── 2. Perform action via native driver
    │       └── e.g., FlaUI AutomationElement.Click()
    │       └── e.g., Appium element.Click()
    │       └── e.g., Selenium IWebElement.Click()
    │
    └── 3. Log action result
```

### 6.13.1 Example Flow (WPF)

```csharp
// User calls
button.Click();

// In WPF ContentControlBase:
// 1. CheckClickable() → WaitClickable() → WaitVisible(true) + WaitEnabled(true)
// 2. If not clickable after timeout → throw AssertionException
// 3. _automationElement.Click()  // Direct FlaUI call
// 4. Logger.LogAction("Click", ...)
```

---

## 6.14 Why Platform-Specific Base Classes?

**Previous Architecture (v1-v2):** Shared base classes in Core with adapter abstraction.

**Current Architecture (v3):** Platform-specific base classes with native driver access.

### Benefits:
- **Performance** - No adapter indirection
- **Flexibility** - Platform-specific optimizations without compromises
- **Simplicity** - Each platform project is self-contained
- **Native Features** - Direct access to platform-specific capabilities

### Trade-offs:
- Code duplication across platforms (minimal - mostly boilerplate)
- No shared base class testing (each platform tests its own hierarchy)

---

*Next: [Wait/Check/Is/Assert Pattern](21d7_WaitCheckIsAssertPattern.md)*
