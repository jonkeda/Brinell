---
applyTo: "**/*Tests*/**/*.cs,**/*UITests*/**/*.cs,**/*Test.cs"
description: "Brinell UI Test writing patterns and best practices"
---

# Brinell UI Test Writing Guide

## Overview

This guide provides patterns for writing UI tests using the Brinell framework. Follow these patterns to create maintainable, reliable tests.

---

## Test Structure

### Basic Test Pattern

```csharp
using Xunit;

public class FeatureTests : MauiUITestBase  // or HtmlUITestBase
{
    [Fact]
    public void ComponentUnderTest_WhenCondition_ExpectedBehavior()
    {
        // ARRANGE - Set up test conditions
        var page = new MainPage(_context);
        page.WaitForDisplayed();
        
        // ACT - Perform the action
        page.SubmitButton.Click();
        
        // ASSERT - Verify the outcome using control assertions
        page.ResultLabel.AssertTextEquals("Success");
    }
}
```

### Test Naming Convention

Use the pattern: `ComponentUnderTest_WhenCondition_ExpectedBehavior`

Examples:
- `IncrementButton_WhenClicked_IncrementsCounterBy1`
- `LoginForm_WithInvalidCredentials_ShowsErrorMessage`
- `Slider_WhenDraggedTo50_UpdatesValueLabel`
- `CheckBox_WhenToggled_ChangesState`

---

## Page Object Creation

### MAUI Page Object Template

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
    
    // ═══════════════════════════════════════════════════════════════
    // CONTROLS - Define all page controls as properties
    // ═══════════════════════════════════════════════════════════════
    
    // Labels
    public LabelControl TitleLabel => new(_context, this, "TitleLabel");
    public LabelControl CounterLabel => new(_context, this, "CounterLabel");
    
    // Buttons
    public ButtonControl IncrementButton => new(_context, this, "IncrementButton");
    public ButtonControl SubmitButton => new(_context, this, "SubmitButton");
    
    // Text Input
    public EntryControl NameEntry => new(_context, this, "NameEntry");
    public EditorControl NotesEditor => new(_context, this, "NotesEditor");
    
    // Toggle Controls
    public SwitchControl EnableSwitch => new(_context, this, "EnableSwitch");
    public CheckBoxControl AgreeCheckBox => new(_context, this, "AgreeCheckBox");
    
    // Range Controls
    public SliderControl VolumeSlider => new(_context, this, "VolumeSlider");
    public ProgressBarControl ProgressBar => new(_context, this, "ProgressBar");
    
    // Selection Controls
    public PickerControl ColorPicker => new(_context, this, "ColorPicker");
    public DatePickerControl DatePicker => new(_context, this, "DatePicker");
    
    // ═══════════════════════════════════════════════════════════════
    // PAGE DETECTION - Override for reliable page identification
    // ═══════════════════════════════════════════════════════════════
    
    public override bool IsDisplayed()
    {
        return TitleLabel.IsVisible();
    }
    
    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS - Multi-step operations that return this page
    // ═══════════════════════════════════════════════════════════════
    
    public MainPage IncrementCounter()
    {
        Log("IncrementCounter()");
        IncrementButton.Click();
        return this;
    }
    
    public MainPage EnterName(string name)
    {
        Log($"EnterName({name})");
        NameEntry.ClearAndEnter(name);
        return this;
    }
    
    public MainPage SetVolume(double value)
    {
        Log($"SetVolume({value})");
        VolumeSlider.SetValue(value);
        return this;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // NAVIGATION METHODS - Operations that navigate to other pages
    // ═══════════════════════════════════════════════════════════════
    
    public SettingsPage NavigateToSettings()
    {
        Log("NavigateToSettings()");
        SettingsButton.Click();
        var settingsPage = new SettingsPage(_context);
        settingsPage.WaitForDisplayed();
        return settingsPage;
    }
}
```

### Blazor Page Object Template

```csharp
using Brinell.Core.Abstractions;
using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace MyApp.UITests.PageObjects;

public class CounterPage : PageBase
{
    public override string AutomationId => "#counter-title";
    
    // ═══════════════════════════════════════════════════════════════
    // CONTROLS - Initialize in constructor with CSS selectors
    // ═══════════════════════════════════════════════════════════════
    
    public LabelControl CounterTitle { get; }
    public LabelControl CountDisplay { get; }
    public ButtonControl IncrementButton { get; }
    public ButtonControl DecrementButton { get; }
    public ButtonControl ResetButton { get; }
    public TextInputControl StepInput { get; }
    
    public CounterPage(SeleniumTestContext context) : base(context)
    {
        CounterTitle = new LabelControl(context, this, "#counter-title");
        CountDisplay = new LabelControl(context, this, "#count-display");
        IncrementButton = new ButtonControl(context, this, "#increment-btn");
        DecrementButton = new ButtonControl(context, this, "#decrement-btn");
        ResetButton = new ButtonControl(context, this, "#reset-btn");
        StepInput = new TextInputControl(context, this, "#step-input");
    }
    
    // ═══════════════════════════════════════════════════════════════
    // PAGE DETECTION
    // ═══════════════════════════════════════════════════════════════
    
