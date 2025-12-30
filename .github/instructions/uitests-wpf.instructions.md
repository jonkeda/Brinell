---
applyTo: "**/UITests/**/*.cs"
description: "Brinell WPF UI testing framework guidelines"
---

# Brinell WPF UI Testing Guidelines

## Framework Overview
- Use Brinell.Wpf with FlaUI for WPF automation
- Base class for tests: `WpfUITestBase`
- Base class for page objects: `PageBase`
- Test context: `FlaUITestContext`

## Page Object Structure
```csharp
using Brinell.Wpf.Controls;
using Brinell.Wpf.Controls.Base;
using Brinell.Wpf.Infrastructure;

public class LoginPage : PageBase
{
    // Controls - initialized in constructor using control classes
    public ButtonControl LoginButton { get; }
    public ButtonControl CancelButton { get; }
    public TextBoxControl UsernameTextBox { get; }
    public TextBoxControl PasswordTextBox { get; }
    public LabelControl ErrorLabel { get; }
    
    public LoginPage(FlaUITestContext context) 
        : base(context, "LoginView")  // AutomationId of root element
    {
        LoginButton = new ButtonControl(context, this, "btnLogin");
        CancelButton = new ButtonControl(context, this, "btnCancel");
        UsernameTextBox = new TextBoxControl(context, this, "txtUsername");
        PasswordTextBox = new TextBoxControl(context, this, "txtPassword");
        ErrorLabel = new LabelControl(context, this, "lblError");
    }
    
    public override bool IsDisplayed()
    {
        return _context.ElementIsVisible(AutomationId);
    }
    
    // Workflow methods (multi-step operations)
    public LoginPage EnterCredentials(string user, string pass)
    {
        Log($"EnterCredentials({user}, ***)");
        UsernameTextBox.SetText(user);
        PasswordTextBox.SetText(pass);
        return this;
    }
    
    public MainPage SubmitLogin()
    {
        Log("SubmitLogin()");
        LoginButton.Click();
        var mainPage = new MainPage(_context);
        mainPage.WaitForDisplayed();
        return mainPage;
    }
}
```

## Test Class Structure
```csharp
using Brinell.Wpf.Testing;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

[Collection("WpfUITests")]
public class LoginTests : WpfUITestBase
{
    public LoginTests(ITestOutputHelper output) : base(output.WriteLine)
    {
    }

    protected override string ApplicationPath => GetAppPath();
    
    private static string GetAppPath()
    {
        // Navigate to the application executable
        var testDir = AppContext.BaseDirectory;
        return Path.Combine(testDir, "..", "..", "..", "..", 
            "MyApp", "bin", "Debug", "net9.0-windows", "MyApp.exe");
    }

    [Fact]
    public void Login_WithValidCredentials_NavigatesToMain()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context!);
        shell.WaitForDisplayed();
        
        // Act
        var loginPage = shell.NavigateToLogin();
        var mainPage = loginPage
            .EnterCredentials("admin", "password123")
            .SubmitLogin();
        
        // Assert
        mainPage.AssertDisplayed("Main page should be displayed");
    }
}
```

## WPF Control Types
- `ButtonControl` - Buttons
- `TextBoxControl` - Text inputs (TextBox)
- `LabelControl` - Labels/TextBlocks
- `ComboBoxControl` - ComboBoxes/Dropdowns
- `CheckBoxControl` - CheckBoxes
- `ListBoxControl` - ListBoxes
- `DataGridControl` - DataGrids
- `MenuControl` - Menus
- `TreeViewControl` - TreeViews
- `TabControl` - TabControls

## Control Methods
- `Click()` - Click the control
- `SetText(string)` - Set text (TextBox)
- `GetText()` - Get text value
- `IsVisible()` - Check if control is visible
- `IsEnabled()` - Check if control is enabled
- `AssertVisible(string message)` - Assert control is visible
- `AssertText(string expected)` - Assert text matches
- `WaitForVisible()` - Wait for element to be visible
- `WaitForEnabled()` - Wait for element to be enabled

## AutomationProperties Setup
In your WPF XAML, set AutomationProperties for testability:
```xml
<Button x:Name="LoginButton" 
        AutomationProperties.AutomationId="btnLogin"
        Content="Login" />
<TextBox AutomationProperties.AutomationId="txtUsername" />
```

## Navigation Pattern
```csharp
// Return new page object after navigation
public SettingsPage NavigateToSettings()
{
    Log("NavigateToSettings()");
    SettingsButton.Click();
    var page = new SettingsPage(_context);
    page.WaitForDisplayed();
    return page;
}
```

## Best Practices
- Controls are instantiated in constructor, not as properties with factory methods
- Use `Log()` method to record actions for debugging
- Return page objects from navigation methods (fluent pattern)
- Keep tests focused on single behaviors
- Tests should be independent and not rely on order
- Use test collections to prevent parallel execution issues
- Always set AutomationProperties.AutomationId on elements you want to test
