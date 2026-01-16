# MAUI Testing Guide

A comprehensive guide to UI testing .NET MAUI applications with Brinell.Maui.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Installation & Setup](#installation--setup)
4. [Quick Start](#quick-start)
5. [Controls Reference](#controls-reference)
6. [Gestures & Touch](#gestures--touch)
7. [Assertions](#assertions)
8. [Platform-Specific Testing](#platform-specific-testing)
9. [Device Services](#device-services)
10. [Multi-Device Testing](#multi-device-testing)
11. [Performance Testing](#performance-testing)
12. [Best Practices](#best-practices)
13. [Troubleshooting](#troubleshooting)
14. [Appium Configuration](#appium-configuration)

---

## Overview

Brinell.Maui provides UI automation capabilities for .NET MAUI applications using Appium WebDriver. It supports testing on:

- **Windows** - WinUI 3 apps via Windows Application Driver
- **Android** - Native Android apps via UIAutomator2
- **iOS** - Native iOS apps via XCUITest
- **macOS** - Mac Catalyst apps via Mac2 driver

### Key Features

- **Control Wrappers**: Type-safe wrappers for all MAUI controls
- **Gesture Support**: Tap, swipe, long-press, pinch, and drag gestures
- **Is/Wait/Check/Assert Pattern**: Consistent API for state verification
- **Screenshot Capture**: Automatic screenshots on failure
- **Cross-Platform**: Single test code runs on all platforms
- **Page Object Pattern**: Built-in support for maintainable tests

---

## Architecture

### Component Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Your UI Tests                           │
├─────────────────────────────────────────────────────────────┤
│                    Page Objects                             │
│            (LoginPage, DashboardPage, etc.)                 │
├─────────────────────────────────────────────────────────────┤
│                   Brinell.Maui                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   Controls   │  │   Gestures   │  │   Services   │       │
│  │   Button     │  │   Tap        │  │   Device     │       │
│  │   Entry      │  │   Swipe      │  │   Alert      │       │
│  │   Label      │  │   LongPress  │  │   Lifecycle  │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
├─────────────────────────────────────────────────────────────┤
│                  AppiumTestContext                          │
│                  AppiumDriverAdapter                        │
├─────────────────────────────────────────────────────────────┤
│                    Appium WebDriver                         │
├─────────────────────────────────────────────────────────────┤
│  Windows Driver  │  UIAutomator2  │  XCUITest  │  Mac2     │
└─────────────────────────────────────────────────────────────┘
```

### Core Classes

| Class | Purpose |
|-------|---------|
| `AppiumTestContext` | Test session context with driver, logging, and services |
| `AppiumDriverAdapter` | Wrapper around Appium driver for element discovery |
| `ControlBase` | Base class for all control wrappers |
| `PageBase` | Base class for page objects |
| `MauiUITestBase` | Base class for test classes |
| `GestureService` | Advanced gesture operations |

---

## Installation & Setup

### Prerequisites

| Component | Version | Required For |
|-----------|---------|--------------|
| .NET SDK | 8.0+ | All platforms |
| Appium Server | 2.x | All platforms |
| Node.js | 18+ | Appium installation |
| WinAppDriver | 1.2.1+ | Windows testing |
| Android SDK | 34+ | Android testing |
| Xcode | 15+ | iOS/macOS testing (Mac only) |

### Install Appium

```bash
# Install Appium globally
npm install -g appium

# Install platform drivers
appium driver install windows      # For Windows apps
appium driver install uiautomator2 # For Android apps
appium driver install xcuitest     # For iOS apps
appium driver install mac2         # For macOS apps
```

### Install NuGet Package

```bash
dotnet add package Brinell.Maui
```

### Windows Setup

1. Enable Developer Mode in Windows Settings
2. Download and install [WinAppDriver](https://github.com/microsoft/WinAppDriver/releases)
3. Start WinAppDriver (or let Appium manage it)

### Android Setup

1. Install Android Studio
2. Create an Android Virtual Device (AVD) or connect a physical device
3. Enable USB debugging on physical devices
4. Set ANDROID_HOME environment variable

### iOS Setup (macOS only)

1. Install Xcode from App Store
2. Install Xcode Command Line Tools: `xcode-select --install`
3. Create iOS Simulator or connect a physical device
4. For physical devices: configure signing certificates

---

## Quick Start

### 1. Create Test Project

```bash
dotnet new xunit -n MyApp.UITests
cd MyApp.UITests
dotnet add package Brinell.Maui
dotnet add package xunit
```

### 2. Create Test Base Class

```csharp
using Brinell.Maui.Testing;
using OpenQA.Selenium.Appium;
using Xunit.Abstractions;

public class MyAppTestBase : MauiUITestBase
{
    public MyAppTestBase(ITestOutputHelper output) 
        : base(output.WriteLine)
    {
    }

    protected override Uri AppiumServerUri => new("http://127.0.0.1:4723");

    protected override AppiumOptions GetAppiumOptions()
    {
        var options = new AppiumOptions();
        options.PlatformName = "Windows";
        options.AutomationName = "Windows";
        options.App = @"C:\Path\To\YourApp.exe";
        return options;
    }
}
```

### 3. Create Page Object

```csharp
using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

public class LoginPage : PageBase
{
    public EntryControl UsernameEntry { get; }
    public EntryControl PasswordEntry { get; }
    public ButtonControl LoginButton { get; }
    public LabelControl ErrorLabel { get; }

    public LoginPage(AppiumTestContext context) 
        : base(context, "LoginPage")
    {
        UsernameEntry = new EntryControl(context, this, "UsernameEntry");
        PasswordEntry = new EntryControl(context, this, "PasswordEntry");
        LoginButton = new ButtonControl(context, this, "LoginButton");
        ErrorLabel = new LabelControl(context, this, "ErrorLabel");
    }

    public override bool IsDisplayed()
    {
        return _context.ElementIsVisible(AutomationId);
    }

    public LoginPage EnterCredentials(string username, string password)
    {
        UsernameEntry.SetText(username);
        PasswordEntry.SetText(password);
        return this;
    }

    public void TapLogin()
    {
        LoginButton.Tap();
    }

    // Semantic assertion
    public void AssertHasLoginError()
    {
        ErrorLabel.AssertVisible();
    }
}
```

### 4. Write Tests

```csharp
using Xunit;
using Xunit.Abstractions;

[Collection("UITests")]
public class LoginTests : MyAppTestBase
{
    public LoginTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Login_WithValidCredentials_Succeeds()
    {
        // Arrange
        LaunchApplication();
        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Act
        loginPage.EnterCredentials("user@test.com", "password123");
        loginPage.TapLogin();

        // Assert
        var dashboard = new DashboardPage(Context!);
        dashboard.AssertDisplayed();
    }

    [Fact]
    public void Login_WithEmptyPassword_ShowsError()
    {
        // Arrange
        LaunchApplication();
        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Act
        loginPage.UsernameEntry.SetText("user@test.com");
        loginPage.TapLogin();

        // Assert
        loginPage.AssertHasLoginError();
    }
}
```

### 5. Set AutomationId in MAUI

```xml
<ContentPage AutomationId="LoginPage"
             xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    <VerticalStackLayout Padding="20">
        <Entry AutomationId="UsernameEntry" 
               Placeholder="Username" />
        <Entry AutomationId="PasswordEntry" 
               Placeholder="Password" 
               IsPassword="True" />
        <Button AutomationId="LoginButton" 
                Text="Login" />
        <Label AutomationId="ErrorLabel" 
               TextColor="Red" 
               IsVisible="{Binding HasError}" />
    </VerticalStackLayout>
</ContentPage>
```

### 6. Run Tests

```bash
# Start Appium server
appium --address 127.0.0.1 --port 4723

# In another terminal, run tests
dotnet test
```

---

## Controls Reference

### Basic Controls

| Control | MAUI Element | Key Methods |
|---------|--------------|-------------|
| `ButtonControl` | Button, ImageButton | `Click()`, `Tap()`, `AssertEnabled()` |
| `LabelControl` | Label, Span | `GetText()`, `AssertTextEquals()` |
| `EntryControl` | Entry | `SetText()`, `Clear()`, `AssertPlaceholder()` |
| `EditorControl` | Editor | `SetText()`, `Clear()`, `Append()` |
| `CheckBoxControl` | CheckBox | `Check()`, `Uncheck()`, `AssertChecked()` |
| `SwitchControl` | Switch | `TurnOn()`, `TurnOff()`, `AssertIsOn()` |
| `SliderControl` | Slider | `SetValue()`, `AssertValue()`, `AssertPercentage()` |
| `ProgressBarControl` | ProgressBar | `GetValue()`, `AssertComplete()` |
| `PickerControl` | Picker | `SelectByText()`, `AssertSelectedText()` |

### Navigation Controls

| Control | MAUI Element | Key Methods |
|---------|--------------|-------------|
| `ShellControl` | Shell | `NavigateToRoute()`, `OpenFlyout()`, `CloseFlyout()` |
| `TabBarControl` | TabBar | `SelectTab()`, `GetSelectedTab()` |
| `FlyoutItemControl` | FlyoutItem | `Tap()`, `AssertVisible()` |

### Advanced Controls

| Control | MAUI Element | Key Methods |
|---------|--------------|-------------|
| `CollectionViewControl` | CollectionView | `GetItems()`, `SelectItem()`, `ScrollTo()` |
| `SearchBarControl` | SearchBar | `Search()`, `Clear()`, `GetSearchText()` |
| `WebViewControl` | WebView | `NavigateTo()`, `GetCurrentUrl()` |
| `SwipeViewControl` | SwipeView | `SwipeLeft()`, `SwipeRight()` |
| `RefreshViewControl` | RefreshView | `PullToRefresh()`, `AssertRefreshing()` |
| `CarouselViewControl` | CarouselView | `SwipeNext()`, `SwipePrevious()` |
| `DatePickerControl` | DatePicker | `SetDate()`, `GetDate()` |
| `TimePickerControl` | TimePicker | `SetTime()`, `GetTime()` |
| `StepperControl` | Stepper | `Increment()`, `Decrement()`, `AssertValue()` |

### Container Controls

| Control | MAUI Element | Key Methods |
|---------|--------------|-------------|
| `ScrollViewControl` | ScrollView | `ScrollToTop()`, `ScrollToBottom()`, `ScrollTo()` |
| `FrameControl` | Frame | `AssertVisible()`, child access |
| `BorderControl` | Border | `AssertVisible()`, child access |
| `ContentViewControl` | ContentView | `AssertVisible()`, child access |

---

## Gestures & Touch

### Control-Level Gestures

All controls inherit gesture methods from `ControlBase`:

```csharp
// Tap gestures
control.Tap();              // Single tap
control.DoubleTap();        // Double tap
control.LongPress();        // Default 1000ms
control.LongPress(2000);    // Custom duration

// Swipe gestures
control.SwipeLeft();        // Default 200px
control.SwipeRight(300);    // Custom distance
control.SwipeUp();
control.SwipeDown();
control.Swipe(SwipeDirection.Left, 250);
```

### GestureService for Advanced Gestures

```csharp
using Brinell.Maui.Gestures;

var gestures = new GestureService(Context);

// Drag and drop
await gestures.DragTo(sourceControl, targetControl);
await gestures.DragByOffset(control, offsetX: 100, offsetY: 50);

// Screen-level gestures
await gestures.SwipeScreen(SwipeDirection.Up);
await gestures.TapAtCoordinates(100, 200);
await gestures.ScrollScreen(SwipeDirection.Down);
```

### Wait Before Gesture

All gesture methods automatically wait for element visibility before executing. This prevents flaky tests due to timing issues.

---

## Assertions

### Common Assertions (All Controls)

```csharp
// Visibility and existence
control.AssertExists();
control.AssertNotExists();
control.AssertVisible();
control.AssertNotVisible();
control.AssertEnabled();
control.AssertDisabled();

// Text assertions
control.AssertTextEquals("Expected Text");
control.AssertTextContains("partial");
control.AssertTextStartsWith("prefix");
control.AssertTextEndsWith("suffix");
control.AssertTextEmpty();
control.AssertTextNotEmpty();
```

### Toggle Control Assertions

```csharp
// CheckBox
checkBox.AssertChecked();
checkBox.AssertUnchecked();

// Switch (with aliases)
toggleSwitch.AssertIsOn();
toggleSwitch.AssertIsOff();
```

### Range Control Assertions

```csharp
slider.AssertValue(50.0);
slider.AssertValueInRange(0, 100);
slider.AssertPercentage(50.0);
slider.AssertAtMinimum();
slider.AssertAtMaximum();

// ProgressBar
progressBar.AssertComplete();
progressBar.AssertNotComplete();
progressBar.AssertProgressAtLeast(75);
```

### Selector Control Assertions

```csharp
picker.AssertSelectedText("Option 1");
picker.AssertSelectedTextContains("Option");
picker.AssertSelectedIndex(0);
picker.AssertItemCount(5);
picker.AssertNoSelection();
```

### Text Input Assertions

```csharp
entry.AssertIsReadOnly();
entry.AssertIsNotReadOnly();
entry.AssertPlaceholder("Enter your name");
entry.AssertPlaceholderContains("name");
```

### Semantic Page Assertions

Create domain-specific assertions in your page objects:

```csharp
public class LoginPage : PageBase
{
    // ... controls ...

    public void AssertHasLoginError()
    {
        ErrorLabel.AssertVisible();
        ErrorLabel.AssertTextContains("Invalid");
    }

    public void AssertLoginButtonEnabled()
    {
        LoginButton.AssertEnabled();
    }
}

// In tests - more readable
loginPage.AssertHasLoginError();
```

---

## Platform-Specific Testing

### Platform Detection

```csharp
public class MyTests : PlatformSpecificTestBase
{
    [Fact]
    public void Feature_WorksCorrectly()
    {
        if (IsAndroid)
        {
            // Android-specific test code
        }
        else if (IsIOS)
        {
            // iOS-specific test code
        }
        else if (IsWindows)
        {
            // Windows-specific test code
        }
    }
}
```

### Conditional Execution

```csharp
public class PlatformTests : PlatformSpecificTestBase
{
    [Fact]
    public void BackButton_NavigatesBack()
    {
        // Only runs on Android
        RunOnAndroid(() =>
        {
            Context.PressBackButton();
            previousPage.AssertDisplayed();
        });
    }

    [Fact]
    public void SwipeFromEdge_ShowsMenu()
    {
        // Only runs on iOS
        RunOnIOS(() =>
        {
            Context.SwipeFromLeftEdge();
            menu.AssertVisible();
        });
    }
}
```

### Platform-Specific Configuration

```csharp
protected override AppiumOptions GetAppiumOptions()
{
    var options = new AppiumOptions();
    
    switch (TargetPlatform)
    {
        case MauiPlatform.Windows:
            options.PlatformName = "Windows";
            options.AutomationName = "Windows";
            options.App = WindowsAppPath;
            break;
            
        case MauiPlatform.Android:
            options.PlatformName = "Android";
            options.AutomationName = "UiAutomator2";
            options.App = AndroidApkPath;
            options.DeviceName = "emulator-5554";
            break;
            
        case MauiPlatform.iOS:
            options.PlatformName = "iOS";
            options.AutomationName = "XCUITest";
            options.App = iOSAppPath;
            options.DeviceName = "iPhone 15 Pro";
            options.PlatformVersion = "17.0";
            break;
    }
    
    return options;
}
```

---

## Device Services

### Device Information

```csharp
var deviceInfo = Context.DeviceInfo;

Console.WriteLine($"Platform: {deviceInfo.Platform}");       // iOS, Android, Windows
Console.WriteLine($"Device Type: {deviceInfo.DeviceType}");  // Phone, Tablet, Desktop
Console.WriteLine($"OS Version: {deviceInfo.OSVersion}");
Console.WriteLine($"Screen Size: {deviceInfo.ScreenSize}");
Console.WriteLine($"Orientation: {deviceInfo.Orientation}");
```

### App Lifecycle

```csharp
var lifecycle = Context.AppLifecycle;

// Background/foreground
await lifecycle.SendToBackground();
await lifecycle.BringToForeground();

// App management
await lifecycle.Terminate();
await lifecycle.Restart();
await lifecycle.ClearAppData();
```

### Alert Handling

```csharp
var alerts = Context.AlertService;

if (alerts.IsAlertDisplayed)
{
    Console.WriteLine($"Alert: {alerts.AlertTitle}");
    Console.WriteLine($"Message: {alerts.AlertMessage}");
    
    await alerts.AcceptAlert();      // Tap OK/Accept
    // or
    await alerts.DismissAlert();     // Tap Cancel/Dismiss
    // or
    await alerts.TapAlertButton("Custom Button");
}
```

---

## Multi-Device Testing

### Device Configurations

```csharp
// Pre-defined configurations
var iphone14 = DeviceConfiguration.iPhone14;
var pixel7 = DeviceConfiguration.Pixel7;
var galaxyTab = DeviceConfiguration.GalaxyTabS8;

// Custom configuration
var customDevice = new DeviceConfiguration
{
    Name = "Custom Android",
    Platform = MauiPlatform.Android,
    DeviceName = "custom-avd",
    ScreenWidth = 1080,
    ScreenHeight = 2400,
    Density = 2.75
};
```

### Multi-Device Test Runner

```csharp
[Fact]
public async Task LoginPage_DisplaysCorrectly_AllDevices()
{
    var runner = new MultiDeviceTestRunner();
    runner.AddDevice(DeviceConfiguration.iPhone14);
    runner.AddDevice(DeviceConfiguration.iPhoneSE);
    runner.AddDevice(DeviceConfiguration.Pixel7);
    runner.AddDevice(DeviceConfiguration.GalaxyTabS8);

    var results = await runner.RunTestOnAllDevices(async context =>
    {
        var page = new LoginPage(context);
        page.AssertDisplayed();
        page.LoginButton.AssertEnabled();
        page.UsernameEntry.AssertVisible();
    });

    // Check results
    Assert.True(results.AllPassed, results.Summary);
}
```

---

## Performance Testing

### Measure Startup Time

```csharp
var performance = Context.PerformanceMonitor;

var metrics = await performance.MeasureStartupTime();

Assert.True(
    metrics.StartupTime < TimeSpan.FromSeconds(3),
    $"App startup took {metrics.StartupTime.TotalSeconds}s, expected < 3s");
```

### Measure Navigation

```csharp
var metrics = await performance.MeasureNavigation(() =>
{
    dashboardPage.NavigateToSettings();
});

Assert.True(
    metrics.NavigationTime < TimeSpan.FromMilliseconds(500),
    $"Navigation took {metrics.NavigationTime.TotalMilliseconds}ms");
```

### Performance Assertions

```csharp
[Fact]
public async Task App_StartsWithin_3Seconds()
{
    var metrics = await Context.PerformanceMonitor.MeasureStartupTime();
    
    Assert.True(metrics.StartupTime < TimeSpan.FromSeconds(3));
    Assert.True(metrics.MemoryUsage < 100_000_000); // 100MB
}
```

---

## Best Practices

### 1. Use Semantic Page Methods

```csharp
// ❌ Avoid: Low-level operations in tests
loginPage.UsernameEntry.SetText("user");
loginPage.PasswordEntry.SetText("pass");
loginPage.LoginButton.Click();

// ✅ Prefer: Semantic page methods
loginPage.LoginWithCredentials("user", "pass");
```

### 2. Use Control Assertions

```csharp
// ❌ Avoid: xUnit assertions
Assert.True(control.IsVisible());
Assert.Equal("Expected", control.GetText());

// ✅ Prefer: Control assertions (with screenshots on failure)
control.AssertVisible();
control.AssertTextEquals("Expected");
```

### 3. Return Page Objects from Navigation

```csharp
// ✅ Fluent navigation pattern
public SettingsPage NavigateToSettings()
{
    SettingsButton.Tap();
    var page = new SettingsPage(_context);
    page.WaitForDisplayed();
    return page;
}

// Usage
var settings = dashboard.NavigateToSettings();
settings.AssertDisplayed();
```

### 4. Initialize Controls in Constructor

```csharp
// ✅ Controls initialized once
public LoginPage(AppiumTestContext context) : base(context, "LoginPage")
{
    UsernameEntry = new EntryControl(context, this, "UsernameEntry");
    PasswordEntry = new EntryControl(context, this, "PasswordEntry");
}
```

### 5. Log Page Actions

```csharp
public void Login(string username, string password)
{
    Log($"Login({username}, ***)");  // Log the action
    UsernameEntry.SetText(username);
    PasswordEntry.SetText(password);
    LoginButton.Tap();
}
```

### 6. Use Consistent AutomationIds

```xml
<!-- ✅ Consistent naming convention -->
<Entry AutomationId="Login_UsernameEntry" />
<Button AutomationId="Login_SubmitButton" />

<!-- Pattern: {PageName}_{ControlName} -->
```

---

## Troubleshooting

### Windows Driver Limitations

The Windows Application Driver has some W3C WebDriver API limitations that differ from Android/iOS drivers:

| Limitation | Impact | Solution |
|------------|--------|----------|
| `GET /timeouts` not supported | `driver.Manage().Timeouts().ImplicitWait` getter throws `UnknownMethodException` | Store timeout values locally instead of reading from driver |
| Limited gesture support | Some touch gestures may not work | Use Windows-specific input methods |
| No `GET /window/rect` in some versions | Cannot read window position | Store window state locally |

#### Timeout Getter Exception

```csharp
// ❌ WRONG - Throws UnknownMethodException on Windows Driver
var currentTimeout = driver.Manage().Timeouts().ImplicitWait;

// ✅ CORRECT - Store timeout value when setting it
private TimeSpan _implicitWait;

public void SetImplicitWait(TimeSpan timeout)
{
    _implicitWait = timeout;
    driver.Manage().Timeouts().ImplicitWait = timeout;
}

public TimeSpan GetImplicitWait() => _implicitWait;
```

#### Debugging with Appium Logs

When element finding silently fails, enable Appium debug logging:

```bash
# Start Appium with debug logging
appium --address 127.0.0.1 --port 4723 --log-level debug
```

Look for:
- `POST /session` - Session creation
- `POST /element` - Element find requests (if missing, exception is being swallowed)
- `POST /timeouts` - Timeout configuration
- `UnknownMethodException` in error responses

> **Warning:** Avoid silent exception catching like `catch (WebDriverException) { return null; }` - it hides root causes and makes debugging extremely difficult.

### Common Issues

#### Element Not Found

```
NoSuchElementException: Element with AutomationId 'MyButton' not found
```

**Solutions:**
1. Verify `AutomationId` is set in XAML
2. Wait for element: `control.WaitVisible()`
3. Check if element is in a different page/state
4. Use Appium Inspector to verify element hierarchy

#### Appium Connection Failed

```
WebDriverException: Could not start a new session
```

**Solutions:**
1. Ensure Appium server is running: `appium --address 127.0.0.1 --port 4723`
2. Check correct driver is installed: `appium driver list`
3. Verify app path exists and is accessible
4. Check platform-specific prerequisites (WinAppDriver, Android SDK, etc.)

#### Gesture Not Working

```
Swipe action did not produce expected result
```

**Solutions:**
1. Verify element is visible before gesture
2. Increase gesture distance for small elements
3. Add delay after gesture for animations
4. Check if element is inside a ScrollView that intercepts gestures

#### Test Flakiness

**Solutions:**
1. Use explicit waits instead of `Thread.Sleep()`
2. Use `WaitFor*` methods for state changes
3. Increase timeout for slow devices/emulators
4. Run tests in isolation (one test at a time)

### Debugging Tips

1. **Enable verbose logging:**
   ```csharp
   Context.LogLevel = LogLevel.Verbose;
   ```

2. **Capture page source on failure:**
   ```csharp
   try { /* test */ }
   catch { Console.WriteLine(Context.PageSource); throw; }
   ```

3. **Use Appium Inspector** to explore element tree

4. **Check screenshots** captured on assertion failures

---

## Appium Configuration

### Windows Configuration

```csharp
var options = new AppiumOptions();
options.PlatformName = "Windows";
options.AutomationName = "Windows";

// Packaged app (MSIX)
options.App = "MyApp_1234567890abc!App";

// Or unpackaged EXE
options.App = @"C:\Path\To\MyApp.exe";

// Additional options
options.AddAdditionalAppiumOption("ms:waitForAppLaunch", "10");
options.AddAdditionalAppiumOption("ms:experimental-webdriver", true);
```

### Android Configuration

```csharp
var options = new AppiumOptions();
options.PlatformName = "Android";
options.AutomationName = "UiAutomator2";
options.DeviceName = "emulator-5554";

// Install APK
options.App = @"C:\Path\To\app.apk";

// Or use installed app
options.AddAdditionalAppiumOption("appPackage", "com.mycompany.myapp");
options.AddAdditionalAppiumOption("appActivity", "crc64xxx.MainActivity");

// Additional options
options.AddAdditionalAppiumOption("autoGrantPermissions", true);
options.AddAdditionalAppiumOption("noReset", false);
```

### iOS Configuration

```csharp
var options = new AppiumOptions();
options.PlatformName = "iOS";
options.AutomationName = "XCUITest";
options.DeviceName = "iPhone 15 Pro";
options.PlatformVersion = "17.0";

// Simulator
options.App = @"/Path/To/MyApp.app";

// Physical device
options.AddAdditionalAppiumOption("udid", "device-udid-here");
options.AddAdditionalAppiumOption("xcodeOrgId", "TEAM_ID");
options.AddAdditionalAppiumOption("xcodeSigningId", "iPhone Developer");
```

### macOS Configuration

```csharp
var options = new AppiumOptions();
options.PlatformName = "Mac";
options.AutomationName = "Mac2";
options.App = @"/Applications/MyApp.app";

// Or bundle ID for installed apps
options.AddAdditionalAppiumOption("bundleId", "com.mycompany.myapp");
```

---

## Resources

### Documentation
- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [Appium Documentation](http://appium.io/docs/en/latest/)
- [Brinell GitHub Repository](https://github.com/yourorg/brinell)

### Tools
- [Appium Inspector](https://github.com/appium/appium-inspector) - UI element inspection
- [Android Studio](https://developer.android.com/studio) - Android emulator management
- [Xcode](https://developer.apple.com/xcode/) - iOS simulator (macOS)

### Community
- [Appium Discuss Forum](https://discuss.appium.io/)
- [.NET MAUI GitHub](https://github.com/dotnet/maui)
- [Stack Overflow - Appium](https://stackoverflow.com/questions/tagged/appium)
