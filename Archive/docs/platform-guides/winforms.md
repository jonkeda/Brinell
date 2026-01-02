# Brinell WinForms Testing Guide

This guide covers testing Windows Forms applications using the Brinell UI testing framework.

## Quick Start

### Installation

1. **Add Brinell.WinForms to your test project:**
   ```bash
   dotnet add package Brinell.WinForms
   ```

2. **Reference the framework in your test project:**
   ```xml
   <ItemGroup>
       <ProjectReference Include="path/to/Brinell.WinForms/Brinell.WinForms.csproj" />
   </ItemGroup>
   ```

3. **Set up AutomationIds in your WinForms application:**
   ```csharp
   var textBox = new TextBox();
   textBox.Name = "txtUsername";  // Used as the AutomationId for FlaUI
   ```

### Basic Test Example

```csharp
using Brinell.WinForms.Infrastructure;
using Brinell.WinForms.Controls;
using Xunit;

public class LoginTests
{
    [Fact]
    public void Login_WithValidCredentials_Succeeds()
    {
        // Arrange
        var driver = new FlaUIDriverAdapter(@"path\to\app.exe");
        var context = new FlaUITestContext(driver);
        var username = new TextBoxControl(context, null, "txtUsername");
        var password = new TextBoxControl(context, null, "txtPassword");
        var loginBtn = new ButtonControl(context, null, "btnLogin");

        // Act
        username.Enter("admin");
        password.Enter("password");
        loginBtn.Click();

        // Assert
        // Verify login succeeded
    }
}
```

## Architecture

### Core Components

#### FlaUITestContext
Manages test execution context and provides:
- Element finding and interaction
- Wait/Check/Assert patterns
- Screenshot capture on failure
- CSV logging for test results

```csharp
var context = new FlaUITestContext(driver);
context.TestName = "MyTest";
context.SetLogger(csvLogger);
```

#### Control Wrappers
Provide a fluent API for interacting with specific control types:

- **TextBoxControl** - Text input fields
- **ButtonControl** - Buttons and links
- **CheckBoxControl** - Checkboxes
- **ComboBoxControl** - Dropdowns and lists
- **ListBoxControl** - List controls
- **LabelControl** - Labels and static text
- **RadioButtonControl** - Radio buttons
- **DataGridViewControl** - Data grids

#### PageBase
Base class for page objects:

```csharp
public class LoginPage : PageBase
{
    private readonly TextBoxControl _username;
    private readonly ButtonControl _loginBtn;

    public LoginPage(FlaUITestContext context) : base(context, "LoginPage")
    {
        _username = new TextBoxControl(context, this, "txtUsername");
        _loginBtn = new ButtonControl(context, this, "btnLogin");
    }

    public override bool IsDisplayed() => _username.IsExists();
    
    public void Login(string username) => _username.Enter(username);
}
```

## Key Patterns

### Is/Wait/Check/Assert Pattern

The framework uses a consistent pattern for control interactions:

```csharp
// Is - Non-blocking check (returns bool)
bool visible = control.IsVisible();

// Wait - Poll with timeout (returns bool)
bool found = control.WaitVisible(timeoutMs: 5000);

// Check - Wait and throw on failure
control.CheckVisible(timeoutMs: 5000); // Throws if not visible

// Assert - Test assertion
control.AssertVisible("expected control to be visible");
```

### Page Object Model

Organize tests using page objects:

```csharp
public class LoginPage : PageBase
{
    // Encapsulate controls
    private readonly TextBoxControl _username;
    private readonly ButtonControl _submitBtn;

    // Provide business-readable methods
    public void EnterUsername(string username) => _username.Enter(username);
    public void Submit() => _submitBtn.Click();

    // Provide state assertions
    public bool IsLoginSuccessful() => _statusLabel.GetText().Contains("Success");
}

// Use in tests
var page = new LoginPage(context);
page.EnterUsername("admin");
page.Submit();
Assert.True(page.IsLoginSuccessful());
```

### Fluent Test Building

Chain operations for readable tests:

```csharp
page
    .EnterUsername("admin")
    .EnterPassword("password")
    .CheckRememberMe(true)
    .SelectRole("Admin")
    .Submit();
```

## Control Interactions

### TextBox

```csharp
control.Enter("text");              // Set text
control.Append(" more");            // Append to existing
control.Clear();                    // Clear all text
var text = control.GetText();       // Get current text
control.IsReadOnly();               // Check if read-only
```

### Button

```csharp
control.Click();                    // Single click
control.DoubleClick();              // Double click
control.RightClick();               // Right click (context menu)
control.AssertTextEquals("Click");  // Verify button text
```

### CheckBox

```csharp
control.Check();                    // Check the box
control.Uncheck();                  // Uncheck
control.SetChecked(true);           // Set to specific state
bool isChecked = control.IsChecked(); // Get current state
control.AssertChecked("expected to be checked");
```

### ComboBox

```csharp
control.SelectByText("Option 1");
control.SelectByIndex(0);
var selected = control.GetSelectedItem();
var items = control.GetItems();
control.AssertSelectedText("Option 1");
```

### ListBox

```csharp
control.SelectByText("Item 1");
var selected = control.GetSelectedItem();
var items = control.GetItems();
var count = control.GetItemCount();
```

### Label

```csharp
var text = control.GetText();
control.AssertTextEquals("Expected Text");
control.AssertTextContains("partial");
control.AssertTextStartsWith("Start");
control.AssertTextEndsWith("End");
```

## Wait Patterns

### Wait for Element to Exist

```csharp
var element = context.WaitForElementInternal("controlId", timeoutMs: 5000);
if (element != null)
{
    // Element found
}
```

