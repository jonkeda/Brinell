---
applyTo: "**/*Blazor*UITests*/**/*.cs,**/*Html*/**/*.cs"
description: "Brinell HTML/Blazor UI Testing patterns with Selenium automation"
---

# Brinell HTML/Blazor UI Testing Framework

## Overview

Brinell.Html provides HTML/Blazor-specific UI test automation using Selenium as the underlying driver. All operations are **synchronous** (the framework handles async internally).

## Framework Components

| Component | Namespace |
|-----------|-----------|
| Test Context | `Brinell.Html.Infrastructure.SeleniumTestContext` |
| Page Base | `Brinell.Html.Controls.Base.PageBase` |
| Loading Page Base | `Brinell.Html.Controls.Base.LoadingPageBase` |
| Controls | `Brinell.Html.Controls.*` |

---

## Page Object Structure

```csharp
using Brinell.Core.Abstractions;
using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace MyApp.UITests.PageObjects;

public class HomePage : PageBase
{
    // Required: CSS selector or data-testid for page identification
    public override string AutomationId => "#page-title";
    
    public HomePage(SeleniumTestContext context) : base(context) { }
    
    // Controls - use CSS selectors, IDs, or data attributes
    public LabelControl PageTitle { get; }
    public LabelControl WelcomeMessage { get; }
    public ButtonControl LoginButton { get; }
    public LinkControl DashboardLink { get; }
    public TextInputControl SearchInput { get; }
    
    public HomePage(SeleniumTestContext context) : base(context)
    {
        // Initialize controls in constructor with selectors
        PageTitle = new LabelControl(context, this, "#page-title");
        WelcomeMessage = new LabelControl(context, this, "#welcome-message");
        LoginButton = new ButtonControl(context, this, "#login-btn");
        DashboardLink = new LinkControl(context, this, "#link-dashboard");
        SearchInput = new TextInputControl(context, this, "#search-input");
    }
    
    // Override IsDisplayed for reliable page detection
    public override bool IsDisplayed()
    {
        return PageTitle.IsVisible() && PageTitle.GetText().Contains("Welcome");
    }
    
    // Navigation workflow
    public DashboardPage NavigateToDashboard()
    {
        Log("NavigateToDashboard()");
        DashboardLink.Click();
        var dashboardPage = new DashboardPage(_context);
        dashboardPage.WaitForDisplayed();
        return dashboardPage;
    }
}
```

---

## Available Control Types

### Basic Controls

| Control | Class | HTML Elements | Key Methods |
|---------|-------|---------------|-------------|
| Button | `ButtonControl` | `<button>`, `<input type="button/submit">` | `Click()`, `DoubleClick()` |
| Label | `LabelControl` | `<span>`, `<div>`, `<p>`, `<h1-6>` | `GetText()`, `AssertTextEquals()` |
| Link | `LinkControl` | `<a>` | `Click()`, `GetHref()` |
| TextInput | `TextInputControl` | `<input type="text/email/password/...">` | `Enter()`, `Clear()` |
| TextArea | `TextAreaControl` | `<textarea>` | `Enter()`, `Clear()` |
| CheckBox | `CheckBoxControl` | `<input type="checkbox">` | `Toggle()`, `IsChecked()` |
| Select | `SelectControl` | `<select>` | `SelectByIndex()`, `SelectByText()` |
| RangeInput | `RangeInputControl` | `<input type="range">` | `SetValue()`, `GetValue()` |
| Progress | `ProgressControl` | `<progress>` | `GetProgress()`, `AssertProgress()` |
| Table | `TableControl` | `<table>` | `GetRowCount()`, `GetCellText()` |
| List | `ListControl` | `<ul>`, `<ol>` | `GetItemCount()`, `GetItemText()` |
| ScrollContainer | `ScrollContainerControl` | Any scrollable `<div>` | `ScrollTo()`, `ScrollToTop()` |

---

## Control-Specific APIs

### ButtonControl / ContentControlBase

```csharp
void Click()                          // Click the button
void DoubleClick()                    // Double-click
void RightClick()                     // Right-click (context menu)
void Hover()                          // Hover over element

// Inherited from ControlBase
void AssertVisible(string? message = null)
void AssertEnabled(string? message = null)
void AssertTextEquals(string expected, string? message = null)
```

### TextInputControl / TextAreaControl

```csharp
void Enter(string text)               // Enter text (types character by character)
void Clear()                          // Clear all text
void ClearAndEnter(string text)       // Clear then enter

// Inherited from ControlBase
string GetText()                      // Get current value
void AssertTextEquals(string expected, string? message = null)
void AssertTextContains(string expected, string? message = null)
void AssertTextEmpty(string? message = null)
```

### LabelControl

```csharp
// Uses base ControlBase methods
string GetText()                      // Get element text
void AssertTextEquals(string expected, string? message = null)
void AssertTextContains(string expected, string? message = null)
void AssertTextStartsWith(string prefix, string? message = null)
void AssertTextEndsWith(string suffix, string? message = null)
void AssertTextMatches(string pattern, string? message = null)
```

