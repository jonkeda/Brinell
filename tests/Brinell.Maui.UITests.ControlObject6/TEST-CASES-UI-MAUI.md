# ControlObject6 MAUI UI Test Cases

**Component:** Brinell.Samples.Maui.UITests.ControlObject6  
**Version:** POC 1.0  
**Created:** January 4, 2026

---

## Overview

These UI tests validate the ControlObject6 POC implementation against a real MAUI application. They follow the existing test patterns in `Brinell.Samples.Maui.UITests` and use the sample MAUI app.

---

## 1. Test Infrastructure

### 1.1 Test Base Class

| File | Description |
|------|-------------|
| `MauiTestBase6.cs` | Base class using MauiTestContext from ControlObject6 |

**Setup Requirements:**
- Appium server running on `http://127.0.0.1:4723`
- WinAppDriver or Windows Application Driver
- Built MAUI sample app

---

## 2. Page Objects

### 2.1 MainPageObject6

Uses ControlObject6 interfaces and controls.

| Control | Type | Locator |
|---------|------|---------|
| TitleLabel | IControlObject | By.AutomationId("TitleLabel") |
| CounterLabel | IControlObject | By.AutomationId("CounterLabel") |
| IncrementButton | IClickableControlObject | By.AutomationId("IncrementButton") |
| DecrementButton | IClickableControlObject | By.AutomationId("DecrementButton") |
| ResetButton | IClickableControlObject | By.AutomationId("ResetButton") |
| NameEntry | ITextControlObject | By.AutomationId("NameEntry") |
| EmailEntry | ITextControlObject | By.AutomationId("EmailEntry") |
| MessageEditor | ITextControlObject | By.AutomationId("MessageEditor") |
| GreetingLabel | IControlObject | By.AutomationId("GreetingLabel") |
| GreetButton | IClickableControlObject | By.AutomationId("GreetButton") |

---

## 3. Counter Tests

### 3.1 CounterTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CT6-001 | Counter_InitialValue_IsZero | Verify counter starts at 0 | P0 |
| CT6-002 | Counter_Increment_IncreasesValue | Click increment, verify counter=1 | P0 |
| CT6-003 | Counter_Decrement_DecreasesValue | Click decrement, verify counter=-1 | P0 |
| CT6-004 | Counter_MultipleIncrements_ShowsCorrectValue | 3 clicks, verify counter=3 | P0 |
| CT6-005 | Counter_Reset_SetsToZero | Increment then reset, verify counter=0 | P0 |
| CT6-006 | IncrementButton_IsVisible_OnLoad | Verify button exists and is visible | P1 |
| CT6-007 | IncrementButton_IsEnabled_OnLoad | Verify button is enabled | P1 |
| CT6-008 | CounterLabel_AssertTextContains_Counter | Text contains "Counter" | P1 |

**Test Pattern:**
```csharp
[Fact]
public void Counter_Increment_IncreasesValue()
{
    // Arrange
    _mainPage.WaitExists(true);
    _mainPage.CounterLabel.AssertTextContains("Counter: 0");

    // Act
    _mainPage.IncrementButton.Click();

    // Assert
    _mainPage.CounterLabel.AssertTextContains("Counter: 1");
}
```

---

## 4. Text Input Tests

### 4.1 TextInputTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| TI6-001 | NameEntry_Enter_ShowsValue | Enter text, verify value | P0 |
| TI6-002 | NameEntry_Clear_RemovesText | Clear text, verify empty | P0 |
| TI6-003 | NameEntry_ClearAndEnter_ReplacesText | Replace text in one call | P0 |
| TI6-004 | NameEntry_Append_AddsToExisting | Append to existing text | P1 |
| TI6-005 | EmailEntry_Enter_ShowsValue | Enter email, verify value | P0 |
| TI6-006 | NameEntry_IsEnabled_True | Verify entry is enabled | P1 |
| TI6-007 | NameEntry_Focus_IsFocused | Focus entry, verify focused | P1 |
| TI6-008 | NameEntry_Blur_IsNotFocused | Blur entry, verify not focused | P1 |
| TI6-009 | GreetButton_WithName_ShowsGreeting | Enter name, click greet | P0 |
| TI6-010 | GreetButton_WithoutName_ShowsError | Empty name, click greet | P0 |
| TI6-011 | MessageEditor_EnterMultiline_ShowsValue | Enter multiline text | P1 |
| TI6-012 | NameEntry_GetTextLength_ReturnsCorrect | Verify text length | P1 |
| TI6-013 | NameEntry_AssertTextEquals_Passes | Assert exact match | P0 |
| TI6-014 | NameEntry_AssertTextStartsWith_Passes | Assert prefix match | P1 |
| TI6-015 | NameEntry_AssertTextEndsWith_Passes | Assert suffix match | P1 |

**Test Pattern:**
```csharp
[Fact]
public void NameEntry_Enter_ShowsValue()
{
    // Arrange
    _mainPage.WaitExists(true);

    // Act
    _mainPage.NameEntry.Enter("John Doe");

    // Assert
    _mainPage.NameEntry.AssertTextEquals("John Doe");
}
```