### Wait for Visibility

```csharp
control.WaitVisible(expected: true, timeoutMs: 5000);
```

### Custom Wait Conditions

```csharp
context.WaitFor(
    () => labelControl.GetText().Contains("Ready"),
    timeoutMs: 10000,
    description: "Page loading"
);
```

## Assertions

### Control State Assertions

```csharp
control.AssertExists("button should exist");
control.AssertVisible("button should be visible");
control.AssertEnabled("button should be enabled");
control.AssertNotExists("error should not appear");
```

### Text Assertions

```csharp
control.AssertTextEquals("Login");
control.AssertTextContains("Success");
control.AssertTextStartsWith("Welcome");
control.AssertTextEndsWith("User");
control.AssertTextEmpty();
control.AssertTextNotEmpty();
```

### Custom Assertions

```csharp
var status = statusLabel.GetText();
Assert.Contains("Success", status);
```

## Advanced Usage

### Handling Dynamic Controls

For controls in list items or grids:

```csharp
// Pass container element to search within
var container = gridRow; // AutomationElement of the row
var control = new TextBoxControl(context, page, container, "cellText");
```

### Screenshot Capture

```csharp
var context = new FlaUITestContext(driver);
var path = context.CaptureFailureScreenshot("form-not-displayed");
```

### CSV Test Logging

```csharp
var logger = new CsvTestLogger("test-results.csv");
context.SetLogger(logger);

// All test interactions are logged to CSV
```

### Application Lifecycle

```csharp
// Launch application
var driver = new FlaUIDriverAdapter(@"C:\App\MyApp.exe");

// Or attach to running application
var app = Application.Attach("MyApp");
var driver = new FlaUIDriverAdapter(app);

// Cleanup
driver.Dispose();
```

## Troubleshooting

### Element Not Found

**Problem:** Tests fail with "Element not found"

**Solutions:**
1. Verify AutomationId matches between form and test
2. Ensure element is visible (not in hidden container)
3. Wait for element instead of immediate access
4. Check element hierarchy

### Tests Timeout

**Problem:** Tests hang waiting for elements

**Solutions:**
1. Increase timeout value if app is slow
2. Check that element actually exists
3. Verify wait condition is achievable
4. Look for dialogs blocking the app

### Element Not Interactable

**Problem:** Click/Enter operations fail silently

**Solutions:**
1. Ensure element is enabled
2. Wait for visibility before interaction
3. Check for overlaying dialogs
4. Try scrolling element into view

### Stale Element References

**Problem:** "Element no longer valid" errors

**Solutions:**
1. Re-find elements after navigation
2. Don't cache AutomationElement references
3. Always use PageObject pattern
4. Avoid storing element references between test steps

## Best Practices

### Use Page Objects
Always wrap UI interactions in page objects:

```csharp
// ❌ Don't: Direct control usage in tests
var control = new TextBoxControl(context, null, "username");
control.Enter("admin");

// ✅ Do: Use page objects
var page = new LoginPage(context);
page.EnterUsername("admin");
```

### Meaningful AutomationIds
Use descriptive, stable IDs:

```csharp
// ❌ Bad
textBox.Name = "txt1";

// ✅ Good
textBox.Name = "txtUsername";
textBox.Name = "txtEmailAddress";
```

### Explicit Waits, Not Thread.Sleep
Always prefer wait operations:

```csharp
// ❌ Bad
Thread.Sleep(2000);
control.Click();

// ✅ Good
control.WaitVisible();
control.Click();
```

### Single Responsibility
Each test should verify one behavior:

```csharp
// ❌ Bad - Tests multiple things
public void LoginFormWorks()
{
    page.EnterUsername("admin");
    page.EnterPassword("pass");
    page.Submit();
    Assert.True(page.IsLoginSuccessful());
    Assert.True(page.HasRememberMeOption());
    // ...
}

// ✅ Good - Tests one thing
public void Login_WithValidCredentials_Succeeds()
{
    page.EnterUsername("admin");
    page.EnterPassword("password");
    page.Submit();
    Assert.True(page.IsLoginSuccessful());
}
```

### Clear Arrange-Act-Assert

```csharp
[Fact]
public void FormSubmit_WithValidData_UpdatesStatus()
{
    // Arrange
    var page = new LoginPage(context);
    var expectedStatus = "Login successful";

    // Act
    page.EnterUsername("admin");
    page.Submit();

    // Assert
    page.AssertStatusMessage(expectedStatus);
}
```

### Error Handling and Cleanup

```csharp
public class LoginTests : IAsyncLifetime
{
    private FlaUIDriverAdapter _driver;
    private FlaUITestContext _context;

    public async Task InitializeAsync()
    {
        _driver = new FlaUIDriverAdapter(@"path\to\app.exe");
        _context = new FlaUITestContext(_driver);
    }

    public async Task DisposeAsync()
    {
        _driver?.Dispose(); // Always cleanup
    }
}
```

## Sample Application

See the `/samples` directory for a complete example including:
- Sample WinForms application (Brinell.Samples.WinForms.App)
- Page objects (LoginPage)
- Test suites (LoginPageTests, AdvancedLoginTests)

Run the sample:
```bash
cd samples
dotnet run --project Brinell.Samples.WinForms.App
```

## See Also

- [Brinell Framework Overview](../02-framework-overview.md)
- [Test Writing Guide](../15-test-writing-guide.md)
- [Best Practices](../12-best-practices.md)
- [Troubleshooting](../13-troubleshooting.md)
- [FlaUI Documentation](https://github.com/Roemer/FlaUI)
