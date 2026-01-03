# SPEC-002b-002: Complete Interface Catalog

**Version:** 1.0  
**Status:** For Review  
**Date:** January 2026

---

## 1. Interface Overview

This document provides a complete catalog of all core interfaces defined in `Brinell.Core.Abstractions.Controls` with method signatures and capability requirements (no implementation details).

---

## 2. IControlObject - Base Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Purpose:** Base interface for all UI control abstractions  
**Implemented By:** All platform control base classes

### Properties

```
string AutomationId { get; }
  Description: The AutomationId used to locate this control
  Usage: Element identification and logging context
  Examples: "btnLogin", "txtUsername", "chkRememberMe"

IPageObject? Page { get; }
  Description: The parent page object (null for global controls)
  Usage: Logging context, page state checks
  Returns: Reference to page or null
```

### Method Groups

#### Existence Verification

```
bool IsExists()
  Purpose: Immediate check if element exists
  Returns: true if element found in DOM
  Wait/Retry: No
  Logging: No

bool WaitExists(bool expected = true, int? timeoutMs = null)
  Purpose: Wait for element to exist or not exist
  Parameters:
    - expected: true = wait for exists, false = wait for not exists
    - timeoutMs: timeout in milliseconds (uses context default if null)
  Returns: true if expected state achieved, false if timeout
  Wait/Retry: Yes
  Logging: Yes (to CSV)

void CheckExists(bool expected = true, int? timeoutMs = null)
  Purpose: Check element exists with precondition semantics
  Parameters: Same as WaitExists
  Throws: TimeoutException if expected state not met
  Used By: Action methods as precondition
  Preconditions: None

void AssertExists(string? message = null)
  Purpose: Assert element exists in test
  Parameters: message = optional assertion message
  Throws: AssertionException if element not found
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: CheckExists first

void AssertNotExists(string? message = null)
  Purpose: Assert element does not exist in test
  Parameters: message = optional assertion message
  Throws: AssertionException if element found
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: CheckExists(false) first
```

#### Visibility Verification

```
bool IsVisible()
  Purpose: Immediate check if element is visible
  Returns: true if element exists and visible
  Wait/Retry: No
  Logging: No

bool WaitVisible(bool expected = true, int? timeoutMs = null)
  Purpose: Wait for element to be visible or hidden
  Parameters: Same as WaitExists
  Returns: true if expected visibility achieved, false if timeout
  Wait/Retry: Yes
  Logging: Yes (to CSV)

void CheckVisible(bool expected = true, int? timeoutMs = null)
  Purpose: Check element visibility as precondition
  Parameters: Same as WaitExists
  Throws: TimeoutException if expected visibility not met
  Used By: Most action methods as precondition
  Preconditions: None (checks visibility directly)

void AssertVisible(string? message = null)
  Purpose: Assert element is visible
  Parameters: message = optional assertion message
  Throws: AssertionException if not visible
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: CheckVisible first

void AssertNotVisible(string? message = null)
  Purpose: Assert element is not visible
  Parameters: message = optional assertion message
  Throws: AssertionException if visible
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: CheckVisible(false) first
```

#### Enabled/Disabled Verification

```
bool IsEnabled()
  Purpose: Immediate check if element is enabled
  Returns: true if element is enabled
  Wait/Retry: No
  Logging: No

bool WaitEnabled(bool expected = true, int? timeoutMs = null)
  Purpose: Wait for element to be enabled or disabled
  Parameters: Same as WaitExists
  Returns: true if expected enabled state achieved, false if timeout
  Wait/Retry: Yes
  Logging: Yes (to CSV)

void CheckEnabled(bool expected = true, int? timeoutMs = null)
  Purpose: Check element enabled state as precondition
  Parameters: Same as WaitExists
  Throws: TimeoutException if expected enabled state not met
  Used By: Click, Enter, Select action methods as precondition
  Preconditions: None (checks enabled directly)

void AssertEnabled(string? message = null)
  Purpose: Assert element is enabled
  Parameters: message = optional assertion message
  Throws: AssertionException if disabled
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: CheckEnabled first

void AssertDisabled(string? message = null)
  Purpose: Assert element is disabled
  Parameters: message = optional assertion message
  Throws: AssertionException if enabled
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: CheckEnabled(false) first
```

