# SPEC-002: Interface Contracts

**Version:** 1.0  
**Status:** Active  
**Last Updated:** January 2026  
**Implements:** REQ-002 (Control Object Pattern), REQ-003 (Page Object Pattern), REQ-004 (State Verification Pattern), REQ-005 (Waiting and Synchronization), REQ-006 (Logging and Diagnostics)

---

## 1. Purpose

This specification defines the core interface contracts that all platform implementations MUST implement. These interfaces provide the foundation for the unified test API across all platforms.

---

## 2. Interface Hierarchy

```
Core Interfaces (Brinell.Core.Abstractions)
│
├── ITestContext (test execution context and configuration)
├── IPageObject (page/view abstraction)
│
└── IControlObject (base for all controls)
    ├── IClickableControl (click, double-click, right-click, hover)
    │   └── IContentControl (extends IClickableControl)
    │
    ├── ITextControl (text input: Enter, Clear, SetText, etc.)
    │
    ├── IToggleControl (checkbox, switch, radio: Check, Uncheck, Toggle)
    │
    ├── ISelectorControl (dropdown, combo box, list: Select, GetSelectedText)
    │
    ├── IRangeControl (slider, progress bar: GetValue, SetValue, Increment)
    │   └── ISlider (extends IRangeControl)
    │
    ├── IItemsControl (collection containers: GetItemCount, ClickItem)
    │
    └── IContainerControl (parent containers: GetChild<T>)
```

---

## 3. ITestContext Interface

**Namespace:** `Brinell.Core.Abstractions`

### 3.1 Purpose

Provides test execution context, configuration, and utility methods. Implemented once per platform (e.g., `AppiumTestContext` for MAUI).

### 3.2 Properties

```csharp
/// <summary>
/// Name of the current test for logging context.
/// Set by test framework during test initialization.
/// </summary>
string TestName { get; set; }

/// <summary>
/// Current platform enum value.
/// Indicates which platform is being tested (WPF, MAUI, Web, etc).
/// </summary>
Platform Platform { get; }

/// <summary>
/// Default timeout in milliseconds for wait operations.
/// Typically 10,000 ms (10 seconds).
/// Used by Wait* and Check* methods unless overridden.
/// </summary>
int DefaultTimeoutMs { get; }

/// <summary>
/// Short timeout in milliseconds for quick checks.
/// Typically 2,000 ms (2 seconds).
/// Used for rapid state checks that should fail quickly.
/// </summary>
int ShortTimeoutMs { get; }

/// <summary>
/// Polling interval in milliseconds for wait operations.
/// Typically 100-250 ms.
/// Controls how frequently conditions are checked during waits.
/// </summary>
int PollingIntervalMs { get; }

/// <summary>
/// Logger instance for structured logging to CSV.
/// May be null if logging is disabled.
/// Must be set via SetLogger() before calling assertion/action methods.
/// </summary>
ITestLogger? Logger { get; }
```

### 3.3 Methods