    public override bool IsDisplayed()
    {
        return CounterTitle.IsVisible() && CounterTitle.GetText() == "Counter";
    }
    
    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════
    
    public CounterPage ClickIncrement()
    {
        Log("ClickIncrement()");
        IncrementButton.Click();
        return this;
    }
    
    public CounterPage IncrementMultiple(int times)
    {
        Log($"IncrementMultiple({times})");
        for (int i = 0; i < times; i++)
        {
            ClickIncrement();
        }
        return this;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // HELPER METHODS - Parse display values
    // ═══════════════════════════════════════════════════════════════
    
    public int GetCurrentCount()
    {
        var text = CountDisplay.GetText();
        // Format: "Current count: 5"
        var parts = text.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var count))
        {
            return count;
        }
        return 0;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // CUSTOM ASSERTIONS - Page-specific assertion helpers
    // ═══════════════════════════════════════════════════════════════
    
    public void AssertCount(int expected, string? message = null)
    {
        var actual = GetCurrentCount();
        if (actual != expected)
        {
            throw new Brinell.Core.Exceptions.AssertionException(
                message ?? $"Expected count {expected} but got {actual}.");
        }
    }
    
    public bool WaitForCount(int expected, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        return _context.WaitFor(
            () => GetCurrentCount() == expected,
            timeout,
            $"count = {expected}");
    }
}
```

---

## Assertions - Use Control Assertions, NOT FluentAssertions

### ❌ WRONG - Using FluentAssertions

```csharp
// DON'T DO THIS
using FluentAssertions;

[Fact]
public void Counter_Increment_WrongWay()
{
    var page = new CounterPage(_context);
    page.IncrementButton.Click();
    
    // ❌ WRONG - Using FluentAssertions
    page.CounterLabel.GetText().Should().Be("1");
    page.CounterLabel.IsVisible().Should().BeTrue();
}
```

### ✅ CORRECT - Using Control Assertions

```csharp
// DO THIS
[Fact]
public void Counter_Increment_CorrectWay()
{
    var page = new CounterPage(_context);
    page.WaitForDisplayed();
    page.IncrementButton.Click();
    
    // ✅ CORRECT - Using Brinell control assertions
    page.CounterLabel.AssertTextEquals("1");
    page.CounterLabel.AssertVisible();
}
```

### Available Control Assertions

```csharp
// Existence and Visibility
control.AssertExists();
control.AssertNotExists();
control.AssertVisible();
control.AssertNotVisible();

// Enabled State
control.AssertEnabled();
control.AssertDisabled();

// Text Assertions
control.AssertTextEquals("expected");
control.AssertTextContains("partial");
control.AssertTextEmpty();
control.AssertTextNotEmpty();
control.AssertTextStartsWith("prefix");
control.AssertTextEndsWith("suffix");
control.AssertTextMatches(@"regex\d+pattern");

// Toggle Assertions (Switch, CheckBox)
toggleControl.AssertOn();
toggleControl.AssertOff();
checkBox.AssertChecked();
checkBox.AssertNotChecked();

// Value Assertions (Slider, Range)
slider.AssertValue(50, tolerance: 1);
slider.AssertValueInRange(0, 100);

// Selection Assertions (Picker, Select)
picker.AssertSelectedIndex(2);
picker.AssertSelectedText("Option 3");
picker.AssertItemCount(5);

