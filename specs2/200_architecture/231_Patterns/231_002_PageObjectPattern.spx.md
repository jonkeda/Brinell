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
    /// Capture screenshot of current page state.
    /// </summary>
    byte[] TakeScreenshot();
}
```

### 3.2 Page Base Class

```csharp
public abstract class PageObjectBase : IPageObject
{
    protected readonly ITestContext _context;
    
    public abstract string Name { get; }
    public virtual LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    protected PageObjectBase(ITestContext context, string name)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
    /// <summary>
    /// Convenience method to create locator using page's default strategy.
    /// </summary>
    protected Locator Locate(string value) => new(DefaultLocatorStrategy, value);
    
    /// <summary>
    /// Wait for page to be ready. Override in derived classes.
    /// </summary>
    public virtual void WaitForPage(int? timeoutMs = null)
    {
        // Default: no wait. Override to wait for specific element.
    }
    
    public byte[] TakeScreenshot() => _context.TakeScreenshot();
}
```

### 3.3 Concrete Page Example

```csharp
public class LoginPage : PageObjectBase
{
    public override string Name => "LoginPage";
    
    // Controls defined as properties using new pattern
    public EntryControl UsernameEntry => new(_context, "UsernameEntry", this);
    public EntryControl PasswordEntry => new(_context, "PasswordEntry", this);
    public ButtonControl LoginButton => new(_context, "LoginButton", this);
    public ButtonControl ForgotPasswordLink => new(_context, "ForgotPasswordLink", this);
    public LabelControl ErrorLabel => new(_context, "ErrorLabel", this);
    
    public LoginPage(ITestContext context) : base(context, "LoginPage")
    {
    }
    
    /// <summary>
    /// Wait for page to be ready by checking for login button.
    /// </summary>
    public override void WaitForPage(int? timeoutMs = null)
    {
        LoginButton.WaitExists(true, timeoutMs);
    }
    
    /// <summary>
    /// Page-level convenience method for entering credentials.
    /// </summary>
    public void EnterCredentials(string? username, string? password)
    {
        UsernameEntry.Enter(username);
        PasswordEntry.Enter(password);
    }
}
```

---

## 4. Key Principles

### 4.1 Controls as Properties

Define controls as expression-bodied properties:

```csharp
// ✅ GOOD: Declarative, visible structure
public EntryControl UsernameEntry => new(_context, "UsernameEntry", this);
public ButtonControl LoginButton => new(_context, "LoginButton", this);

// ❌ BAD: Factory methods hide structure
public EntryControl GetUsernameEntry() => new(_context, "UsernameEntry", this);
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
public class HomePage : PageObjectBase
{
    public LabelControl WelcomeLabel => new(_context, "WelcomeLabel", this);
    
    public override void WaitForPage(int? timeoutMs = null)
    {
        WelcomeLabel.WaitExists(true, timeoutMs);
    }
}

public class DashboardPage : PageObjectBase
{
    public ContainerControl DataGrid => new(_context, "DataGrid", this);
    
    public override void WaitForPage(int? timeoutMs = null)
    {
        DataGrid.WaitVisible(true, timeoutMs);
    }
}
```

### 4.4 Pass Page Reference to Controls

Controls receive `this` to enable:
- Scoped logging (page name in logs)
- Default locator strategy from page
- Container scoping

```csharp
// Page reference enables logging context
public EntryControl Username => new(_context, "Username", this);
//                                                        ^^^^
```

---

## 5. Usage

### 5.1 Basic Test Flow

```csharp
[Fact]
public void Login_WithValidCredentials_NavigatesToHome()
{
    // Arrange
    var loginPage = new LoginPage(_context);
    loginPage.WaitForPage();
    
    // Act
    loginPage.UsernameEntry.Enter("testuser");
    loginPage.PasswordEntry.Enter("password123");
    loginPage.LoginButton.Click();
    
    // Assert
    var homePage = new HomePage(_context);
    homePage.WaitForPage();
    homePage.WelcomeLabel.AssertTextContains("Welcome");
}
```

### 5.2 Using Page-Level Methods

```csharp
[Fact]
public void Login_WithInvalidCredentials_ShowsError()
{
    var loginPage = new LoginPage(_context);
    loginPage.WaitForPage();
    
    // Use page-level convenience method
    loginPage.EnterCredentials("invalid", "wrong");
    loginPage.LoginButton.Click();
    
    loginPage.ErrorLabel.AssertTextContains("Invalid credentials");
}
```

### 5.3 Multi-Page Flow

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
    homePage.WaitForPage();
    homePage.SettingsButton.Click();
    
    // Settings
    var settingsPage = new SettingsPage(_context);
    settingsPage.WaitForPage();
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

- [ ] Page implements IPageObject interface
- [ ] Controls defined as expression-bodied properties
- [ ] Controls receive page reference (`this`)
- [ ] Navigation methods do NOT return other pages
- [ ] WaitForPage() uses control existence checks
- [ ] No page holds references to other pages
- [ ] Page-level operations are convenience methods only

---

## Related Documents

- [231_001 Control Object Pattern](231_001_ControlObjectPattern.spx.md)
- [231_004 Container Pattern](231_004_ContainerPattern.spx.md)
- [211_004 PageContext](../211_Modules/211_004_PageContext.spx.md)
- [FR-101 Page Object](../../100_requirements/120_functional/120_101_PageObject.spx.md)
