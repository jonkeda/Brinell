# Interface Usage Guide for Brinell Framework

**Document Version:** 1.0  
**Status:** Phase 1, Task 5, Step 6 - Documentation  
**Last Updated:** January 3, 2026  
**Framework Requirement:** FR-002.7 (Unified Interface Hierarchy)

---

## 1. Overview

This guide documents all 49 interfaces implemented across 6 automation platforms (MAUI, WPF/WinForms, Html/Selenium, Html.Playwright, Stride) and how to use them in your test code.

### Key Concepts

- **Platform-Independent Interfaces:** Core interfaces (IVisualElement, IClickable, ITextInputControl, etc.) are implemented consistently across all platforms
- **Enhanced Base Classes:** Each platform provides enhanced base classes that implement all applicable interfaces
- **Unified Method Signatures:** All platforms use the same method names and signatures for equivalent operations
- **Consistent Behavior:** Operations behave identically regardless of underlying automation framework

### 49 Core Interfaces

| Category | Interfaces | Platforms |
|----------|-----------|-----------|
| **Basic Visual** | IVisualElement, IInteractive, IClickable | All 6 |
| **Text Input** | ITextInputControl, IEditableTextControl | All 6 |
| **Selection** | ISingleSelectControl, ISelectableControl | All 6 |
| **Toggles** | IToggleControl, ICheckableControl | All 6 |
| **Ranges** | IRangeInputControl, ISliderControl | All 6 |
| **Collections** | ICollectionControl, IClickableCollectionControl, IScrollableCollectionControl | All 6 |
| **Containers** | IContainerControl, IScrollableControl | All 6 |
| **Navigation** | INavigableControl | Web platforms (Html/Selenium, Html.Playwright) |

---

## 2. Basic Visual Elements

### IVisualElement

Provides fundamental element visibility and state information.

**Methods:**
```csharp
bool IsExists();              // Check if element exists in DOM/visual tree
bool IsVisible();             // Check if element is visible
bool IsEnabled();             // Check if element is enabled (not disabled)
IPageObject Page { get; }     // Get parent page object
string AutomationId { get; }  // Get unique element identifier
```

**Platform Implementation:**
- **MAUI:** Uses AppiumElement visibility checks
- **WPF/WinForms:** Uses FlaUI.Core.AutomationElements with visual state inspection
- **Html/Selenium:** Uses Selenium WebDriver element presence and display checks
- **Html.Playwright:** Uses Playwright ILocator.IsVisibleAsync()
- **Stride:** Uses StrideTestContext GetState() calls

**Example Usage:**
```csharp
// Check if login button exists before attempting interaction
var loginButton = page.GetControl<IClickable>("loginButton");
if (loginButton.IsExists())
{
    loginButton.Click();
}
```

### IInteractive

Provides focus and interaction capabilities.

**Methods:**
```csharp
void Focus();                 // Set focus to element
void Blur();                  // Remove focus from element
void WaitExists(bool exists, int timeoutMs);    // Wait for existence state
void WaitVisible(bool visible, int timeoutMs);  // Wait for visibility state
void WaitEnabled(bool enabled, int timeoutMs);  // Wait for enabled state
```

**Platform Implementation:**
- **MAUI:** Uses element.SendKeys(Key.Tab) for blur, tap for focus
- **WPF/WinForms:** Uses element.Focus() and SendKeys(Key.Tab)
- **Html/Selenium:** Uses IJavaScriptExecutor to call blur() and focus()
- **Html.Playwright:** Uses FocusAsync() and EvaluateAsync for blur
- **Stride:** Uses SetFocus() network command

**Example Usage:**
```csharp
// Wait for element to become visible with timeout
var saveButton = page.GetControl<IInteractive>("saveButton");
saveButton.WaitVisible(true, 5000);  // Wait up to 5 seconds
saveButton.Focus();
```

### IClickable

Provides mouse interaction capabilities.

**Methods:**
```csharp
void Click();                 // Single left click
void DoubleClick();           // Double left click
void RightClick();            // Right mouse button click
void WaitClickable(bool clickable, int timeoutMs);  // Wait for clickable state
```