```csharp
/// <summary>
/// Set the CSV logger for this context.
/// Must be called during test setup before performing actions.
/// </summary>
/// <param name="logger">The logger instance to use.</param>
void SetLogger(ITestLogger logger);

/// <summary>
/// Log a message with test context prefix.
/// Format: "[TestName] message"
/// Used for diagnostic logging during test execution.
/// </summary>
void Log(string message);

/// <summary>
/// Log an error with exception details.
/// Includes full exception stack trace.
/// Used when exception occurs outside normal action flow.
/// </summary>
/// <param name="ex">The exception to log.</param>
/// <param name="context">Description of what was being attempted.</param>
void LogError(Exception ex, string context);

/// <summary>
/// Wait for a condition to become true.
/// Polls the condition at PollingIntervalMs intervals until timeout.
/// </summary>
/// <param name="condition">Lambda returning true when condition is met.</param>
/// <param name="timeoutMs">Optional timeout override. Uses DefaultTimeoutMs if null.</param>
/// <param name="description">Description for diagnostic logging ("element 'btnOK' visible").</param>
/// <returns>True if condition met within timeout, false if timeout expired.</returns>
/// <remarks>
/// This is a low-level utility for framework implementation.
/// Test code should use control.Wait*() or control.Check*() instead.
/// </remarks>
bool WaitFor(Func<bool> condition, int? timeoutMs = null, string description = "condition");

/// <summary>
/// Take a screenshot and save to temp folder.
/// Returns path to saved screenshot file.
/// </summary>
/// <param name="name">Base name for screenshot (no extension, no spaces).</param>
/// <returns>Full path to saved screenshot file.</returns>
string? TakeScreenshot(string name);

/// <summary>
/// Capture a failure screenshot with automatic naming.
/// Called automatically before throwing assertion exceptions.
/// Only captures if logger is configured.
/// </summary>
/// <param name="suffix">Descriptive suffix for filename (e.g., "page-not-displayed").</param>
/// <returns>Path to screenshot, or empty string if capture failed.</returns>
/// <remarks>
/// This is called automatically by LogAssertionFailed and other failure methods.
/// Test code typically does not call this directly.
/// Filename format: "{TestName}_{suffix}_{timestamp}.png"
/// </remarks>
string CaptureFailureScreenshot(string suffix = "failure");
```

### 3.4 Platform Enum

```csharp
/// <summary>
/// Supported test platforms.
/// Used to identify which platform implementation is in use.
/// </summary>
public enum Platform
{
    /// <summary>WPF desktop on Windows using FlaUI.</summary>
    Windows,
    
    /// <summary>MAUI on Windows using Appium.</summary>
    WindowsMaui,
    
    /// <summary>Android using Appium.</summary>
    Android,
    
    /// <summary>iOS using Appium.</summary>
    iOS,
    
    /// <summary>Web browser using Selenium WebDriver.</summary>
    Web,
    
    /// <summary>Stride 3D game engine using named pipe automation.</summary>
    Stride
}
```

### 3.5 Platform Extension Methods

Platform enum SHOULD provide extension methods for capability queries:

```csharp
/// <summary>
/// Returns true if platform is mobile (Android or iOS).
/// </summary>
bool IsMobile(this Platform platform);

/// <summary>
/// Returns true if platform is desktop (Windows, WindowsMaui, or Stride).
/// </summary>
bool IsDesktop(this Platform platform);

/// <summary>
/// Returns true if platform is web-based (Web).
/// </summary>
bool IsWeb(this Platform platform);

/// <summary>
/// Returns true if platform uses Appium (WindowsMaui, Android, iOS).
/// </summary>
bool UsesAppium(this Platform platform);
```

---

## 4. IPageObject Interface

**Namespace:** `Brinell.Core.Abstractions`

### 4.1 Purpose

Represents a page, view, or screen in the application. Each test creates page object instances to interact with application pages.

### 4.2 Properties

```csharp
/// <summary>
/// Name of the page for logging and identification.
/// Examples: "LoginPage", "DashboardPage", "SettingsView"
/// </summary>
string Name { get; }

/// <summary>
/// The AutomationId of the page root element.
/// Typically the main container or window AutomationId.
/// Used to verify the page is displayed.
/// </summary>
string AutomationId { get; }

/// <summary>
/// The test context for this page.
/// Provides access to logging, configuration, and driver.
/// </summary>
ITestContext Context { get; }
```

### 4.3 Methods

#### State Checking Methods

```csharp
/// <summary>
/// Immediate check if the page is currently displayed (no wait).
/// Returns false immediately if page not visible.
/// Does NOT wait for page readiness.
/// </summary>
/// <returns>True if page root element is visible.</returns>
bool IsDisplayed();

/// <summary>
/// Immediate check if the page is ready for interaction.
/// Returns false immediately if page not ready (may be loading).
/// Default implementation: IsDisplayed() && !IsBusy()
/// Can be overridden to check additional readiness conditions.
/// </summary>
/// <returns>True if page is displayed and not busy.</returns>
bool IsReady();
```

