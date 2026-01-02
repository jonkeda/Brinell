# 21I. UI Test Writing Instructions (AI Agent Reference)

**Purpose:** Condensed instructions for AI agents to correctly write WPF UI Tests using the Oravey UITestFramework.

---

## 1. Core Concepts

### Test Structure
```
Test Project (Oravey.*.UITests)
├── PageObjects/           # Page Object classes
│   ├── MainWindowPage.cs
│   ├── LoginPage.cs
│   └── SettingsPage.cs
├── Tests/                 # Test classes
│   ├── LoginTests.cs
│   └── NavigationTests.cs
└── appsettings.json       # App path, timeouts
```

### Key Classes
| Class | Purpose |
|-------|---------|
| `WpfUITestBase` | Base class for all UI tests |
| `WpfPageBase` | Base class for page objects |
| `TestContext` | Shared state: Logger, TestName, App reference |

---

## 2. Page Object Pattern

### Page Object Template
```csharp
public class LoginPage : WpfPageBase
{
    public override string PageName => "LoginPage";
    public override string PageId => "LoginWindow";  // AutomationId of root element
    
    // Controls - use AutomationId from XAML
    public TextBox Username => TextBox("txtUsername");
    public TextBox Password => TextBox("txtPassword");
    public Button LoginButton => Button("btnLogin");
    public Label ErrorMessage => Label("lblError");
    
    public LoginPage(TestContext context) : base(context) { }
    
    // Page actions (fluent pattern)
    public LoginPage EnterCredentials(string user, string pass)
    {
        Username.EnterText(user);
        Password.EnterText(pass);
        return this;
    }
    
    public MainPage SubmitLogin()
    {
        LoginButton.Click();
        return NavigateTo<MainPage>();
    }
}
```

### Control Factory Methods (in WpfPageBase)
```csharp
protected Button Button(string automationId) => new(Context, this, automationId);
protected TextBox TextBox(string automationId) => new(Context, this, automationId);
protected Label Label(string automationId) => new(Context, this, automationId);
protected ComboBox ComboBox(string automationId) => new(Context, this, automationId);
protected CheckBox CheckBox(string automationId) => new(Context, this, automationId);
protected ListBox ListBox(string automationId) => new(Context, this, automationId);
protected DataGrid DataGrid(string automationId) => new(Context, this, automationId);
protected Menu Menu(string automationId) => new(Context, this, automationId);
protected TreeView TreeView(string automationId) => new(Context, this, automationId);
protected TabControl TabControl(string automationId) => new(Context, this, automationId);
```

---

## 3. Control Actions

### Common Control Methods
| Control | Key Methods |
|---------|-------------|
| **All** | `AssertVisible()`, `AssertNotVisible()`, `WaitUntilVisible()`, `WaitUntilReady()` |
| **Button** | `Click()`, `AssertEnabled()`, `AssertDisabled()` |
| **TextBox** | `EnterText(string)`, `Clear()`, `AssertText(string)`, `GetText()` |
| **Label** | `AssertText(string)`, `GetText()` |
| **CheckBox** | `Check()`, `Uncheck()`, `Toggle()`, `AssertChecked()`, `AssertUnchecked()` |
| **ComboBox** | `SelectItem(string)`, `SelectIndex(int)`, `AssertSelectedItem(string)` |
| **ListBox** | `SelectItem(string)`, `SelectIndex(int)`, `AssertItemExists(string)` |
| **DataGrid** | `SelectRow(int)`, `GetCell(int row, int col)`, `AssertRowCount(int)` |
| **Menu** | `ClickItem(string path)` e.g., `Menu.ClickItem("File|Save")` |
| **TabControl** | `SelectTab(string)`, `SelectTab(int)` |

### Action Sequence Rules
1. **Check before act**: Controls auto-check visibility/enabled before actions
2. **Log after success**: Actions log to CSV only after succeeding
3. **Throw on failure**: Failed checks throw with logged context

---

## 4. Test Class Template