**Platform Implementation:**
- **MAUI:** Uses element.Click(), multi-tap for double-click
- **WPF/WinForms:** Uses FlaUI click operations and DoubleClick()
- **Html/Selenium:** Uses Actions for clicks and double-clicks
- **Html.Playwright:** Uses ClickAsync() with mouse button parameter
- **Stride:** Uses LeftClick() network command

**Example Usage:**
```csharp
// Click button and wait for response
var submitButton = page.GetControl<IClickable>("submit");
submitButton.WaitClickable(true, 3000);
submitButton.Click();
```

---

## 3. Text Input & Editing

### ITextInputControl

Provides text entry and retrieval capabilities.

**Methods:**
```csharp
void Enter(string text);      // Enter text into field (clears first)
void AppendText(string text); // Append text without clearing
void Clear();                 // Clear all text
string GetText();             // Get current text
void AssertTextEquals(string expected, string message = null);
void AssertTextMatches(string pattern, string message = null);
void AssertTextContains(string substring, string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses SendKeys() for text entry with keyboard support
- **WPF/WinForms:** Uses keyboard shortcuts (Ctrl+A, Delete for clear)
- **Html/Selenium:** Uses SendKeys() and Clear()
- **Html.Playwright:** Uses TypeAsync() with FillAsync() for clear-and-enter
- **Stride:** Uses SetText() network command

**Example Usage:**
```csharp
// Enter text with validation
var emailField = page.GetControl<ITextInputControl>("email");
emailField.Enter("user@example.com");
emailField.AssertTextEquals("user@example.com", "Email should match input");
```

### IEditableTextControl

Extends text input with keyboard editing operations.

**Methods:**
```csharp
// All ITextInputControl methods, plus:
void SelectAll();             // Select all text (Ctrl+A)
void Copy();                  // Copy selected text (Ctrl+C)
void Paste();                 // Paste from clipboard (Ctrl+V)
void Cut();                   // Cut selected text (Ctrl+X)
void Undo();                  // Undo last edit (Ctrl+Z)
void Redo();                  // Redo last undo (Ctrl+Y)
string GetSelectedText();     // Get currently selected text
```

**Platform Implementation:**
- **MAUI:** Uses SendKeys() with Key enumerations (Control + C, etc.)
- **WPF/WinForms:** Uses VirtualKeyShort enum (CONTROL, KEY_C, etc.)
- **Html/Selenium:** Uses SendKeys(Keys.Control + "c") pattern
- **Html.Playwright:** Uses PressAsync("Control+C") pattern
- **Stride:** Uses keyboard network commands

**Example Usage:**
```csharp
// Complex text editing scenario
var textArea = page.GetControl<IEditableTextControl>("notes");
textArea.Enter("Original text");
textArea.SelectAll();
textArea.Copy();
textArea.Clear();
textArea.Paste();  // Verify copy/paste works
```

---

## 4. Selection Controls

### ISingleSelectControl

Provides dropdown/combobox selection capabilities.

**Methods:**
```csharp
void SelectByText(string itemText);      // Select item by display text
void SelectByValue(string value);        // Select item by value attribute
void SelectByIndex(int index);           // Select by zero-based index
void SelectByPattern(string pattern);    // Select first matching pattern
string GetSelectedText();                // Get display text of selection
string GetSelectedValue();               // Get value of selection
int GetSelectedIndex();                  // Get index of selection
List<string> GetAllOptions();            // Get all available options
void AssertSelectedEquals(string expected, string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses SelectElement pattern from Selenium/Appium
- **WPF/WinForms:** Uses element selection and item navigation
- **Html/Selenium:** Uses SelectElement.SelectByText(), SelectByValue()
- **Html.Playwright:** Uses SelectOptionAsync() with option labels
- **Stride:** Uses network selection commands

**Example Usage:**
```csharp
// Select from dropdown
var countryDropdown = page.GetControl<ISingleSelectControl>("country");
countryDropdown.SelectByText("United States");
var selected = countryDropdown.GetSelectedText();
countryDropdown.AssertSelectedEquals("United States");

// Get all available options
var options = countryDropdown.GetAllOptions();
Assert.Contains("Canada", options);
```

### ISelectableControl

Provides list/menu selection capabilities.

**Methods:**
```csharp
// Similar to ISingleSelectControl but for multi-select lists
void SelectByText(string itemText);
void SelectByValue(string value);
void SelectByIndex(int index);
void DeselectByText(string itemText);
void DeselectByValue(string value);
void DeselectByIndex(int index);
void ClearSelection();
List<string> GetSelectedValues();
List<int> GetSelectedIndices();
void AssertSelectedContains(string value, string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses Appium multi-select list operations
- **WPF/WinForms:** Uses ListBox.SelectedItems collection
- **Html/Selenium:** Uses Ctrl+Click for multi-select
- **Html.Playwright:** Uses ClickAsync() with Shift/Ctrl modifiers
- **Stride:** Uses network multi-select commands

**Example Usage:**
```csharp
// Multi-select list
var permissionsList = page.GetControl<ISelectableControl>("permissions");
permissionsList.SelectByText("Read");
permissionsList.SelectByText("Write");
var selected = permissionsList.GetSelectedValues();
Assert.Equal(2, selected.Count);
```

---

## 5. Toggle & Checkbox Controls

### IToggleControl

Provides switch/toggle ON/OFF capabilities.

**Methods:**
```csharp
void Toggle();                // Toggle ON ↔ OFF
void SetOn();                 // Ensure toggle is ON
void SetOff();                // Ensure toggle is OFF
bool IsOn();                  // Get current state
void AssertOn(string message = null);
void AssertOff(string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses click() to toggle switch state
- **WPF/WinForms:** Uses element.Toggle() or click for toggle button
- **Html/Selenium:** Uses click() for toggle input type="checkbox"
- **Html.Playwright:** Uses ClickAsync() on toggle element
- **Stride:** Uses network toggle command

**Example Usage:**
```csharp
// Toggle switch
var enableNotifications = page.GetControl<IToggleControl>("notifications");
enableNotifications.SetOn();
enableNotifications.AssertOn("Notifications should be enabled");
enableNotifications.Toggle();
enableNotifications.AssertOff();
```

### ICheckableControl

Extends toggle with explicit check/uncheck semantics.

**Methods:**
```csharp
void Check();                 // Ensure checkbox is checked
void Uncheck();               // Ensure checkbox is unchecked
bool IsChecked();             // Get current checked state
void SetChecked(bool value);  // Set checked state
void AssertChecked(string message = null);
void AssertUnchecked(string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses AppiumElement Check() / Uncheck() methods
- **WPF/WinForms:** Uses IsToggled property or click operations
- **Html/Selenium:** Uses Selected property and click() for toggling
- **Html.Playwright:** Uses IsCheckedAsync() and ClickAsync()
- **Stride:** Uses network check/uncheck commands

**Example Usage:**
```csharp
// Checkbox interaction
var agreeTerms = page.GetControl<ICheckableControl>("termsCheckbox");
agreeTerms.Check();
agreeTerms.AssertChecked("User must agree to terms");
agreeTerms.SetChecked(false);
```

---

## 6. Range & Slider Controls

### IRangeInputControl

Provides numeric input with min/max range capabilities.

**Methods:**
```csharp
void SetValue(double value);  // Set numeric value
double GetValue();            // Get current numeric value
double GetMinimum();          // Get minimum allowed value
double GetMaximum();          // Get maximum allowed value
void SetPercentage(double pct);  // Set value as percentage (0-100)
double GetPercentage();       // Get value as percentage
void AssertValueEquals(double expected, double tolerance = 0.1, string message = null);
void AssertValueBetween(double min, double max, string message = null);
```

**Percentage Calculation (All Platforms):**
```
SetPercentage(50):   value = min + (max - min) * 0.5
GetPercentage():     return (current - min) / (max - min) * 100
```

**Platform Implementation:**
- **MAUI:** Uses Slider.Value property and GetState() API
- **WPF/WinForms:** Uses Slider.Value / NumericUpDown.Value
- **Html/Selenium:** Uses GetAttribute("value") and SendKeys()
- **Html.Playwright:** Uses FillAsync() for input value
- **Stride:** Uses network value set/get commands

**Example Usage:**
```csharp
// Set slider to 75%
var volumeSlider = page.GetControl<IRangeInputControl>("volume");
volumeSlider.SetPercentage(75);
Assert.Equal(75, volumeSlider.GetPercentage(), 0.1);

// Set to specific value
var ageInput = page.GetControl<IRangeInputControl>("age");
ageInput.SetValue(25);
ageInput.AssertValueEquals(25);
```

### ISliderControl

Extends range control with step/increment capabilities.

**Methods:**
```csharp
// All IRangeInputControl methods, plus:
double GetStep();             // Get increment step
void Increment(int steps = 1);  // Increase by N steps
void Decrement(int steps = 1);  // Decrease by N steps
void AssertStepEquals(double expected, string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses Slider.StepSize property
- **WPF/WinForms:** Uses TickFrequency property
- **Html/Selenium:** Uses step attribute and arrow keys
- **Html.Playwright:** Uses PressAsync("ArrowUp/Down") for stepping
- **Stride:** Uses network step command

**Example Usage:**
```csharp
// Increment slider by steps
var qualitySlider = page.GetControl<ISliderControl>("quality");
qualitySlider.Increment(5);  // Increase by 5 steps
var newValue = qualitySlider.GetValue();
```

---

## 7. Collection Controls

### ICollectionControl

Provides iteration and count capabilities for list-like controls.

**Methods:**
```csharp
int GetItemCount();           // Get total number of items
bool ContainsItem(string itemText);  // Check if item exists
string GetItemAt(int index);  // Get item text by index
List<string> GetAllItems();   // Get all item texts
void AssertItemCount(int expected, string message = null);
void AssertContainsItem(string itemText, string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses FindItemsAsync() and count collection
- **WPF/WinForms:** Uses ItemsControl.Items.Count
- **Html/Selenium:** Uses FindElements() to collect items
- **Html.Playwright:** Uses LocatorAsync().CountAsync()
- **Stride:** Uses network item enumeration

**Example Usage:**
```csharp
// Verify list contents
var todoList = page.GetControl<ICollectionControl>("todoItems");
todoList.AssertItemCount(3);
todoList.AssertContainsItem("Buy groceries");

var allItems = todoList.GetAllItems();
Assert.Equal(new[] { "Task 1", "Task 2", "Task 3" }, allItems);
```

### IClickableCollectionControl

Extends collection control with item clicking capabilities.

**Methods:**
```csharp
// All ICollectionControl methods, plus:
void ClickItem(string itemText);   // Click item by text
void ClickItemAt(int index);       // Click item by index
void DoubleClickItem(string itemText);
void RightClickItem(string itemText);
```

**Platform Implementation:**
- **MAUI:** Uses FindItemsAsync() then Click on result
- **WPF/WinForms:** Uses ItemContainerGenerator to find and click
- **Html/Selenium:** Uses Actions.ClickAsync() on found item
- **Html.Playwright:** Uses LocatorAsync().ClickAsync()
- **Stride:** Uses network item click command

**Example Usage:**
```csharp
// Click item in list
var searchResults = page.GetControl<IClickableCollectionControl>("results");
searchResults.ClickItem("Result Item 1");

// Or click by index
searchResults.ClickItemAt(0);
```

### IScrollableCollectionControl

Extends clickable collection with scroll-into-view capabilities.

**Methods:**
```csharp
// All IClickableCollectionControl methods, plus:
void ScrollToItem(string itemText);
void ScrollToItemAt(int index);
bool IsItemVisible(string itemText);
int GetFirstVisibleIndex();
int GetLastVisibleIndex();
```

**Platform Implementation:**
- **MAUI:** Uses ScrollTo() on CollectionView
- **WPF/WinForms:** Uses BringIntoView() on item container
- **Html/Selenium:** Uses JavaScriptExecutor scrollIntoView()
- **Html.Playwright:** Uses ScrollIntoViewIfNeededAsync()
- **Stride:** Uses network scroll command

**Example Usage:**
```csharp
// Scroll to item and verify visibility
var longList = page.GetControl<IScrollableCollectionControl>("items");
longList.ScrollToItem("Item 50");
Assert.True(longList.IsItemVisible("Item 50"));
```

---

## 8. Container Controls

### IContainerControl

Provides child element access and management.

**Methods:**
```csharp
int GetChildCount();          // Count direct children
bool ChildExists(string automationId);  // Check if child exists
IControlObject GetChild(string automationId);  // Get child by ID
T GetChild<T>(string automationId) where T : IControlObject;  // Get typed child
List<IControlObject> GetAllChildren();  // Get all child controls
void AssertChildCount(int expected, string message = null);
void AssertChildExists(string automationId, string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses FindChildAsync() and VisualTreeHelper
- **WPF/WinForms:** Uses LogicalTreeHelper.GetChildren()
- **Html/Selenium:** Uses FindElements(".//*[@AutomationId='id']")
- **Html.Playwright:** Uses QuerySelectorAllAsync()
- **Stride:** Uses network child enumeration

**Example Usage:**
```csharp
// Access child controls
var panel = page.GetControl<IContainerControl>("mainPanel");
panel.AssertChildCount(5);

var button = panel.GetChild<IClickable>("saveButton");
button.Click();

var allChildren = panel.GetAllChildren();
```

### IScrollableControl

Provides scroll position and scrolling capabilities.

**Methods:**
```csharp
void Scroll(int amount);      // Scroll by pixel amount
void ScrollTop();             // Scroll to top
void ScrollBottom();          // Scroll to bottom
int GetScrollPosition();       // Get current scroll position
int GetScrollHeight();        // Get total scrollable height
bool IsScrollable();          // Check if element can scroll
void AssertCanScroll(string message = null);
```

**Platform Implementation:**
- **MAUI:** Uses ScrollView.ScrollToAsync()
- **WPF/WinForms:** Uses ScrollViewer.ScrollToVerticalOffset()
- **Html/Selenium:** Uses JavaScriptExecutor scrollTop/scrollHeight
- **Html.Playwright:** Uses EvaluateAsync("el => el.scrollTop += amount")
- **Stride:** Uses network scroll command

**Example Usage:**
```csharp
// Scroll within container
var scrollContainer = page.GetControl<IScrollableControl>("content");
scrollContainer.ScrollBottom();
scrollContainer.AssertCanScroll("Container should be scrollable");

// Get scroll state
int position = scrollContainer.GetScrollPosition();
int maxScroll = scrollContainer.GetScrollHeight();
```

---

## 9. Navigation Controls

### INavigableControl

Provides page navigation capabilities (web platforms only).

**Methods:**
```csharp
void GoBack();                // Navigate back (browser back button)
void GoForward();             // Navigate forward
void Reload();                // Reload current page
void Goto(string url);        // Navigate to URL
string GetCurrentUrl();       // Get current page URL
string GetTitle();            // Get page title
void WaitForNavigation();     // Wait for page load
void AssertUrlEquals(string expectedUrl, string message = null);
void AssertTitleEquals(string expectedTitle, string message = null);
```

**Platform Implementation:**
- **Html/Selenium:** Uses IWebDriver navigation methods
- **Html.Playwright:** Uses IPage.GotoAsync(), GoBackAsync()
- **MAUI/WPF/WinForms/Stride:** Not applicable (no navigation)

**Example Usage (Web Only):**
```csharp
// Web navigation
var page = testContext.GetPage<INavigableControl>();
page.Goto("https://example.com");
page.AssertUrlEquals("https://example.com");

string title = page.GetTitle();
page.GoBack();
```

---

## 10. Platform-Specific Implementation Details

### MAUI (Appium-Based)

**Enhanced Base Classes:**
- EnhancedControlBase
- EnhancedTextControlBase
- EnhancedSelectorControlBase
- EnhancedToggleControlBase
- EnhancedRangeControlBase
- EnhancedItemsControlBase
- EnhancedContentControlBase
- EnhancedPageBase

**Key Features:**
- Uses AppiumElement for element interaction
- Keyboard shortcuts via Key enumeration
- Tap gestures for click operations
- Element scrolling via ScrollTo()

**Example Control Usage:**
```csharp
// MAUI-specific implementation
var loginControl = testContext.GetControl<EnhancedTextControlBase>("username");
await loginControl.Enter("testuser");
```

### WPF/WinForms (FlaUI-Based)

**Enhanced Base Classes (Shared):**
- EnhancedControlBase
- EnhancedTextControlBase
- EnhancedSelectorControlBase
- EnhancedToggleControlBase
- EnhancedRangeControlBase
- EnhancedItemsControlBase
- EnhancedContentControlBase

**Key Features:**
- Uses FlaUI.Core.AutomationElements
- Visual Studio UI Automation backend
- VirtualKeyShort for keyboard operations
- Direct element manipulation

**Example Control Usage:**
```csharp
// WPF/WinForms implementation (identical interface)
var loginControl = testContext.GetControl<EnhancedTextControlBase>("username");
loginControl.Enter("testuser");
```

### Html/Selenium (WebDriver-Based)

**Enhanced Base Classes (8 total):**
- EnhancedControlBase
- EnhancedTextControlBase
- EnhancedSelectorControlBase
- EnhancedToggleControlBase
- EnhancedRangeControlBase
- EnhancedItemsControlBase
- EnhancedContentControlBase
- EnhancedPageBase

**Key Features:**
- Uses OpenQA.Selenium WebDriver
- SelectElement for dropdown operations
- IJavaScriptExecutor for advanced operations
- Actions for multi-button clicks

**Example Control Usage:**
```csharp
// Html/Selenium implementation
var loginControl = testContext.GetControl<EnhancedTextControlBase>("username");
loginControl.Enter("testuser");
loginControl.Copy();  // Uses Ctrl+C via JavaScript
```

### Html/Playwright (Playwright-Based)

**Enhanced Base Classes (7 total):**
- EnhancedControlBase
- EnhancedTextControlBase
- EnhancedSelectorControlBase
- EnhancedToggleControlBase
- EnhancedRangeControlBase
- EnhancedItemsControlBase
- EnhancedContentControlBase
- EnhancedPageBase

**Key Features:**
- Uses Microsoft.Playwright ILocator
- Async method support with .Wait() wrapping
- EvaluateAsync for JavaScript execution
- Native async/await patterns

**Example Control Usage:**
```csharp
// Html.Playwright implementation (async-aware)
var loginControl = testContext.GetControl<EnhancedTextControlBase>("username");
loginControl.Enter("testuser");  // Wraps async operations
```

### Stride (Game Engine)

**Enhanced Base Classes (6 total):**
- EnhancedStrideControlBase
- EnhancedStrideTextControlBase
- EnhancedStrideSelectorControlBase
- EnhancedStrideToggleControlBase
- EnhancedStrideRangeControlBase
- EnhancedStrideContentControlBase

**Key Features:**
- Network-based game automation
- StrideTestContext for element communication
- GetState() for element state queries
- LogAction() for operations without direct implementation

**Example Control Usage:**
```csharp
// Stride implementation
var gameButton = testContext.GetControl<EnhancedStrideControlBase>("button");
gameButton.Click();  // Uses LeftClick() network command
```

---

## 11. Common Usage Patterns

### Pattern 1: Verify-Then-Interact

```csharp
public void VerifyLoginFormAndSubmit(IPageObject page, string username, string password)
{
    // Verify elements exist
    var usernameField = page.GetControl<ITextInputControl>("username");
    usernameField.WaitExists(true, 5000);

    // Interact with controls
    usernameField.Enter(username);
    var passwordField = page.GetControl<ITextInputControl>("password");
    passwordField.Enter(password);

    // Verify state before submit
    usernameField.AssertTextEquals(username);
    var submitButton = page.GetControl<IClickable>("submit");
    submitButton.WaitClickable(true, 3000);
    submitButton.Click();
}
```

### Pattern 2: Dropdown Selection with Validation

```csharp
public void SelectCountryAndVerify(IPageObject page, string country)
{
    var countryDropdown = page.GetControl<ISingleSelectControl>("country");

    // Get all options to verify selection exists
    var options = countryDropdown.GetAllOptions();
    Assert.Contains(country, options);

    // Select and verify
    countryDropdown.SelectByText(country);
    countryDropdown.AssertSelectedEquals(country);
}
```

### Pattern 3: List Interaction with Scrolling

```csharp
public void SelectFromLongList(IPageObject page, string itemText)
{
    var itemsList = page.GetControl<IScrollableCollectionControl>("items");

    // First verify item exists
    if (!itemsList.IsItemVisible(itemText))
    {
        itemsList.ScrollToItem(itemText);
    }

    // Click the item
    itemsList.ClickItem(itemText);
}
```

### Pattern 4: Complex Control Manipulation

```csharp
public void EditTextWithKeyboardShortcuts(IPageObject page, string originalText, string newText)
{
    var textControl = page.GetControl<IEditableTextControl>("content");

    // Set original text
    textControl.Enter(originalText);

    // Complex editing with keyboard operations
    textControl.SelectAll();     // Ctrl+A
    textControl.Copy();          // Ctrl+C
    textControl.Cut();           // Ctrl+X (cuts the copied text)
    textControl.Paste();         // Ctrl+V (pastes it back)
    textControl.Undo();          // Ctrl+Z (undo operations)
    textControl.Redo();          // Ctrl+Y (redo)

    // Verify final state
    textControl.AssertTextEquals(originalText);
}
```

### Pattern 5: Container and Child Control Access

```csharp
public void InteractWithPanelChildren(IPageObject page)
{
    var mainPanel = page.GetControl<IContainerControl>("mainPanel");

    // Verify structure
    mainPanel.AssertChildCount(5);
    mainPanel.AssertChildExists("saveButton");

    // Get and interact with children
    var saveButton = mainPanel.GetChild<IClickable>("saveButton");
    var cancelButton = mainPanel.GetChild<IClickable>("cancelButton");

    // Work with all children
    var children = mainPanel.GetAllChildren();
    foreach (var child in children)
    {
        if (child is IClickable clickable)
        {
            // Handle clickable children
        }
    }
}
```

---

## 12. Assertion Patterns

### Standard Assertions

```csharp
// Text input assertions
textControl.AssertTextEquals("expected value");
textControl.AssertTextMatches(@"^\d{3}-\d{4}$");  // Pattern regex
textControl.AssertTextContains("substring");

// Selection assertions
dropdown.AssertSelectedEquals("Option 1");
list.AssertSelectedContains("Item 2");

// Checkbox assertions
checkbox.AssertChecked("Checkbox should be checked");
checkbox.AssertUnchecked("Checkbox should be unchecked");

// Toggle assertions
toggle.AssertOn("Toggle should be ON");
toggle.AssertOff("Toggle should be OFF");

// Range assertions
slider.AssertValueEquals(50, 0.5);  // Allow 0.5 tolerance
slider.AssertValueBetween(40, 60);

// Collection assertions
list.AssertItemCount(5);
list.AssertContainsItem("Expected Item");

// Container assertions
panel.AssertChildCount(3);
panel.AssertChildExists("childId");

// Visibility assertions
control.AssertExists("Control should exist");
control.AssertVisible("Control should be visible");
control.AssertEnabled("Control should be enabled");
```

### Custom Assertion Helpers

```csharp
public static class InterfaceAssertions
{
    public static void AssertAllItemsVisible(this ICollectionControl control)
    {
        var items = control.GetAllItems();
        Assert.NotEmpty(items);
        // Additional validation...
    }

    public static void AssertTextInputValid(this ITextInputControl control, string pattern)
    {
        var text = control.GetText();
        Assert.Matches(pattern, text);
    }
}

// Usage
textControl.AssertTextInputValid(@"^\w+@\w+\.\w+$");  // Email pattern
```

---

## 13. Error Handling & Timeouts

### Timeout Handling

```csharp
public void InteractWithDelayedElement(IPageObject page)
{
    var element = page.GetControl<IClickable>("delayedButton");

    try
    {
        // Wait for element to exist (5 second timeout)
        element.WaitExists(true, 5000);
        element.Click();
    }
    catch (TimeoutException ex)
    {
        // Handle timeout - element didn't appear in time
        throw new AssertionException($"Element did not appear within 5 seconds", ex);
    }
}
```

### Retry Logic

```csharp
public void ClickWithRetry(IClickable element, int maxRetries = 3)
{
    int attempts = 0;
    while (attempts < maxRetries)
    {
        try
        {
            element.WaitClickable(true, 2000);
            element.Click();
            return;  // Success
        }
        catch
        {
            attempts++;
            if (attempts >= maxRetries)
                throw;

            // Brief pause before retry
            System.Threading.Thread.Sleep(500);
        }
    }
}
```

---

## 14. Platform Migration Guide

### Switching Platforms

The interface-based approach makes switching between platforms straightforward:

**Original MAUI Test:**
```csharp
var control = testContext.GetControl<MauiButtonControl>("button");
control.TapAsync();
```

**Migrated to Html/Selenium (Using Interface):**
```csharp
var control = testContext.GetControl<IClickable>("button");
control.Click();  // Same method, different platform
```

**Same code works on all platforms:**
- MAUI: Uses AppiumElement.Click()
- WPF/WinForms: Uses FlaUI Click()
- Html/Selenium: Uses Selenium WebDriver click()
- Html.Playwright: Uses ILocator.ClickAsync()
- Stride: Uses network click command

### Interface-Based Test Code

```csharp
// This test code works on ALL 6 platforms without modification
public void LoginTest(IPageObject page, string username, string password)
{
    // Type username
    var usernameField = page.GetControl<ITextInputControl>("username");
    usernameField.Enter(username);

    // Type password
    var passwordField = page.GetControl<ITextInputControl>("password");
    passwordField.Enter(password);

    // Click submit
    var submitButton = page.GetControl<IClickable>("submit");
    submitButton.Click();

    // Verify redirect
    page.WaitVisible(true, 5000);
}

// Call same test with different page implementations:
// - MAUI page object
// - WPF page object
// - Html/Selenium page object
// - Html/Playwright page object
// - Stride page object
```

---

## 15. Quick Reference

### Method Availability by Interface

| Method | IVisualElement | IInteractive | IClickable | ITextInputControl | IEditableTextControl | ISingleSelectControl |
|--------|---|---|---|---|---|---|
| IsExists() | ✅ | | | | | |
| IsVisible() | ✅ | | | | | |
| IsEnabled() | ✅ | | | | | |
| Focus() | | ✅ | | | | |
| Blur() | | ✅ | | | | |
| Click() | | | ✅ | | | |
| Enter() | | | | ✅ | ✅ | |
| Clear() | | | | ✅ | ✅ | |
| Copy() | | | | | ✅ | |
| SelectByText() | | | | | | ✅ |
| GetSelectedText() | | | | | | ✅ |

### Timeout Methods (Available on All Controls)

```csharp
WaitExists(bool exists, int timeoutMs);
WaitVisible(bool visible, int timeoutMs);
WaitEnabled(bool enabled, int timeoutMs);
WaitClickable(bool clickable, int timeoutMs);
```

### Assertion Methods (Available on All Controls)

```csharp
AssertExists(string message = null);
AssertVisible(string message = null);
AssertEnabled(string message = null);
AssertClickable(string message = null);
```

---

## 16. Common Issues & Solutions

### Issue: Element Not Found After Interface Refactoring

**Cause:** Automation ID might differ between platforms

**Solution:**
```csharp
// Use platform-specific automation ID if needed
var element = page.GetControl<IClickable>("platformSpecificId");

// Or use Find() with custom locator
// var element = page.Find(By.Name("buttonName"));
```

### Issue: Keyboard Shortcut Not Working

**Cause:** Platform doesn't support keyboard shortcut in that context

**Solution:**
```csharp
// Instead of Copy() which uses Ctrl+C
var text = textControl.GetText();

// Or use platform-specific method if needed
// var element = page.GetControl<PlatformSpecificControl>("id");
```

### Issue: Async Operations in Playwright

**Cause:** Playwright uses async/await, but IEditableTextControl is sync

**Solution:**
```csharp
// The .Wait() wrapper handles async-to-sync conversion
var text = textControl.Enter("value");  // Internally uses .Wait()

// No need to use await in test code
```

---

## 17. Next Steps

- Review [17-platform-specific-implementation-guides.md](17-platform-specific-implementation-guides.md) for detailed platform examples
- See [18-test-writer-migration-guide.md](18-test-writer-migration-guide.md) for migration examples
- Consult [19-interface-capability-matrix.md](19-interface-capability-matrix.md) for cross-platform reference

---

**Document Status:** Complete for Phase 1, Task 5, Step 6  
**Last Modified:** January 3, 2026  
**Next Review:** When Phase 2 begins (interface extensions)
