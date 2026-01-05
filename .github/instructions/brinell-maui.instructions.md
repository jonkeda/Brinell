---
applyTo: "**/*Maui*UITests*/**/*.cs,**/*MAUI*/**/*.cs"
description: "Brinell MAUI UI Testing patterns with Appium automation"
---

# Brinell MAUI UI Testing Framework

## Overview

Brinell.Maui provides MAUI-specific UI test automation using Appium as the underlying driver. All operations are **synchronous** (no async/await required).

## Framework Components

| Component | Namespace |
|-----------|-----------|
| Test Context | `Brinell.Maui.Infrastructure.AppiumTestContext` |
| Page Base | `Brinell.Maui.Controls.Base.PageBase` |
| Controls | `Brinell.Maui.Controls.*` |

---

## Page Object Structure

```csharp
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace MyApp.UITests.Pages;

public class MainPage : PageBase
{
    public override string AutomationId => "MainPage";
    
    public MainPage(AppiumTestContext context) : base(context) { }
    
    // Controls - use MAUI control types
    public ButtonControl SubmitButton => new(_context, this, "SubmitButton");
    public LabelControl TitleLabel => new(_context, this, "TitleLabel");
    public EntryControl NameEntry => new(_context, this, "NameEntry");
    public EditorControl NotesEditor => new(_context, this, "NotesEditor");
    public SwitchControl EnableSwitch => new(_context, this, "EnableSwitch");
    public CheckBoxControl AgreeCheckBox => new(_context, this, "AgreeCheckBox");
    public SliderControl VolumeSlider => new(_context, this, "VolumeSlider");
    public PickerControl ColorPicker => new(_context, this, "ColorPicker");
    public DatePickerControl DatePicker => new(_context, this, "BirthDate");
    public TimePickerControl TimePicker => new(_context, this, "ReminderTime");
    public ProgressBarControl Progress => new(_context, this, "Progress");
    public ActivityIndicatorControl Loading => new(_context, this, "Loading");
    public ScrollViewControl ScrollView => new(_context, this, "MainScroll");
    
    // Override IsDisplayed for reliable page detection
    public override bool IsDisplayed()
    {
        return TitleLabel.IsVisible();
    }
    
    // Workflow methods
    public MainPage EnterName(string name)
    {
        Log($"EnterName({name})");
        NameEntry.ClearAndEnter(name);
        return this;
    }
}
```

---

## Available Control Types

### Basic Controls

| Control | Class | XAML Element | Key Methods |
|---------|-------|--------------|-------------|
| Button | `ButtonControl` | `Button` | `Click()`, `Tap()`, `LongPress()` |
| Label | `LabelControl` | `Label` | `GetText()`, `AssertTextEquals()` |
| Entry | `EntryControl` | `Entry` | `Enter()`, `Clear()`, `ClearAndEnter()` |
| Editor | `EditorControl` | `Editor` | `Enter()`, `Clear()`, `ClearAndEnter()` |
| Image | `ImageControl` | `Image` | `IsVisible()`, `AssertVisible()` |

### Toggle Controls

| Control | Class | XAML Element | Key Methods |
|---------|-------|--------------|-------------|
| Switch | `SwitchControl` | `Switch` | `Toggle()`, `SetOn()`, `SetOff()`, `IsOn()` |
| CheckBox | `CheckBoxControl` | `CheckBox` | `Toggle()`, `SetChecked()`, `IsChecked()` |

### Range Controls

| Control | Class | XAML Element | Key Methods |
|---------|-------|--------------|-------------|
| Slider | `SliderControl` | `Slider` | `SetValue()`, `GetValue()`, `AssertValue()` |
| ProgressBar | `ProgressBarControl` | `ProgressBar` | `GetProgress()`, `AssertProgress()` |
| Stepper | `StepperControl` | `Stepper` | `Increment()`, `Decrement()`, `GetValue()` |

### Selection Controls

| Control | Class | XAML Element | Key Methods |
|---------|-------|--------------|-------------|
| Picker | `PickerControl` | `Picker` | `SelectByIndex()`, `SelectByText()`, `GetSelectedText()` |
| DatePicker | `DatePickerControl` | `DatePicker` | `SetDate()`, `GetDate()`, `AssertDate()` |
| TimePicker | `TimePickerControl` | `TimePicker` | `SetTime()`, `GetTime()`, `AssertTime()` |

### Container Controls

| Control | Class | XAML Element | Key Methods |
|---------|-------|--------------|-------------|
| ScrollView | `ScrollViewControl` | `ScrollView` | `ScrollTo()`, `ScrollToTop()`, `ScrollToBottom()` |
| ContentView | `ContentViewControl` | `ContentView` | `IsVisible()`, `GetChild()` |
| Frame | `FrameControl` | `Frame` | `IsVisible()`, `GetChild()` |