```csharp
public class LoginTests : WpfUITestBase
{
    [Fact]
    public void Login_WithValidCredentials_NavigatesToMain()
    {
        // Arrange
        var loginPage = LaunchApp<LoginPage>();
        
        // Act
        var mainPage = loginPage
            .EnterCredentials("admin", "password123")
            .SubmitLogin();
        
        // Assert
        mainPage.AssertVisible();
        mainPage.WelcomeLabel.AssertText("Welcome, admin!");
    }
    
    [Fact]
    public void Login_WithInvalidCredentials_ShowsError()
    {
        var loginPage = LaunchApp<LoginPage>();
        
        loginPage
            .EnterCredentials("wrong", "wrong")
            .LoginButton.Click();
        
        loginPage.ErrorMessage.AssertVisible();
        loginPage.ErrorMessage.AssertText("Invalid credentials");
    }
    
    [Theory]
    [InlineData("", "pass", "Username required")]
    [InlineData("user", "", "Password required")]
    public void Login_MissingField_ShowsValidation(string user, string pass, string error)
    {
        var loginPage = LaunchApp<LoginPage>();
        
        loginPage.EnterCredentials(user, pass);
        loginPage.LoginButton.Click();
        
        loginPage.ErrorMessage.AssertText(error);
    }
}
```

---

## 5. Navigation Patterns

### Page-to-Page Navigation
```csharp
// In page object - returns new page type
public SettingsPage OpenSettings()
{
    SettingsButton.Click();
    return NavigateTo<SettingsPage>();
}

// NavigateTo<T>() waits for target page to be visible
```

### Dialog Handling
```csharp
// In page object
public ConfirmDialog ClickDelete()
{
    DeleteButton.Click();
    return OpenDialog<ConfirmDialog>();
}

// In dialog
public class ConfirmDialog : WpfDialogBase
{
    public Button YesButton => Button("btnYes");
    public Button NoButton => Button("btnNo");
    
    public void Confirm() => YesButton.Click();
    public void Cancel() => NoButton.Click();
}
```

### Menu Navigation
```csharp
// Use pipe-delimited path
MainMenu.ClickItem("File|Export|CSV");
MainMenu.ClickItem("Edit|Preferences");
```

---

## 6. Wait Strategies

### Implicit Waits (built into controls)
```csharp
// All actions wait for element visibility by default
button.Click();  // Waits for visible + enabled
textBox.EnterText("value");  // Waits for visible + enabled
```

### Explicit Waits
```csharp
// Wait for specific conditions
element.WaitUntilVisible(timeoutMs: 5000);
element.WaitUntilReady(timeoutMs: 10000);
element.WaitUntilText("Expected", timeoutMs: 3000);

// Page-level waits
page.WaitUntilLoaded();
page.WaitForBusyIndicator();
```

### Polling Pattern
```csharp
// For complex conditions
Wait.Until(() => dataGrid.GetRowCount() > 0, timeoutMs: 10000);
Wait.Until(() => statusLabel.GetText() == "Complete", timeoutMs: 30000);
```

---

## 7. Assertions

### Control Assertions
```csharp
// Visibility
control.AssertVisible();
control.AssertNotVisible();

// State
button.AssertEnabled();
button.AssertDisabled();
checkBox.AssertChecked();
checkBox.AssertUnchecked();

// Content
label.AssertText("Expected Text");
textBox.AssertText("Expected Value");
comboBox.AssertSelectedItem("Option 1");
```

### Page Assertions
```csharp
page.AssertVisible();         // Page root element visible
page.AssertTitle("My Page");  // Window title
```

### Collection Assertions
```csharp
listBox.AssertItemCount(5);
listBox.AssertItemExists("Item Name");
dataGrid.AssertRowCount(10);
```

---

## 8. Logging

### Environment Variables (for AI agent runs)
```powershell
$env:UITEST_LOG_OUTPUT = "both"      # csv, console, both
$env:UITEST_CONSOLE_FORMAT = "formatted"  # formatted, csv
```

### Log Output Format
```
Timestamp;TestName;PageName;ControlId;Action;Value;ExpectedValue;Result;Message
```