#### Text/Content Queries

```
string GetText()
  Purpose: Get element text or content
  Returns: Text content of element
  Behavior:
    - For text inputs: returns input value
    - For labels/buttons: returns display text
    - For empty elements: returns empty string
  Wait/Retry: No (immediate)
  Preconditions: None (may return empty string)

void AssertTextEquals(string expected, string? message = null)
  Purpose: Assert text equals expected value
  Parameters:
    - expected: the expected text
    - message: optional assertion message
  Throws: AssertionException if text does not match
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: Waits for text equality before assertion

void AssertTextContains(string expected, string? message = null)
  Purpose: Assert text contains expected substring
  Parameters:
    - expected: the expected substring
    - message: optional assertion message
  Throws: AssertionException if text does not contain substring
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: Waits for text to contain substring before assertion
```

---

## 3. IClickableControl - Clickable Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IControlObject  
**Purpose:** Represents controls that can be clicked  
**Examples:** Button, Link, Tab, Menu Item  
**Implemented By:** ContentControlBase (MAUI/Blazor)

### Methods

```
void Click()
  Purpose: Click the control
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail, InvalidOperationException if click fails
  Logging: Action logged to CSV
  Behavior: Waits for preconditions then performs click action

void DoubleClick()
  Purpose: Double-click the control
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Waits for preconditions then performs double-click

void RightClick()
  Purpose: Right-click the control (context menu)
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Waits for preconditions then performs right-click

void Hover()
  Purpose: Hover over the control without clicking
  Preconditions: Exists → Visible (no enabled check)
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Moves mouse over element to trigger hover states/tooltips
```

---

## 4. IContentControl - Content Display Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IClickableControl  
**Purpose:** Marker interface for clickable controls with content  
**Examples:** Button, Label, Frame  
**Implemented By:** ContentControlBase (MAUI/Blazor)

**Note:** This is a marker interface. All methods inherited from IClickableControl.

---

## 5. ITextControl - Text Input Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IControlObject  
**Purpose:** Represents text input controls  
**Examples:** TextInput, TextBox, TextArea, Entry, SearchBar  
**Implemented By:** TextControlBase (MAUI/Blazor)

### Input Methods

```
void Enter(string text)
  Purpose: Enter text into the control (appends to existing)
  Parameters: text = text to enter
  Preconditions: Exists → Visible → Enabled → Not ReadOnly
  Throws: TimeoutException if preconditions fail, InvalidOperationException if field is read-only
  Logging: Action logged to CSV with text parameter
  Behavior: Does NOT clear existing text first

void Clear()
  Purpose: Clear all text in the control
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Removes all text from control

void ClearAndEnter(string text)
  Purpose: Clear existing text and enter new text
  Parameters: text = new text to set
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV with text parameter
  Behavior: Equivalent to Clear() then Enter(text)

void SetText(string text)
  Purpose: Alias for ClearAndEnter for backward compatibility
  Parameters: text = new text to set
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Same as ClearAndEnter

void Append(string text)
  Purpose: Append text to existing text
  Parameters: text = text to append
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Adds text to end of existing content
```

### State Checks

```
bool IsReadOnly()
  Purpose: Check if control is read-only
  Returns: true if control cannot be edited
  Wait/Retry: No (immediate)
  Logging: No

int GetTextLength()
  Purpose: Get the length of the current text
  Returns: Character count (0 if empty)
  Wait/Retry: No (immediate)
  Logging: No
```

### Text Assertions

