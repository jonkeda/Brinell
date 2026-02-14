# specification CheckBoxControl

- **id**: SPC-104
- **version**: 1.0
- **created**: January 8, 2026
- **status**: Draft
- **level**: 1
- **requirement**: FR-100, FR-140
- **interfaces**: IControlObject, IToggleControlObject

---

## Overview

The CheckBox control represents a binary toggle element that can be checked or unchecked. It validates the toggle state pattern which is reused by Switch, RadioButton, and other toggle controls.

---

## behavior

### Core Behaviors (IControlObject)

1. CheckBox can be located by automation ID, name, or other locator strategies
2. CheckBox reports existence state via `IsExists()` returning `bool`
3. CheckBox reports visibility state via `IsVisible()` returning `bool`
4. CheckBox reports enabled state via `IsEnabled()` returning `bool`
5. CheckBox supports waiting for state changes with configurable timeout
6. CheckBox supports assertion methods that throw on failure

### Toggle Behaviors (IToggleControlObject)

7. CheckBox has checked state accessible via `IsChecked()` returning `bool`
8. CheckBox can be checked via `Check()` method
9. CheckBox can be unchecked via `Uncheck()` method
10. CheckBox can be toggled via `Toggle()` method (inverts current state)
11. CheckBox supports setting state via `SetChecked(bool value)` method
12. CheckBox supports assertion via `AssertChecked(bool? expected, string? message)`
13. CheckBox supports waiting via `WaitChecked(bool? expected, int? timeoutMs)`

### State Transition Behaviors

14. `Check()` on already-checked checkbox does nothing
15. `Uncheck()` on already-unchecked checkbox does nothing
16. `Toggle()` always changes state
17. `SetChecked(true)` calls `Check()`, `SetChecked(false)` calls `Uncheck()`

---

## boundary

### Toggle Boundaries

- `Check()` on disabled checkbox does nothing and does not throw
- `Uncheck()` on disabled checkbox does nothing and does not throw
- `Toggle()` on disabled checkbox does nothing and does not throw
- `SetChecked()` on disabled checkbox does nothing and does not throw
- All toggle operations on hidden checkbox wait for visibility (with timeout)

### State Boundaries

- `IsChecked()` returns `true` for checked state, `false` for unchecked
- `IsChecked()` for indeterminate state returns `false` (treat as unchecked)
- `IsEnabled()` returns `false` for disabled checkboxes
- `IsVisible()` returns `false` for hidden checkboxes

### Wait Boundaries

- `WaitChecked()` uses boolean equality comparison
- `WaitChecked()` with null expected value skips wait (nullable skip pattern)
- Wait timeout uses `DefaultWait` from context when not specified

### Assert Boundaries

- `AssertChecked()` with null expected value skips assertion
- Assert methods throw `AssertionException` with custom message on failure

### Indeterminate State Boundaries

- Indeterminate (tri-state) is treated as unchecked for `IsChecked()`
- `Check()` on indeterminate checkbox checks it
- `Toggle()` on indeterminate checkbox behavior is platform-dependent
- Framework does not distinguish indeterminate from unchecked

---

## acceptance

### Existence and Location

```gherkin
Scenario: CheckBox is located by automation ID
  Given a page with a checkbox having AutomationId "agreeCheckBox"
  When I create a CheckBox control with locator By.AutomationId("agreeCheckBox")
  Then IsExists() returns true
```

### State Retrieval

```gherkin
Scenario: IsChecked returns true for checked checkbox
  Given a checked checkbox
  When I call IsChecked()
  Then it returns true

Scenario: IsChecked returns false for unchecked checkbox
  Given an unchecked checkbox
  When I call IsChecked()
  Then it returns false
```

### Check Operations

```gherkin
Scenario: Check sets checkbox to checked
  Given an unchecked checkbox
  When I call Check()
  Then IsChecked() returns true

Scenario: Check on already checked checkbox does nothing
  Given a checked checkbox
  When I call Check()
  Then IsChecked() still returns true

Scenario: Uncheck sets checkbox to unchecked
  Given a checked checkbox
  When I call Uncheck()
  Then IsChecked() returns false

Scenario: Uncheck on already unchecked checkbox does nothing
  Given an unchecked checkbox
  When I call Uncheck()
  Then IsChecked() still returns false
```

### Toggle Operations

```gherkin
Scenario: Toggle inverts checked state
  Given a checked checkbox
  When I call Toggle()
  Then IsChecked() returns false

Scenario: Toggle inverts unchecked state
  Given an unchecked checkbox
  When I call Toggle()
  Then IsChecked() returns true
```

### SetChecked Operations

```gherkin
Scenario: SetChecked(true) checks the checkbox
  Given an unchecked checkbox
  When I call SetChecked(true)
  Then IsChecked() returns true

Scenario: SetChecked(false) unchecks the checkbox
  Given a checked checkbox
  When I call SetChecked(false)
  Then IsChecked() returns false
```

### Disabled State

```gherkin
Scenario: Check on disabled checkbox does nothing
  Given a disabled unchecked checkbox
  When I call Check()
  Then IsChecked() still returns false
  And no exception is thrown

Scenario: Toggle on disabled checkbox does nothing
  Given a disabled checked checkbox
  When I call Toggle()
  Then IsChecked() still returns true
  And no exception is thrown
```

