# 7. Wait/Check/Is/Assert Pattern

**Parent:** [Documentation Index](21d0_UITestFramework_Index.md)  
**Code Examples:** [21d7_WaitCheckIsAssertPattern_CodeExamples.md](21d7_WaitCheckIsAssertPattern_CodeExamples.md)  
**Previous:** [ControlObject Hierarchy](21d6_ControlObjectHierarchy.md)  
**Version:** 3.0 (Updated December 2025)

---

## 7.1 Overview

The Wait/Check/Is/Assert pattern provides a four-tier approach to state verification:

| Prefix | Description | Returns | On Failure | Logging |
|--------|-------------|---------|------------|---------|
| `Is*` | Immediate state check | `bool` | Returns current state | None |
| `Wait*` | Poll until condition or timeout | `bool` | Returns `false` | Minimal |
| `Check*` | Wait + throw on failure | `void` | Throws `AssertionException` | Error only |
| `Assert*` | Semantic assertion with full logging | `void` | Throws `AssertionException` | Full CSV |

**Key Distinction (v3):** `Check*` methods are for **preconditions** (called internally before actions). `Assert*` methods are for **test assertions** (called by tests to verify expected state).

---

## 7.2 Pattern Comparison

### 7.2.1 When to Use Each

| Use Case | Method | Example |
|----------|--------|---------|
| Conditional logic | `Is*` | `if (button.IsVisible()) { ... }` |
| Wait for async operation | `Wait*` | `WaitVisible(true)` |
| Precondition for action | `Check*` | `CheckClickable()` before `Click()` |
| Test assertion | `Assert*` | `AssertText("Expected")` |

### 7.2.2 Return Values

```csharp
// Is* - Returns current state immediately
bool isVisible = control.IsVisible();  // true or false

// Wait* - Returns whether condition was met
bool waitResult = control.WaitVisible(true);  // true = condition met
if (!waitResult) { /* handle timeout */ }

// Check* - Returns void, throws on failure
control.CheckVisible(true);  // throws AssertionException if fails

// Assert* - Returns void, throws on failure with logging
control.AssertVisible(true);  // logs to CSV, throws if fails
```

---

## 7.3 Method Naming Convention

### 7.3.1 State Properties

| Property | Is* | Wait* | Check* | Assert* |
|----------|-----|-------|--------|---------|
| Existence | `IsExists()` | `WaitExists(bool)` | `CheckExists(bool)` | `AssertExists(bool)` |
| Visibility | `IsVisible()` | `WaitVisible(bool)` | `CheckVisible(bool)` | `AssertVisible(bool)` |
| Enabled | `IsEnabled()` | `WaitEnabled(bool)` | `CheckEnabled(bool)` | `AssertEnabled(bool)` |
| Clickable | `IsClickable()` | `WaitClickable()` | `CheckClickable()` | `AssertClickable()` |
| Checked | `IsChecked()` | `WaitChecked(bool)` | `CheckChecked(bool)` | `AssertChecked(bool)` |

### 7.3.2 Value Properties

| Property | Get* | Wait* | Check* | Assert* |
|----------|------|-------|--------|---------|
| Text | `GetText()` | `WaitText(string)` | `CheckText(string)` | `AssertText(string)` |
| Text Contains | - | `WaitTextContains(string)` | `CheckTextContains(string)` | `AssertTextContains(string)` |
| Attribute | `GetAttribute(name)` | `WaitAttribute(name, value)` | - | `AssertAttribute(name, value)` |
| Item Count | `GetItemCount()` | `WaitItemCount(int)` | - | `AssertItemCount(int)` |

---

## 7.4 Timeout Configuration

### 7.4.1 Default Timeouts

| Timeout | Default | Use Case |
|---------|---------|----------|
| `DefaultTimeoutMs` | 10000 | Standard waits |
| `ShortTimeoutMs` | 3000 | Quick checks |
| `PollingIntervalMs` | 250 | Polling frequency |

### 7.4.2 Specifying Custom Timeouts

```csharp
// Use default timeout
control.WaitVisible(true);

// Use explicit timeout
control.WaitVisible(true, timeoutMs: 5000);

// Use short timeout
control.WaitVisible(true, timeoutMs: Context.ShortTimeoutMs);
```

---

## 7.5 Always Check Before Action

### 7.5.1 Rule

**EVERY action method MUST call a Check method before performing the action.**