```
void AssertTextEmpty(string? message = null)
  Purpose: Assert text is empty or null
  Parameters: message = optional assertion message
  Throws: AssertionException if text is not empty
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: None (waits for empty text before assertion)

void AssertTextNotEmpty(string? message = null)
  Purpose: Assert text is not empty
  Parameters: message = optional assertion message
  Throws: AssertionException if text is empty
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: None (waits for non-empty text before assertion)

void AssertTextStartsWith(string prefix, string? message = null)
  Purpose: Assert text starts with expected prefix
  Parameters:
    - prefix: expected prefix string
    - message: optional assertion message
  Throws: AssertionException if text does not start with prefix
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: None (waits for prefix match)

void AssertTextEndsWith(string suffix, string? message = null)
  Purpose: Assert text ends with expected suffix
  Parameters:
    - suffix: expected suffix string
    - message: optional assertion message
  Throws: AssertionException if text does not end with suffix
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: None (waits for suffix match)

void AssertTextMatches(string pattern, string? message = null)
  Purpose: Assert text matches regex pattern
  Parameters:
    - pattern: regex pattern to match
    - message: optional assertion message
  Throws: AssertionException if text does not match pattern
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: None (waits for pattern match)
```

---

## 6. IEditableTextControl - Editable Text Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** ITextControl  
**Purpose:** Text controls with editing capabilities  
**Examples:** TextBox, TextArea, MultilineEntry  
**Implemented By:** EditableTextControlBase (MAUI/Blazor)

### Methods

```
void Focus()
  Purpose: Set focus to the control
  Preconditions: Exists → Visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Focuses the control

void SelectAll()
  Purpose: Select all text in the control
  Preconditions: Exists → Visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Selects all text content

void Copy()
  Purpose: Copy selected text to clipboard
  Preconditions: Exists → Visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Copies current selection to system clipboard

void Cut()
  Purpose: Cut selected text to clipboard
  Preconditions: Exists → Visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Cuts current selection to system clipboard and removes from control

void Paste()
  Purpose: Paste from clipboard
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Pastes clipboard content at cursor position
```

---

## 7. IToggleControl - Toggle/Boolean Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IControlObject  
**Purpose:** Represents toggle/boolean controls  
**Examples:** CheckBox, Switch, RadioButton, ToggleButton  
**Implemented By:** ToggleControlBase (MAUI/Blazor)

### State Checks

```
bool IsChecked()
  Purpose: Check if control is currently checked/on
  Returns: true if checked, false if unchecked
  Wait/Retry: No (immediate)
  Logging: No

bool WaitChecked(bool expected = true, int? timeoutMs = null)
  Purpose: Wait for checked state to change
  Parameters:
    - expected: true = wait for checked, false = wait for unchecked
    - timeoutMs: timeout in milliseconds
  Returns: true if expected state achieved, false if timeout
  Wait/Retry: Yes
  Logging: Yes (to CSV)
```

### Action Methods

```
void Toggle()
  Purpose: Toggle the control state (checked ↔ unchecked)
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Toggles between checked and unchecked states

void Check()
  Purpose: Set the control to checked/on state
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Ensures control is checked; if already checked, no-op

void Uncheck()
  Purpose: Set the control to unchecked/off state
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Ensures control is unchecked; if already unchecked, no-op

void SetChecked(bool value)
  Purpose: Set checked state to specific value
  Parameters: value = true to check, false to uncheck
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Equivalent to Check() if value=true, Uncheck() if value=false
```

### Assertions

```
void AssertChecked(string? message = null)
  Purpose: Assert control is checked
  Parameters: message = optional assertion message
  Throws: AssertionException if unchecked
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: Waits for checked state before assertion

void AssertUnchecked(string? message = null)
  Purpose: Assert control is unchecked
  Parameters: message = optional assertion message
  Throws: AssertionException if checked
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: Waits for unchecked state before assertion
```

---

## 8. ISelectorControl - Selector Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IControlObject  
**Purpose:** Represents selection controls  
**Examples:** Dropdown, ComboBox, ListBox, Picker  
**Implemented By:** SelectorControlBase (MAUI/Blazor)

