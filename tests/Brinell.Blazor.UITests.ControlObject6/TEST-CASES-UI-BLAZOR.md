# ControlObject6 Blazor UI Test Cases

**Component:** Brinell.Samples.Blazor.UITests.ControlObject6  
**Version:** POC 1.0  
**Created:** January 4, 2026

---

## Overview

These UI tests validate the async ControlObject6 POC implementation against a real Blazor application. They follow the existing test patterns in `Brinell.Samples.Blazor.UITests` and use the sample Blazor app with Playwright.

---

## 1. Test Infrastructure

### 1.1 Test Base Class

| File | Description |
|------|-------------|
| `BlazorTestBase6.cs` | Base class using BlazorTestContext from ControlObject6 |
| `BlazorTestFixture6.cs` | Collection fixture for browser lifecycle |

**Setup Requirements:**
- Blazor sample app running on `http://localhost:5180`
- Playwright browsers installed (`playwright install`)
- NET 10.0 SDK

---

## 2. Page Objects

### 2.1 CounterPage6

Uses async ControlObject6 interfaces and controls.

| Control | Type | Locator |
|---------|------|---------|
| CounterTitle | IAsyncControlObject | By.TestId("counter-title") |
| CountDisplay | IAsyncControlObject | By.TestId("count-display") |
| IncrementButton | IAsyncClickableControlObject | By.TestId("increment-btn") |
| ResetButton | IAsyncClickableControlObject | By.TestId("reset-btn") |

### 2.2 LoginPage6

| Control | Type | Locator |
|---------|------|---------|
| UsernameInput | IAsyncTextControlObject | By.TestId("username-input") |
| PasswordInput | IAsyncTextControlObject | By.TestId("password-input") |
| LoginButton | IAsyncClickableControlObject | By.TestId("login-btn") |
| ErrorMessage | IAsyncControlObject | By.TestId("error-message") |

### 2.3 HomePage6

| Control | Type | Locator |
|---------|------|---------|
| WelcomeTitle | IAsyncControlObject | By.Css("h1") |
| NavigationLinks | IAsyncControlObject | By.Css(".nav-link") |

---

## 3. Counter Tests

### 3.1 CounterTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BCT6-001 | Counter_InitialLoad_ShowsZeroCount | Page loads with count=0 | P0 |
| BCT6-002 | Counter_ClickIncrement_IncreasesCount | Click +1, verify count=1 | P0 |
| BCT6-003 | Counter_MultipleIncrements_CountsCorrectly | 5 clicks, verify count=5 | P0 |
| BCT6-004 | Counter_Reset_SetsCountToZero | Reset after increment | P0 |
| BCT6-005 | Counter_IncrementAfterReset_CountsFromZero | Reset then increment | P0 |
| BCT6-006 | Counter_ButtonsAreVisible_OnLoad | Verify UI elements visible | P1 |
| BCT6-007 | CounterTitle_AssertText_EqualsCounter | Title text verification | P1 |
| BCT6-008 | IncrementButton_IsEnabled_True | Button is enabled | P1 |

**Test Pattern (Async):**
```csharp
[Fact]
public async Task Counter_ClickIncrement_IncreasesCount()
{
    // Arrange
    await _counterPage.WaitExistsAsync(true);
    await _counterPage.CountDisplay.AssertTextContainsAsync("Current count: 0");

    // Act
    await _counterPage.IncrementButton.ClickAsync();

    // Assert
    await _counterPage.CountDisplay.AssertTextContainsAsync("Current count: 1");
}
```

---

## 4. Login Tests

### 4.1 LoginTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BLT6-001 | Login_ValidCredentials_NavigatesToDashboard | Successful login | P0 |
| BLT6-002 | Login_InvalidCredentials_ShowsError | Failed login shows error | P0 |
| BLT6-003 | Login_EmptyUsername_ShowsValidation | Empty username validation | P0 |
| BLT6-004 | Login_EmptyPassword_ShowsValidation | Empty password validation | P0 |
| BLT6-005 | UsernameInput_Enter_ShowsValue | Text input works | P0 |
| BLT6-006 | PasswordInput_Enter_MasksValue | Password masking | P1 |
| BLT6-007 | UsernameInput_Clear_RemovesText | Clear text | P0 |
| BLT6-008 | UsernameInput_ClearAndEnter_ReplacesText | Replace text | P0 |
| BLT6-009 | UsernameInput_Append_AddsToExisting | Append text | P1 |
| BLT6-010 | UsernameInput_Focus_IsFocused | Focus works | P1 |
| BLT6-011 | UsernameInput_Blur_RemovesFocus | Blur works | P1 |
| BLT6-012 | LoginButton_Click_SubmitsForm | Button click | P0 |