---

## 5. Control State Tests

### 5.1 ControlStateTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CS6-001 | Control_IsExists_ReturnsTrue | Verify control exists | P0 |
| CS6-002 | Control_IsExists_ReturnsFalse | Non-existent control returns false | P0 |
| CS6-003 | Control_WaitExists_WaitsForElement | Wait for dynamic element | P0 |
| CS6-004 | Control_CheckExists_ThrowsOnTimeout | Timeout throws UITestTimeoutException | P0 |
| CS6-005 | Control_AssertExists_ThrowsOnFailure | Missing element throws AssertionException | P0 |
| CS6-006 | Control_IsVisible_ReturnsTrue | Visible control returns true | P0 |
| CS6-007 | Control_IsVisible_ReturnsFalse | Hidden control returns false | P0 |
| CS6-008 | Control_WaitVisible_WaitsForElement | Wait for visibility change | P1 |
| CS6-009 | Control_IsEnabled_ReturnsTrue | Enabled control returns true | P0 |
| CS6-010 | Control_IsEnabled_ReturnsFalse | Disabled control returns false | P0 |
| CS6-011 | Control_GetText_ReturnsText | Get text content | P0 |
| CS6-012 | Control_AssertTextContains_Passes | Text contains substring | P0 |
| CS6-013 | Control_AssertTextMatches_Passes | Text matches regex | P1 |
| CS6-014 | Control_AssertTextEmpty_Passes | Empty text verification | P1 |

**Test Pattern:**
```csharp
[Fact]
public void Control_IsExists_ReturnsTrue()
{
    // Arrange
    _mainPage.WaitExists(true);

    // Act
    var exists = _mainPage.TitleLabel.IsExists();

    // Assert
    exists.Should().BeTrue();
}
```

---

## 6. Click Operation Tests

### 6.1 ClickTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| CK6-001 | Button_Click_TriggersAction | Single click works | P0 |
| CK6-002 | Button_DoubleClick_TriggersAction | Double click works | P1 |
| CK6-003 | Button_RightClick_TriggersAction | Context click works | P2 |
| CK6-004 | Button_Hover_TriggersHover | Mouse hover works | P2 |
| CK6-005 | Button_LongPress_TriggersAction | Long press 500ms works | P1 |
| CK6-006 | Button_Click_WaitsForEnabled | Click waits for enabled state | P0 |
| CK6-007 | Button_Click_WaitsForVisible | Click waits for visible state | P0 |

**Test Pattern:**
```csharp
[Fact]
public void Button_Click_TriggersAction()
{
    // Arrange
    _mainPage.WaitExists(true);
    _mainPage.CounterLabel.AssertTextContains("Counter: 0");

    // Act
    _mainPage.IncrementButton.Click();

    // Assert
    _mainPage.CounterLabel.AssertTextContains("Counter: 1");
}
```

---

## 7. Page Object Tests

### 7.1 PageObjectTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| PO6-001 | Page_IsLoaded_ReturnsTrue | Page loaded detection | P0 |
| PO6-002 | Page_WaitLoaded_WaitsForPage | Wait for page load | P0 |
| PO6-003 | Page_GetControl_ReturnsControl | Get control by locator | P0 |
| PO6-004 | Page_TryGetControl_ReturnsNull | Missing control returns null | P1 |
| PO6-005 | Page_ControlExists_ReturnsTrue | Control exists check | P0 |
| PO6-006 | Page_ControlExists_ReturnsFalse | Missing control check | P0 |
| PO6-007 | Page_TakeScreenshot_SavesFile | Screenshot capture | P2 |

---

## 8. Test Context Tests

### 8.1 TestContextTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| TC6-001 | Context_DefaultTimeout_Is30Seconds | Default timeout value | P1 |
| TC6-002 | Context_CreateControl_ReturnsControl | Control factory works | P0 |
| TC6-003 | Context_Log_WritesToOutput | Logging works | P2 |
| TC6-004 | Context_TakeScreenshot_SavesFile | Screenshot works | P2 |

---

## 9. Test Priority Summary

| Priority | Description | Count |
|----------|-------------|-------|
| P0 (Critical) | Core functionality | 28 |
| P1 (High) | Important features | 17 |
| P2 (Medium) | Nice-to-have | 6 |
| **Total** | | **51** |

---

## 10. Test Dependencies

- Running MAUI sample app
- Appium server on localhost:4723
- WinAppDriver installed
- NET 10.0 SDK

---

## 11. Test Execution

```powershell
# Start Appium server
appium --allow-insecure chromedriver_autodownload

# Build MAUI app
cd samples/Brinell.Samples.Maui.App
dotnet build -c Debug

# Start MAUI app manually or let tests start it
& ".\bin\Debug\net10.0-windows10.0.19041.0\win-x64\Brinell.Samples.Maui.App.exe"

# Run tests
cd samples/Brinell.Samples.Maui.UITests.ControlObject6
dotnet test
```