### Selection Methods

```
void SelectByIndex(int index)
  Purpose: Select an item by zero-based index
  Parameters: index = 0-based index of item
  Preconditions: Exists → Visible → Enabled
  Throws:
    - TimeoutException if preconditions fail
    - ArgumentOutOfRangeException if index out of range
  Logging: Action logged to CSV with index parameter
  Behavior: Selects the item at specified index

void SelectByText(string text)
  Purpose: Select an item by its display text
  Parameters: text = display text of item to select
  Preconditions: Exists → Visible → Enabled
  Throws:
    - TimeoutException if preconditions fail
    - ArgumentException if item with text not found
  Logging: Action logged to CSV with text parameter
  Behavior: Finds and selects item with matching text
```

### State Queries

```
string? GetSelectedText()
  Purpose: Get the text of currently selected item
  Returns: Display text of selected item, or null if nothing selected
  Wait/Retry: No (immediate)
  Logging: No

int GetSelectedIndex()
  Purpose: Get the index of currently selected item
  Returns: Zero-based index, or -1 if nothing selected
  Wait/Retry: No (immediate)
  Logging: No

IReadOnlyList<string> GetItems()
  Purpose: Get all available items
  Returns: Read-only list of item display texts
  Wait/Retry: No (immediate)
  Logging: No

int GetItemCount()
  Purpose: Get the count of available items
  Returns: Number of items in selector
  Wait/Retry: No (immediate)
  Logging: No
```

### Assertions

```
void AssertSelectedText(string expected, string? message = null)
  Purpose: Assert selected item text equals expected
  Parameters:
    - expected: expected selected item text
    - message: optional assertion message
  Throws: AssertionException if selected text does not match
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: Waits for selection to match before assertion
```

---

## 9. IRangeControl - Range Value Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IControlObject  
**Purpose:** Represents controls with numeric range values  
**Examples:** Slider, ProgressBar, RangeInput, Stepper  
**Implemented By:** RangeControlBase (MAUI/Blazor)

### Value Access

```
double GetValue()
  Purpose: Get the current value
  Returns: Current numeric value
  Wait/Retry: No (immediate)
  Logging: No

double GetMinimum()
  Purpose: Get the minimum allowed value
  Returns: Minimum value
  Wait/Retry: No (immediate)
  Logging: No

double GetMaximum()
  Purpose: Get the maximum allowed value
  Returns: Maximum value
  Wait/Retry: No (immediate)
  Logging: No
```

### Value Changes

```
void SetValue(double value)
  Purpose: Set the value to a specific number
  Parameters: value = new value (must be between min and max)
  Preconditions: Exists → Visible → Enabled
  Throws:
    - TimeoutException if preconditions fail
    - ArgumentOutOfRangeException if value outside min/max range
  Logging: Action logged to CSV with value parameter
  Behavior: Sets value; throws if value outside valid range

void Increment()
  Purpose: Increment the value by one step
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Increases value by platform-defined step

void Decrement()
  Purpose: Decrement the value by one step
  Preconditions: Exists → Visible → Enabled
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Decreases value by platform-defined step
```

### Assertions

```
void AssertValue(double expected, double tolerance = 0.001, string? message = null)
  Purpose: Assert value equals expected (with floating-point tolerance)
  Parameters:
    - expected: expected value
    - tolerance: tolerance for floating-point comparison
    - message: optional assertion message
  Throws: AssertionException if value does not match (within tolerance)
  Logging: Pass/Fail to CSV, screenshot on failure
  Preconditions: Waits for value to equal expected before assertion
```

---

## 10. ISlider - Slider Specialization

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IRangeControl  
**Purpose:** Specialized interface for slider controls  
**Examples:** Slider, SeekBar  
**Implemented By:** RangeControlBase (MAUI/Blazor)

**Note:** Inherits all methods from IRangeControl. May add slider-specific methods in future.

---