**Implementation Notes:**
- `IsDisplayed()` - quick check, returns bool, no wait
- `IsReady()` - includes busy state check, returns bool, no wait
- Both are immediate (no waiting) for use in polling loops

#### Wait Methods (Polling with Timeout)

```csharp
/// <summary>
/// Wait for the page to be displayed.
/// Polls IsDisplayed() until true or timeout expires.
/// </summary>
/// <param name="timeoutMs">Optional timeout in milliseconds. Uses context default if null.</param>
/// <returns>True if page became displayed within timeout, false if timeout.</returns>
bool WaitForDisplayed(int? timeoutMs = null);

/// <summary>
/// Wait for the page to be ready.
/// Polls IsReady() until true or timeout expires.
/// Includes waiting for busy state to clear.
/// </summary>
/// <param name="timeoutMs">Optional timeout in milliseconds. Uses context default if null.</param>
/// <returns>True if page became ready within timeout, false if timeout.</returns>
bool WaitForReady(int? timeoutMs = null);
```

#### Check Methods (Wait + Throw)

```csharp
/// <summary>
/// Wait for page to be displayed, throw if timeout.
/// Used as precondition check in test setup.
/// </summary>
/// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
/// <exception cref="TimeoutException">Thrown if page not displayed within timeout.</exception>
void CheckDisplayed(int? timeoutMs = null);

/// <summary>
/// Wait for page to be ready, throw if timeout.
/// Used to ensure page has completed loading/initialization.
/// </summary>
/// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
/// <exception cref="TimeoutException">Thrown if page not ready within timeout.</exception>
void CheckReady(int? timeoutMs = null);
```

#### Diagnostic Methods

```csharp
/// <summary>
/// Capture a screenshot of the current page.
/// </summary>
/// <param name="suffix">Optional descriptive suffix for filename.</param>
/// <returns>Path to saved screenshot file, or null if capture failed.</returns>
string? TakeScreenshot(string suffix = "");
```

### 4.4 Implementation Pattern

Platform implementations MUST provide a `PageBase` class:

```csharp
public abstract class PageBase : IPageObject
{
    protected readonly ITestContext _context;
    
    public string Name { get; }
    public string AutomationId { get; }
    public ITestContext Context => _context;
    
    // Must implement IsDisplayed(), IsReady()
    // Must implement WaitForDisplayed(), WaitForReady()
    // Must implement CheckDisplayed(), CheckReady()
    // Must implement TakeScreenshot()
}
```

Some platforms SHOULD provide `BusyPageBase` for pages with async operations:

```csharp
public abstract class BusyPageBase : PageBase
{
    /// <summary>
    /// Check if page is showing busy/loading indicator.
    /// Override to check for spinner, progress bar, or modal dialog.
    /// </summary>
    protected virtual bool IsBusy() => false;
    
    /// <summary>
    /// Override to include busy check in readiness.
    /// </summary>
    public override bool IsReady() => IsDisplayed() && !IsBusy();
}
```

---

## 5. IControlObject Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 5.1 Purpose

Base interface for all UI control abstractions. Defines the Is/Wait/Check/Assert pattern for state verification.

### 5.2 Properties

```csharp
/// <summary>
/// The AutomationId used to locate this control in the UI.
/// Must be unique within the page (or within container if scoped).
/// Examples: "btnLogin", "txtUsername", "chkRememberMe"
/// </summary>
string AutomationId { get; }

/// <summary>
/// The parent page object (may be null for global controls).
/// Used for logging context and page state checks.
/// </summary>
IPageObject? Page { get; }
```

### 5.3 Constructor Patterns

All platform control base classes MUST support these constructor patterns:

```csharp
// Pattern 1: With page context (most common)
public MyControl(TestContext context, IPageObject? page, string automationId)

// Pattern 2: With container (for controls inside lists/repeaters)
public MyControl(TestContext context, IPageObject? page, Element? container, string automationId)

// Pattern 3: Global control without page
public MyControl(TestContext context, string automationId)
```

### 5.4 Exists Verification Methods

