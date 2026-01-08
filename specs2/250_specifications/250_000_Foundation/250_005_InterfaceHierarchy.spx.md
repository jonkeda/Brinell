# 250.005 Interface Hierarchy Specification

**Block Type:** SPC (Specification)  
**ID:** 250.005  
**Title:** Complete Interface Hierarchy Specification  
**Status:** Draft  
**Version:** 1.0  
**Level:** 0 - Foundation

---

## 1. Overview

This specification defines the complete interface hierarchy for the Brinell framework. All capability interfaces extend `IControlObject` and add specific functionality for different control types.

### Hierarchy Principle

> **Capability-based, not control-based.** Interfaces define what a control can do, not what it is.

---

## 2. Interface Hierarchy Diagram

```
IControlObject                          # Base for all controls
│
├── IClickableControlObject             # Click capability
│   └── ILongPressControlObject         # Long press (mobile)
│
├── ITextControlObject                  # Text display
│   └── IEditableTextControlObject      # Text input
│
├── IToggleControlObject                # On/off state
│
├── ISelectorControlObject              # Single selection
│   └── IMultiSelectorControlObject     # Multi-selection
│
├── IRangeControlObject                 # Numeric range
│
├── IContainerControlObject             # Single-content container (ContentControl, Frame)
│   └── IListContainerControlObject     # List container (ListView, ItemsControl)
│       └── IDataGridControlObject      # Row/cell access
│
├── IWindowControlObject                # Window/dialog
│
├── IScrollableControlObject            # Scrolling
│
├── IDateTimeControlObject              # Date/time
│
└── IWebViewControlObject               # Web content

IPageObject                             # Base for all pages
│
├── IContainerControl                   # Page-level container scoping
│
└── IBusyPageObject                     # Busy/loading state tracking
```

---

## 3. Capability Interfaces

### 3.1 IClickableControlObject

Click actions for buttons, links, images.

