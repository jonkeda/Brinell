# ControlObject6 Blazor Test Cases

**Component:** Brinell.Blazor.ControlObject6  
**Version:** POC 1.0  
**Created:** January 4, 2026

---

## 1. BlazorTestContext Tests

### 1.1 Constructor and Properties

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| BTC-001 | Constructor with null page throws | ArgumentNullException |
| BTC-002 | Constructor sets Page property | Page accessible |
| BTC-003 | DefaultTimeoutMs default is 30000 | Property equals 30000 |
| BTC-004 | DefaultPollingIntervalMs default is 100 | Property equals 100 |
| BTC-005 | CurrentPage is null initially | Property is null |
| BTC-006 | DefaultTimeoutMs can be changed | Set/Get works |
| BTC-007 | DefaultPollingIntervalMs can be changed | Set/Get works |

### 1.2 Navigation (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| BTC-010 | NavigateToAsync(null) does nothing | No exception, no navigation |
| BTC-011 | NavigateToAsync(route) navigates | Page.GotoAsync called |
| BTC-012 | NavigateToAsync<TPage>() creates page | Returns page instance |
| BTC-013 | NavigateToAsync<TPage>() sets CurrentPage | CurrentPage is set |
| BTC-014 | NavigateToAsync<TPage>() waits for page load | WaitLoadedAsync called |

### 1.3 Control Creation

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| BTC-020 | CreateControl<IAsyncClickableControlObject>() | Returns ButtonControl |
| BTC-021 | CreateControl<IAsyncTextControlObject>() | Returns InputControl |
| BTC-022 | CreateControl with unknown interface throws | InvalidOperationException |

### 1.4 Screenshot and Logging

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| BTC-030 | TakeScreenshotAsync(null) does nothing | No exception |
| BTC-031 | TakeScreenshotAsync(name) saves file | Page.ScreenshotAsync called |
| BTC-032 | Log(null) does nothing | No exception |
| BTC-033 | Log(message) writes to console | Console output contains message |
| BTC-034 | LogError(message) writes to stderr | Error output contains message |

---

## 2. AsyncControlObjectBase Tests

### 2.1 Constructor

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ACO-001 | Constructor with null context throws | ArgumentNullException |
| ACO-002 | Constructor with null locator throws | ArgumentNullException |
| ACO-003 | Constructor sets Locator property | Property matches |
| ACO-004 | Constructor sets Page property | Property matches |

### 2.2 Existence Methods (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ACO-010 | IsExistsAsync() returns true when count > 0 | True |
| ACO-011 | IsExistsAsync() returns false when count = 0 | False |
| ACO-012 | WaitExistsAsync(null) returns true immediately | True, no wait |
| ACO-013 | WaitExistsAsync(true) waits for attached | Locator.WaitForAsync(Attached) |
| ACO-014 | WaitExistsAsync(false) waits for detached | Locator.WaitForAsync(Detached) |
| ACO-015 | WaitExistsAsync times out returns false | False after timeout |
| ACO-016 | CheckExistsAsync(null) does nothing | No exception |
| ACO-017 | CheckExistsAsync(true) throws on timeout | UITestTimeoutException |
| ACO-018 | AssertExistsAsync(null) does nothing | No exception |
| ACO-019 | AssertExistsAsync(true) throws on failure | AssertionException |

### 2.3 Visibility Methods (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ACO-020 | IsVisibleAsync() calls locator.IsVisibleAsync | Result from Playwright |
| ACO-021 | WaitVisibleAsync(true) waits for visible | Locator.WaitForAsync(Visible) |
| ACO-022 | WaitVisibleAsync(false) waits for hidden | Locator.WaitForAsync(Hidden) |
| ACO-023 | WaitVisibleAsync(null) returns true immediately | True, no wait |
| ACO-024 | CheckVisibleAsync(true) throws on timeout | UITestTimeoutException |
| ACO-025 | AssertVisibleAsync(true) throws on failure | AssertionException |