**Test Pattern (Async):**
```csharp
[Fact]
public async Task Login_ValidCredentials_NavigatesToDashboard()
{
    // Arrange
    await _loginPage.WaitExistsAsync(true);

    // Act
    await _loginPage.UsernameInput.EnterAsync("testuser");
    await _loginPage.PasswordInput.EnterAsync("password123");
    await _loginPage.LoginButton.ClickAsync();

    // Assert
    var dashboard = new DashboardPage6(_context);
    await dashboard.WaitExistsAsync(true);
    await dashboard.WelcomeMessage.AssertTextContainsAsync("Welcome");
}
```

---

## 5. Navigation Tests

### 5.1 NavigationTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BNT6-001 | Navigation_ToCounter_LoadsCounterPage | Navigate to /counter | P0 |
| BNT6-002 | Navigation_ToHome_LoadsHomePage | Navigate to / | P0 |
| BNT6-003 | Navigation_NavLink_Click_Navigates | Click nav link | P1 |
| BNT6-004 | Page_WaitLoaded_WaitsForContent | Wait for page load | P0 |
| BNT6-005 | Page_IsLoaded_ReturnsTrue | Page loaded check | P0 |
| BNT6-006 | Page_GetTitle_ReturnsTitle | Page title | P1 |

---

## 6. Control State Tests (Async)

### 6.1 ControlStateTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BCS6-001 | Control_IsExistsAsync_ReturnsTrue | Existing control | P0 |
| BCS6-002 | Control_IsExistsAsync_ReturnsFalse | Non-existent control | P0 |
| BCS6-003 | Control_WaitExistsAsync_WaitsForElement | Wait for dynamic element | P0 |
| BCS6-004 | Control_CheckExistsAsync_ThrowsOnTimeout | Timeout exception | P0 |
| BCS6-005 | Control_AssertExistsAsync_ThrowsOnFailure | Assertion exception | P0 |
| BCS6-006 | Control_IsVisibleAsync_ReturnsTrue | Visible element | P0 |
| BCS6-007 | Control_IsVisibleAsync_ReturnsFalse | Hidden element | P0 |
| BCS6-008 | Control_WaitVisibleAsync_WaitsForState | Wait for visibility | P1 |
| BCS6-009 | Control_IsEnabledAsync_ReturnsTrue | Enabled element | P0 |
| BCS6-010 | Control_IsEnabledAsync_ReturnsFalse | Disabled element | P0 |
| BCS6-011 | Control_GetTextAsync_ReturnsText | Get text content | P0 |
| BCS6-012 | Control_AssertTextAsync_Passes | Exact text match | P0 |
| BCS6-013 | Control_AssertTextContainsAsync_Passes | Contains substring | P0 |
| BCS6-014 | Control_AssertTextStartsWithAsync_Passes | Starts with | P1 |
| BCS6-015 | Control_AssertTextEndsWithAsync_Passes | Ends with | P1 |
| BCS6-016 | Control_AssertTextMatchesAsync_Passes | Regex match | P1 |
| BCS6-017 | Control_AssertTextEmptyAsync_Passes | Empty check | P1 |

**Test Pattern (Async):**
```csharp
[Fact]
public async Task Control_IsExistsAsync_ReturnsTrue()
{
    // Arrange
    await _homePage.WaitExistsAsync(true);

    // Act
    var exists = await _homePage.WelcomeTitle.IsExistsAsync();

    // Assert
    exists.Should().BeTrue();
}
```

---

## 7. Click Operation Tests (Async)

### 7.1 ClickTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BCK6-001 | Button_ClickAsync_TriggersAction | Single click | P0 |
| BCK6-002 | Button_DoubleClickAsync_TriggersAction | Double click | P1 |
| BCK6-003 | Button_RightClickAsync_TriggersAction | Context click | P2 |
| BCK6-004 | Button_HoverAsync_TriggersHover | Mouse hover | P2 |
| BCK6-005 | Button_ClickAsync_WaitsForEnabled | Wait for enabled | P0 |
| BCK6-006 | Button_ClickAsync_WaitsForVisible | Wait for visible | P0 |

---

## 8. Text Input Tests (Async)

