# ControlObject6 MAUI Test Cases

**Component:** Brinell.Maui.ControlObject6  
**Version:** POC 1.0  
**Created:** January 4, 2026

---

## 1. MauiTestContext Tests

### 1.1 Constructor and Properties

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| MTC-001 | Constructor with null driver throws | ArgumentNullException |
| MTC-002 | Constructor sets Driver property | Driver accessible |
| MTC-003 | DefaultTimeoutMs default is 30000 | Property equals 30000 |
| MTC-004 | DefaultPollingIntervalMs default is 100 | Property equals 100 |
| MTC-005 | CurrentPage is null initially | Property is null |
| MTC-006 | DefaultTimeoutMs can be changed | Set/Get works |
| MTC-007 | DefaultPollingIntervalMs can be changed | Set/Get works |

### 1.2 Navigation

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| MTC-010 | NavigateTo(null) does nothing | No exception, no navigation |
| MTC-011 | NavigateTo(route) navigates | Driver.Navigate().GoToUrl called |
| MTC-012 | NavigateTo<TPage>() creates page | Returns page instance |
| MTC-013 | NavigateTo<TPage>() sets CurrentPage | CurrentPage is set |
| MTC-014 | NavigateTo<TPage>() waits for page load | WaitLoaded called |

### 1.3 Control Creation

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| MTC-020 | CreateControl<IClickableControlObject>() | Returns ButtonControl |
| MTC-021 | CreateControl<ITextControlObject>() | Returns EntryControl |
| MTC-022 | CreateControl with unknown interface throws | InvalidOperationException |

### 1.4 Screenshot and Logging

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| MTC-030 | TakeScreenshot(null) does nothing | No exception |
| MTC-031 | TakeScreenshot(name) saves file | File created with timestamp |
| MTC-032 | Log(null) does nothing | No exception |
| MTC-033 | Log(message) writes to console | Console output contains message |
| MTC-034 | LogError(message) writes to stderr | Error output contains message |

---

## 2. ControlObjectBase Tests

### 2.1 Constructor

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| COB-001 | Constructor with null context throws | ArgumentNullException |
| COB-002 | Constructor with null locator throws | ArgumentNullException |
| COB-003 | Constructor sets Locator property | Property matches |
| COB-004 | Constructor sets Page property | Property matches |

### 2.2 Existence Methods

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| COB-010 | IsExists() returns true when element found | True |
| COB-011 | IsExists() returns false when not found | False |
| COB-012 | WaitExists(null) returns true immediately | True, no wait |
| COB-013 | WaitExists(true) waits for existence | Returns true when found |
| COB-014 | WaitExists(false) waits for non-existence | Returns true when removed |
| COB-015 | WaitExists times out returns false | False after timeout |
| COB-016 | CheckExists(null) does nothing | No exception |
| COB-017 | CheckExists(true) throws on timeout | UITestTimeoutException |
| COB-018 | AssertExists(null) does nothing | No exception |
| COB-019 | AssertExists(true) throws on failure | AssertionException |

### 2.3 Visibility Methods

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| COB-020 | IsVisible() returns true when displayed | True |
| COB-021 | IsVisible() returns false when not displayed | False |
| COB-022 | IsVisible() returns false when not found | False |
| COB-023 | WaitVisible(null) returns true immediately | True, no wait |
| COB-024 | WaitVisible(true) waits for visibility | Returns true when visible |
| COB-025 | CheckVisible(true) throws on timeout | UITestTimeoutException |
| COB-026 | AssertVisible(true) throws on failure | AssertionException |

### 2.4 Enabled Methods

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| COB-030 | IsEnabled() returns true when enabled | True |
| COB-031 | IsEnabled() returns false when disabled | False |
| COB-032 | WaitEnabled(null) returns true immediately | True, no wait |
| COB-033 | WaitEnabled(true) waits for enabled | Returns true when enabled |
| COB-034 | CheckEnabled(true) throws on timeout | UITestTimeoutException |
| COB-035 | AssertEnabled(false) passes when disabled | No exception |