### 2.4 Enabled Methods (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ACO-030 | IsEnabledAsync() calls locator.IsEnabledAsync | Result from Playwright |
| ACO-031 | WaitEnabledAsync(true) polls until enabled | Returns true when enabled |
| ACO-032 | WaitEnabledAsync(null) returns true immediately | True, no wait |
| ACO-033 | CheckEnabledAsync(true) throws on timeout | UITestTimeoutException |
| ACO-034 | AssertEnabledAsync(false) passes when disabled | No exception |

### 2.5 Text Methods (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ACO-040 | GetTextAsync() returns inner text | Locator.InnerTextAsync() |
| ACO-041 | AssertTextAsync(null) does nothing | No exception |
| ACO-042 | AssertTextAsync("expected") passes on match | No exception |
| ACO-043 | AssertTextAsync("expected") fails on mismatch | AssertionException |
| ACO-044 | AssertTextContainsAsync("sub") passes | No exception |
| ACO-045 | AssertTextStartsWithAsync("pre") passes | No exception |
| ACO-046 | AssertTextEndsWithAsync("suf") passes | No exception |
| ACO-047 | AssertTextMatchesAsync("\\d+") passes on pattern | No exception |
| ACO-048 | AssertTextEmptyAsync(true) passes when empty | No exception |

### 2.6 Locator Conversion

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| ACO-050 | AutomationId uses data-automation-id selector | `[data-automation-id='value']` |
| ACO-051 | TestId uses GetByTestId | Page.GetByTestId |
| ACO-052 | Id uses # selector | `#value` |
| ACO-053 | ClassName uses . selector | `.value` |
| ACO-054 | XPath uses xpath= prefix | `xpath=...` |
| ACO-055 | Css uses raw selector | Raw CSS selector |
| ACO-056 | Text uses GetByText exact | Page.GetByText(exact) |
| ACO-057 | PartialText uses GetByText | Page.GetByText |
| ACO-058 | Label uses GetByLabel | Page.GetByLabel |
| ACO-059 | Placeholder uses GetByPlaceholder | Page.GetByPlaceholder |
| ACO-060 | Role uses GetByRole | Page.GetByRole |

---

## 3. ButtonControl Tests

### 3.1 Click Operations (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| BC-001 | ClickAsync() waits for visible | CheckVisibleAsync called |
| BC-002 | ClickAsync() waits for enabled | CheckEnabledAsync called |
| BC-003 | ClickAsync() clicks locator | Locator.ClickAsync called |
| BC-004 | ClickAsync() uses timeout option | Timeout passed to ClickAsync |
| BC-005 | DoubleClickAsync() performs double click | Locator.DblClickAsync called |
| BC-006 | RightClickAsync() uses right button | MouseButton.Right used |
| BC-007 | HoverAsync() moves to element | Locator.HoverAsync called |

---

## 4. InputControl Tests

### 4.1 Focus Operations (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IC-001 | IsFocusedAsync() evaluates activeElement | JavaScript executed |
| IC-002 | FocusAsync() focuses element | Locator.FocusAsync called |
| IC-003 | BlurAsync() blurs element | Locator.BlurAsync called |
| IC-004 | WaitFocusedAsync(null) returns immediately | True, no wait |

### 4.2 Text Input Operations (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IC-010 | EnterAsync(null) does nothing | No exception |
| IC-011 | EnterAsync("text") clears and fills | ClearAsync then FillAsync |
| IC-012 | ClearAsync() clears element | Locator.ClearAsync called |
| IC-013 | ClearAndEnterAsync(null) only clears | ClearAsync called, no FillAsync |
| IC-014 | ClearAndEnterAsync("text") clears and fills | ClearAsync then FillAsync |
| IC-015 | AppendAsync(null) does nothing | No exception |
| IC-016 | AppendAsync("text") types sequentially | PressSequentiallyAsync called |

### 4.3 ReadOnly Operations (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IC-020 | IsReadOnlyAsync() checks readonly attribute | GetAttributeAsync("readonly") |
| IC-021 | IsReadOnlyAsync() returns true when attr exists | True |
| IC-022 | IsReadOnlyAsync() returns false when no attr | False |
| IC-023 | AssertReadOnlyAsync(null) does nothing | No exception |

