---
applyTo: "**/UITests/**/*.cs"
description: "Brinell MAUI UI testing framework guidelines"
---

# Brinell MAUI UI Testing Guidelines

## Framework Overview
- Use Brinell.Maui with Appium for mobile/desktop automation
- Base class for tests: `MauiUITestBase`
- Base class for page objects: `PageBase`
- Test context: `AppiumTestContext`
- Supports Android, iOS, Windows, and macOS platforms

## Page Object Structure
```csharp
using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

public class LoginPage : PageBase
{
    // Controls - initialized in constructor using control classes
    public ButtonControl LoginButton { get; }
    public EntryControl EmailEntry { get; }
    public EntryControl PasswordEntry { get; }
    public LabelControl ErrorLabel { get; }
    
    public LoginPage(AppiumTestContext context) 
        : base(context, "LoginPage")  // AutomationId of root element
    {
        LoginButton = new ButtonControl(context, this, "LoginButton");
        EmailEntry = new EntryControl(context, this, "EmailEntry");
        PasswordEntry = new EntryControl(context, this, "PasswordEntry");
        ErrorLabel = new LabelControl(context, this, "ErrorLabel");
    }
    
    public override bool IsDisplayed()
    {
        return _context.ElementIsVisible(AutomationId);
    }
    
    // Workflow methods
    public LoginPage EnterCredentials(string email, string password)
    {
        Log($"EnterCredentials({email}, ***)");
        EmailEntry.SetText(email);
        PasswordEntry.SetText(password);
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
using Brinell.Maui.Testing;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

[Collection("MauiUITests")]
public class LoginTests : MauiUITestBase
{
    public LoginTests(ITestOutputHelper output) : base(output.WriteLine)
    {
    }

    protected override AppiumOptions GetAppiumOptions()
    {
        var options = new AppiumOptions();
        
        // Windows configuration
        options.PlatformName = "Windows";
        options.AutomationName = "Windows";
        options.App = GetAppPath();
        
        return options;
    }
    
    protected override Uri AppiumServerUri => new("http://127.0.0.1:4723");

    [Fact]
    public void Login_WithValidCredentials_NavigatesToMain()
    {
        // Arrange
        LaunchApplication();
        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();
        
        // Act
        var mainPage = loginPage
            .EnterCredentials("test@example.com", "password123")
            .SubmitLogin();
        
        // Assert
        mainPage.AssertDisplayed("Main page should be displayed after login");
    }
}
```

## MAUI Control Types
- `ButtonControl` - Buttons
- `EntryControl` - Entry (text input)
- `LabelControl` - Labels
- `EditorControl` - Editor (multi-line text)
- `CheckBoxControl` - CheckBoxes
- `SwitchControl` - Switches/Toggles
- `PickerControl` - Pickers/Dropdowns
- `SliderControl` - Sliders
- `CollectionViewControl` - CollectionViews
- `ListViewControl` - ListViews

## Control Methods
- `Click()` / `Tap()` - Tap the control
- `DoubleTap()` - Double tap the control
- `LongPress(durationMs)` - Long press (default 1000ms)
- `SwipeLeft(distance)` / `SwipeRight(distance)` - Swipe horizontally
- `SwipeUp(distance)` / `SwipeDown(distance)` - Swipe vertically
- `SetText(string)` - Set text value
- `Clear()` - Clear text content
- `GetText()` - Get text value
- `IsVisible()` - Check if control is visible
- `IsEnabled()` - Check if control is enabled

## Control Assertions
Use control-level assertions instead of xUnit Assert methods:

### Common Assertions (All Controls)
```csharp
// Visibility and state
control.AssertVisible();
control.AssertNotVisible();
control.AssertEnabled();
control.AssertDisabled();
control.AssertExists();
control.AssertNotExists();

// Text assertions
control.AssertTextEquals("Expected Text");
control.AssertTextContains("partial");
control.AssertTextStartsWith("prefix");
control.AssertTextEndsWith("suffix");
control.AssertTextEmpty();
control.AssertTextNotEmpty();
```

### Toggle Controls (CheckBox, Switch)
```csharp
checkBox.AssertChecked();
checkBox.AssertUnchecked();

// Switch-specific aliases
toggleSwitch.AssertIsOn();
toggleSwitch.AssertIsOff();
```

### Range Controls (Slider, ProgressBar)
```csharp
slider.AssertValue(50.0);
slider.AssertValueInRange(0, 100);
slider.AssertPercentage(50.0);
slider.AssertAtMinimum();
slider.AssertAtMaximum();

// ProgressBar-specific
progressBar.AssertComplete();
progressBar.AssertNotComplete();
progressBar.AssertProgressAtLeast(75);
```

### Selector Controls (Picker)
```csharp
picker.AssertSelectedText("Option 1");
picker.AssertSelectedTextContains("Option");
picker.AssertSelectedIndex(0);
picker.AssertItemCount(5);
picker.AssertNoSelection();
```