### 7.5.2 Implementation Pattern (Platform Base Class)

```csharp
// In WPF ContentControlBase
public virtual void Click()
{
    // STEP 1: Check precondition
    CheckClickable();  // Throws if not clickable
    
    // STEP 2: Perform action via native driver
    var element = GetAutomationElement();
    element.Click();  // Direct FlaUI call
    
    // STEP 3: Log success
    Logger.LogAction(...);
}
```

### 7.5.3 Action → Check Mapping

| Action Method | Check Called |
|---------------|--------------|
| `Click()` | `CheckClickable()` |
| `DoubleClick()` | `CheckClickable()` |
| `EnterText()` | `CheckEnabled()` |
| `Clear()` | `CheckEnabled()` |
| `AppendText()` | `CheckEnabled()` |
| `Toggle()` | `CheckClickable()` |
| `SetChecked()` | `CheckClickable()` |
| `ClickItem()` | `CheckClickable()` |

### 7.5.4 Why This Matters

Without checking first:
```
ERROR: Element not interactable
  at SeleniumDriver.Click()
  at ButtonControl.Click()
  at Test.Navigate_To_Settings()
```

With checking first:
```
AssertionException: Control 'SettingsButton' is not clickable.
  Visible: false, Enabled: true
  Expected: clickable=true
```

---

## 7.6 Assert Methods for Value Properties

### 7.6.1 Text Assertions

| Method | Description |
|--------|-------------|
| `AssertText(expected)` | Exact text match |
| `AssertTextContains(substring)` | Text contains substring |
| `AssertTextStartsWith(prefix)` | Text starts with prefix |
| `AssertTextEndsWith(suffix)` | Text ends with suffix |
| `AssertTextMatches(regex)` | Text matches regex pattern |

### 7.6.2 State Assertions

| Method | Description |
|--------|-------------|
| `AssertExists(expected)` | Element existence |
| `AssertVisible(expected)` | Element visibility |
| `AssertEnabled(expected)` | Element enabled state |
| `AssertClickable()` | Visible AND enabled |
| `AssertChecked(expected)` | Toggle/checkbox state |

### 7.6.3 Collection Assertions

| Method | Description |
|--------|-------------|
| `AssertItemCount(expected)` | Number of items |
| `AssertItemExists(text)` | Item with text exists |
| `AssertItemAtIndex(index, text)` | Item at index has text |

---

## 7.7 Logging Behavior

### 7.7.1 Logging by Method Type

| Method Type | Logs Success | Logs Failure | CSV Entry |
|-------------|--------------|--------------|-----------|
| `Is*` | ❌ | ❌ | None |
| `Wait*` | Minimal | Minimal | None |
| `Check*` | ❌ | ✅ | Error only |
| `Assert*` | ✅ | ✅ | Always |

### 7.7.2 Assert Logging Format

```csv
Timestamp;TestName;PageName;ControlId;AssertionType;ActualValue;ExpectedValue;Passed;Message
2025-12-22T10:15:30;NavigationTests;Shell;SettingsButton;AssertVisible;true;true;true;
2025-12-22T10:15:31;NavigationTests;Settings;PageTitle;AssertText;Settings;Settings;true;
2025-12-22T10:15:32;NavigationTests;Settings;ThemeToggle;AssertChecked;false;true;false;Expected checked=true, was false
```

---

## 7.8 Decision Tree

```
Need to check element state?
│
├── For conditional logic (if/else)?
│   └── Use Is*() → Returns bool immediately
│
├── For waiting on async operation?
│   └── Use Wait*() → Returns bool after timeout
│
├── As precondition for action?
│   └── Use Check*() → Throws on failure
│
└── For test assertion?
    └── Use Assert*() → Logs to CSV, throws on failure
```

---

## 7.9 Common Patterns

### 7.9.1 Wait Then Assert

```csharp
// Wait for page, then assert specific content
page.WaitForDisplayed();
page.Title.AssertText("Expected Title");
```

### 7.9.2 Conditional Action

```csharp
// Only click if visible
if (dialog.CloseButton.IsVisible())
{
    dialog.CloseButton.Click();
}
```

### 7.9.3 Wait for Dynamic Content

```csharp
// Wait for loading to complete
loadingIndicator.WaitVisible(false);

// Now assert content
contentList.AssertItemCount(10);
```

---

*Next: [IsBusy-Based State Tracking](21d8_IsBusyStateTracking.md)*
