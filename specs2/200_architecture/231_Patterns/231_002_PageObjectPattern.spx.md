# 231_002 Page Object Pattern

## pattern PageObject

- **title**: Page Object Pattern
- **type**: Structural
- **purpose**: Encapsulate page structure and provide access to page controls

---

## Description

The Page Object pattern represents application pages/screens as classes that encapsulate the page structure and provide access to controls. This separates test logic from UI structure, making tests more maintainable and readable.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Tests that directly interact with UI elements:
- Duplicate locator strings across tests
- Break when UI structure changes
- Mix test logic with UI structure details
- Lack clear organization

**Solution:** Create page objects that:
- Encapsulate all locators for a page
- Provide typed access to controls
- Define page-specific operations
- Abstract UI structure from tests

---

## 2. Structure

### 2.1 Participants

| Participant | Role |
|-------------|------|
| IPageObject | Core interface for page behavior |
| PageObjectBase | Abstract base with common functionality |
| LoginPage | Concrete page for login screen |
| HomePage | Concrete page for home screen |
| Controls | Control objects owned by page |

### 2.2 Page-Control Relationship

```
┌─────────────────────────────────────────────┐
│                  LoginPage                   │
│                                             │
│  Properties (Controls):                     │
│  ├── UsernameEntry : EntryControl          │
│  ├── PasswordEntry : EntryControl          │
│  ├── LoginButton : ButtonControl           │
│  └── ErrorLabel : LabelControl             │
│                                             │
│  Methods (Page Operations):                 │
│  ├── WaitForPage()                         │
│  └── EnterCredentials(user, pass)          │
└─────────────────────────────────────────────┘
```

---

## 3. Implementation

### 3.1 Page Interface

```csharp
/// <summary>
/// Base page object - identity and state.
/// </summary>
public interface IPageObject
{
    /// <summary>
    /// Page name for logging and identification.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Default locator strategy for controls on this page.
    /// </summary>
    LocatorStrategy DefaultLocatorStrategy { get; }
    
    /// <summary>
    /// Check if page is loaded and ready.
    /// </summary>
    bool IsLoaded(int? timeoutMs = null);
}

/// <summary>
/// Generic page object - page is also an element scope.
/// </summary>
/// <typeparam name="TElement">Platform-specific element type.</typeparam>
public interface IPageObject<TElement> : IPageObject, IElementScope<TElement>
    where TElement : class
{
}

/// <summary>
/// MAUI page object - typed for Appium.
/// </summary>
public interface IMauiPageObject : IPageObject<AppiumElement>, IMauiElementScope
{
}

/// <summary>
/// Blazor page object - typed for Selenium.
/// </summary>
public interface IBlazorPageObject : IPageObject<IWebElement>, IBlazorElementScope
{
}
```

> **Key Change:** `IPageObject<TElement>` now extends `IElementScope<TElement>`, making every page a scope that can find elements. Controls receive the page as their scope.

### 3.2 Page Base Class

