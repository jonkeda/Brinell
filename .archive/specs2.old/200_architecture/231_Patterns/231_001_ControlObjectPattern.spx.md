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

| Participant       | Role                                                |
| ----------------- | --------------------------------------------------- |
| IControlObject    | Core interface defining common control capabilities |
| IClickableControl | Interface for clickable elements                    |
| ITextControl      | Interface for text display/input                    |
| IToggleControl    | Interface for on/off elements                       |
| ISelectorControl  | Interface for selection elements                    |
| ControlBase       | Abstract base implementing common behavior          |
| ButtonControl     | Concrete control for buttons                        |
| EntryControl      | Concrete control for text input                     |
| CheckboxControl   | Concrete control for checkboxes                     |

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
    Locator Locator { get; }
    IElementScope Scope { get; }     // NEW: Owning scope (page or container)
    IPageObject? Page { get; }       // Derived from scope hierarchy
  
    // State queries
    bool IsExists();
    bool? IsVisible();
    bool? IsEnabled();
  
    // Wait methods (return bool, don't throw; use nullable skip pattern)
    bool WaitExists(bool? exists, int? timeoutMs = null);
    bool WaitVisible(bool? visible, int? timeoutMs = null);
    bool WaitEnabled(bool? enabled, int? timeoutMs = null);
  
    // Assert methods (wait then throw on failure; use nullable skip pattern)
    void AssertExists(bool? exists, string? message = null, int? timeoutMs = null);
    void AssertVisible(bool? visible, string? message = null, int? timeoutMs = null);
    void AssertEnabled(bool? enabled, string? message = null, int? timeoutMs = null);
}
```

> **Note:** Controls now receive an `IElementScope` (page or container) instead of `ITestContext`. The scope handles element finding within its bounds. See [231_007 Scoped Element Finder](231_007_ScopedElementFinder.spx.md) for details.

> **Nullable Skip Pattern:** When `expected` parameter is null, the operation is skipped and returns true (for Wait) or returns immediately (for Assert). This allows test methods to conditionally skip assertions based on test data.

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
    void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
}

public interface IEditableTextControl : ITextControl
{
    void Enter(string? text, int? timeoutMs = null);
    void Clear(int? timeoutMs = null);
    void SetText(string? text, int? timeoutMs = null);
}

public interface IToggleControl : IControlObject
{
    bool? IsChecked();
    void SetChecked(bool? isChecked, int? timeoutMs = null);
    void Toggle(int? timeoutMs = null);
    void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null);
}

public interface ISelectorControl : IControlObject
{
    void SelectByText(string? text, int? timeoutMs = null);
    void SelectByIndex(int? index, int? timeoutMs = null);
    string? GetSelectedText(int? timeoutMs = null);
    int? GetSelectedIndex(int? timeoutMs = null);
    IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);
    void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
}
```

### 3.3 Abstract Base Class

```csharp
/// <summary>
/// Generic control base - works with any platform element type.
/// </summary>
/// <typeparam name="TElement">Platform-specific element type (AppiumElement, IWebElement, etc.).</typeparam>
/// <typeparam name="TScope">Platform-specific scope type.</typeparam>
public abstract class ControlBase<TElement, TScope> : IControlObject
    where TElement : class
    where TScope : IElementScope<TElement>
{
    protected readonly TScope _scope;
    protected readonly Locator _locator;
    protected readonly ITestLogger? _logger;
  
    public Locator Locator => _locator;
    public IElementScope Scope => _scope;
    public IPageObject? Page => FindPage(_scope);
  
    protected ControlBase(TScope scope, Locator locator)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _logger = scope.Context.Logger;
    }
  
    // Convenience constructor with string locator
    protected ControlBase(TScope scope, string automationId)
        : this(scope, Locator.ByAutomationId(automationId))
    {
    }
  
    // Typed element access - no casting needed!
    protected TElement? TryFindElement() => _scope.TryFindElement(_locator);
    protected TElement FindElement() => _scope.FindElement(_locator);
  
    // Typed context access
    protected ITestContext<TElement> Context => _scope.Context;
  
    // State methods
    public bool IsExists() => TryFindElement() != null;
    public abstract bool? IsVisible();
    public abstract bool? IsEnabled();
  
    // Walk scope hierarchy to find page
    private static IPageObject? FindPage(IElementScope scope)
    {
        if (scope is IPageObject page) return page;
        if (scope is IControlObject control) return FindPage(control.Scope);
        return null;
    }
  
    // Run pattern for logging (from 221_001)
    protected void Run(string action, Action operation) { /* ... */ }
    protected void Run<T>(string action, T? value, Action operation) { /* ... */ }
    protected TResult Run<TResult>(string action, Func<TResult> operation) { /* ... */ }
  
    // RunAssert pattern for assertions (from 221_001)
    protected void RunAssert<T>(string assertType, T? expected, Func<T?> getActual, string? message = null) 
        where T : IComparable? { /* ... */ }
}

/// <summary>
/// MAUI control base - typed for AppiumElement.
/// </summary>
public abstract class MauiControlBase : ControlBase<AppiumElement, IMauiElementScope>
{
    protected MauiControlBase(IMauiElementScope scope, Locator locator) 
        : base(scope, locator) { }
  
    protected MauiControlBase(IMauiElementScope scope, string automationId) 
        : base(scope, automationId) { }
  
    // Typed MAUI context access
    protected new IMauiTestContext Context => _scope.Context;
  
    public override bool? IsVisible() => TryFindElement()?.Displayed;
    public override bool? IsEnabled() => TryFindElement()?.Enabled;
}

/// <summary>
/// Blazor control base - typed for IWebElement.
/// </summary>
public abstract class BlazorControlBase : ControlBase<IWebElement, IBlazorElementScope>
{
    protected BlazorControlBase(IBlazorElementScope scope, Locator locator) 
        : base(scope, locator) { }
  
    protected BlazorControlBase(IBlazorElementScope scope, string cssSelector) 
        : base(scope, Locator.ByCssSelector(cssSelector)) { }
  
    protected new IBlazorTestContext Context => _scope.Context;
  
    public override bool? IsVisible() => TryFindElement()?.Displayed;
    public override bool? IsEnabled() => TryFindElement()?.Enabled;
}
```