### LinkControl

```csharp
void Click()                          // Click the link
string GetHref()                      // Get href attribute
string GetTarget()                    // Get target attribute

// Inherited from ContentControlBase
void DoubleClick()
void RightClick()
void Hover()
```

### CheckBoxControl

```csharp
bool IsChecked()                      // Check if checked
void Toggle()                         // Toggle state
void SetChecked(bool value)           // Set checked state
void Check()                          // Set to checked
void Uncheck()                        // Set to unchecked

bool WaitChecked(bool expected = true, int? timeoutMs = null)
void CheckChecked(bool expected = true, int? timeoutMs = null)
void AssertChecked(string? message = null)
void AssertNotChecked(string? message = null)
```

### SelectControl

```csharp
void SelectByIndex(int index)         // Select by 0-based index
void SelectByText(string text)        // Select by visible text
void SelectByValue(string value)      // Select by value attribute
int GetSelectedIndex()                // Get selected index
string GetSelectedText()              // Get selected option text
string GetSelectedValue()             // Get selected value
IReadOnlyList<string> GetOptions()    // Get all option texts
int GetOptionCount()                  // Get number of options

void AssertSelectedIndex(int expected, string? message = null)
void AssertSelectedText(string expected, string? message = null)
void AssertOptionCount(int expected, string? message = null)
```

### RangeInputControl

```csharp
double GetValue()                     // Get current value
void SetValue(double value)           // Set value
double GetMin()                       // Get min attribute
double GetMax()                       // Get max attribute
double GetStep()                      // Get step attribute

bool WaitValue(double expected, double tolerance = 0.01, int? timeoutMs = null)
void AssertValue(double expected, double tolerance = 0.01, string? message = null)
void AssertValueInRange(double min, double max, string? message = null)
```

### ProgressControl

```csharp
double GetProgress()                  // Get progress value (0.0 to 1.0)
double GetValue()                     // Get raw value
double GetMax()                       // Get max attribute

void AssertProgress(double expected, double tolerance = 0.01, string? message = null)
void AssertProgressComplete(string? message = null)
```

### TableControl

```csharp
int GetRowCount()                     // Get number of data rows
int GetColumnCount()                  // Get number of columns
string GetCellText(int row, int column)  // Get cell text
string GetHeaderText(int column)      // Get header cell text
IReadOnlyList<string> GetRowTexts(int row)    // Get all cells in row
IReadOnlyList<string> GetColumnTexts(int col) // Get all cells in column

void ClickCell(int row, int column)   // Click a cell
void AssertRowCount(int expected, string? message = null)
void AssertCellText(int row, int col, string expected, string? message = null)
```

### ListControl

```csharp
int GetItemCount()                    // Get number of items
string GetItemText(int index)         // Get item text by index
IReadOnlyList<string> GetAllItemTexts()  // Get all item texts
bool HasItem(string text)             // Check if item exists

void ClickItem(int index)             // Click item by index
void ClickItem(string text)           // Click item by text
void AssertItemCount(int expected, string? message = null)
void AssertHasItem(string text, string? message = null)
```

---

## Selector Support

HTML controls support multiple selector strategies:

```csharp
// CSS selector (default)
new ButtonControl(context, this, "#submit-btn")
new ButtonControl(context, this, ".btn.btn-primary")
new ButtonControl(context, this, "button[type='submit']")

// ID selector (prefix with #)
new LabelControl(context, this, "#page-title")

// Class selector (prefix with .)
new LabelControl(context, this, ".error-message")

// Data attributes
new ButtonControl(context, this, "[data-testid='submit']")
new InputControl(context, this, "[data-automation='email']")

// XPath (starts with // or /)
new ButtonControl(context, this, "//button[@id='submit']")
new LabelControl(context, this, "/html/body/div/span")

// Complex selectors
new LabelControl(context, this, "form#login .error:first-child")
new ButtonControl(context, this, "nav > ul > li:nth-child(2) > a")
```

### Recommended Selector Priority

1. **`[data-testid]`** - Test-specific, stable
2. **`#id`** - Unique, fast
3. **`.class`** - If specific enough
4. **CSS attribute** - `[name='field']`
5. **XPath** - Last resort, fragile

---

## HTML Element Setup

Add `data-testid` or `id` attributes for test accessibility:

```html
<!-- Blazor component -->
@page "/"

<div class="container">
    <h1 id="page-title">Welcome</h1>
    
    <p id="welcome-message">Welcome to the application</p>
    
    <div class="form-group">
        <input type="text" 
               id="search-input" 
               data-testid="search-input"
               placeholder="Search..." />
    </div>
    
    <button id="login-btn" 
            data-testid="login-button"
            class="btn btn-primary">
        Login
    </button>
    
    <nav id="main-nav">
        <a id="link-counter" href="/counter">Counter</a>
        <a id="link-dashboard" href="/dashboard">Dashboard</a>
    </nav>
    
    <select id="color-select" data-testid="color-picker">
        <option value="red">Red</option>
        <option value="green">Green</option>
        <option value="blue">Blue</option>
    </select>
</div>
```