```csharp
/// <summary>
/// Generic page base - implementation uses TContext for typed access.
/// </summary>
/// <typeparam name="TElement">Platform-specific element type.</typeparam>
/// <typeparam name="TContext">Platform-specific context type.</typeparam>
public abstract class PageObjectBase<TElement, TContext> : IPageObject<TElement>
    where TElement : class
    where TContext : ITestContext<TElement>
{
    protected readonly TContext _context;
    protected readonly string _name;
    
    public string Name => _name;
    public virtual LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    protected PageObjectBase(TContext context, string name)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    // Typed context access for subclasses
    protected TContext Context => _context;
    
    // IElementScope<TElement> - page delegates to context (searches from driver root)
    public TElement? ScopeRoot => null;  // Page uses driver root
    object? IElementScope.ScopeRoot => null;
    
    ITestContext<TElement> IElementScope<TElement>.Context => _context;
    ITestContext IElementScope.Context => _context;
    
    public TElement? TryFindElement(Locator locator) => _context.TryFindElement(locator);
    public TElement FindElement(Locator locator) => _context.FindElement(locator);
    public IReadOnlyList<TElement> FindElements(Locator locator) => _context.FindElements(locator);
    
    /// <summary>
    /// Wait for page to be ready. Override in derived classes.
    /// </summary>
    public virtual bool IsLoaded(int? timeoutMs = null) => true;
    
    public byte[] TakeScreenshot() => _context.TakeScreenshot();
}

/// <summary>
/// MAUI page base - typed alias for common use.
/// </summary>
public abstract class MauiPageObjectBase : PageObjectBase<AppiumElement, IMauiTestContext>, IMauiPageObject
{
    protected MauiPageObjectBase(IMauiTestContext context, string name) 
        : base(context, name) { }
    
    // IMauiElementScope - narrow Context type
    IMauiTestContext IMauiElementScope.Context => _context;
}

/// <summary>
/// Blazor page base - typed alias for common use.
/// </summary>
public abstract class BlazorPageObjectBase : PageObjectBase<IWebElement, IBlazorTestContext>, IBlazorPageObject
{
    protected BlazorPageObjectBase(IBlazorTestContext context, string name) 
        : base(context, name) { }
    
    public override LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.DataTestId;
    
    IBlazorTestContext IBlazorElementScope.Context => _context;
}
```

### 3.3 Concrete Page Example

```csharp
public class LoginPage : MauiPageObjectBase
{
    // Controls defined as properties - 'this' (page) is their scope
    public MauiEntryControl UsernameEntry => new(this, "UsernameEntry");
    public MauiEntryControl PasswordEntry => new(this, "PasswordEntry");
    public MauiButtonControl LoginButton => new(this, "LoginButton");
    public MauiButtonControl ForgotPasswordLink => new(this, "ForgotPasswordLink");
    public MauiLabelControl ErrorLabel => new(this, "ErrorLabel");
    
    public LoginPage(IMauiTestContext context) : base(context, "LoginPage")
    {
    }
    
    /// <summary>
    /// Check if page is loaded by verifying login button exists.
    /// </summary>
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return LoginButton.WaitExists(true, timeoutMs);
    }
    
    /// <summary>
    /// Page-level convenience method for entering credentials.
    /// </summary>
    public void EnterCredentials(string? username, string? password)
    {
        UsernameEntry.Enter(username);
        PasswordEntry.Enter(password);
    }
    
    /// <summary>
    /// Platform-specific operation using typed context.
    /// </summary>
    public void HideKeyboardIfShown()
    {
        if (Context.IsKeyboardShown())
            Context.HideKeyboard();  // No casting needed!
    }
}

public class LoginPageBlazor : BlazorPageObjectBase
{
    public BlazorEntryControl UsernameEntry => new(this, "username-input");
    public BlazorEntryControl PasswordEntry => new(this, "password-input");
    public BlazorButtonControl LoginButton => new(this, "login-button");
    public BlazorLabelControl ErrorLabel => new(this, "error-label");
    
    public LoginPageBlazor(IBlazorTestContext context) : base(context, "LoginPage")
    {
    }
    
    public override bool IsLoaded(int? timeoutMs = null)
    {
        Context.WaitForBlazorReady(timeoutMs);  // No casting needed!
        return LoginButton.WaitExists(true, timeoutMs);
    }
}
```

> **Key Change:** Controls receive `this` (page) as their scope. The page is both an identity object and an element scope. Controls use simple locators - no more `ScopedTo()` chaining.

---

## 4. Key Principles

### 4.1 Controls as Properties

Define controls as expression-bodied properties, passing `this` (page) as scope:

```csharp
// ✅ GOOD: Declarative, visible structure, page is scope
public MauiEntryControl UsernameEntry => new(this, "UsernameEntry");
public MauiButtonControl LoginButton => new(this, "LoginButton");

// ❌ BAD: Factory methods hide structure
public MauiEntryControl GetUsernameEntry() => new(this, "UsernameEntry");

// ❌ OBSOLETE: Old pattern with context + page
public EntryControl UsernameEntry => new(_context, "UsernameEntry", this);
```