// Progress Assertions
progress.AssertProgress(0.5, tolerance: 0.01);
```

---

## Common Test Patterns

### Counter Test

```csharp
[Fact]
public void Counter_MultipleIncrements_ShowsCorrectValue()
{
    // Arrange
    var page = new MainPage(_context);
    page.WaitForDisplayed();
    
    // Act - increment 3 times
    page.IncrementButton.Click();
    page.IncrementButton.Click();
    page.IncrementButton.Click();
    
    // Assert
    page.CounterLabel.AssertTextEquals("3");
}
```

### Form Input Test

```csharp
[Fact]
public void Form_EnterText_DisplaysGreeting()
{
    // Arrange
    var page = new MainPage(_context);
    page.WaitForDisplayed();
    
    // Act
    page.NameEntry.ClearAndEnter("John");
    page.GreetButton.Click();
    
    // Assert
    page.GreetingLabel.WaitVisible();  // Wait for async update
    page.GreetingLabel.AssertTextContains("Hello, John");
}
```

### Toggle Control Test

```csharp
[Fact]
public void Switch_Toggle_ChangesState()
{
    // Arrange
    var page = new MainPage(_context);
    page.WaitForDisplayed();
    
    // Verify initial state
    page.EnableSwitch.AssertOff();
    
    // Act
    page.EnableSwitch.Toggle();
    
    // Assert
    page.EnableSwitch.AssertOn();
}
```

### Slider Control Test

```csharp
[Fact]
public void Slider_SetValue_UpdatesLabel()
{
    // Arrange
    var page = new MainPage(_context);
    page.WaitForDisplayed();
    
    // Act
    page.VolumeSlider.SetValue(75);
    
    // Assert
    page.VolumeSlider.AssertValue(75, tolerance: 1);
    page.VolumeLabel.AssertTextContains("75");
}
```

### Picker/Select Test

```csharp
[Fact]
public void Picker_SelectItem_UpdatesDisplay()
{
    // Arrange
    var page = new MainPage(_context);
    page.WaitForDisplayed();
    
    // Act
    page.ColorPicker.SelectByText("Blue");
    
    // Assert
    page.ColorPicker.AssertSelectedText("Blue");
    page.SelectedColorLabel.AssertTextEquals("Blue");
}
```

### Navigation Test

```csharp
[Fact]
public void MainPage_ClickSettings_NavigatesToSettings()
{
    // Arrange
    var mainPage = new MainPage(_context);
    mainPage.WaitForDisplayed();
    
    // Act
    var settingsPage = mainPage.NavigateToSettings();
    
    // Assert
    settingsPage.AssertDisplayed();
    settingsPage.TitleLabel.AssertTextEquals("Settings");
}
```

### Wait for Async Content Test

```csharp
[Fact]
public void LoadData_WhenComplete_ShowsResults()
{
    // Arrange
    var page = new DataPage(_context);
    page.WaitForDisplayed();
    
    // Act
    page.LoadButton.Click();
    
    // Assert - wait for async loading
    page.LoadingIndicator.WaitNotVisible(timeoutMs: 10000);
    page.ResultsList.AssertItemCount(expected: 5);
    page.ResultsList.AssertHasItem("Expected Item");
}
```

---

## Error Handling

### Expected Errors

```csharp
[Fact]
public void Form_InvalidInput_ShowsValidationError()
{
    // Arrange
    var page = new FormPage(_context);
    page.WaitForDisplayed();
    
    // Act - submit without required field
    page.SubmitButton.Click();
    
    // Assert - error message appears
    page.ErrorLabel.WaitVisible();
    page.ErrorLabel.AssertTextContains("required");
}
```

### Waiting for State Changes

```csharp
[Fact]
public void ProgressBar_WhenComplete_Shows100Percent()
{
    // Arrange
    var page = new ProgressPage(_context);
    page.WaitForDisplayed();
    
    // Act
    page.StartButton.Click();
    
    // Assert - wait for completion
    page.ProgressBar.WaitProgress(1.0, tolerance: 0.01, timeoutMs: 30000);
    page.StatusLabel.AssertTextEquals("Complete");
}
```

---

## Test Organization

### File Structure

```
MyApp.UITests/
├── Pages/                     # Page objects (MAUI)
│   ├── MainPage.cs
│   ├── SettingsPage.cs
│   └── LoginPage.cs
├── PageObjects/               # Page objects (Blazor)
│   ├── HomePage.cs
│   ├── CounterPage.cs
│   └── DashboardPage.cs
├── Tests/
│   ├── CounterTests.cs
│   ├── NavigationTests.cs
│   ├── FormTests.cs
│   └── SettingsTests.cs
├── TestBase/                  # Custom test base classes
│   └── CustomTestBase.cs
└── xunit.runner.json          # xUnit configuration
```

### Test Categories

Use traits to categorize tests:

```csharp
[Trait("Category", "Smoke")]
[Fact]
public void App_Launches_DisplaysMainPage()
{
    var page = new MainPage(_context);
    page.WaitForDisplayed();
    page.TitleLabel.AssertVisible();
}

[Trait("Category", "Regression")]
[Fact]
public void Counter_EdgeCase_HandlesNegativeValues()
{
    // ...
}

[Trait("Feature", "Authentication")]
[Fact]
public void Login_ValidCredentials_Succeeds()
{
    // ...
}
```

---

## Debugging Tips

### Take Screenshots

```csharp
[Fact]
public void DebugTest_TakeScreenshots()
{
    var page = new MainPage(_context);
    page.WaitForDisplayed();
    
    // Take screenshot at any point
    page.TakeScreenshot("before_action");
    
    page.SubmitButton.Click();
    
    page.TakeScreenshot("after_action");
}
```

### Check Element State

```csharp
[Fact]
public void DebugTest_CheckStates()
{
    var page = new MainPage(_context);
    page.WaitForDisplayed();
    
    // Debug: check what text is actually there
    var actualText = page.CounterLabel.GetText();
    Console.WriteLine($"Counter text: '{actualText}'");
    
    // Debug: check visibility
    var isVisible = page.SubmitButton.IsVisible();
    var isEnabled = page.SubmitButton.IsEnabled();
    Console.WriteLine($"Button visible: {isVisible}, enabled: {isEnabled}");
}
```

### Increase Timeout for Debugging

```csharp
[Fact]
public void SlowTest_IncreasedTimeout()
{
    var page = new MainPage(_context);
    
    // Use longer timeout for slow operations
    page.WaitForDisplayed(timeoutMs: 30000);
    
    page.LoadButton.Click();
    
    // Wait longer for slow loading
    page.Results.WaitVisible(timeoutMs: 60000);
}
```

---

## Version

- **Framework Version:** 1.0
- **Spec Reference:** SPEC-006 (ControlObject Framework)