```csharp
/// <summary>
/// Immediate check if element exists (no wait).
/// </summary>
/// <returns>True if element found in DOM.</returns>
bool IsExists();

/// <summary>
/// Wait for element to exist or not exist.
/// </summary>
/// <param name="expected">If true, wait for exists. If false, wait for not exists.</param>
/// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
/// <returns>True if expected state achieved within timeout, false if timeout.</returns>
bool WaitExists(bool expected = true, int? timeoutMs = null);

/// <summary>
/// Check element exists - throws if not.
/// Used as precondition in action methods.
/// </summary>
/// <param name="expected">If true, require exists. If false, require not exists.</param>
/// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
/// <exception cref="TimeoutException">If expected state not met within timeout.</exception>
void CheckExists(bool expected = true, int? timeoutMs = null);

/// <summary>
/// Assert element exists.
/// Called from test assertions to verify control is present.
/// </summary>
/// <param name="message">Optional assertion message for failure.</param>
/// <exception cref="AssertionException">If element not found.</exception>
void AssertExists(string? message = null);

/// <summary>
/// Assert element does not exist.
/// Called from test assertions to verify control is not present.
/// </summary>
/// <param name="message">Optional assertion message for failure.</param>
/// <exception cref="AssertionException">If element found.</exception>
void AssertNotExists(string? message = null);
```

### 5.5 Visibility Verification Methods

```csharp
/// <summary>
/// Immediate check if element is visible (no wait).
/// </summary>
/// <returns>True if element exists and is visible.</returns>
bool IsVisible();

/// <summary>
/// Wait for element to be visible or hidden.
/// </summary>
/// <param name="expected">If true, wait for visible. If false, wait for hidden.</param>
/// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
/// <returns>True if expected visibility achieved within timeout.</returns>
bool WaitVisible(bool expected = true, int? timeoutMs = null);

/// <summary>
/// Check element is visible - throws if not.
/// Used as precondition in action methods (most actions require visible).
/// </summary>
/// <param name="expected">If true, require visible. If false, require hidden.</param>
/// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
/// <exception cref="TimeoutException">If expected visibility not achieved within timeout.</exception>
void CheckVisible(bool expected = true, int? timeoutMs = null);

/// <summary>
/// Assert element is visible.
/// Called from test assertions to verify control is displayed.
/// </summary>
/// <param name="message">Optional assertion message for failure.</param>
/// <exception cref="AssertionException">If element not visible.</exception>
void AssertVisible(string? message = null);

/// <summary>
/// Assert element is not visible.
/// Called from test assertions to verify control is hidden.
/// </summary>
/// <param name="message">Optional assertion message for failure.</param>
/// <exception cref="AssertionException">If element visible.</exception>
void AssertNotVisible(string? message = null);
```

### 5.6 Enabled/Disabled Verification Methods

```csharp
/// <summary>
/// Immediate check if element is enabled (no wait).
/// </summary>
/// <returns>True if element is enabled (can be interacted with).</returns>
bool IsEnabled();

/// <summary>
/// Wait for element to be enabled or disabled.
/// </summary>
/// <param name="expected">If true, wait for enabled. If false, wait for disabled.</param>
/// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
/// <returns>True if expected enabled state achieved within timeout.</returns>
bool WaitEnabled(bool expected = true, int? timeoutMs = null);

/// <summary>
/// Check element is enabled - throws if not.
/// Most action methods (Click, Enter, etc) call this automatically.
/// </summary>
/// <param name="expected">If true, require enabled. If false, require disabled.</param>
/// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
/// <exception cref="TimeoutException">If expected enabled state not achieved within timeout.</exception>
void CheckEnabled(bool expected = true, int? timeoutMs = null);

/// <summary>
/// Assert element is enabled.
/// </summary>
/// <param name="message">Optional assertion message for failure.</param>
/// <exception cref="AssertionException">If element disabled.</exception>
void AssertEnabled(string? message = null);

/// <summary>
/// Assert element is disabled.
/// </summary>
/// <param name="message">Optional assertion message for failure.</param>
/// <exception cref="AssertionException">If element enabled.</exception>
void AssertDisabled(string? message = null);
```