### 8.1 TextInputTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BTI6-001 | Input_EnterAsync_SetsValue | Enter text | P0 |
| BTI6-002 | Input_ClearAsync_RemovesText | Clear text | P0 |
| BTI6-003 | Input_ClearAndEnterAsync_ReplacesText | Replace text | P0 |
| BTI6-004 | Input_AppendAsync_AddsToExisting | Append text | P1 |
| BTI6-005 | Input_FocusAsync_SetsFocus | Focus input | P1 |
| BTI6-006 | Input_BlurAsync_RemovesFocus | Blur input | P1 |
| BTI6-007 | Input_IsFocusedAsync_ReturnsState | Check focus state | P1 |
| BTI6-008 | Input_IsReadOnlyAsync_ReturnsTrue | Read-only check | P1 |
| BTI6-009 | Input_GetTextLengthAsync_ReturnsLength | Text length | P1 |
| BTI6-010 | Input_GetTextAsync_ReturnsValue | Get input value | P0 |
| BTI6-011 | Input_AssertTextLengthAsync_Passes | Length assertion | P1 |

---

## 9. Page Object Tests (Async)

### 9.1 PageObjectTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BPO6-001 | Page_IsLoadedAsync_ReturnsTrue | Page loaded | P0 |
| BPO6-002 | Page_WaitLoadedAsync_WaitsForPage | Wait for load | P0 |
| BPO6-003 | Page_GetControl_ReturnsControl | Get control | P0 |
| BPO6-004 | Page_TryGetControlAsync_ReturnsNull | Missing control | P1 |
| BPO6-005 | Page_ControlExistsAsync_ReturnsTrue | Control exists | P0 |
| BPO6-006 | Page_ControlExistsAsync_ReturnsFalse | Missing control | P0 |
| BPO6-007 | Page_TakeScreenshotAsync_SavesFile | Screenshot | P2 |
| BPO6-008 | Page_ScrollToControlAsync_Scrolls | Scroll to element | P2 |
| BPO6-009 | Page_GetTitleAsync_ReturnsTitle | Get page title | P1 |
| BPO6-010 | Page_AssertTitleAsync_Passes | Title assertion | P1 |

---

## 10. Test Context Tests (Async)

### 10.1 TestContextTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BTC6-001 | Context_DefaultTimeout_Is30Seconds | Default timeout | P1 |
| BTC6-002 | Context_CreateControl_ReturnsControl | Control factory | P0 |
| BTC6-003 | Context_NavigateToAsync_Navigates | Navigation | P0 |
| BTC6-004 | Context_Log_WritesToOutput | Logging | P2 |
| BTC6-005 | Context_TakeScreenshotAsync_SavesFile | Screenshot | P2 |

---

## 11. Locator Conversion Tests

### 11.1 LocatorTests6.cs

| ID | Test Name | Description | Priority |
|----|-----------|-------------|----------|
| BLC6-001 | TestId_Locator_FindsElement | data-testid selector | P0 |
| BLC6-002 | Css_Locator_FindsElement | CSS selector | P0 |
| BLC6-003 | Id_Locator_FindsElement | #id selector | P0 |
| BLC6-004 | ClassName_Locator_FindsElement | .class selector | P1 |
| BLC6-005 | XPath_Locator_FindsElement | XPath selector | P1 |
| BLC6-006 | Text_Locator_FindsElement | Text locator | P1 |
| BLC6-007 | Label_Locator_FindsElement | Label locator | P1 |
| BLC6-008 | Placeholder_Locator_FindsElement | Placeholder locator | P1 |
| BLC6-009 | Role_Locator_FindsElement | Role locator | P2 |

---

## 12. Test Priority Summary

| Priority | Description | Count |
|----------|-------------|-------|
| P0 (Critical) | Core functionality | 42 |
| P1 (High) | Important features | 28 |
| P2 (Medium) | Nice-to-have | 9 |
| **Total** | | **79** |

---

## 13. Test Dependencies

- Running Blazor sample app on localhost:5180
- Playwright installed (`dotnet tool install playwright`)
- Playwright browsers (`playwright install chromium`)
- NET 10.0 SDK

---

## 14. Test Execution

```powershell
# Install Playwright
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium

# Start Blazor app
cd samples/Brinell.Samples.Blazor.App
dotnet run &

# Wait for app to start
Start-Sleep -Seconds 5

# Run tests
cd samples/Brinell.Samples.Blazor.UITests.ControlObject6
dotnet test

# Or with headless mode
$env:HEADLESS = "true"
dotnet test
```

---

## 15. Async Test Considerations

- All test methods use `async Task` return type
- Use `await` for all async operations
- Blazor uses SignalR for updates - wait for state changes
- Use `WaitExistsAsync(true)` after navigation
- Use `WaitForCount()` pattern for polling state changes
