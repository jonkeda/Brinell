# 4. Platform Implementations

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d4_PlatformImplementations_CodeExamples.md](21d4_PlatformImplementations_CodeExamples.md)  
**Previous:** [Core Framework](21d3_CoreFramework.md)  
**Version:** 3.0 (Updated December 2025)

---

## 4.1 Overview

Each platform project is **fully self-contained** with:
- Test context (implements `ITestContext` + platform-specific element operations)
- Base class hierarchy (`ControlBase`, `PageBase`, etc.)
- Control implementations
- Native driver access (**no adapter layer**)

| Platform | Project | Context Class | Native Driver |
|----------|---------|---------------|---------------|
| WPF | `.Wpf` | `FlaUITestContext` | FlaUI directly |
| MAUI | `.Maui` | `AppiumTestContext` | Appium directly |
| HTML | `.Html` | `SeleniumTestContext` | Selenium directly |

---

## 4.2 WPF Platform (FlaUI)

### 4.2.1 FlaUITestContext

Implements `ITestContext` plus WPF-specific element operations.  
Directly uses FlaUI - **no adapter layer**.

| Feature | Description |
|---------|-------------|
| Automation | UIA3 (UI Automation 3) |
| Native Access | `MainWindow`, `Driver`, `FindElement()` |
| App Launch | `FlaUIDriverAdapter.Launch(path)` |
| Platform | `Platform.Windows` |

### 4.2.2 WPF Element Operations

`FlaUITestContext` provides direct element access (not in `ITestContext`):

| Method | Description |
|--------|-------------|
| `FindElement(automationId)` | Find by AutomationId |
| `FindElementByXPath(xpath)` | Find by XPath |
| `FindElements(automationId)` | Find all matching |
| `ElementExists(automationId)` | Check existence |
| `ElementIsVisible(automationId)` | Check visibility |
| `ElementIsEnabled(automationId)` | Check enabled state |
| `WaitForElement(automationId, timeout)` | Wait for element |
| `WaitForElementVisible(automationId, timeout)` | Wait visible |

### 4.2.3 WPF Base Classes

| Class | Purpose |
|-------|---------|
| `ControlBase` | Base for all controls, Is/Wait/Check/Assert pattern |
| `PageBase` | Base for page objects |
| `BusyPageBase` | Pages with IsBusy loading states |
| `ContentControlBase` | Clickable controls (Button, Label) |
| `TextControlBase` | Text input (TextBox, PasswordBox) |
| `ToggleControlBase` | Toggle controls (CheckBox, RadioButton) |
| `SelectorControlBase` | Selection controls (ComboBox, ListBox) |
| `RangeControlBase` | Range controls (Slider, ProgressBar) |
| `ItemsControlBase` | Collection controls |

---

## 4.3 MAUI Platform (Appium 8.x)

### 4.3.1 AppiumTestContext

Implements `ITestContext` plus MAUI-specific operations.

| Feature | Description |
|---------|-------------|
| Automation | Appium 8.x (W3C compliant) |
| Platforms | Windows, Android, iOS |
| Gestures | Swipe, scroll, long press |
| Platform | `Platform.WindowsMaui`, `Platform.Android`, `Platform.iOS` |

### 4.3.2 MAUI Element Operations

| Method | Description |
|--------|-------------|
| `FindElement(automationId)` | Find by AccessibilityId |
| `FindElements(automationId)` | Find all matching |
| `ElementExists(automationId)` | Check existence |
| `ElementIsVisible(automationId)` | Check visibility |
| `ElementIsEnabled(automationId)` | Check enabled state |

### 4.3.3 Platform-Specific Factory Methods

| Method | Target |
|--------|--------|
| `CreateWindows(appPath)` | Windows MAUI app |
| `CreateAndroid(appPath, serverUri)` | Android device/emulator |
| `CreateiOS(appPath, serverUri)` | iOS device/simulator |

### 4.3.4 MAUI Base Classes

| Class | Purpose |
|-------|---------|
| `ControlBase` | Base for all controls |
| `PageBase` | Base for page objects |
| `ContentControlBase` | Clickable controls |
| `TextControlBase` | Text input (Entry, Editor) |
| `ToggleControlBase` | Toggle controls (CheckBox, Switch) |
| `SelectorControlBase` | Selection controls (Picker) |
| `RangeControlBase` | Range controls (Slider) |
| `ItemsControlBase` | Collection controls |

---

## 4.4 HTML Platform (Selenium)

### 4.4.1 SeleniumTestContext

Implements `ITestContext` plus HTML-specific operations.

| Feature | Description |
|---------|-------------|
| Automation | Selenium WebDriver 4.x |
| Browsers | Chrome, Firefox, Edge, Safari |
| Platform | `Platform.Web` |

### 4.4.2 HTML Element Operations

| Method | Description |
|--------|-------------|
| `FindElement(locator)` | Find by locator (CSS, ID, data-testid) |
| `FindElements(locator)` | Find all matching |
| `ElementExists(locator)` | Check existence |
| `ElementIsVisible(locator)` | Check visibility |
| `ElementIsEnabled(locator)` | Check enabled state |

### 4.4.3 Element Location Strategies

| Priority | Strategy | Selector |
|----------|----------|----------|
| 1 | `data-automation-id` | `[data-automation-id="..."]` |
| 2 | `id` | `#elementId` |
| 3 | `data-testid` | `[data-testid="..."]` |

