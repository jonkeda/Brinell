# Issue 022: ButtonControlTests failing - elements not found after Appium connection

## Status: Resolved
## Date: 2026-01-15
## Version: Current (net10.0)

## Summary

All 12 ButtonControlTests failed with 'Element was not clickable' or 'IsExists() = false' errors despite the Appium server running successfully on port 4723 and the MAUI sample app executable being present and launchable. The root cause was that the Windows Appium driver does NOT support the `GET /timeouts` API - it only supports `SET /timeouts`. When `TryFindElement()` tried to save the current implicit wait timeout before setting it to zero, it threw an `UnknownMethodException` which was caught silently by the `catch (WebDriverException)` block, returning `null` and never actually attempting to find the element.

## Symptoms

1. Tests fail with `System.TimeoutException : Element was not clickable within 10000ms. Locator: AutomationId:ResetButton`
2. Tests fail with `Expected Page.IncrementButton.IsExists() to be true, but found False`
3. Tests fail with `Expected Page.IncrementButton.IsVisible() to be true, but found <null>` (element not found)
4. The MAUI app launches manually without issues and contains the expected AutomationIds in XAML
5. Appium server responds with `{"value":{"ready":true,...}}` confirming it's running
6. **Appium debug logs showed NO `/element` find requests were ever made** - only session creation/deletion

## Evidence

### Error Messages

```
System.TimeoutException : Element was not clickable within 10000ms. Locator: AutomationId:ResetButton
   at Brinell.Maui.Controls.MauiButtonControl`1.CheckClickable(Nullable`1 timeoutMs)
   at Brinell.Maui.Controls.MauiButtonControl`1.<Click>b__2_0()
   at Brinell.Maui.Controls.MauiControlBase`1.Run[T](String action, T value, Action operation)
```

```
Expected Page.IncrementButton.IsExists() to be true, but found False.
   at Brinell.Maui.UITests.Tests.ButtonControlTests.Button_IsExists_ReturnsTrue()
```

### The Critical Finding

Appium debug logs during test execution showed:
```
POST /session (success - app launches)
POST /timeouts {"implicit":5000}
GET /timeouts <-- NO MORE REQUESTS
DELETE /session
```

No `/element` or `/elements` requests were ever made, despite `IsExists()` being called. This indicated an exception was occurring before `FindElement()` was ever called.

### Steps to Reproduce

1. Start Appium server: `Start-Process cmd.exe -ArgumentList "/c","appium --address 127.0.0.1 --port 4723 --relaxed-security"`
2. Verify Appium is running: `(Invoke-WebRequest -Uri "http://127.0.0.1:4723/status" -UseBasicParsing).Content`
3. Run tests: `dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~ButtonControlTests" --no-build`
4. Expected: Tests pass, finding buttons and clicking them
5. Actual (before fix): All tests fail - elements not found

## Environment

- **Version**: .NET 10.0, Appium 3.1.2, Windows driver
- **OS**: Windows 11
- **App Path**: `samples/Brinell.Samples.Maui.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Brinell.Samples.Maui.App.exe`
- **Appium**: Running on 127.0.0.1:4723

## Root Cause Analysis

### Investigation Findings

1. Created step-by-step debug test that logged each operation
2. Discovered exception at `Timeouts.get_ImplicitWait()`:
   ```
   UnknownMethodException: The requested command matched a known URL but did not match an method for that URL.
   Stack: at OpenQA.Selenium.Timeouts.get_ImplicitWait()
   ```
3. Windows Driver API limitation: supports `SET /timeouts` but NOT `GET /timeouts`

### Hypotheses Tested

| Hypothesis | Result |
|------------|--------|
| Appium server not running | Rejected - server responds with ready:true |
| App executable missing | Rejected - file exists, last build 2026-01-15 12:59:04 |
| AutomationIds missing in XAML | Rejected - MainPage.xaml contains correct AutomationIds |
| Windows driver not finding app window | Rejected - app launches and is visible |
| App not launching via Appium | Rejected - debug test showed app launches |
| Element search scope incorrect | Rejected - correct AutomationId locator strategy |
| **Exception being silently caught** | **CONFIRMED - get_ImplicitWait() throws** |

### Root Cause

In `MauiTestContext.TryFindElement()`, the code tried to save the current implicit wait timeout before setting it to zero:

```csharp
// BEFORE (broken):
var originalTimeout = _rawDriver.Manage().Timeouts().ImplicitWait;  // THROWS on Windows!
_rawDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
```

The `Timeouts.ImplicitWait` getter calls `GET /session/{id}/timeouts` which Windows Driver doesn't implement. This threw `UnknownMethodException` (a subclass of `WebDriverException`) which was caught by the `catch (WebDriverException) { return null; }` block, preventing any element search from ever occurring.

### Affected Components

- `srcnew/Brinell.Maui/Context/MauiTestContext.cs` - Root cause

## Solution

### Approach

Instead of reading the implicit wait from the driver (which Windows doesn't support), use the stored timeout value from `_timeouts.ElementFind` which we already have from the options.

### Implementation

```csharp
// AFTER (fixed):
var originalTimeoutMs = _timeouts.ElementFind;  // Use stored value instead of reading from driver
_rawDriver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
// ... find element ...
_rawDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(originalTimeoutMs);
```

### Files Modified

| File | Change |
|------|--------|
| `srcnew/Brinell.Maui/Context/MauiTestContext.cs` | Changed `TryFindElement()` to use stored `_timeouts.ElementFind` instead of reading `ImplicitWait` from driver |

## Verification

### Test Steps

1. Run single test: `dotnet test --filter "Button_IsExists_ReturnsTrue"` ✅ Passed
2. Run all tests: `dotnet test --filter "ButtonControlTests"` ✅ All 12 passed

### Verified In

- [x] Development environment
- [x] All ButtonControlTests pass (12/12)

## Related Issues

- [Issue 021: UI Tests Element Locator Mismatch](./021-ui-tests-element-locator-mismatch.isu.spx.md)

## Learnings

1. **Silent exception catching is dangerous**: The `catch (WebDriverException) { return null; }` pattern hid the real error. Consider logging or only catching specific expected exceptions.

2. **Appium driver capabilities vary by platform**: Windows Driver doesn't support all W3C WebDriver commands. Always test with actual platform drivers, not just mocks.

3. **Debug logging is essential**: Appium debug logging (`--log-level debug`) was critical to discovering that NO element find requests were being made.

4. **Step-by-step debugging tests**: Creating a test that logs each step helped pinpoint exactly where the failure occurred.

## Resolution

**Fixed in version**: Current
**Resolution date**: 2026-01-15