## 11. IItemsControl - Collection Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IControlObject  
**Purpose:** Represents controls containing collections of items  
**Examples:** List, Grid, DataTable, Carousel, CollectionView  
**Implemented By:** ItemsControlBase (MAUI/Blazor)

### Item Access

```
int GetItemCount()
  Purpose: Get the count of items in the collection
  Returns: Number of items
  Wait/Retry: No (immediate)
  Logging: No

string GetItemText(int index)
  Purpose: Get the text/content of an item at specific index
  Parameters: index = 0-based index
  Returns: Display text of item
  Throws: ArgumentOutOfRangeException if index out of range
  Wait/Retry: No (immediate)
  Logging: No

bool HasItem(string text)
  Purpose: Check if item with specific text exists
  Parameters: text = text to search for
  Returns: true if item found
  Wait/Retry: No (immediate)
  Logging: No
```

### Item Interaction

```
void ClickItem(int index)
  Purpose: Click an item by index
  Parameters: index = 0-based index
  Preconditions: Exists → Visible → Item exists
  Throws:
    - TimeoutException if preconditions fail
    - ArgumentOutOfRangeException if index out of range
  Logging: Action logged to CSV with index parameter
  Behavior: Clicks the item at specified index

void ClickItem(string text)
  Purpose: Click an item by text
  Parameters: text = display text of item to click
  Preconditions: Exists → Visible → Item exists
  Throws:
    - TimeoutException if preconditions fail
    - ArgumentException if item not found
  Logging: Action logged to CSV with text parameter
  Behavior: Finds and clicks item with matching text
```

---

## 12. IContainerControl - Container Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IControlObject  
**Purpose:** Represents container controls holding child controls  
**Examples:** Panel, Frame, GroupBox, ContentView  
**Implemented By:** ContainerControlBase (MAUI/Blazor)

### Child Access

```
int GetChildCount()
  Purpose: Get the count of child controls
  Returns: Number of direct children
  Wait/Retry: No (immediate)
  Logging: No

IReadOnlyList<string> GetChildNames()
  Purpose: Get AutomationIds of all child controls
  Returns: Read-only list of child AutomationIds
  Wait/Retry: No (immediate)
  Logging: No

bool ChildExists(string childName)
  Purpose: Check if a specific child control exists
  Parameters: childName = AutomationId of child
  Returns: true if child exists
  Wait/Retry: No (immediate)
  Logging: No

T GetChild<T>(string childName) where T : IControlObject
  Purpose: Get a child control by AutomationId
  Parameters: childName = AutomationId of child
  Returns: Child control typed as T
  Throws: ArgumentException if child not found
  Wait/Retry: No (immediate)
  Logging: No
  Note: Used for container-scoped controls and typed access
```

---

## 13. IScrollableControl - Scrollable Controls

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IControlObject  
**Purpose:** Represents controls supporting scrolling  
**Examples:** ScrollView, ScrollableContainer  
**Implemented By:** ScrollableControlBase (MAUI/Blazor)

### Scroll Methods

```
void ScrollToElement(string automationId)
  Purpose: Scroll until element with ID is visible
  Parameters: automationId = automation ID of element to scroll to
  Preconditions: Container exists → visible
  Throws: TimeoutException if element cannot be made visible
  Logging: Action logged to CSV
  Behavior: Scrolls container until target element is in viewport

void ScrollToTop()
  Purpose: Scroll to top of content
  Preconditions: Container exists → visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Scrolls to start of content

void ScrollToBottom()
  Purpose: Scroll to bottom of content
  Preconditions: Container exists → visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Scrolls to end of content

void ScrollUp(int distance = 100)
  Purpose: Scroll up by specified distance
  Parameters: distance = platform-specific units (pixels, points, etc.)
  Preconditions: Container exists → visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Scrolls up by distance

void ScrollDown(int distance = 100)
  Purpose: Scroll down by specified distance
  Parameters: distance = platform-specific units
  Preconditions: Container exists → visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Scrolls down by distance

void ScrollLeft(int distance = 100)
  Purpose: Scroll left by specified distance
  Parameters: distance = platform-specific units
  Preconditions: Container exists → visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Scrolls left by distance

void ScrollRight(int distance = 100)
  Purpose: Scroll right by specified distance
  Parameters: distance = platform-specific units
  Preconditions: Container exists → visible
  Throws: TimeoutException if preconditions fail
  Logging: Action logged to CSV
  Behavior: Scrolls right by distance
```