### 5.7 Text/Content Methods

```csharp
/// <summary>
/// Get element text or content.
/// For text inputs: returns the input value.
/// For labels/buttons: returns the display text.
/// </summary>
/// <returns>Text content of the element.</returns>
string GetText();

/// <summary>
/// Assert text equals expected value.
/// Waits for expected text before asserting.
/// </summary>
/// <param name="expected">The expected text value.</param>
/// <param name="message">Optional assertion message for failure.</param>
/// <exception cref="AssertionException">If text does not match.</exception>
void AssertTextEquals(string expected, string? message = null);

/// <summary>
/// Assert text contains expected substring.
/// Waits for text to contain substring before asserting.
/// </summary>
/// <param name="expected">The expected substring.</param>
/// <param name="message">Optional assertion message for failure.</param>
/// <exception cref="AssertionException">If text does not contain substring.</exception>
void AssertTextContains(string expected, string? message = null);
```

---

## 6. IClickableControl Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 6.1 Purpose

Represents controls that can be clicked: buttons, links, menu items, etc.

### 6.2 Interface Definition

```csharp
public interface IClickableControl : IControlObject
{
    /// <summary>
    /// Click the control.
    /// Automatically verifies: exists, visible, enabled.
    /// </summary>
    /// <exception cref="TimeoutException">If preconditions not met within timeout.</exception>
    /// <exception cref="InvalidOperationException">If click fails.</exception>
    void Click();

    /// <summary>
    /// Double-click the control.
    /// Automatically verifies: exists, visible, enabled.
    /// </summary>
    /// <exception cref="TimeoutException">If preconditions not met within timeout.</exception>
    void DoubleClick();

    /// <summary>
    /// Right-click the control (context menu).
    /// Automatically verifies: exists, visible, enabled.
    /// </summary>
    /// <exception cref="TimeoutException">If preconditions not met within timeout.</exception>
    void RightClick();

    /// <summary>
    /// Hover over the control without clicking.
    /// Used to trigger hover states or tooltips.
    /// </summary>
    /// <exception cref="TimeoutException">If preconditions not met within timeout.</exception>
    void Hover();
}
```

### 6.3 Implementation Pattern

```csharp
public override void Click()
{
    CheckVisible(expected: true);      // Wait for visible
    CheckEnabled(expected: true);      // Wait for enabled
    
    var element = FindElement();       // Get element
    element.Click();                   // Perform click
    
    LogAction("Click");                // Log to CSV
}
```

### 6.4 Precondition Verification

Click actions MUST verify:
1. Element exists
2. Element is visible
3. Element is enabled

If any precondition fails, throw `TimeoutException` with context about what failed.

---

## 7. IContentControl Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 7.1 Purpose

Represents clickable controls that display content (buttons, labels, content containers).

### 7.2 Interface Definition

```csharp
public interface IContentControl : IClickableControl
{
    // Inherits all IClickableControl methods
    // Additional methods may be added for content-specific behavior
}
```

This is a marker interface that extends `IClickableControl` for semantic clarity. Controls like buttons, labels, and frames implement this.

---

## 8. ITextControl Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 8.1 Purpose

Represents text input controls: text boxes, search bars, editors, etc.

### 8.2 Interface Definition