### LogResult Values
| Result | Meaning |
|--------|---------|
| `Ok` | Action succeeded |
| `Fail` | Assertion/check failed |
| `Error` | Exception occurred |
| `Info` | Informational message |
| `Skip` | Action skipped |

### Console Output (formatted mode)
```
[10:15:01.123] LoginPage.txtUsername    Enter              admin        Ok
[10:15:01.456] LoginPage.txtPassword    Enter              ****         Ok
[10:15:01.789] LoginPage.btnLogin       Click                           Ok
[10:15:02.234] MainPage                 Wait.Ready                      Ok    elapsed=445ms
```

---

## 9. Best Practices

### DO ✅
- Use descriptive page and control names matching XAML AutomationIds
- Return page objects from navigation methods (fluent pattern)
- Keep tests focused on single behaviors
- Use page actions to encapsulate complex interactions
- Wait for state changes after triggering async operations
- Use `[Theory]` with `[InlineData]` for data-driven tests

### DON'T ❌
- Access UI elements directly in tests (use page objects)
- Hard-code timeouts (use config or defaults)
- Assert multiple unrelated things in one test
- Ignore test isolation (each test starts fresh)
- Use `Thread.Sleep()` (use explicit waits)
- Put business logic in page objects

### Naming Conventions
```csharp
// Test methods: Action_Scenario_ExpectedResult
public void Login_WithValidCredentials_NavigatesToMain()
public void Save_WithInvalidData_ShowsValidationErrors()
public void Delete_WhenConfirmed_RemovesItem()

// Page objects: {Feature}Page
public class LoginPage, MainWindowPage, SettingsPage

// Controls: Match XAML AutomationId
x:Name="txtUsername" → TextBox("txtUsername")
x:Name="btnSubmit" → Button("btnSubmit")
```

---

## 10. Project Setup

### Required References
```xml
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<ProjectReference Include="..\UITestFramework\Oravey.UITestFramework.Wpf.csproj" />
```

### appsettings.json
```json
{
  "UITest": {
    "ApplicationPath": "..\\..\\..\\MyApp\\bin\\Debug\\net8.0-windows\\MyApp.exe",
    "DefaultTimeoutMs": 5000,
    "WaitTimeoutMs": 30000,
    "LogOutputPath": "logs"
  }
}
```

### Running Tests
```powershell
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~LoginTests"

# Run with console logging for AI
$env:UITEST_LOG_OUTPUT = "both"
dotnet test --filter "TestName"
```

---

## 11. Quick Reference Card

```
┌─────────────────────────────────────────────────────────────────┐
│ CONTROL ACTIONS                                                 │
├─────────────────────────────────────────────────────────────────┤
│ button.Click()              textBox.EnterText("value")          │
│ checkBox.Check()            comboBox.SelectItem("name")         │
│ listBox.SelectIndex(0)      menu.ClickItem("File|Save")         │
│ tab.SelectTab("Settings")   dataGrid.SelectRow(0)               │
├─────────────────────────────────────────────────────────────────┤
│ ASSERTIONS                                                      │
├─────────────────────────────────────────────────────────────────┤
│ .AssertVisible()            .AssertNotVisible()                 │
│ .AssertEnabled()            .AssertDisabled()                   │
│ .AssertText("expected")     .AssertChecked()                    │
│ .AssertSelectedItem("x")    .AssertItemCount(n)                 │
├─────────────────────────────────────────────────────────────────┤
│ WAITS                                                           │
├─────────────────────────────────────────────────────────────────┤
│ .WaitUntilVisible()         .WaitUntilReady()                   │
│ .WaitUntilText("x")         Wait.Until(() => condition)         │
├─────────────────────────────────────────────────────────────────┤
│ NAVIGATION                                                      │
├─────────────────────────────────────────────────────────────────┤
│ NavigateTo<TargetPage>()    OpenDialog<DialogType>()            │
│ LaunchApp<StartPage>()      CloseApp()                          │
└─────────────────────────────────────────────────────────────────┘
```

---

*Related: [Standardized Logging](21d12_StandardizedLogging.md) | [Page Object Architecture](21d11_PageObjectArchitecture.md) | [Application UITest Projects](21d13_ApplicationUITestProjects.md)*