### Indicator Controls

| Control | Class | XAML Element | Key Methods |
|---------|-------|--------------|-------------|
| ActivityIndicator | `ActivityIndicatorControl` | `ActivityIndicator` | `IsRunning()`, `WaitForStopped()` |

---

## Control-Specific APIs

### ButtonControl / ContentControlBase

```csharp
// Inherited from ControlBase
void Click()              // Alias for Tap()
void Tap()                // Single tap
void DoubleTap()          // Double tap
void LongPress(int durationMs = 1000)
void DoubleClick()        // Alias for DoubleTap()
void RightClick()         // Alias for LongPress() on mobile
void Hover()              // Alias for Tap() on mobile

// Inherited assertions
void AssertVisible(string? message = null)
void AssertEnabled(string? message = null)
void AssertTextEquals(string expected, string? message = null)
```

### EntryControl / EditorControl (Text Input)

```csharp
void Enter(string text)               // Enter text (appends)
void Clear()                          // Clear all text
void ClearAndEnter(string text)       // Clear then enter
string GetText()                      // Get current text

// Inherited from ControlBase
void AssertTextEquals(string expected, string? message = null)
void AssertTextContains(string expected, string? message = null)
void AssertTextEmpty(string? message = null)
```

### SwitchControl

```csharp
bool IsOn()                           // Check if switch is on
void Toggle()                         // Toggle state
void SetOn()                          // Set to on
void SetOff()                         // Set to off

bool WaitOn(bool expected = true, int? timeoutMs = null)
void CheckOn(bool expected = true, int? timeoutMs = null)
void AssertOn(string? message = null)
void AssertOff(string? message = null)
```

### CheckBoxControl

```csharp
bool IsChecked()                      // Check if checked
void Toggle()                         // Toggle state
void SetChecked(bool value)           // Set checked state
void Check()                          // Set to checked
void Uncheck()                        // Set to unchecked

bool WaitChecked(bool expected = true, int? timeoutMs = null)
void CheckChecked(bool expected = true, int? timeoutMs = null)
void AssertChecked(string? message = null)
void AssertNotChecked(string? message = null)
```

### SliderControl

```csharp
double GetValue()                     // Get current value
void SetValue(double value)           // Set value
double GetMinimum()                   // Get minimum value
double GetMaximum()                   // Get maximum value

bool WaitValue(double expected, double tolerance = 0.01, int? timeoutMs = null)
void CheckValue(double expected, double tolerance = 0.01, int? timeoutMs = null)
void AssertValue(double expected, double tolerance = 0.01, string? message = null)
void AssertValueInRange(double min, double max, string? message = null)
```

### PickerControl

```csharp
void SelectByIndex(int index)         // Select by index
void SelectByText(string text)        // Select by display text
int GetSelectedIndex()                // Get selected index
string GetSelectedText()              // Get selected text
IReadOnlyList<string> GetItems()      // Get all items
int GetItemCount()                    // Get number of items

void AssertSelectedIndex(int expected, string? message = null)
void AssertSelectedText(string expected, string? message = null)
void AssertItemCount(int expected, string? message = null)
```

### DatePickerControl

```csharp
DateTime GetDate()                    // Get selected date
void SetDate(DateTime date)           // Set date
void SetDate(int year, int month, int day)

void AssertDate(DateTime expected, string? message = null)
void AssertDateInRange(DateTime min, DateTime max, string? message = null)
```

### TimePickerControl

```csharp
TimeSpan GetTime()                    // Get selected time
void SetTime(TimeSpan time)           // Set time
void SetTime(int hour, int minute)

void AssertTime(TimeSpan expected, string? message = null)
```

### ActivityIndicatorControl

```csharp
bool IsRunning()                      // Check if animating
bool WaitForRunning(bool expected = true, int? timeoutMs = null)
bool WaitForStopped(int? timeoutMs = null)
void AssertRunning(string? message = null)
void AssertNotRunning(string? message = null)
```

### ProgressBarControl

```csharp
double GetProgress()                  // Get progress (0.0 to 1.0)
bool WaitProgress(double expected, double tolerance = 0.01, int? timeoutMs = null)
void AssertProgress(double expected, double tolerance = 0.01, string? message = null)
void AssertProgressInRange(double min, double max, string? message = null)
```

---

## Gesture Methods (All Controls)

All controls inherit gesture support from `ControlBase`:

```csharp
void Tap()
void Click()                          // Alias for Tap()
void DoubleTap()
void LongPress(int durationMs = 1000)
void Swipe(SwipeDirection direction, int distance = 200)
void SwipeLeft(int distance = 200)
void SwipeRight(int distance = 200)
void SwipeUp(int distance = 200)
void SwipeDown(int distance = 200)
```

---

## XAML AutomationId Setup

Set `AutomationId` in XAML for test accessibility:

```xml
<ContentPage x:Class="MyApp.MainPage"
             AutomationId="MainPage">
    
    <StackLayout>
        <Label AutomationId="TitleLabel" 
               Text="Welcome" />
        
        <Entry AutomationId="NameEntry" 
               Placeholder="Enter name" />
        
        <Button AutomationId="SubmitButton" 
                Text="Submit" />
        
        <Switch AutomationId="EnableSwitch" />
        
        <Slider AutomationId="VolumeSlider" 
                Minimum="0" 
                Maximum="100" />
        
        <Picker AutomationId="ColorPicker">
            <Picker.Items>
                <x:String>Red</x:String>
                <x:String>Green</x:String>
                <x:String>Blue</x:String>
            </Picker.Items>
        </Picker>
    </StackLayout>
</ContentPage>
```

---

## Test Example

```csharp
using Xunit;
using MyApp.UITests.Pages;

public class CounterTests : MauiUITestBase
{
    [Fact]
    public void Counter_Increment_UpdatesLabel()
    {
        // Arrange
        var mainPage = new MainPage(_context);
        mainPage.WaitForDisplayed();
        
        // Act
        mainPage.IncrementButton.Click();
        
        // Assert - use control assertions, NOT FluentAssertions
        mainPage.CounterLabel.AssertTextEquals("1");
    }
    
    [Fact]
    public void Switch_Toggle_ChangesState()
    {
        // Arrange
        var mainPage = new MainPage(_context);
        mainPage.WaitForDisplayed();
        
        // Verify initial state
        mainPage.EnableSwitch.AssertOff();
        
        // Act
        mainPage.EnableSwitch.Toggle();
        
        // Assert
        mainPage.EnableSwitch.AssertOn();
    }
    
    [Fact]
    public void Slider_SetValue_UpdatesDisplay()
    {
        // Arrange
        var mainPage = new MainPage(_context);
        mainPage.WaitForDisplayed();
        
        // Act
        mainPage.VolumeSlider.SetValue(75);
        
        // Assert with tolerance
        mainPage.VolumeSlider.AssertValue(75, tolerance: 1);
        mainPage.VolumeLabel.AssertTextContains("75");
    }
}
```

---

## Platform-Specific Configuration

### Windows

```csharp
var options = new AppiumOptions();
options.AddAdditionalCapability("platformName", "Windows");
options.AddAdditionalCapability("app", "path/to/app.exe");
options.AddAdditionalCapability("deviceName", "WindowsPC");
```

### Android

```csharp
var options = new AppiumOptions();
options.AddAdditionalCapability("platformName", "Android");
options.AddAdditionalCapability("app", "/path/to/app.apk");
options.AddAdditionalCapability("deviceName", "Android Emulator");
options.AddAdditionalCapability("automationName", "UiAutomator2");
```

### iOS

```csharp
var options = new AppiumOptions();
options.AddAdditionalCapability("platformName", "iOS");
options.AddAdditionalCapability("app", "/path/to/app.ipa");
options.AddAdditionalCapability("deviceName", "iPhone Simulator");
options.AddAdditionalCapability("automationName", "XCUITest");
```

### macOS

```csharp
var options = new AppiumOptions();
options.AddAdditionalCapability("platformName", "Mac");
options.AddAdditionalCapability("app", "/path/to/app.app");
options.AddAdditionalCapability("automationName", "Mac2");
```

---

## Best Practices

### ✅ DO

1. **Use `WaitForDisplayed()` before interactions**
2. **Override `IsDisplayed()` for reliable page detection**
3. **Use workflow methods** for multi-step operations
4. **Use control assertions** (`control.AssertTextEquals()`)
5. **Set meaningful `AutomationId` values** on all interactive elements

### ❌ DON'T

1. **Don't use `Thread.Sleep()`** - use Wait methods
2. **Don't use FluentAssertions** - use Brinell assertions
3. **Don't access driver directly** - use page objects and controls
4. **Don't hardcode selectors in tests** - put them in page objects
5. **Don't forget to wait for page load** after navigation

---

## Version

- **Framework Version:** 1.0
- **Spec Reference:** SPEC-006 (ControlObject Framework)
- **Driver:** Appium