---

## 14. Specialized Control Interfaces

### 14.1 IButton - Button Control

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IContentControl  
**Purpose:** Specialization for button controls  
**Examples:** Button, PrimaryButton, AccentButton  
**Note:** Marker interface; inherits all methods from IContentControl

### 14.2 ILabel - Label Control

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IContentControl  
**Purpose:** Specialization for label/text display controls  
**Examples:** Label, StaticText  
**Note:** Marker interface; inherits all methods from IContentControl

### 14.3 ICheckBox - CheckBox Control

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IToggleControl  
**Purpose:** Specialization for checkbox controls  
**Examples:** CheckBox, MultiSelectCheckBox  
**Note:** Marker interface; inherits all methods from IToggleControl

### 14.4 IComboBox - ComboBox Control

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** ISelectorControl  
**Purpose:** Specialization for combo box/dropdown controls  
**Examples:** ComboBox, DropdownList, Picker  
**Note:** Marker interface; inherits all methods from ISelectorControl

### 14.5 IListBox - ListBox Control

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** ISelectorControl  
**Purpose:** Specialization for list box controls  
**Examples:** ListBox, SelectList  
**Note:** Marker interface; inherits all methods from ISelectorControl

### 14.6 ITextBox - TextBox Control

**Namespace:** `Brinell.Core.Abstractions.Controls`  
**Extends:** IEditableTextControl  
**Purpose:** Specialization for text box controls  
**Examples:** TextBox, Entry  
**Note:** Marker interface; inherits all methods from IEditableTextControl

---

## 15. Proposed New Interfaces (Future)

### 15.1 IContainer - Generic Container

**Proposed For:** Better distinction between generic and items containers

```
Extends: IContainerControl
Methods: GetChild<T>(string) - typed child access
Purpose: Generic container without items collection
```

### 15.2 IListContainer - Items Container

**Proposed For:** Containers that hold repeated items

```
Extends: IContainerControl, IItemsControl
Methods: GetItemCount(), GetItemText(int), ClickItem()
Purpose: Container with both child access and items collection
```

---

## 16. Method Signature Pattern Reference

### 16.1 Consistent Parameter Naming

```
string message = null        // Assertion message
int? timeoutMs = null        // Timeout override
bool expected = true         // For boolean waits
int distance = 100           // Scroll distance
double tolerance = 0.001     // For floating-point comparisons
int index                    // Zero-based index
string text                  // Display text or input string
double value                 // Numeric value
string pattern               // Regex pattern
string automationId          // Element identifier
string childName             // Child element identifier
```

### 16.2 Return Value Patterns

```
bool      // Is/Wait methods (immediate state or polling success)
void      // Check/Assert/Action methods (throw on failure)
string    // Text queries
int       // Count/index queries
double    // Value queries
T         // Generic child queries
```

---

## 17. Exception Types

| Exception | Thrown By | Meaning |
|-----------|-----------|---------|
| **TimeoutException** | Check*, Wait* (on false), Action* | Condition not met within timeout |
| **AssertionException** | Assert* | Test assertion failed |
| **ArgumentException** | SelectByText, ClickItem(string), GetChild<T> | Parameter value not found |
| **ArgumentOutOfRangeException** | SelectByIndex, ClickItem(int), SetValue | Index/value out of valid range |
| **InvalidOperationException** | Click, Enter (read-only) | Operation not valid for state |

---

*End of Catalog*
