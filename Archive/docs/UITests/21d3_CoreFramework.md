# 3. Core Framework

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d3_CoreFramework_CodeExamples.md](21d3_CoreFramework_CodeExamples.md)  
**Previous:** [Architecture](21d2_Architecture.md)  
**Version:** 3.0 (Updated December 2025)

---

## 3.1 Platform Enum

Replaces string-based platform identification and removes `IsWindows`/`IsMobile` boolean properties.

### 3.1.1 Enum Values

| Value | Description | Automation Library |
|-------|-------------|-------------------|
| `Platform.Windows` | Windows desktop WPF | FlaUI (UIA3) |
| `Platform.WindowsMaui` | Windows MAUI app | Appium |
| `Platform.Android` | Android mobile | Appium |
| `Platform.iOS` | iOS mobile | Appium |
| `Platform.Web` | Web browser HTML | Selenium |

### 3.1.2 Extension Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `IsMobile()` | `bool` | True for Android/iOS |
| `IsDesktop()` | `bool` | True for Windows/WindowsMaui |
| `IsWeb()` | `bool` | True for Web |
| `SupportsGestures()` | `bool` | True for mobile platforms |
| `IsWindowsDesktop()` | `bool` | True for Windows (WPF) |
| `IsMaui()` | `bool` | True for MAUI platforms |
| `GetAutomationLibrary()` | `string` | Library name for platform |

---

## 3.2 ITestContext Interface

Simplified platform-agnostic interface for logging, configuration, and waiting.  
**Element operations are in platform-specific contexts, not in ITestContext.**

### 3.2.1 Properties

| Property | Type | Description |
|----------|------|-------------|
| `TestName` | `string` | Current test name for logging |
| `Platform` | `Platform` | Platform enum value |
| `Logger` | `ITestLogger?` | CSV format logger (optional) |
| `DefaultTimeoutMs` | `int` | Default timeout (typically 10000ms) |
| `ShortTimeoutMs` | `int` | Short timeout for quick checks (typically 2000ms) |
| `PollingIntervalMs` | `int` | Wait loop polling interval (typically 100ms) |

### 3.2.2 Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `SetLogger(logger)` | `void` | Set the CSV logger |
| `Log(message)` | `void` | Log with test context prefix |
| `LogError(ex, context)` | `void` | Log error with exception details |
| `WaitFor(condition, timeout, description)` | `bool` | Generic polling wait |
| `TakeScreenshot(name)` | `string?` | Capture screenshot |

---

## 3.3 Control Interfaces

Core defines interfaces for control capabilities. Platform implementations provide concrete classes in their own base class hierarchies.

### 3.3.1 IControlObject (Base Interface)

All controls implement this interface for common state checking and assertions.

| Member | Type | Description |
|--------|------|-------------|
| `AutomationId` | `string` | Identifier for locating control |
| `Context` | `ITestContext?` | Test context reference |
| `Page` | `IPageObject?` | Parent page object |

**State Methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `IsExists()` | `bool` | Check if element exists |
| `IsVisible()` | `bool` | Check if element is visible |
| `IsEnabled()` | `bool` | Check if element is enabled |
| `GetText()` | `string` | Get element text content |

**Wait Methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `WaitExists(expected, timeout)` | `bool` | Wait for existence state |
| `WaitVisible(expected, timeout)` | `bool` | Wait for visibility state |
| `WaitEnabled(expected, timeout)` | `bool` | Wait for enabled state |

**Check Methods (throw on failure):**

| Method | Returns | Description |
|--------|---------|-------------|
| `CheckExists(expected, timeout)` | `void` | Check existence, throw if fails |
| `CheckVisible(expected, timeout)` | `void` | Check visibility, throw if fails |
| `CheckEnabled(expected, timeout)` | `void` | Check enabled, throw if fails |

**Assert Methods:**

| Method | Returns | Description |
|--------|---------|-------------|
| `AssertExists(message)` | `void` | Assert element exists |
| `AssertVisible(message)` | `void` | Assert element is visible |
| `AssertNotVisible(message)` | `void` | Assert element is not visible |
| `AssertEnabled(message)` | `void` | Assert element is enabled |
| `AssertDisabled(message)` | `void` | Assert element is disabled |
| `AssertTextEquals(expected, message)` | `void` | Assert text matches |
| `AssertTextContains(expected, message)` | `void` | Assert text contains |

