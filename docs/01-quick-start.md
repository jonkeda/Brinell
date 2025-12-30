# Quick Start Guide

Get up and running with the UI Test Framework in 5 minutes.

---

## Installation

### 1. Add Package References

```xml
<PackageReference Include="xunit" Version="2.9.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.9.*" />

<!-- Choose your platform -->
<ProjectReference Include="..\..\UITestFramework\Oravey.UITestFramework.Wpf.csproj" />
<!-- OR -->
<ProjectReference Include="..\..\UITestFramework\Oravey.UITestFramework.Maui.csproj" />
<!-- OR -->
<ProjectReference Include="..\..\UITestFramework\Oravey.UITestFramework.Html.csproj" />
```

### 2. Create Configuration File

Create `appsettings.json` in your test project:

```json
{
  "UITest": {
    "Platform": "Windows",
    "ApplicationPath": "path/to/YourApp.exe",
    "DefaultTimeoutMs": 10000,
    "LogOutputPath": "logs"
  }
}
```

---

## Your First Test (WPF Example)

### Step 1: Create a Page Object

```csharp
using Oravey.UITestFramework.Wpf;

public class MainWindowPage : BusyPageBase
{
    protected override string BusyIndicatorId => "MainBusyIndicator";
    
    public ButtonControl SettingsButton { get; }
    public LabelControl WelcomeLabel { get; }
    
    public MainWindowPage(FlaUITestContext context) 
        : base(context, "MainWindow")
    {
        SettingsButton = new ButtonControl(context, this, "SettingsButton");
        WelcomeLabel = new LabelControl(context, this, "WelcomeLabel");
    }
    
    public void NavigateToSettings()
    {
        SettingsButton.Click();
    }
}
```

### Step 2: Create a Test Class

```csharp
using Xunit;
using Oravey.UITestFramework.Wpf.Testing;

public class MainWindowTests : WpfUITestBase
{
    [Fact]
    public void MainWindow_Displays_Welcome_Message()
    {
        // Arrange
        var mainPage = LaunchApp<MainWindowPage>();
        mainPage.WaitForPageReady();
        
        // Assert
        mainPage.WelcomeLabel.AssertText("Welcome!");
    }
    
    [Fact]
    public void Settings_Button_Navigates_To_Settings()
    {
        // Arrange
        var mainPage = LaunchApp<MainWindowPage>();
        mainPage.WaitForPageReady();
        
        // Act
        mainPage.NavigateToSettings();
        
        var settingsPage = new SettingsPage(Context);
        settingsPage.WaitForPageReady();
        
        // Assert
        settingsPage.AssertDisplayed();
    }
}
```

### Step 3: Run Tests

```bash
dotnet test
```

---

## Key Concepts in 60 Seconds

### 1. Page Objects Encapsulate Structure

```csharp
public class LoginPage : PageBase
{
    public TextBoxControl Username { get; }
    public TextBoxControl Password { get; }
    public ButtonControl LoginButton { get; }
    
    public void Login(string user, string pass)
    {
        Username.EnterText(user);
        Password.EnterText(pass);
        LoginButton.Click();
    }
}
```

### 2. Controls Have Built-In Waits

```csharp
// No need for manual waits - controls wait automatically
button.Click();  // Waits for visible + enabled
textBox.EnterText("value");  // Waits for enabled
```

### 3. Four-Tier State Verification

```csharp
// Is* - Immediate check
if (button.IsVisible()) { }

// Wait* - Poll until condition
button.WaitVisible(true);

// Check* - Wait + throw on failure
button.CheckClickable();

// Assert* - Semantic assertion with logging
button.AssertText("Expected");
```

### 4. IsBusy Tracking for Page Readiness

```csharp
// Navigate and wait for page ready
shell.NavigateToSettings();
var settings = new SettingsPage(Context);
settings.WaitForPageReady();  // Waits for IsBusy = false

// Now safe to interact
settings.SaveButton.Click();
```

---

## Next Steps

- **[Framework Overview](02-framework-overview.md)** - Understand the architecture
- **[Control Objects](04-control-objects.md)** - Learn about control patterns
- **[Page Objects](05-page-objects.md)** - Master page encapsulation
- **[Test Writing Guide](15-test-writing-guide.md)** - Quick reference for common patterns

---

## Common Patterns

### Navigation Pattern

```csharp
// In page object
public void NavigateToSettings()
{
    SettingsButton.Click();
}

// In test
homePage.NavigateToSettings();
var settingsPage = new SettingsPage(Context);
settingsPage.WaitForPageReady();
```

### Assertion Pattern

```csharp
// Visibility assertions
element.AssertVisible();
element.AssertNotVisible();

// State assertions
button.AssertEnabled();
checkbox.AssertChecked();

// Value assertions
label.AssertText("Expected Text");
textBox.AssertText("Expected Value");
```

### Waiting Pattern

```csharp
// Wait for element state
element.WaitVisible(true);
element.WaitEnabled(true);

// Wait for specific value
element.WaitText("Expected");

// Wait for page ready
page.WaitForPageReady();
```

---

*Next: [Framework Overview](02-framework-overview.md)*
