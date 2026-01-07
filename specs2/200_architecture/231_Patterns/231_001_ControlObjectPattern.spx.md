# 231_001 Control Object Pattern

## pattern ControlObject

- **title**: Control Object Pattern
- **type**: Structural
- **purpose**: Encapsulate UI element interactions in typed, reusable objects

---

## Description

The Control Object pattern wraps UI elements in strongly-typed objects that provide actions and state queries. Instead of interacting directly with automation elements, tests interact with control objects that know how to perform operations safely and reliably.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Direct interaction with automation elements leads to:
- Repetitive code for waiting, error handling, logging
- Brittle tests that break when UI implementation changes
- No type safety for element capabilities
- Inconsistent interaction patterns across tests

**Solution:** Create control objects that:
- Encapsulate element location and interaction
- Provide type-safe methods for capabilities
- Handle waiting and error conditions consistently
- Support logging and diagnostics automatically

---

## 2. Structure

### 2.1 Participants

| Participant | Role |
|-------------|------|
| IControlObject | Core interface defining common control capabilities |
| IClickableControl | Interface for clickable elements |
| ITextControl | Interface for text display/input |
| IToggleControl | Interface for on/off elements |
| ISelectorControl | Interface for selection elements |
| ControlBase | Abstract base implementing common behavior |
| ButtonControl | Concrete control for buttons |
| EntryControl | Concrete control for text input |
| CheckboxControl | Concrete control for checkboxes |

### 2.2 Interface Hierarchy

```
IControlObject (core: existence, visibility, enabled)
├── IClickableControl (click, double-click)
├── ITextControl (get text, enter text)
│   └── IEditableTextControl (clear, set text)
├── IToggleControl (toggle, is checked)
├── ISelectorControl (select, get selected)
└── IRangeControl (get/set value, min, max)
```

---

## 3. Implementation

### 3.1 Core Interface

```csharp
public interface IControlObject
{
    // Identity
    string AutomationId { get; }
    IPageObject? Page { get; }
    
    // State queries (null = element not found)
    bool? IsExists(int? timeoutMs = null);
    bool? IsVisible(int? timeoutMs = null);
    bool? IsEnabled(int? timeoutMs = null);
    
    // Wait methods (return bool, don't throw)
    bool WaitExists(bool exists, int? timeoutMs = null);
    bool WaitVisible(bool visible, int? timeoutMs = null);
    bool WaitEnabled(bool enabled, int? timeoutMs = null);
    
    // Check methods (throw on failure)
    void CheckExists(bool exists, int? timeoutMs = null, string? message = null);
    void CheckVisible(bool visible, int? timeoutMs = null, string? message = null);
    void CheckEnabled(bool enabled, int? timeoutMs = null, string? message = null);
    
    // Assert methods (immediate, throw on failure)
    void AssertExists(string? message = null);
    void AssertVisible(string? message = null);
    void AssertEnabled(string? message = null);
}
```

### 3.2 Capability Interfaces

```csharp
public interface IClickableControl : IControlObject
{
    void Click(int? timeoutMs = null);
    void DoubleClick(int? timeoutMs = null);
    void RightClick(int? timeoutMs = null);
}

public interface ITextControl : IControlObject
{
    string? GetText(int? timeoutMs = null);
    void AssertTextEquals(string? expected, string? message = null);
    void AssertTextContains(string? expected, string? message = null);
}

public interface IEditableTextControl : ITextControl
{
    void Enter(string? text, int? timeoutMs = null);
    void Clear(int? timeoutMs = null);
    void SetText(string? text, int? timeoutMs = null);
}

public interface IToggleControl : IControlObject
{
    bool? IsChecked(int? timeoutMs = null);
    void SetChecked(bool? isChecked, int? timeoutMs = null);
    void Toggle(int? timeoutMs = null);
    void AssertChecked(bool? expected, string? message = null);
}

public interface ISelectorControl : IControlObject
{
    void SelectByText(string? text, int? timeoutMs = null);
    void SelectByIndex(int? index, int? timeoutMs = null);
    string? GetSelectedText(int? timeoutMs = null);
    int? GetSelectedIndex(int? timeoutMs = null);
    IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);
    void AssertSelectedText(string? expected, string? message = null);
}
```

### 3.3 Abstract Base Class