---

## HTML-Specific Methods

All HTML controls have additional methods:

```csharp
// Get any attribute
string? GetAttribute(string attributeName)

// Get CSS property
string? GetCssValue(string propertyName)

// Check for CSS class
bool HasClass(string className)

// Assert CSS class
void AssertHasClass(string className, string? message = null)
void AssertNotHasClass(string className, string? message = null)

// Assert attribute value
void AssertAttribute(string attrName, string expected, string? message = null)
```

---

## Blazor-Specific Considerations

### Loading States

Use `LoadingPageBase` for pages with loading indicators:

```csharp
public class DashboardPage : LoadingPageBase
{
    protected override string? LoadingIndicatorSelector => "#loading-spinner";
    
    public override string AutomationId => "#dashboard-content";
    
    public DashboardPage(SeleniumTestContext context) : base(context) { }
    
    // IsReady() automatically waits for loading to complete
}
```

### Blazor Rendering

Blazor may need extra time for component rendering:

```csharp
// Wait for element to appear after async operation
control.WaitVisible(true, timeoutMs: 5000);

// Wait for text to update
control.WaitTextEquals("Expected Value", timeoutMs: 3000);

// Wait for page to be ready (displayed AND not loading)
page.WaitForReady();
```

### Dynamic Content

For content that updates dynamically:

```csharp
// Wait for element to appear
button.CheckExists();  // Throws if not found within timeout

// Wait for specific text
label.WaitTextContains("Success");

// Wait for element count
listControl.WaitItemCount(5, timeoutMs: 3000);
```

---

## Test Example

```csharp
using Xunit;
using MyApp.UITests.PageObjects;

public class CounterTests : HtmlUITestBase
{
    [Fact]
    public void Counter_Increment_UpdatesDisplay()
    {
        // Navigate
        _context.NavigateTo("/counter");
        
        // Arrange
        var counterPage = new CounterPage(_context);
        counterPage.WaitForDisplayed();
        
        // Act
        counterPage.IncrementButton.Click();
        
        // Assert - use control assertions, NOT FluentAssertions
        counterPage.CountDisplay.AssertTextContains("1");
    }
    
    [Fact]
    public void Login_ValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        var loginPage = new LoginPage(_context);
        loginPage.WaitForDisplayed();
        
        // Act
        loginPage.UsernameInput.ClearAndEnter("testuser");
        loginPage.PasswordInput.ClearAndEnter("password123");
        loginPage.LoginButton.Click();
        
        // Assert - verify navigation
        var dashboardPage = new DashboardPage(_context);
        dashboardPage.WaitForDisplayed();
        dashboardPage.WelcomeLabel.AssertTextContains("Welcome, testuser");
    }
    
    [Fact]
    public void Form_InvalidInput_ShowsError()
    {
        // Arrange
        var formPage = new FormPage(_context);
        formPage.WaitForDisplayed();
        
        // Act - submit empty form
        formPage.SubmitButton.Click();
        
        // Assert - check error message appears
        formPage.ErrorMessage.WaitVisible();
        formPage.ErrorMessage.AssertTextContains("required");
        formPage.ErrorMessage.AssertHasClass("text-danger");
    }
}
```

---

## Page Navigation

```csharp
public class BasePage : PageBase
{
    // Navigation using workflow methods
    public CounterPage NavigateToCounter()
    {
        Log("NavigateToCounter()");
        _context.NavigateTo("/counter");
        var page = new CounterPage(_context);
        page.WaitForDisplayed();
        return page;
    }
    
    // Or using links
    public DashboardPage ClickDashboardLink()
    {
        Log("ClickDashboardLink()");
        DashboardLink.Click();
        var page = new DashboardPage(_context);
        page.WaitForDisplayed();
        return page;
    }
}
```

---

## Best Practices

### ✅ DO

1. **Use `data-testid` attributes** for test-specific selectors
2. **Use `LoadingPageBase`** for pages with async loading
3. **Use `WaitForReady()`** to ensure page is interactive
4. **Wait for dynamic content** before assertions
5. **Use control assertions** (`control.AssertTextEquals()`)

### ❌ DON'T

1. **Don't use FluentAssertions** - use Brinell assertions
2. **Don't rely on text selectors** - use IDs or data attributes
3. **Don't assume immediate rendering** - Blazor is async
4. **Don't use `Thread.Sleep()`** - use Wait methods
5. **Don't access driver directly** - use page objects

---

## Version

- **Framework Version:** 1.0
- **Spec Reference:** SPEC-006 (ControlObject Framework)
- **Driver:** Selenium WebDriver