### 4.2 No Navigation Returns

Navigation methods do NOT return target pages:

```csharp
// ✅ GOOD: Action only, test creates target page
public void ClickLogin()
{
    LoginButton.Click();
}

// Test usage:
loginPage.ClickLogin();
var homePage = new HomePage(_context);  // Test controls lifecycle

// ❌ BAD: Method creates and returns page
public HomePage ClickLogin()
{
    LoginButton.Click();
    return new HomePage(_context);  // Navigation might fail!
}
```

**Rationale:**
- Tests control page object lifecycle
- Navigation destination may vary (error page, MFA page)
- Clearer test intent and flow

### 4.3 Page Load Detection

Pages define their own load indicators:

```csharp
public class HomePage : MauiPageObjectBase
{
    public MauiLabelControl WelcomeLabel => new(this, "WelcomeLabel");
    
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return WelcomeLabel.WaitExists(true, timeoutMs);
    }
}

public class DashboardPage : MauiPageObjectBase
{
    public MauiContainerControl DataGrid => new(this, "DataGrid");
    
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return DataGrid.WaitVisible(true, timeoutMs);
    }
}
```

### 4.4 Page is the Scope for Controls

Controls receive `this` (page) as their scope. The page implements `IElementScope<TElement>`:

```csharp
// Page is IElementScope - controls use it for element finding
public MauiEntryControl Username => new(this, "Username");
//                                      ^^^^ page IS scope

// Inside the control:
// _scope.TryFindElement(_locator) → page searches from driver root
```

This replaces the previous pattern where controls received both context and page reference.

---

## 5. Usage

### 5.1 Basic Test Flow (MAUI)

```csharp
[Fact]
public void Login_WithValidCredentials_NavigatesToHome()
{
    // Arrange - typed page with typed context
    var loginPage = new LoginPage(_context);  // _context is IMauiTestContext
    loginPage.IsLoaded();
    
    // Act - controls are typed (MauiEntryControl, MauiButtonControl)
    loginPage.UsernameEntry.Enter("testuser");
    loginPage.PasswordEntry.Enter("password123");
    loginPage.LoginButton.Click();
    
    // Assert
    var homePage = new HomePage(_context);
    homePage.IsLoaded();
    homePage.WelcomeLabel.AssertTextContains("Welcome");
}
```

### 5.2 Basic Test Flow (Blazor)

```csharp
[Fact]
public void Login_WithValidCredentials_NavigatesToHome()
{
    // Arrange - typed page with typed context
    var loginPage = new LoginPageBlazor(_context);  // _context is IBlazorTestContext
    loginPage.IsLoaded();
    
    // Act - controls are typed (BlazorEntryControl, BlazorButtonControl)
    loginPage.UsernameEntry.Enter("testuser");
    loginPage.PasswordEntry.Enter("password123");
    loginPage.LoginButton.Click();
    
    // Assert
    var homePage = new HomePageBlazor(_context);
    homePage.IsLoaded();
    homePage.WelcomeLabel.AssertTextContains("Welcome");
}
```

### 5.3 Using Page-Level Methods

```csharp
[Fact]
public void Login_WithInvalidCredentials_ShowsError()
{
    var loginPage = new LoginPage(_context);
    loginPage.IsLoaded();
    
    // Use page-level convenience method
    loginPage.EnterCredentials("invalid", "wrong");
    loginPage.LoginButton.Click();
    
    loginPage.ErrorLabel.AssertTextContains("Invalid credentials");
}
```

### 5.4 Multi-Page Flow