### 3.3.2 ITextControl

Extends IControlObject for text input controls (TextBox, Entry, TextInput, TextArea).

| Method | Description |
|--------|-------------|
| `Enter(text)` | Enter text into control |
| `Clear()` | Clear control content |
| `ClearAndEnter(text)` | Clear then enter text |
| `SetText(text)` | Alias for ClearAndEnter |
| `Append(text)` | Append text to existing content |

### 3.3.3 IToggleControl

Extends IControlObject for checkbox/switch/radio controls.

| Method | Description |
|--------|-------------|
| `IsChecked()` | Get current checked state |
| `Toggle()` | Toggle the current state |
| `Check()` | Set to checked |
| `Uncheck()` | Set to unchecked |
| `SetChecked(value)` | Set specific state |
| `WaitChecked(expected, timeout)` | Wait for checked state |
| `AssertChecked(message)` | Assert is checked |
| `AssertUnchecked(message)` | Assert is unchecked |

### 3.3.4 ISelectorControl

Extends IControlObject for selection controls (ComboBox, Picker, Select).

| Method | Description |
|--------|-------------|
| `GetSelectedText()` | Get selected item text |
| `GetSelectedIndex()` | Get selected item index |
| `SelectByIndex(index)` | Select by position |
| `SelectByText(text)` | Select by display text |
| `WaitSelectedText(expected, timeout)` | Wait for selection |
| `AssertSelectedText(expected, message)` | Assert selection |

### 3.3.5 IRangeControl

Extends IControlObject for slider/progress/range controls.

| Method | Description |
|--------|-------------|
| `GetValue()` | Get current value |
| `GetMinimum()` | Get minimum value |
| `GetMaximum()` | Get maximum value |
| `SetValue(value)` | Set to specific value |
| `AssertValue(expected, tolerance, message)` | Assert value within tolerance |

### 3.3.6 IItemsControl

Extends IControlObject for list/collection controls.

| Method | Description |
|--------|-------------|
| `GetItems()` | Get all item texts |
| `GetItemCount()` | Get number of items |

### 3.3.7 IContentControl

Extends IControlObject for clickable content controls (Button, Label, Link).

| Method | Description |
|--------|-------------|
| `Click()` | Click the control |
| `DoubleClick()` | Double-click the control |
| `RightClick()` | Right-click the control |

---

## 3.4 IPageObject Interface

Interface for page objects. Each platform provides its own `PageBase` implementation.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Page identifier for logging |
| `Context` | `ITestContext` | Test context reference |

| Method | Returns | Description |
|--------|---------|-------------|
| `IsDisplayed()` | `bool` | Check if page is displayed |
| `WaitForDisplayed(timeout)` | `void` | Wait for page to display |
| `WaitForHidden(timeout)` | `void` | Wait for page to hide |

---

## 3.5 ITestLogger Interface

Structured logging for test execution.

### 3.5.1 Methods

| Method | Description |
|--------|-------------|
| `LogAction(...)` | Log a control action (click, type, etc.) |
| `LogAssert(...)` | Log an assertion result |
| `LogNavigation(...)` | Log page navigation |
| `LogError(...)` | Log an error |
| `LogInfo(...)` | Log informational message |

### 3.5.2 CSV Format

```
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
2025-12-22T10:15:30;NavigationTests.Navigate_To_Settings;Shell;SettingsButton;Click;;;Success;
2025-12-22T10:15:31;NavigationTests.Navigate_To_Settings;Settings;PageTitle;AssertText;Settings;Settings;Pass;
```

---

## 3.6 What's NOT in Core (v3 Changes)

The following have been **removed from Core** and are now in platform projects:

| Removed | Replacement |
|---------|-------------|
| `IDriverAdapter` | Platform contexts use native drivers directly |
| `IElementAdapter` | Platform contexts return native elements |
| `ControlObjectBase` | Platform-specific `ControlBase` classes |
| `PageObjectBase` | Platform-specific `PageBase` classes |
| `ContentControlBase` | Platform-specific versions |
| `TextControlBase` | Platform-specific versions |
| All other base classes | Platform-specific versions |

**Rationale:** Each platform (WPF, MAUI, HTML) now has its own complete base class hierarchy that uses native drivers directly. This eliminates the adapter abstraction and allows platform-specific optimizations.

---

*Next: [Platform Implementations](21d4_PlatformImplementations.md)*