```csharp
public interface IClickableControlObject : IControlObject
{
    /// <summary>
    /// Check if the control is clickable (visible and enabled).
    /// </summary>
    /// <returns>True if clickable, false if not, null if element not found.</returns>
    bool? IsClickable();
    
    /// <summary>
    /// Perform a single click on the control.
    /// </summary>
    /// <param name="timeoutMs">Timeout to wait for clickable state. Null = use default.</param>
    void Click(int? timeoutMs = null);
    
    /// <summary>
    /// Perform a double-click on the control.
    /// </summary>
    void DoubleClick(int? timeoutMs = null);
    
    /// <summary>
    /// Perform a right-click (context click) on the control.
    /// </summary>
    void RightClick(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until control is clickable (visible and enabled).
    /// </summary>
    /// <param name="clickable">Expected clickable state. Null = skip.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    bool WaitClickable(bool? clickable, int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: Button, ImageButton, Image, TapGestureRecognizer
- Blazor: button, a, img, clickable div
- WPF: Button, Image, Hyperlink

### 3.2 ILongPressControlObject

Long press for mobile platforms.

```csharp
public interface ILongPressControlObject : IClickableControlObject
{
    /// <summary>
    /// Perform a long press (press and hold) on the control.
    /// </summary>
    /// <param name="durationMs">Hold duration in milliseconds. Null = platform default.</param>
    /// <param name="timeoutMs">Timeout to wait for element. Null = use default.</param>
    void LongPress(int? durationMs = null, int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: Controls with LongPressGestureRecognizer
- Blazor: Not applicable (use context menu)
- WPF: Not applicable

### 3.3 ITextControlObject

Text display and verification.

```csharp
public interface ITextControlObject : IControlObject
{
    // GetText, AssertText, AssertTextContains inherited from IControlObject
    
    /// <summary>
    /// Wait until text matches expected value.
    /// </summary>
    /// <param name="expected">Expected text. Null = skip.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    bool WaitTextEquals(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until text contains expected substring.
    /// </summary>
    bool WaitTextContains(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert text matches pattern (regex).
    /// </summary>
    /// <param name="pattern">Regex pattern. Null = skip.</param>
    /// <param name="message">Custom failure message. Null = use default.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: Label, Span
- Blazor: span, label, p, h1-h6, div
- WPF: Label, TextBlock

### 3.4 IEditableTextControlObject

Text input capabilities.

```csharp
public interface IEditableTextControlObject : ITextControlObject
{
    /// <summary>
    /// Enter text into the control (appends to existing).
    /// </summary>
    /// <param name="text">Text to enter. Null = skip.</param>
    /// <param name="timeoutMs">Timeout to wait for element. Null = use default.</param>
    void Enter(string? text, int? timeoutMs = null);
    
    /// <summary>
    /// Clear the control's text content.
    /// </summary>
    void Clear(int? timeoutMs = null);
    
    /// <summary>
    /// Set the control's text (clears first, then enters).
    /// </summary>
    /// <param name="text">Text to set. Null = skip.</param>
    void SetText(string? text, int? timeoutMs = null);
    
    /// <summary>
    /// Get the placeholder/hint text.
    /// </summary>
    /// <returns>Placeholder text, or null if not available.</returns>
    string? GetPlaceholder();
    
    /// <summary>
    /// Check if the control is read-only.
    /// </summary>
    /// <returns>True if read-only, false if editable, null if element not found.</returns>
    bool? IsReadOnly();
}
```

**Platform Coverage:**
- MAUI: Entry, Editor, SearchBar
- Blazor: input[text], input[email], input[password], textarea
- WPF: TextBox, RichTextBox, PasswordBox

### 3.5 IToggleControlObject

Toggle state for checkboxes, switches.

```csharp
public interface IToggleControlObject : IControlObject
{
    /// <summary>
    /// Check if the control is in checked/on state.
    /// </summary>
    /// <returns>True if checked, false if unchecked, null if element not found.</returns>
    bool? IsChecked();
    
    /// <summary>
    /// Toggle the control state.
    /// </summary>
    void Toggle(int? timeoutMs = null);
    
    /// <summary>
    /// Set the control to checked/unchecked state.
    /// </summary>
    /// <param name="checked">Desired state. Null = skip.</param>
    void SetChecked(bool? @checked, int? timeoutMs = null);
    
    /// <summary>
    /// Set to checked state (convenience method).
    /// </summary>
    void Check(int? timeoutMs = null);
    
    /// <summary>
    /// Set to unchecked state (convenience method).
    /// </summary>
    void Uncheck(int? timeoutMs = null);
    
    /// <summary>
    /// Assert checked state matches expected value.
    /// </summary>
    void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until checked state matches expected value.
    /// </summary>
    bool WaitChecked(bool? expected, int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: CheckBox, Switch, RadioButton
- Blazor: input[checkbox], input[radio]
- WPF: CheckBox, RadioButton, ToggleButton

### 3.6 ISelectorControlObject

Single selection from list.

```csharp
public interface ISelectorControlObject : IControlObject
{
    /// <summary>
    /// Select item by visible text.
    /// </summary>
    /// <param name="text">Text to select. Null = skip.</param>
    void SelectByText(string? text, int? timeoutMs = null);
    
    /// <summary>
    /// Select item by index (0-based).
    /// </summary>
    /// <param name="index">Index to select. Null = skip.</param>
    void SelectByIndex(int? index, int? timeoutMs = null);
    
    /// <summary>
    /// Select item by value attribute.
    /// </summary>
    /// <param name="value">Value to select. Null = skip.</param>
    void SelectByValue(string? value, int? timeoutMs = null);
    
    /// <summary>
    /// Get the currently selected item's text.
    /// </summary>
    /// <returns>Selected text, or null if element not found.</returns>
    string? GetSelectedText(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until selected text matches expected value.
    /// </summary>
    /// <param name="expected">Expected text. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitSelectedText(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert selected text matches expected value.
    /// </summary>
    void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get the currently selected item's index.
    /// </summary>
    /// <returns>Selected index, or null if element not found.</returns>
    int? GetSelectedIndex(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until selected index matches expected value.
    /// </summary>
    /// <param name="expected">Expected index. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert selected index matches expected value.
    /// </summary>
    void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get all available item texts.
    /// </summary>
    /// <returns>List of item texts, or null if element not found.</returns>
    IReadOnlyList<string>? GetItemTexts(int? timeoutMs = null);
    
    /// <summary>
    /// Get the count of available items.
    /// </summary>
    /// <returns>Item count, or null if element not found.</returns>
    int? GetItemCount(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until item count matches expected value.
    /// </summary>
    /// <param name="expected">Expected count. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitItemCount(int? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert item count matches expected value.
    /// </summary>
    void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: Picker
- Blazor: select
- WPF: ComboBox

### 3.7 IMultiSelectorControlObject

Multi-selection from list.

```csharp
public interface IMultiSelectorControlObject : ISelectorControlObject
{
    /// <summary>
    /// Select multiple items by text.
    /// </summary>
    /// <param name="texts">Texts to select. Null = skip.</param>
    void SelectMultipleByText(IEnumerable<string>? texts, int? timeoutMs = null);
    
    /// <summary>
    /// Select multiple items by indices.
    /// </summary>
    /// <param name="indices">Indices to select. Null = skip.</param>
    void SelectMultipleByIndex(IEnumerable<int>? indices, int? timeoutMs = null);
    
    /// <summary>
    /// Deselect item by text.
    /// </summary>
    void DeselectByText(string? text, int? timeoutMs = null);
    
    /// <summary>
    /// Deselect item by index.
    /// </summary>
    void DeselectByIndex(int? index, int? timeoutMs = null);
    
    /// <summary>
    /// Deselect all items.
    /// </summary>
    void DeselectAll(int? timeoutMs = null);
    
    /// <summary>
    /// Get all selected item texts.
    /// </summary>
    /// <returns>List of selected texts, or null if element not found.</returns>
    IReadOnlyList<string>? GetSelectedTexts(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until selected texts match expected values.
    /// </summary>
    /// <param name="expected">Expected texts. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitSelectedTexts(IEnumerable<string>? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert selected texts match expected values.
    /// </summary>
    void AssertSelectedTexts(IEnumerable<string>? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get all selected item indices.
    /// </summary>
    /// <returns>List of selected indices, or null if element not found.</returns>
    IReadOnlyList<int>? GetSelectedIndices(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until selected indices match expected values.
    /// </summary>
    /// <param name="expected">Expected indices. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitSelectedIndices(IEnumerable<int>? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert selected indices match expected values.
    /// </summary>
    void AssertSelectedIndices(IEnumerable<int>? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get count of selected items.
    /// </summary>
    /// <returns>Selected count, or null if element not found.</returns>
    int? GetSelectedCount(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until selected count matches expected value.
    /// </summary>
    /// <param name="expected">Expected count. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitSelectedCount(int? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert selected count matches expected value.
    /// </summary>
    void AssertSelectedCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: Not natively supported (custom implementation)
- Blazor: select[multiple]
- WPF: ListBox (SelectionMode=Multiple)

### 3.8 IRangeControlObject

Numeric range for sliders, progress bars.

```csharp
public interface IRangeControlObject : IControlObject
{
    /// <summary>
    /// Get the current value.
    /// </summary>
    /// <returns>Current value, or null if element not found.</returns>
    double? GetValue(int? timeoutMs = null);
    
    /// <summary>
    /// Set the value.
    /// </summary>
    /// <param name="value">Value to set. Null = skip.</param>
    void SetValue(double? value, int? timeoutMs = null);
    
    /// <summary>
    /// Get the minimum allowed value.
    /// </summary>
    /// <returns>Minimum value, or null if element not found.</returns>
    double? GetMinimum(int? timeoutMs = null);
    
    /// <summary>
    /// Get the maximum allowed value.
    /// </summary>
    /// <returns>Maximum value, or null if element not found.</returns>
    double? GetMaximum(int? timeoutMs = null);
    
    /// <summary>
    /// Get the step/increment value.
    /// </summary>
    /// <returns>Step value, or null if element not found.</returns>
    double? GetStep(int? timeoutMs = null);
    
    /// <summary>
    /// Assert value equals expected (within tolerance).
    /// </summary>
    /// <param name="expected">Expected value. Null = skip.</param>
    /// <param name="tolerance">Acceptable difference. Default 0.001.</param>
    void AssertValue(double? expected, double tolerance = 0.001, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until value equals expected (within tolerance).
    /// </summary>
    bool WaitValue(double? expected, double tolerance = 0.001, int? timeoutMs = null);
    
    /// <summary>
    /// Increment value by step amount.
    /// </summary>
    void Increment(int? timeoutMs = null);
    
    /// <summary>
    /// Decrement value by step amount.
    /// </summary>
    void Decrement(int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: Slider, Stepper, ProgressBar (read-only)
- Blazor: input[range], progress
- WPF: Slider, ProgressBar (read-only)

### 3.9 IContainerControlObject

Single-content container for ContentControl, Frame, Border, Panel.

See [250_003_IContainerControlObject.spx.md](250_003_IContainerControlObject.spx.md) for full specification.

### 3.10 IListContainerControlObject

List-based container for ListView, CollectionView, ItemsControl, repeating elements.

See [250_003b_IListContainerControlObject.spx.md](250_003b_IListContainerControlObject.spx.md) for full specification.

### 3.11 IWindowControlObject

Window/dialog for modals, popups.

```csharp
public interface IWindowControlObject : IControlObject
{
    /// <summary>
    /// Check if the window/dialog is open.
    /// </summary>
    /// <returns>True if open, false if closed, null if element not found.</returns>
    bool? IsOpen(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until window open state matches expected.
    /// </summary>
    bool WaitOpen(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Close the window/dialog.
    /// </summary>
    void Close(int? timeoutMs = null);
    
    /// <summary>
    /// Get the window title.
    /// </summary>
    /// <returns>Window title, or null if element not found.</returns>
    string? GetWindowTitle(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until window title matches expected value.
    /// </summary>
    /// <param name="expected">Expected title. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitWindowTitle(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert window title matches expected value.
    /// </summary>
    void AssertWindowTitle(string? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Assert window is open/closed.
    /// </summary>
    void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: Popup, custom dialogs
- Blazor: Modal dialogs (Bootstrap, MudBlazor, etc.)
- WPF: Window, dialogs

### 3.12 IDataGridControlObject

Row/cell access for data grids.

```csharp
public interface IDataGridControlObject : IListContainerControlObject
{
    /// <summary>
    /// Get number of columns.
    /// </summary>
    /// <returns>Column count, or null if element not found.</returns>
    int? GetColumnCount(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until column count matches expected value.
    /// </summary>
    /// <param name="expected">Expected count. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitColumnCount(int? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert column count matches expected value.
    /// </summary>
    void AssertColumnCount(int? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get column headers.
    /// </summary>
    /// <returns>List of column headers, or null if element not found.</returns>
    IReadOnlyList<string>? GetColumnHeaders(int? timeoutMs = null);
    
    /// <summary>
    /// Get cell value by row and column index.
    /// </summary>
    /// <returns>Cell text, or null if element not found.</returns>
    string? GetCellText(int rowIndex, int columnIndex, int? timeoutMs = null);
    
    /// <summary>
    /// Get cell value by row index and column name.
    /// </summary>
    /// <returns>Cell text, or null if element not found.</returns>
    string? GetCellText(int rowIndex, string columnName, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until cell text matches expected value.
    /// </summary>
    /// <param name="rowIndex">Row index.</param>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="expected">Expected text. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitCellText(int rowIndex, int columnIndex, string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert cell text matches expected value.
    /// </summary>
    void AssertCellText(int rowIndex, int columnIndex, string? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get locator for cell at position.
    /// </summary>
    Locator GetCellLocator(int rowIndex, int columnIndex);
    
    /// <summary>
    /// Get all values in a row.
    /// </summary>
    /// <returns>List of row values, or null if element not found.</returns>
    IReadOnlyList<string>? GetRowValues(int rowIndex, int? timeoutMs = null);
    
    /// <summary>
    /// Get all values in a column.
    /// </summary>
    /// <returns>List of column values, or null if element not found.</returns>
    IReadOnlyList<string>? GetColumnValues(int columnIndex, int? timeoutMs = null);
    
    /// <summary>
    /// Find row index containing text in specified column.
    /// </summary>
    int? FindRowIndex(int columnIndex, string text, int? timeoutMs = null);
    
    /// <summary>
    /// Click on cell to select/edit.
    /// </summary>
    void ClickCell(int rowIndex, int columnIndex, int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: Custom DataGrid controls
- Blazor: table, DataGrid components
- WPF: DataGrid

### 3.13 IScrollableControlObject

Scrolling capability.

```csharp
public interface IScrollableControlObject : IControlObject
{
    /// <summary>
    /// Scroll to top of the scrollable area.
    /// </summary>
    void ScrollToTop(int? timeoutMs = null);
    
    /// <summary>
    /// Scroll to bottom of the scrollable area.
    /// </summary>
    void ScrollToEnd(int? timeoutMs = null);
    
    /// <summary>
    /// Scroll by specified amount (positive = down/right).
    /// </summary>
    /// <param name="deltaX">Horizontal scroll amount.</param>
    /// <param name="deltaY">Vertical scroll amount.</param>
    void ScrollBy(int deltaX, int deltaY, int? timeoutMs = null);
    
    /// <summary>
    /// Scroll to make element at locator visible.
    /// </summary>
    /// <param name="locator">Element to scroll to.</param>
    void ScrollTo(Locator locator, int? timeoutMs = null);
    
    /// <summary>
    /// Get vertical scroll position (0-100 percent).
    /// </summary>
    /// <returns>Scroll position, or null if element not found.</returns>
    double? GetScrollPosition(int? timeoutMs = null);
    
    /// <summary>
    /// Set vertical scroll position (0-100 percent).
    /// </summary>
    void SetScrollPosition(double percent, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until scroll position matches expected value.
    /// </summary>
    /// <param name="expected">Expected position (0-100). Null = skip.</param>
    /// <param name="tolerance">Acceptable difference. Default 1.0.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitScrollPosition(double? expected, double tolerance = 1.0, int? timeoutMs = null);
    
    /// <summary>
    /// Assert scroll position matches expected value.
    /// </summary>
    void AssertScrollPosition(double? expected, double tolerance = 1.0, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Check if more content is available to scroll.
    /// </summary>
    /// <returns>True if can scroll down, false if at bottom, null if element not found.</returns>
    bool? CanScrollDown(int? timeoutMs = null);
    
    /// <summary>
    /// Check if can scroll up from current position.
    /// </summary>
    /// <returns>True if can scroll up, false if at top, null if element not found.</returns>
    bool? CanScrollUp(int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: ScrollView, ListView, CollectionView
- Blazor: Scrollable div elements
- WPF: ScrollViewer

### 3.14 IDateTimeControlObject

Date and time input.

```csharp
public interface IDateTimeControlObject : IControlObject
{
    /// <summary>
    /// Get the current date value.
    /// </summary>
    DateTime? GetDate(int? timeoutMs = null);
    
    /// <summary>
    /// Set the date value.
    /// </summary>
    /// <param name="date">Date to set. Null = skip.</param>
    void SetDate(DateTime? date, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until date matches expected value.
    /// </summary>
    /// <param name="expected">Expected date. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitDate(DateTime? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert date matches expected value.
    /// </summary>
    void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get the current time value.
    /// </summary>
    TimeSpan? GetTime(int? timeoutMs = null);
    
    /// <summary>
    /// Set the time value.
    /// </summary>
    /// <param name="time">Time to set. Null = skip.</param>
    void SetTime(TimeSpan? time, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until time matches expected value.
    /// </summary>
    /// <param name="expected">Expected time. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitTime(TimeSpan? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert time matches expected value.
    /// </summary>
    void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get combined date and time.
    /// </summary>
    DateTime? GetDateTime(int? timeoutMs = null);
    
    /// <summary>
    /// Set combined date and time.
    /// </summary>
    void SetDateTime(DateTime? dateTime, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until date and time match expected value.
    /// </summary>
    /// <param name="expected">Expected date and time. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitDateTime(DateTime? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert date and time match expected value.
    /// </summary>
    void AssertDateTime(DateTime? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Get minimum allowed date.
    /// </summary>
    DateTime? GetMinDate(int? timeoutMs = null);
    
    /// <summary>
    /// Get maximum allowed date.
    /// </summary>
    DateTime? GetMaxDate(int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: DatePicker, TimePicker
- Blazor: input[date], input[time], input[datetime-local]
- WPF: DatePicker

### 3.15 IWebViewControlObject

Embedded web content.

```csharp
public interface IWebViewControlObject : IControlObject
{
    /// <summary>
    /// Get the current URL.
    /// </summary>
    /// <returns>Current URL, or null if element not found.</returns>
    string? GetUrl(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until URL matches expected value.
    /// </summary>
    /// <param name="expected">Expected URL. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitUrl(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert URL matches expected value.
    /// </summary>
    void AssertUrl(string? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Navigate to URL.
    /// </summary>
    void Navigate(string url, int? timeoutMs = null);
    
    /// <summary>
    /// Execute JavaScript and return result.
    /// </summary>
    /// <typeparam name="T">Expected return type.</typeparam>
    /// <param name="script">JavaScript to execute.</param>
    T? ExecuteScript<T>(string script, int? timeoutMs = null);
    
    /// <summary>
    /// Wait for page to finish loading.
    /// </summary>
    bool WaitForLoad(int? timeoutMs = null);
    
    /// <summary>
    /// Get the page title.
    /// </summary>
    /// <returns>Page title, or null if element not found.</returns>
    string? GetTitle(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until page title matches expected value.
    /// </summary>
    /// <param name="expected">Expected title. Null = skip.</param>
    /// <returns>True if matched, false if timeout.</returns>
    bool WaitTitle(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert page title matches expected value.
    /// </summary>
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Check if web content is loaded.
    /// </summary>
    /// <returns>True if loaded, false if not loaded, null if element not found.</returns>
    bool? IsLoaded(int? timeoutMs = null);
}
```

**Platform Coverage:**
- MAUI: WebView
- Blazor: iframe
- WPF: WebBrowser, WebView2

---

## 4. Page Object Interfaces

### 4.1 IBusyPageObject

Busy/loading state tracking for pages with loading indicators.

```csharp
public interface IBusyPageObject : IPageObject
{
    /// <summary>
    /// Check if the page is currently showing a busy indicator.
    /// </summary>
    /// <returns>True if busy indicator is visible, false otherwise.</returns>
    bool IsBusy();
    
    /// <summary>
    /// Wait until the page is no longer busy.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    /// <returns>True if page became not busy, false if timeout.</returns>
    bool WaitForNotBusy(int? timeoutMs = null);
    
    /// <summary>
    /// Assert that the page is not in busy state.
    /// </summary>
    /// <param name="message">Custom failure message. Null = use default.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Null = use default.</param>
    void AssertNotBusy(string? message = null, int? timeoutMs = null);
}
```

**Implementation Notes:**
- Pages implement this interface when they have identifiable busy indicators
- Busy indicator locator is page-specific (spinner, overlay, skeleton loader, etc.)
- Implementation typically checks for presence/visibility of busy indicator element
- `IsBusy()` returns false if element cannot be found (no indicator = not busy)

**Platform Coverage:**
- MAUI: ActivityIndicator, custom loading overlays
- Blazor: Loading spinners, skeleton screens, progress overlays
- WPF: ProgressBar (indeterminate), custom busy indicators

---

## 5. Platform Coverage Matrix

| Interface | MAUI | Blazor | WPF |
|-----------|------|--------|-----|
| IControlObject | ✅ | ✅ | ✅ |
| IClickableControlObject | ✅ | ✅ | ✅ |
| ILongPressControlObject | ✅ | ❌ | ❌ |
| ITextControlObject | ✅ | ✅ | ✅ |
| IEditableTextControlObject | ✅ | ✅ | ✅ |
| IToggleControlObject | ✅ | ✅ | ✅ |
| ISelectorControlObject | ✅ | ✅ | ✅ |
| IMultiSelectorControlObject | ⚠️ | ✅ | ✅ |
| IRangeControlObject | ✅ | ✅ | ✅ |
| IContainerControlObject | ✅ | ✅ | ✅ |
| IListContainerControlObject | ✅ | ✅ | ✅ |
| IWindowControlObject | ⚠️ | ✅ | ✅ |
| IDataGridControlObject | ⚠️ | ✅ | ✅ |
| IScrollableControlObject | ✅ | ✅ | ✅ |
| IDateTimeControlObject | ✅ | ✅ | ✅ |
| IWebViewControlObject | ✅ | ✅ | ✅ |
| IContainerControl | ✅ | ✅ | ✅ |
| IBusyPageObject | ✅ | ✅ | ✅ |

Legend: ✅ Full support | ⚠️ Partial/custom | ❌ Not applicable

---

## 5. Assumptions

- **ASM-001:** Controls implement only applicable capability interfaces
- **ASM-002:** Platform limitations may prevent some interface implementations
- **ASM-003:** Multiple interface implementation is supported via composition
- **ASM-004:** All interfaces follow nullable skip pattern consistently

---

## 6. Exclusions

- **EXC-001:** Platform-specific extensions beyond these interfaces
- **EXC-002:** Async versions of methods
- **EXC-003:** Fluent API patterns
- **EXC-004:** Event handling/subscriptions

---

## Related Documents

- [IControlObject Specification](250_001_IControlObject.spx.md)
- [IPageObject Specification](250_002_IPageObject.spx.md)
- [IContainerControlObject Specification](250_003_IContainerControlObject.spx.md)
- [IContainerControl Specification](250_003a_IContainerControl.spx.md)
- [IListContainerControlObject Specification](250_003b_IListContainerControlObject.spx.md)
- [Interfaces Module](../../200_architecture/211_Modules/211_001_Interfaces.spx.md)
- [ADR-004 Control Hierarchy](../../200_architecture/202_Decisions/202_004_ControlHierarchy.spx.md)
- [Platform Base Classes](250_006_MauiBaseClasses.spx.md)