```csharp
[Fact]
public void UserJourney_LoginToSettings()
{
    // Login
    var loginPage = new LoginPage(_context);
    loginPage.EnterCredentials("user", "pass");
    loginPage.LoginButton.Click();
    
    // Home
    var homePage = new HomePage(_context);
    homePage.IsLoaded();
    homePage.SettingsButton.Click();
    
    // Settings
    var settingsPage = new SettingsPage(_context);
    settingsPage.IsLoaded();
    settingsPage.ThemeSelector.SelectByText("Dark");
}
```

---

## 6. Platform-Specific Navigation

Navigation is platform-specific and NOT part of `IPageObject`:

### 6.1 MAUI/Mobile Navigation

```csharp
// Test navigates by clicking controls
loginPage.LoginButton.Click();
var homePage = new HomePage(_context);

// Or use device back button
_context.Driver.Navigate().Back();
```

### 6.2 Blazor/Web Navigation

```csharp
// Direct URL navigation
_context.Driver.Navigate().GoToUrl("https://app.example.com/settings");
var settingsPage = new SettingsPage(_context);

// Or click navigation links
homePage.SettingsLink.Click();
var settingsPage = new SettingsPage(_context);
```

### 6.3 WPF/Desktop Navigation

```csharp
// Desktop apps typically navigate via menu/buttons
mainWindow.FileMenu.Click();
mainWindow.SettingsMenuItem.Click();
var settingsDialog = new SettingsDialog(_context);
```

---

## 7. Anti-Patterns

### 7.1 Don't Store Other Pages

```csharp
// ❌ BAD: Page holds reference to another page
public class LoginPage
{
    private HomePage _homePage;  // Wrong!
}

// ✅ GOOD: Tests create pages as needed
var homePage = new HomePage(_context);
```

### 7.2 Don't Return Pages from Actions

```csharp
// ❌ BAD: Assumes navigation succeeds
public HomePage Login(string user, string pass)
{
    UsernameEntry.Enter(user);
    PasswordEntry.Enter(pass);
    LoginButton.Click();
    return new HomePage(_context);
}

// ✅ GOOD: Separate action from page creation
public void ClickLogin()
{
    LoginButton.Click();
}
// Test creates: var homePage = new HomePage(_context);
```

### 7.3 Don't Hide Control Structure

```csharp
// ❌ BAD: Control details hidden in methods
public void SetUsername(string value) => UsernameEntry.Enter(value);
public void SetPassword(string value) => PasswordEntry.Enter(value);

// ✅ GOOD: Expose controls directly
public EntryControl UsernameEntry => new(...);
public EntryControl PasswordEntry => new(...);
```

### 7.4 Don't Mix Assertion Styles

```csharp
// ❌ BAD: Some assertions in page, some in test
public void VerifyLoginError(string message)
{
    ErrorLabel.AssertTextEquals(message);  // Assertion in page
}

// ✅ GOOD: All assertions in test
// Page provides: public LabelControl ErrorLabel => ...
// Test uses: loginPage.ErrorLabel.AssertTextEquals("Invalid");
```

---

## 8. Validation Rules

The Page Object pattern is valid when:

- [ ] Page implements `IPageObject<TElement>` (or platform interface like `IMauiPageObject`)
- [ ] Page extends appropriate base class (`MauiPageObjectBase`, `BlazorPageObjectBase`)
- [ ] Controls defined as expression-bodied properties
- [ ] Controls receive `this` (page) as scope, not context
- [ ] Controls use typed platform classes (`MauiButtonControl`, not `ButtonControl`)
- [ ] Navigation methods do NOT return other pages
- [ ] `IsLoaded()` uses control existence/visibility checks
- [ ] No page holds references to other pages
- [ ] Page-level operations are convenience methods only
- [ ] Platform-specific operations use typed `Context` property (no casting)

---

## Related Documents

- [231_001 Control Object Pattern](231_001_ControlObjectPattern.spx.md)
- [231_004 Container Pattern](231_004_ContainerPattern.spx.md)
- [211_004 PageContext](../211_Modules/211_004_PageContext.spx.md)
- [FR-101 Page Object](../../100_requirements/120_functional/120_101_PageObject.spx.md)