```csharp
public interface ITextControl : IControlObject
{
    #region Input Methods
    
    /// <summary>
    /// Enter text into the control.
    /// Does NOT clear existing text. Text is appended.
    /// </summary>
    /// <param name="text">The text to enter.</param>
    void Enter(string text);

    /// <summary>
    /// Clear all text in the control.
    /// </summary>
    void Clear();

    /// <summary>
    /// Clear existing text and enter new text.
    /// Equivalent to Clear() followed by Enter(text).
    /// </summary>
    /// <param name="text">The text to set.</param>
    void ClearAndEnter(string text);

    /// <summary>
    /// Set text (alias for ClearAndEnter for backward compatibility).
    /// </summary>
    /// <param name="text">The text to set.</param>
    void SetText(string text);

    /// <summary>
    /// Append text to existing text.
    /// </summary>
    /// <param name="text">The text to append.</param>
    void Append(string text);

    #endregion

    #region State Checks
    
    /// <summary>
    /// Check if control is read-only.
    /// </summary>
    /// <returns>True if control cannot be edited.</returns>
    bool IsReadOnly();

    /// <summary>
    /// Get the length of the current text.
    /// </summary>
    /// <returns>Character count of text.</returns>
    int GetTextLength();

    #endregion

    #region Assertions
    
    /// <summary>
    /// Assert text is empty or null.
    /// </summary>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertTextEmpty(string? message = null);

    /// <summary>
    /// Assert text is not empty.
    /// </summary>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertTextNotEmpty(string? message = null);

    /// <summary>
    /// Assert text starts with expected prefix.
    /// </summary>
    /// <param name="prefix">The expected prefix.</param>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertTextStartsWith(string prefix, string? message = null);

    /// <summary>
    /// Assert text ends with expected suffix.
    /// </summary>
    /// <param name="suffix">The expected suffix.</param>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertTextEndsWith(string suffix, string? message = null);

    /// <summary>
    /// Assert text matches the specified regex pattern.
    /// </summary>
    /// <param name="pattern">The regex pattern to match.</param>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertTextMatches(string pattern, string? message = null);

    #endregion
}
```

### 8.3 Precondition Verification

Text input methods (Enter, Clear, ClearAndEnter) MUST verify:
1. Element exists
2. Element is visible
3. Element is enabled
4. Element is not read-only

---

## 9. IToggleControl Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 9.1 Purpose

Represents toggle/boolean controls: checkboxes, switches, radio buttons.

### 9.2 Interface Definition

```csharp
public interface IToggleControl : IControlObject
{
    #region State Checks
    
    /// <summary>
    /// Check if the control is currently checked/on.
    /// </summary>
    /// <returns>True if checked, false if unchecked.</returns>
    bool IsChecked();

    /// <summary>
    /// Wait for checked state to change.
    /// </summary>
    /// <param name="expected">The expected checked state (true/false).</param>
    /// <param name="timeoutMs">Optional timeout override in milliseconds.</param>
    /// <returns>True if expected state achieved within timeout.</returns>
    bool WaitChecked(bool expected = true, int? timeoutMs = null);

    #endregion

    #region Action Methods
    
    /// <summary>
    /// Toggle the control state (checked -> unchecked or vice versa).
    /// </summary>
    void Toggle();

    /// <summary>
    /// Set the control to checked/on state.
    /// </summary>
    void Check();

    /// <summary>
    /// Set the control to unchecked/off state.
    /// </summary>
    void Uncheck();

    /// <summary>
    /// Set checked state to specific value.
    /// Equivalent to Check() if value=true, Uncheck() if value=false.
    /// </summary>
    /// <param name="value">True to check, false to uncheck.</param>
    void SetChecked(bool value);

    #endregion

    #region Assertions
    
    /// <summary>
    /// Assert control is checked.
    /// </summary>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertChecked(string? message = null);

    /// <summary>
    /// Assert control is unchecked.
    /// </summary>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertUnchecked(string? message = null);

    #endregion
}
```

---

## 10. ISelectorControl Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 10.1 Purpose

Represents controls that select from a list of items: dropdowns, combo boxes, list boxes, pickers.

### 10.2 Interface Definition

