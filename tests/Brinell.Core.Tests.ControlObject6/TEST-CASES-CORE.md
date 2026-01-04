# ControlObject6 Core Test Cases

**Component:** Brinell.Core.ControlObject6  
**Version:** POC 1.0  
**Created:** January 4, 2026

---

## 1. Locator System Tests

### 1.1 LocatorStrategy Enum Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| LS-001 | All enum values are defined | 17 strategies exist |
| LS-002 | AutomationId is default (0) | `LocatorStrategy.AutomationId == 0` |
| LS-003 | Enum values are unique | No duplicate values |

### 1.2 ControlLocator Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| CL-001 | Constructor sets Strategy and Value | Properties match constructor args |
| CL-002 | Then() creates chained locator | Returns new locator with Parent set |
| CL-003 | WithIndex() sets Index | Returns new locator with Index property |
| CL-004 | First() sets Index to 0 | Returns locator with Index=0 |
| CL-005 | Last() sets Index to -1 | Returns locator with Index=-1 |
| CL-006 | Nth(n) sets Index to n | Returns locator with Index=n |
| CL-007 | Implicit string conversion uses AutomationId | `ControlLocator loc = "myId"` uses AutomationId |
| CL-008 | ToString() returns readable format | Returns `"Strategy: Value"` format |
| CL-009 | ToString() includes parent chain | Shows `"Parent > Child"` format |
| CL-010 | Null Value throws ArgumentNullException | Constructor throws on null value |

### 1.3 By Static Factory Tests

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| BY-001 | By.AutomationId(value) | Returns locator with AutomationId strategy |
| BY-002 | By.Id(value) | Returns locator with Id strategy |
| BY-003 | By.Name(value) | Returns locator with Name strategy |
| BY-004 | By.ClassName(value) | Returns locator with ClassName strategy |
| BY-005 | By.XPath(value) | Returns locator with XPath strategy |
| BY-006 | By.Css(value) | Returns locator with Css strategy |
| BY-007 | By.Text(value) | Returns locator with Text strategy |
| BY-008 | By.PartialText(value) | Returns locator with PartialText strategy |
| BY-009 | By.AccessibilityId(value) | Returns locator with AccessibilityId strategy |
| BY-010 | By.TagName(value) | Returns locator with TagName strategy |
| BY-011 | By.Label(value) | Returns locator with Label strategy |
| BY-012 | By.Placeholder(value) | Returns locator with Placeholder strategy |
| BY-013 | By.Title(value) | Returns locator with Title strategy |
| BY-014 | By.Role(value) | Returns locator with Role strategy |
| BY-015 | By.TestId(value) | Returns locator with TestId strategy |
| BY-016 | By.DataAttribute(name, value) | Returns locator with DataAttribute strategy and DataAttributeName |

---

## 2. Interface Contract Tests

### 2.1 IControlObject Interface

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ICO-001 | Interface defines Locator property | Property exists |
| ICO-002 | Interface defines Page property | Property exists, nullable |
| ICO-003 | IsExists() defined | Method exists |
| ICO-004 | WaitExists(bool?, int?) defined | Method exists with optional params |
| ICO-005 | CheckExists(bool?, int?) defined | Method exists |
| ICO-006 | AssertExists(bool?, string?, int?) defined | Method exists |
| ICO-007 | All visibility methods defined | IsVisible, WaitVisible, CheckVisible, AssertVisible |
| ICO-008 | All text methods defined | GetText, AssertText, AssertTextContains, etc. |

### 2.2 IInteractiveControlObject Interface

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IIC-001 | Extends IControlObject | Inheritance verified |
| IIC-002 | IsEnabled() defined | Method exists |
| IIC-003 | WaitEnabled(bool?, int?) defined | Method exists |
| IIC-004 | CheckEnabled(bool?, int?) defined | Method exists |
| IIC-005 | AssertEnabled(bool?, string?, int?) defined | Method exists |

### 2.3 IFocusableControlObject Interface

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IFC-001 | Extends IInteractiveControlObject | Inheritance verified |
| IFC-002 | IsFocused() defined | Method exists |
| IFC-003 | Focus(int?) defined | Method exists |
| IFC-004 | Blur(int?) defined | Method exists |

### 2.4 IClickableControlObject Interface

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ICC-001 | Extends IInteractiveControlObject | Inheritance verified |
| ICC-002 | Click(int?) defined | Method exists |
| ICC-003 | DoubleClick(int?) defined | Method exists |
| ICC-004 | RightClick(int?) defined | Method exists |
| ICC-005 | Hover(int?) defined | Method exists |
| ICC-006 | LongPress(int?, int?) defined | Method exists |

### 2.5 ITextControlObject Interface

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ITC-001 | Extends IFocusableControlObject | Inheritance verified |
| ITC-002 | Enter(string?, int?) defined | Method exists |
| ITC-003 | Clear(int?) defined | Method exists |
| ITC-004 | ClearAndEnter(string?, int?) defined | Method exists |
| ITC-005 | Append(string?, int?) defined | Method exists |
| ITC-006 | IsReadOnly() defined | Method exists |
| ITC-007 | GetTextLength(int?) defined | Method exists |

### 2.6 IPageObject Interface

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IPO-001 | Name property defined | Property exists |
| IPO-002 | IsLoaded(int?) defined | Method exists |
| IPO-003 | GetControl<T>(ControlLocator, int?) defined | Generic method exists |
| IPO-004 | TryGetControl<T>(ControlLocator, int?) defined | Returns nullable |
| IPO-005 | ControlExists(ControlLocator, int?) defined | Method exists |
| IPO-006 | TakeScreenshot(string?, int?) defined | Method exists |
| IPO-007 | ScrollToControl(ControlLocator?, int?) defined | Method exists |

### 2.7 ITestContext Interface

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ITC-001 | DefaultTimeoutMs property defined | Get/Set property |
| ITC-002 | DefaultPollingIntervalMs property defined | Get/Set property |
| ITC-003 | CurrentPage property defined | Nullable property |
| ITC-004 | NavigateTo(string?, int?) defined | Method exists |
| ITC-005 | NavigateTo<TPage>(int?) defined | Generic method exists |
| ITC-006 | CreateControl<T>(ControlLocator) defined | Generic method exists |
| ITC-007 | TakeScreenshot(string?) defined | Method exists |
| ITC-008 | Log(string?) defined | Method exists |
| ITC-009 | LogError(string?) defined | Method exists |

---

## 3. Test Priority

| Priority | Category | Test Count |
|----------|----------|------------|
| P0 (Critical) | Locator creation, By factory | 16 |
| P1 (High) | Interface contracts | 35 |
| P2 (Medium) | Chaining, ToString | 5 |

---

## 4. Test Data Requirements

- Valid locator values: "button1", "txtInput", "//div[@id='test']"
- Empty/null values for negative tests
- Various index values: 0, 1, -1, 5

---

## 5. Dependencies

- xUnit 2.9.3
- FluentAssertions 6.12.0
- No mocking required (pure unit tests)