### 4.4.4 HTML Base Classes

| Class | Purpose | Extra Features |
|-------|---------|----------------|
| `ControlBase` | Base for all controls | `GetAttribute()`, `GetCssValue()`, `HasClass()` |
| `PageBase` | Base for page objects | |
| `LoadingPageBase` | Pages with loading states | `IsLoading()`, `WaitForLoaded()` |
| `ContentControlBase` | Clickable controls | `Hover()` |
| `TextControlBase` | Text input | `Focus()`, `Blur()`, `GetPlaceholder()` |
| `ToggleControlBase` | Checkbox controls | |
| `SelectorControlBase` | Select controls | `SelectByValue()`, `IsMultiple()` |
| `RangeControlBase` | Range inputs | `GetStep()`, `GetPercentage()` |

### 4.4.5 Supported Browsers

| Browser | Driver | Notes |
|---------|--------|-------|
| Chrome | ChromeDriver | Auto-managed |
| Firefox | GeckoDriver | Auto-managed |
| Edge | EdgeDriver | Auto-managed |
| Safari | SafariDriver | macOS only |

---

## 4.5 Platform Comparison

### 4.5.1 Context Features

| Feature | WPF | MAUI | HTML |
|---------|-----|------|------|
| Native Type | `AutomationElement` | `AppiumElement` | `IWebElement` |
| AutomationId Source | `AutomationId` property | `AccessibilityId` | `data-automation-id` or `id` |
| Visibility Check | `IsOffscreen` | `Displayed` | `Displayed` |
| Enabled Check | `IsEnabled` | `Enabled` | `Enabled` |

### 4.5.2 Base Class Features

| Base Class | WPF Extra | MAUI Extra | HTML Extra |
|------------|-----------|------------|------------|
| `ControlBase` | FlaUI patterns | Appium patterns | `GetAttribute()`, `HasClass()` |
| `PageBase` | | | |
| Busy/Loading | `BusyPageBase` | - | `LoadingPageBase` |
| `ContentControlBase` | | | `Hover()` |
| `TextControlBase` | | | `Focus()`, `Blur()`, `GetPlaceholder()` |
| `SelectorControlBase` | | | `SelectByValue()`, `IsMultiple()` |
| `RangeControlBase` | | | `GetStep()`, `GetPercentage()` |

---

## 4.6 Control Mapping

| WPF Control | MAUI Control | HTML Element | Interface |
|-------------|--------------|--------------|-----------|
| `ButtonControl` | `ButtonControl` | `ButtonControl` | `IContentControl` |
| `LabelControl` | `LabelControl` | `LabelControl` | `IContentControl` |
| - | - | `LinkControl` | `IContentControl` |
| `TextBoxControl` | `EntryControl` | `TextInputControl` | `ITextControl` |
| - | - | `TextAreaControl` | `ITextControl` |
| `CheckBoxControl` | `CheckBoxControl` | `CheckBoxControl` | `IToggleControl` |
| - | `SwitchControl` | - | `IToggleControl` |
| `ComboBoxControl` | `PickerControl` | `SelectControl` | `ISelectorControl` |
| `ListBoxControl` | - | - | `ISelectorControl` |
| `SliderControl` | `SliderControl` | `RangeInputControl` | `IRangeControl` |
| `ProgressBarControl` | `ProgressBarControl` | `ProgressControl` | `IRangeControl` |

---

## 4.7 Driver Lifecycle

### 4.7.1 WPF Lifecycle

```
1. Launch application via FlaUIDriverAdapter.Launch(path)
2. Wait for main window to appear
3. Create FlaUITestContext with main window
4. Run tests (use FlaUI directly via context)
5. Dispose context (closes application)
```

### 4.7.2 MAUI/Appium Lifecycle

```
1. Start Appium server (external)
2. Create AppiumOptions with capabilities
3. Create platform-specific context
4. Run tests (use Appium directly via context)
5. Dispose context (closes app, session ends)
```

### 4.7.3 Selenium Lifecycle

```
1. Create browser options
2. Create SeleniumTestContext (auto-downloads driver)
3. Navigate to base URL
4. Run tests (use Selenium directly via context)
5. Dispose context (closes browser)
```

---

## 4.8 Platform-Specific Features

### 4.8.1 WPF Only

| Feature | Access |
|---------|--------|
| Invoke pattern | `element.AsButton().Invoke()` |
| Value pattern | `element.AsTextBox().Text` |
| Toggle pattern | `element.AsCheckBox().Toggle()` |
| Window management | `MainWindow.SetForeground()` |
| XPath queries | `FindElementByXPath(xpath)` |

### 4.8.2 Mobile Only (Appium)

| Feature | Method |
|---------|--------|
| Swipe | `TouchActions.Swipe(...)` |
| Long press | `TouchActions.LongPress(...)` |
| Scroll | `driver.ExecuteScript("mobile: scroll", ...)` |
| Hide keyboard | `driver.HideKeyboard()` |

### 4.8.3 Web Only (Selenium)

| Feature | Method |
|---------|--------|
| JavaScript execution | `driver.ExecuteScript(...)` |
| Cookie management | `driver.Manage().Cookies` |
| Window handles | `driver.WindowHandles` |
| Navigation | `driver.Navigate().Back()` |
| CSS selector queries | Full CSS selector support |

---

*Next: [Multi-Platform Support](21d5_MultiPlatformSupport.md)*