```csharp
public interface ISelectorControl : IControlObject
{
    #region Selection Methods
    
    /// <summary>
    /// Select an item by its index (0-based).
    /// </summary>
    /// <param name="index">Zero-based index of item to select.</param>
    /// <exception cref="ArgumentOutOfRangeException">If index out of range.</exception>
    void SelectByIndex(int index);

    /// <summary>
    /// Select an item by its display text.
    /// </summary>
    /// <param name="text">The text of the item to select.</param>
    /// <exception cref="ArgumentException">If item with text not found.</exception>
    void SelectByText(string text);

    #endregion

    #region State Queries
    
    /// <summary>
    /// Get the text of the currently selected item.
    /// </summary>
    /// <returns>Display text of selected item, or null if nothing selected.</returns>
    string? GetSelectedText();

    /// <summary>
    /// Get the index of the currently selected item.
    /// </summary>
    /// <returns>Zero-based index of selected item, or -1 if nothing selected.</returns>
    int GetSelectedIndex();

    /// <summary>
    /// Get all available items.
    /// </summary>
    /// <returns>Read-only list of item display texts.</returns>
    IReadOnlyList<string> GetItems();

    /// <summary>
    /// Get the count of available items.
    /// </summary>
    /// <returns>Number of items in selector.</returns>
    int GetItemCount();

    #endregion

    #region Assertions
    
    /// <summary>
    /// Assert selected item text equals expected value.
    /// Waits for selection to match before asserting.
    /// </summary>
    /// <param name="expected">The expected selected item text.</param>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertSelectedText(string expected, string? message = null);

    #endregion
}
```

---

## 11. IRangeControl Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 11.1 Purpose

Represents controls with numeric range values: sliders, progress bars, steppers.

### 11.2 Interface Definition

```csharp
public interface IRangeControl : IControlObject
{
    #region Value Access
    
    /// <summary>
    /// Get the current value.
    /// </summary>
    /// <returns>Current numeric value.</returns>
    double GetValue();

    /// <summary>
    /// Get the minimum allowed value.
    /// </summary>
    /// <returns>Minimum value.</returns>
    double GetMinimum();

    /// <summary>
    /// Get the maximum allowed value.
    /// </summary>
    /// <returns>Maximum value.</returns>
    double GetMaximum();

    #endregion

    #region Value Changes
    
    /// <summary>
    /// Set the value to a specific number.
    /// </summary>
    /// <param name="value">The value to set (must be between min and max).</param>
    /// <exception cref="ArgumentOutOfRangeException">If value outside min/max range.</exception>
    void SetValue(double value);

    /// <summary>
    /// Increment the value by one step.
    /// </summary>
    void Increment();

    /// <summary>
    /// Decrement the value by one step.
    /// </summary>
    void Decrement();

    #endregion

    #region Assertions
    
    /// <summary>
    /// Assert value equals expected (with tolerance for floating-point comparison).
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="tolerance">Tolerance for floating-point comparison (default 0.001).</param>
    /// <param name="message">Optional assertion message for failure.</param>
    void AssertValue(double expected, double tolerance = 0.001, string? message = null);

    #endregion
}
```

---

## 12. ISlider Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 12.1 Purpose

Specialized interface for slider controls. Extends `IRangeControl` for slider-specific behavior.

### 12.2 Interface Definition

```csharp
public interface ISlider : IRangeControl
{
    // Inherits all IRangeControl methods
    // Additional slider-specific methods may be added as needed
}
```

---

## 13. IItemsControl Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 13.1 Purpose

Represents controls containing collections of items: lists, grids, data tables, carousels.

### 13.2 Interface Definition

```csharp
public interface IItemsControl : IControlObject
{
    #region Item Access
    
    /// <summary>
    /// Get the count of items in the collection.
    /// </summary>
    /// <returns>Number of items.</returns>
    int GetItemCount();

    /// <summary>
    /// Get the text/content of an item at specific index.
    /// </summary>
    /// <param name="index">Zero-based index of item.</param>
    /// <returns>Display text of item.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If index out of range.</exception>
    string GetItemText(int index);

    /// <summary>
    /// Check if item with specific text exists.
    /// </summary>
    /// <param name="text">The text to search for.</param>
    /// <returns>True if item found.</returns>
    bool HasItem(string text);

    #endregion

    #region Item Interaction
    
    /// <summary>
    /// Click an item by index.
    /// </summary>
    /// <param name="index">Zero-based index of item to click.</param>
    /// <exception cref="ArgumentOutOfRangeException">If index out of range.</exception>
    void ClickItem(int index);

    /// <summary>
    /// Click an item by text.
    /// </summary>
    /// <param name="text">The text of item to click.</param>
    /// <exception cref="ArgumentException">If item not found.</exception>
    void ClickItem(string text);

    #endregion
}
```