### 2.5 Text Methods

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| COB-040 | GetText() returns element text | Text value |
| COB-041 | GetText() throws when not found | ElementNotFoundException |
| COB-042 | AssertText(null) does nothing | No exception |
| COB-043 | AssertText("expected") passes on match | No exception |
| COB-044 | AssertText("expected") fails on mismatch | AssertionException |
| COB-045 | AssertTextContains("sub") passes | No exception |
| COB-046 | AssertTextStartsWith("pre") passes | No exception |
| COB-047 | AssertTextEndsWith("suf") passes | No exception |
| COB-048 | AssertTextMatches("\\d+") passes on pattern | No exception |
| COB-049 | AssertTextEmpty(true) passes when empty | No exception |

### 2.6 Locator Conversion

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| COB-050 | AutomationId converts to AccessibilityId | MobileBy.AccessibilityId |
| COB-051 | Id converts to By.Id | By.Id |
| COB-052 | XPath converts to By.XPath | By.XPath |
| COB-053 | Text converts to XPath with text() | XPath expression |
| COB-054 | Unsupported strategy throws | NotSupportedException |

---

## 3. ButtonControl Tests

### 3.1 Click Operations

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| BC-001 | Click() waits for visible | CheckVisible called |
| BC-002 | Click() waits for enabled | CheckEnabled called |
| BC-003 | Click() clicks element | element.Click() called |
| BC-004 | DoubleClick() performs double click | Actions.DoubleClick called |
| BC-005 | RightClick() performs context click | Actions.ContextClick called |
| BC-006 | Hover() moves to element | Actions.MoveToElement called |
| BC-007 | LongPress() performs touch press | Pointer actions performed |
| BC-008 | LongPress(500) uses 500ms duration | Duration set correctly |

---

## 4. EntryControl Tests

### 4.1 Focus Operations

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| EC-001 | IsFocused() checks active element | Compares to active element |
| EC-002 | Focus() clicks element to focus | element.Click() called |
| EC-003 | Blur() sends tab to blur | SendKeys("\t") called |
| EC-004 | WaitFocused(null) returns immediately | True, no wait |

### 4.2 Text Input Operations

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| EC-010 | Enter(null) does nothing | No exception |
| EC-011 | Enter("text") clears and types | Clear() then SendKeys() |
| EC-012 | Clear() clears element | element.Clear() called |
| EC-013 | ClearAndEnter(null) only clears | Clear() called, no SendKeys |
| EC-014 | ClearAndEnter("text") clears and types | Clear() then SendKeys() |
| EC-015 | Append(null) does nothing | No exception |
| EC-016 | Append("text") types without clearing | SendKeys() only |

### 4.3 ReadOnly Operations

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| EC-020 | IsReadOnly() returns true when readonly | True |
| EC-021 | IsReadOnly() returns false when editable | False |
| EC-022 | AssertReadOnly(null) does nothing | No exception |
| EC-023 | AssertReadOnly(true) passes when readonly | No exception |

### 4.4 Text Length Operations

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| EC-030 | GetTextLength() returns text length | Correct length |
| EC-031 | AssertTextLength(5) passes on match | No exception |
| EC-032 | AssertTextLength(5) fails on mismatch | AssertionException |

---

## 5. PageObjectBase Tests

### 5.1 Page State

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| POB-001 | IsLoaded() checks page locator visible | Uses PageLocator |
| POB-002 | WaitLoaded(null) returns immediately | True, no wait |
| POB-003 | WaitLoaded(true) waits for page | Returns true when loaded |
| POB-004 | AssertLoaded(true) throws on failure | AssertionException |

### 5.2 Control Access

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| POB-010 | GetControl<T>() returns control | Control created |
| POB-011 | GetControl<T>() checks existence | CheckExists called |
| POB-012 | TryGetControl<T>() returns null if not found | Null when not exists |
| POB-013 | ControlExists() returns bool | True/false based on existence |
| POB-014 | Button() helper returns clickable control | IClickableControlObject |
| POB-015 | TextInput() helper returns text control | ITextControlObject |

---

## 6. Test Priority

| Priority | Category | Test Count |
|----------|----------|------------|
| P0 (Critical) | Click, Enter, Existence checks | 25 |
| P1 (High) | Visibility, Enabled, Text assertions | 30 |
| P2 (Medium) | Page navigation, Screenshots | 15 |

---

## 7. Mocking Requirements

- **AppiumDriver**: Mock for all tests
- **AppiumElement**: Mock for element operations
- **Actions**: Mock for gesture operations

---

## 8. Dependencies

- xUnit 2.9.3
- FluentAssertions 6.12.0
- Moq 4.20.70
- Appium.WebDriver 8.0.1 (for types only)