### Text Input Controls (Entry, Editor)
```csharp
entry.AssertIsReadOnly();
entry.AssertIsNotReadOnly();
entry.AssertPlaceholder("Enter your name");
entry.AssertPlaceholderContains("name");
```

### Page-Level Semantic Assertions
```csharp
// In your page object class
public void AssertLoginError()
{
    ErrorLabel.AssertVisible();
    ErrorLabel.AssertTextContains("Invalid");
}

// In tests
loginPage.AssertLoginError();  // Semantic, self-documenting
```

## MAUI AutomationId Setup
Set AutomationId in your MAUI XAML:
```xml
<ContentPage AutomationId="LoginPage">
    <VerticalStackLayout>
        <Entry AutomationId="EmailEntry" 
               Placeholder="Email" />
        <Entry AutomationId="PasswordEntry" 
               Placeholder="Password" 
               IsPassword="True" />
        <Button AutomationId="LoginButton" 
                Text="Login" />
        <Label AutomationId="ErrorLabel" 
               TextColor="Red" />
    </VerticalStackLayout>
</ContentPage>
```

## Platform-Specific Configuration

### Windows (WinUI)
```csharp
protected override AppiumOptions GetAppiumOptions()
{
    var options = new AppiumOptions();
    options.PlatformName = "Windows";
    options.AutomationName = "Windows";
    options.App = @"C:\Path\To\App.exe";
    // Or use app package family name for packaged apps
    // options.App = "MyApp_1234567890abc!App";
    return options;
}
```

### Android
```csharp
protected override AppiumOptions GetAppiumOptions()
{
    var options = new AppiumOptions();
    options.PlatformName = "Android";
    options.AutomationName = "UiAutomator2";
    options.DeviceName = "emulator-5554";
    options.App = @"C:\Path\To\app.apk";
    // Or use package name for installed apps
    // options.AddAdditionalAppiumOption("appPackage", "com.myapp");
    // options.AddAdditionalAppiumOption("appActivity", ".MainActivity");
    return options;
}
```

### iOS
```csharp
protected override AppiumOptions GetAppiumOptions()
{
    var options = new AppiumOptions();
    options.PlatformName = "iOS";
    options.AutomationName = "XCUITest";
    options.DeviceName = "iPhone 15 Pro";
    options.PlatformVersion = "17.0";
    options.App = @"/Path/To/App.app";
    return options;
}
```

### macOS (Mac Catalyst)
```csharp
protected override AppiumOptions GetAppiumOptions()
{
    var options = new AppiumOptions();
    options.PlatformName = "Mac";
    options.AutomationName = "Mac2";
    options.App = @"/Applications/MyApp.app";
    return options;
}
```

## Gestures and Touch Actions

### Control-Level Gestures (Single Element)
All gestures wait for element visibility before executing:
```csharp
// Tap gestures
control.Tap();           // Single tap
control.DoubleTap();     // Double tap
control.LongPress(1500); // Long press (ms)

// Swipe gestures on control
control.SwipeLeft();     // Default 200px
control.SwipeRight(300); // Custom distance
control.SwipeUp();
control.SwipeDown(400);
control.Swipe(SwipeDirection.Left, 200);
```

### GestureService (Advanced Multi-Element Gestures)
Use `GestureService` for complex gestures:
```csharp
using Brinell.Maui.Gestures;

var gestures = new GestureService(Context);

// Drag and drop between elements
await gestures.DragTo(sourceControl, targetControl);
await gestures.DragByOffset(control, offsetX: 100, offsetY: 50);

// Screen-level gestures
await gestures.SwipeScreen(SwipeDirection.Up);
await gestures.TapAtCoordinates(100, 200);
await gestures.ScrollScreen(SwipeDirection.Down);
```

## Navigation Pattern
```csharp
// Return new page object after navigation
public SettingsPage NavigateToSettings()
{
    Log("NavigateToSettings()");
    SettingsButton.Tap();
    var page = new SettingsPage(_context);
    page.WaitForDisplayed();
    return page;
}
```

## Appium Server Setup
Start Appium server before running tests:
```bash
# Install Appium
npm install -g appium

# Install platform drivers
appium driver install windows
appium driver install uiautomator2
appium driver install xcuitest
appium driver install mac2

# Start Appium server
appium --address 127.0.0.1 --port 4723
```

## Best Practices
- Controls are instantiated in constructor
- Use `Log()` method to record actions for debugging
- Return page objects from navigation methods (fluent pattern)
- Use platform-specific test collections to run tests in isolation
- Always set AutomationId on MAUI elements you want to test
- Use implicit waits for element discovery
- Handle platform-specific behaviors with conditional logic if needed
- Tests should be independent and not rely on order
- Run Appium server before executing tests