### Wait and Assert Operations

```gherkin
Scenario: WaitChecked succeeds when state matches
  Given a checkbox that becomes checked after 500ms
  When I call WaitChecked(true, 2000)
  Then it returns true

Scenario: AssertChecked passes for matching state
  Given a checked checkbox
  When I call AssertChecked(true, "Should be checked")
  Then no exception is thrown

Scenario: AssertChecked fails for non-matching state
  Given an unchecked checkbox
  When I call AssertChecked(true, "Should be checked")
  Then AssertionException is thrown with message "Should be checked"
```

### Nullable Skip Pattern

```gherkin
Scenario: AssertChecked with null skips assertion
  Given any checkbox
  When I call AssertChecked(null, "Message")
  Then no assertion is performed
  And no exception is thrown
```

---

## assumption

### Platform Assumptions

1. Underlying automation library supports toggle state retrieval
2. Platform correctly reports checked/unchecked state
3. Toggle action is supported by automation framework
4. State change events propagate correctly

### Framework Assumptions

1. TestContext is initialized before checkbox operations
2. Logging is available via `_context.Logger`
3. Timeout settings are configured in `_context.Timeouts`

### Element Assumptions

1. CheckBox has accessible automation properties
2. CheckBox toggle pattern or invoke pattern is available
3. State changes are reflected immediately in automation properties

---

## exclusion

### Explicitly Out of Scope

1. **Tri-state/indeterminate handling** — Treated as unchecked; no separate API
2. **Associated label click** — Clicking label to toggle is app behavior
3. **Visual state verification** — Checkmark appearance is platform-specific
4. **Group behavior** — Checkbox groups are managed by app, not framework
5. **Validation rules** — Required checkbox validation is app logic
6. **Animation during toggle** — Visual transitions are platform-specific

### Deferred to Platform Implementation

1. Focus management before toggle
2. Scroll into view behavior
3. Keyboard activation (Space key)
4. Click area calculation

---

## Platform Implementation Notes

### MAUI (AppiumElement)

```
Control: CheckBox
Locator: AutomationId, Name, XPath
IsChecked: element.GetAttribute("Checked") == "true" or element.Selected
Check: if (!IsChecked()) element.Click()
Uncheck: if (IsChecked()) element.Click()
Toggle: element.Click()
Note: Some platforms use element.Selected instead of attribute
```

### Blazor (IWebElement)

```
Control: <input type="checkbox">
Locator: id, data-testid, name, CSS selector, XPath
IsChecked: element.Selected
Check: if (!element.Selected) element.Click()
Uncheck: if (element.Selected) element.Click()
Toggle: element.Click()
Note: Use element.Selected property, not GetAttribute("checked")
```

### WPF (AutomationElement)

```
Control: System.Windows.Controls.CheckBox
Locator: AutomationId, Name
IsChecked: TogglePattern.Current.ToggleState == ToggleState.On
Check: if (state != On) TogglePattern.Toggle() until On
Uncheck: if (state != Off) TogglePattern.Toggle() until Off
Toggle: TogglePattern.Toggle()
Note: ToggleState can be On, Off, or Indeterminate
```

---

## Method Signatures

### IControlObject Methods

| Method | Signature | Returns | Description |
| ------ | --------- | ------- | ----------- |
| IsExists | `IsExists()` | `bool` | Check if element exists |
| IsVisible | `IsVisible()` | `bool` | Check if element is visible |
| IsEnabled | `IsEnabled()` | `bool` | Check if element is enabled |
| WaitExists | `WaitExists(bool? expected, int? timeoutMs)` | `bool` | Wait for existence state |
| WaitVisible | `WaitVisible(bool? expected, int? timeoutMs)` | `bool` | Wait for visibility state |
| WaitEnabled | `WaitEnabled(bool? expected, int? timeoutMs)` | `bool` | Wait for enabled state |
| AssertExists | `AssertExists(bool? expected, string? message, int? timeoutMs)` | `void` | Assert existence state |
| AssertVisible | `AssertVisible(bool? expected, string? message, int? timeoutMs)` | `void` | Assert visibility state |
| AssertEnabled | `AssertEnabled(bool? expected, string? message, int? timeoutMs)` | `void` | Assert enabled state |

### IToggleControlObject Methods

| Method | Signature | Returns | Description |
| ------ | --------- | ------- | ----------- |
| IsChecked | `IsChecked()` | `bool` | Get current checked state |
| Check | `Check()` | `void` | Set to checked state |
| Uncheck | `Uncheck()` | `void` | Set to unchecked state |
| Toggle | `Toggle()` | `void` | Invert current state |
| SetChecked | `SetChecked(bool value)` | `void` | Set to specific state |
| WaitChecked | `WaitChecked(bool? expected, int? timeoutMs)` | `bool` | Wait for checked state |
| AssertChecked | `AssertChecked(bool? expected, string? message, int? timeoutMs)` | `void` | Assert checked state |

---

## Related Documents

- [250_100_INDEX.md](250_100_INDEX.md) — Core Controls Index
- [250_001_IControlObject.spx.md](../250_000_Foundation/250_001_IControlObject.spx.md) — Base interface
- [250_005_InterfaceHierarchy.spx.md](../250_000_Foundation/250_005_InterfaceHierarchy.spx.md) — Interface hierarchy