### 3.4 Concrete Control Example

```csharp
public class MauiButtonControl : MauiControlBase, IClickableControl
{
    public MauiButtonControl(IMauiElementScope scope, Locator locator)
        : base(scope, locator) { }
  
    public MauiButtonControl(IMauiElementScope scope, string automationId)
        : base(scope, automationId) { }
  
    public void Click(int? timeoutMs = null)
    {
        Run("Click", () =>
        {
            var element = TryFindElement();  // Returns AppiumElement - no casting!
            CheckClickable(timeoutMs);
            element.Click();
  
            // Platform-specific: hide keyboard if shown
            if (Context.IsKeyboardShown())
                Context.HideKeyboard();  // Typed context access!
        });
    }
}

public class BlazorButtonControl : BlazorControlBase, IClickableControl
{
    public BlazorButtonControl(IBlazorElementScope scope, Locator locator)
        : base(scope, locator) { }
  
    public BlazorButtonControl(IBlazorElementScope scope, string testId)
        : base(scope, Locator.ByDataTestId(testId)) { }
  
    public void Click(int? timeoutMs = null)
    {
        Run("Click", () =>
        {
            var element = TryFindElement();  // Returns IWebElement - no casting!
            CheckClickable(timeoutMs);
  
            // Scroll into view (web-specific)
            Context.ExecuteScript("arguments[0].scrollIntoView(true);", element);
 // Typed context access!
        });
    }
}
```

---

## 4. Usage

### 4.1 Creating Controls

```csharp
// In page object - controls receive 'this' (page) as their scope
public class LoginPage : MauiPageObjectBase
{
    public MauiEntryControl UsernameEntry => new(this, "UsernameEntry");
    public MauiEntryControl PasswordEntry => new(this, "PasswordEntry");
    public MauiButtonControl LoginButton => new(this, "LoginButton");
    public MauiLabelControl ErrorLabel => new(this, "ErrorLabel");
  
    public LoginPage(IMauiTestContext context) : base(context, "LoginPage") { }
}

// In container - controls receive container as their scope
public class ProductCard : MauiContainerBase
{
    public MauiLabelControl Name => new(this, "ProductName");
    public MauiLabelControl Price => new(this, "ProductPrice");
    public MauiButtonControl AddToCart => new(this, "AddToCart");
  
    public ProductCard(IMauiElementScope parentScope, Locator locator)
        : base(parentScope, locator) { }
}
```

> **Key Change:** Controls receive a scope (page or container), not a context. The scope handles finding elements within its bounds. This replaces the previous `(context, locator, page)` pattern.

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

### 4.3 Nullable Parameter Pattern (Skip Pattern)

```csharp
// If expected value is null, the operation is skipped
loginPage.UsernameEntry.Enter(optionalUsername);  // Skips if null
loginPage.PasswordEntry.SetText(null);            // No-op

// Wait/Assert methods skip verification when expected is null
loginPage.ErrorLabel.AssertText(expectedError);   // Skips if null
loginPage.LoginButton.WaitVisible(null);          // Returns true immediately

// This enables data-driven tests with optional verifications
public void TestLogin(string? expectedError)
{
    loginPage.Login("user", "pass");
    loginPage.ErrorLabel.AssertText(expectedError);  // Only asserts if expectedError is not null
}
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

### 5.3 Return Values

State queries have specific return semantics:

```csharp
bool exists = control.IsExists();  // Always returns true or false
bool? visible = control.IsVisible();  // null if element doesn't exist
bool? enabled = control.IsEnabled();  // null if element doesn't exist

// IsVisible and IsEnabled return null when element not found
// because visibility/enabled state cannot be determined without the element
// IsExists always returns bool because existence itself is the question
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
- [ ] IsExists() returns bool; IsVisible/IsEnabled return null when element not found
- [ ] Actions handle null parameters (skip operation)
- [ ] Wait/Assert methods support nullable skip pattern
- [ ] Elements are re-found on each operation
- [ ] Raw automation elements are never exposed
- [ ] Controls receive `IElementScope` (page or container), not `ITestContext`
- [ ] Controls use typed platform base classes (`MauiControlBase`, `BlazorControlBase`)
- [ ] No casting is required to access platform context

---

## 8. Constructor Signature Summary

| Before                                                                            | After                                                            |
| --------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| `ControlBase(ITestContext context, Locator locator, IPageObject? page)`         | `ControlBase<TElement, TScope>(TScope scope, Locator locator)` |
| `MauiControlBase(IMauiTestContext context, Locator locator, IPageObject? page)` | `MauiControlBase(IMauiElementScope scope, Locator locator)`    |
| `_context.FindElement(_locator)`                                                | `_scope.TryFindElement(_locator)`                              |

---

## Related Documents

- [211_001 Interfaces](../211_Modules/211_001_Interfaces.spx.md)
- [211_002 BaseClasses](../211_Modules/211_002_BaseClasses.spx.md)
- [211_003 Controls](../211_Modules/211_003_Controls.spx.md)
- [221_001 Logging](../221_Foundation/221_001_Logging.spx.md)
- [FR-100 Control Object](../../100_requirements/120_functional/120_100_ControlObject.spx.md)