---

## 14. IContainerControl Interface

**Namespace:** `Brinell.Core.Abstractions.Controls`

### 14.1 Purpose

Represents container controls that hold child controls: panels, groups, frames, stacks.

### 14.2 Interface Definition

```csharp
public interface IContainerControl : IControlObject
{
    #region Child Access
    
    /// <summary>
    /// Get the count of child controls.
    /// </summary>
    /// <returns>Number of direct children.</returns>
    int GetChildCount();

    /// <summary>
    /// Get AutomationIds of all child controls.
    /// </summary>
    /// <returns>Read-only list of child AutomationIds.</returns>
    IReadOnlyList<string> GetChildNames();

    /// <summary>
    /// Check if a specific child control exists.
    /// </summary>
    /// <param name="childName">AutomationId of child to check.</param>
    /// <returns>True if child exists.</returns>
    bool ChildExists(string childName);

    /// <summary>
    /// Get a child control by AutomationId and cast to specified type.
    /// Typically used with container-scoped controls.
    /// </summary>
    /// <typeparam name="T">The control type to return.</typeparam>
    /// <param name="childName">AutomationId of child control.</param>
    /// <returns>The child control typed as T.</returns>
    /// <exception cref="ArgumentException">If child not found.</exception>
    T GetChild<T>(string childName) where T : IControlObject;

    #endregion
}
```

---

## 15. Implementation Requirements

### 15.1 All Platform Implementations MUST

1. Implement all interfaces defined in this specification
2. Support the constructor patterns described in section 5.3
3. Implement the Is/Wait/Check/Assert pattern consistently
4. Automatically verify preconditions in action methods
5. Log all actions to CSV via `ITestLogger`
6. Capture failure screenshots on assertion failures
7. Use consistent method names and signatures
8. Provide virtual methods for extensibility

### 15.2 Platform Implementation Examples

#### MAUI Example (Appium)

```csharp
// Platform project: Brinell.Maui
public abstract class ControlBase : IControlObject
{
    protected readonly AppiumTestContext _context;
    
    // Find element using AppiumDriver
    protected AppiumElement? FindElement() { ... }
    
    // Implement Is* methods (immediate, no wait)
    public bool IsVisible() => FindElement()?.Displayed ?? false;
    
    // Implement Wait* methods (polling with timeout)
    public bool WaitVisible(bool expected = true, int? timeoutMs = null) 
    { ... }
    
    // Implement Check* methods (wait + throw)
    public void CheckVisible(bool expected = true, int? timeoutMs = null) 
    { ... }
    
    // Implement Assert* methods (check first, then assert)
    public void AssertVisible(string? message = null) 
    { ... }
}
```

---

## 16. Cross-References

| Specification | Description |
|---------------|-------------|
| [SPEC-003: Control Objects](SPEC-003-control-objects.md) | Implementation details for control patterns |
| [SPEC-004: Page Objects](SPEC-004-page-objects.md) | Page object implementation patterns |
| [SPEC-005: State Verification](SPEC-005-state-verification.md) | Wait/Check/Assert pattern specifications |
| [SPEC-006: Logging](SPEC-006-logging.md) | CSV logging format and requirements |
| [SPEC-007: Platform Implementations](SPEC-007-platform-implementations.md) | Platform-specific implementation details |

---

## 17. Requirements Traceability

| Requirement | Coverage |
|-------------|----------|
| FR-002: Control Object Pattern | Sections 5-14 |
| FR-003: Page Object Pattern | Section 4 |
| FR-004: State Verification Pattern | Sections 5.3-5.7 |
| FR-005: Waiting and Synchronization | Sections 4.3, 5.4-5.6 |
| FR-006: Logging and Diagnostics | Section 3.3, 5.7 |

---

## 18. Change History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Jan 2026 | Initial specification based on v3.1 implementation |

---

*Next: [SPEC-003: Control Object Specification](SPEC-003-control-objects.md)*