### 4.4 Text Retrieval

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IC-030 | GetTextAsync() returns input value | Locator.InputValueAsync |
| IC-031 | GetTextLengthAsync() returns value length | Correct length |
| IC-032 | AssertTextLengthAsync(5) passes on match | No exception |

---

## 5. AsyncPageObjectBase Tests

### 5.1 Page State (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| APO-001 | IsLoadedAsync() checks page locator visible | Uses PageLocator |
| APO-002 | WaitLoadedAsync(null) returns immediately | True, no wait |
| APO-003 | WaitLoadedAsync(true) waits for page | Returns true when loaded |
| APO-004 | AssertLoadedAsync(true) throws on failure | AssertionException |

### 5.2 Title (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| APO-010 | GetTitleAsync() returns page title | Page.TitleAsync called |
| APO-011 | AssertTitleAsync(null) does nothing | No exception |
| APO-012 | AssertTitleAsync("title") passes on match | No exception |
| APO-013 | AssertTitleAsync("title") fails on mismatch | AssertionException |

### 5.3 Control Access (Async)

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| APO-020 | GetControl<T>() returns control | Control created |
| APO-021 | TryGetControlAsync<T>() returns null if not found | Null when not exists |
| APO-022 | ControlExistsAsync() returns bool | True/false based on existence |
| APO-023 | Button() helper returns clickable control | IAsyncClickableControlObject |
| APO-024 | TextInput() helper returns text control | IAsyncTextControlObject |

### 5.4 Screenshot and Scrolling

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| APO-030 | TakeScreenshotAsync() calls context method | Context.TakeScreenshotAsync |
| APO-031 | ScrollToControlAsync(null) does nothing | No exception |
| APO-032 | ScrollToControlAsync() scrolls into view | ScrollIntoViewIfNeededAsync |

---

## 6. Async Interface Tests

### 6.1 IAsyncControlObject

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IAC-001 | All methods are async (return Task) | Method signatures verified |
| IAC-002 | All methods accept CancellationToken | Parameter present |
| IAC-003 | Interface defines Locator property | Property exists |
| IAC-004 | Interface defines Page property | Property exists, nullable |

### 6.2 IAsyncClickableControlObject

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IACC-001 | Extends IAsyncInteractiveControlObject | Inheritance verified |
| IACC-002 | ClickAsync defined | Method exists |
| IACC-003 | DoubleClickAsync defined | Method exists |
| IACC-004 | RightClickAsync defined | Method exists |
| IACC-005 | HoverAsync defined | Method exists |

### 6.3 IAsyncTextControlObject

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| IATC-001 | Extends IAsyncFocusableControlObject | Inheritance verified |
| IATC-002 | EnterAsync defined | Method exists |
| IATC-003 | ClearAsync defined | Method exists |
| IATC-004 | AppendAsync defined | Method exists |
| IATC-005 | IsReadOnlyAsync defined | Method exists |

---

## 7. Test Priority

| Priority | Category | Test Count |
|----------|----------|------------|
| P0 (Critical) | Click, Enter, Existence checks | 25 |
| P1 (High) | Visibility, Enabled, Text assertions | 30 |
| P2 (Medium) | Page navigation, Screenshots | 15 |
| P3 (Low) | Locator conversion edge cases | 10 |

---

## 8. Mocking Requirements

- **IPage**: Mock for all context tests
- **ILocator**: Mock for element operations
- Return values for:
  - `CountAsync()` - return 0 or 1
  - `IsVisibleAsync()` - return true/false
  - `IsEnabledAsync()` - return true/false
  - `InnerTextAsync()` - return text
  - `InputValueAsync()` - return value
  - `GetAttributeAsync()` - return attribute or null

---

## 9. Dependencies

- xUnit 2.9.3
- FluentAssertions 6.12.0
- Moq 4.20.70
- Microsoft.Playwright 1.50.0 (for types only)

---

## 10. Async Testing Notes

- Use `async Task` test methods
- Use `.ConfigureAwait(false)` in implementation if needed
- Test cancellation token propagation
- Test timeout behavior with `Task.Delay` in mocks