```csharp
public abstract class ControlBase : IControlObject
{
    protected readonly ITestContext _context;
    protected readonly Locator _locator;
    protected readonly IPageObject? _page;
    protected readonly ITestLogger? _logger;
    protected readonly string _testName;
    
    public string AutomationId => _locator.Value;
    public IPageObject? Page => _page;
    
    protected ControlBase(ITestContext context, Locator locator, IPageObject? page = null)
    {
        _context = context;
        _locator = locator;
        _page = page;
        _logger = context.Logger;
        _testName = context.TestName ?? "UnknownTest";
    }
    
    // Convenience constructor with string locator
    protected ControlBase(ITestContext context, string automationId, IPageObject? page = null)
        : this(context, new Locator(page?.DefaultLocatorStrategy ?? LocatorStrategy.AutomationId, automationId), page)
    {
    }
    
    // Abstract methods for platform-specific implementation
    protected abstract object? FindElement();
    protected abstract bool? ElementExists();
    protected abstract bool? ElementVisible();
    protected abstract bool? ElementEnabled();
    
    // Run pattern for logging (from 221_001)
    protected void Run(string action, Action operation) { /* ... */ }
    protected void Run<T>(string action, T? value, Action operation) { /* ... */ }
    protected TResult Run<TResult>(string action, Func<TResult> operation) { /* ... */ }
    
    // RunAssert pattern for assertions (from 221_001)
    protected void RunAssert<T>(string assertType, T? expected, Func<T?> getActual, string? message = null) 
        where T : IComparable? { /* ... */ }
}
```

### 3.4 Concrete Control Example

```csharp
public class ButtonControl : ClickableControlBase
{
    public ButtonControl(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public ButtonControl(IMauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
    
    public override void Click(int? timeoutMs = null)
    {
        Run("Click", () =>
        {
            var element = FindElement();
            WaitClickable(timeoutMs);
            ClickElement(element);
        });
    }
}
```

---

## 4. Usage

### 4.1 Creating Controls

```csharp
// In page object - controls as properties
public class LoginPage : PageBase
{
    public EntryControl UsernameEntry => new(_context, "UsernameEntry", this);
    public EntryControl PasswordEntry => new(_context, "PasswordEntry", this);
    public ButtonControl LoginButton => new(_context, "LoginButton", this);
    public LabelControl ErrorLabel => new(_context, "ErrorLabel", this);
}
```

### 4.2 Using Controls in Tests

```csharp
[Fact]
public void Login_WithValidCredentials_ShowsHomePage()
{
    var loginPage = new LoginPage(_context);
    
    loginPage.UsernameEntry.Enter("testuser");
    loginPage.PasswordEntry.Enter("password123");
    loginPage.LoginButton.Click();
    
    var homePage = new HomePage(_context);
    homePage.WelcomeLabel.AssertTextContains("Welcome");
}
```

### 4.3 Nullable Parameter Pattern

```csharp
// If value is null, no action is performed
loginPage.UsernameEntry.Enter(optionalUsername);  // Safe with null
loginPage.PasswordEntry.SetText(null);            // No-op

// Assertions with null expected skip verification
loginPage.ErrorLabel.AssertTextEquals(expectedError);  // Skips if null
```

---

## 5. Key Principles

### 5.1 Lazy Element Location

Controls don't find elements until needed:

```csharp
// Element is NOT found here
var button = new ButtonControl(_context, "SubmitButton", page);

// Element IS found here (on first action)
button.Click();
```

### 5.2 Re-find on Each Operation

Controls re-find elements for each operation:

```csharp
button.Click();           // Finds element, clicks
Thread.Sleep(1000);       // UI might re-render
button.Click();           // Finds element again, clicks
```

This handles dynamic UIs where elements are recreated.

### 5.3 Null Safety

State queries return `null` when element doesn't exist:

```csharp
bool? exists = control.IsExists();
if (exists == null)
    // Element not found in UI tree
else if (exists == true)
    // Element exists
else
    // Element exists but condition false (rare for IsExists)
```

---

## 6. Anti-Patterns

### 6.1 Don't Expose Raw Elements

```csharp
// ❌ BAD: Exposes implementation details
public AppiumElement GetElement() => FindElement();

// ✅ GOOD: Encapsulate all interactions
public void Click() => Run("Click", () => ClickElement(FindElement()));
```

### 6.2 Don't Cache Elements

```csharp
// ❌ BAD: Element may become stale
private AppiumElement _element;
public void Click() => _element.Click();

// ✅ GOOD: Re-find on each operation
public void Click() => FindElement().Click();
```

### 6.3 Don't Create Controls in Methods

```csharp
// ❌ BAD: Factory methods hide control creation
public EntryControl GetUsernameField() => new(_context, "Username", this);

// ✅ GOOD: Properties make structure visible
public EntryControl UsernameField => new(_context, "Username", this);
```

---

## 7. Validation Rules

The Control Object pattern is valid when:

- [ ] Controls implement appropriate capability interfaces
- [ ] All public methods use Run/RunAssert for logging
- [ ] State queries return null when element not found
- [ ] Actions handle null parameters (no-op)
- [ ] Elements are re-found on each operation
- [ ] Raw automation elements are never exposed
- [ ] Controls accept optional page reference for scoping

---

## Related Documents

- [211_001 Interfaces](../211_Modules/211_001_Interfaces.spx.md)
- [211_002 BaseClasses](../211_Modules/211_002_BaseClasses.spx.md)
- [211_003 Controls](../211_Modules/211_003_Controls.spx.md)
- [221_001 Logging](../221_Foundation/221_001_Logging.spx.md)
- [FR-100 Control Object](../../100_requirements/120_functional/120_100_ControlObject.spx.md)
