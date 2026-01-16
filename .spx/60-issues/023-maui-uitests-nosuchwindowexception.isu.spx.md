# Issue 023: MAUI UITests NoSuchWindowException - session management failure

## Status: Resolved
## Date: 2026-01-15
## Version: srcnew/Brinell.Maui

## Summary

Running the Brinell.Maui.UITests from testsnew results in 28 out of 30 tests failing. The primary error is `OpenQA.Selenium.NoSuchWindowException: Currently selected window has been closed`, indicating the MAUI app window closes unexpectedly between or during tests. This is a test isolation/session management issue where the Appium driver loses connection to the application window.

## Symptoms

1. First few tests pass, subsequent tests fail with NoSuchWindowException
2. Tests fail with "Currently selected window has been closed" error
3. Some tests fail with ElementNotFoundException after 10000ms timeout
4. Tests fail with TimeoutException "Element was not clickable within 10000ms"
5. Only 2 out of 30 tests pass

## Evidence

### Error Messages

```
OpenQA.Selenium.NoSuchWindowException : Currently selected window has been closed
   at OpenQA.Selenium.WebDriver.UnpackAndThrowOnError(Response errorResponse, String commandToExecute)
   at OpenQA.Selenium.WebDriver.ExecuteAsync(String driverCommandToExecute, Dictionary`2 parameters)
   at OpenQA.Selenium.WebDriver.FindElement(By by)
   at OpenQA.Selenium.Appium.AppiumDriver.FindElement(By by)
   at Brinell.Maui.Context.MauiTestContext.TryFindElement(Locator locator)
```

```
Brinell.Core.Exceptions.ElementNotFoundException : Element not found within 10000ms. Locator: AutomationId:NameEntry
```

```
System.TimeoutException : Element was not clickable within 10000ms. Locator: AutomationId:ResetButton
```

### Console/Log Output

```
Test summary: total: 30, failed: 28, succeeded: 2, skipped: 0, duration: 196.4s
Build failed with 28 error(s) in 199.3s
```

### Steps to Reproduce

1. Start Appium server: `appium --address 127.0.0.1 --port 4723 --relaxed-security`
2. Build the MAUI app: `dotnet build samples/Brinell.Samples.Maui.App -f net10.0-windows10.0.19041.0`
3. Navigate to test project: `cd testsnew/Brinell.Maui.UITests`
4. Run tests: `dotnet test --verbosity normal`
5. Observe: First ~2 tests pass, then remaining tests fail with NoSuchWindowException

## Environment

- **Version**: srcnew/Brinell.Maui (net10.0)
- **OS**: Windows 10/11
- **Appium**: 3.1.2
- **Related Config**: Windows driver, Appium on 127.0.0.1:4723

## Root Cause Analysis

### Investigation Findings

**DebugTest.cs is causing session interference.**

The test project uses a shared xUnit collection fixture pattern:
- `AppiumCollection.cs` defines `[CollectionDefinition("Appium")]` with `ICollectionFixture<AppiumFixture>`
- `AppiumFixture.cs` creates and manages a shared `MauiTestContext` for all tests
- `ButtonControlTests.cs` and `EntryControlTests.cs` correctly use `[Collection("Appium")]`

However, `DebugTest.cs` does NOT use the `[Collection("Appium")]` attribute. Instead, it:
1. Creates its own `MauiTestContext` with a hardcoded app path
2. Runs in the middle of the test execution (alphabetically between Button and Entry tests)
3. Disposes its context when done, calling `_rawDriver?.Quit()` which terminates the app

When xUnit runs tests:
1. ButtonControlTests run first (12 tests) - uses shared fixture, app stays open ✓
2. DebugTest runs (1 test) - creates own context, then disposes → **closes the app**
3. EntryControlTests run (17 tests) - shared fixture's driver now points to closed window ✗

### Test Results Comparison

| Test Run | Passed | Failed | Filter Used |
|----------|--------|--------|-------------|
| All tests | 2 | 28 | None |
| Without DebugTest | 23 | 6 | `FullyQualifiedName!~DebugTest` |

**Excluding DebugTest improved pass rate from 7% to 79%.**

### Hypotheses Tested

| Hypothesis | Result |
|------------|--------|
| App crashes during test execution | ❌ Not the cause |
| Test fixture not properly managing app lifecycle | ❌ Fixture is correct |
| DebugTest interfering with shared fixture | ✅ **CONFIRMED** |
| App closes after DebugTest and shared session invalid | ✅ **CONFIRMED** |
| Race condition in test setup/teardown | ❌ Not the cause |

### Root Cause

**DebugTest.cs lacks `[Collection("Appium")]` attribute**, causing it to create and dispose its own MauiTestContext. This terminates the MAUI app, invalidating the shared AppiumFixture's driver session for subsequent tests.

### Affected Components

- `testsnew/Brinell.Maui.UITests/` - Test project
- `srcnew/Brinell.Maui/Context/MauiTestContext.cs` - Context/session management
- Test base classes managing app lifecycle

## Solution

### Approach

**Option A (Recommended):** Delete `DebugTest.cs` - it's a debug/diagnostic test that shouldn't be in the test suite.

**Option B:** Add `[Collection("Appium")]` attribute to DebugTest class and inject the shared fixture instead of creating its own context.

### Implementation

Delete `testsnew/Brinell.Maui.UITests/Tests/DebugTest.cs`

### Files Modified

| File | Change |
|------|--------|
| `testsnew/Brinell.Maui.UITests/Tests/DebugTest.cs` | Deleted |

## Verification

### Test Steps

1. Run all MAUI UI tests
2. Verify NoSuchWindowException errors are eliminated
3. Verify pass rate improved from 7% to 79%

### Test Results After Fix

```
Failed!  - Failed:     6, Passed:    23, Skipped:     0, Total:    29, Duration: 2 m 9 s
```

**NoSuchWindowException is eliminated.** The remaining 6 failures are separate issues:
- `GetPlaceholder` returns null (placeholder API issue)
- `GreetButton` not clickable within timeout (UI responsiveness/test timing issue)
- These should be tracked as separate issues

### Verified In

- [x] Development environment
- [ ] CI pipeline

## Related Issues

- [Issue 021: UI Tests Element Locator Mismatch](./021-ui-tests-element-locator-mismatch.isu.spx.md)
- [Issue 022: ButtonControlTests Elements Not Found](./022-buttoncontroltests-elements-not-found.isu.spx.md)

## Learnings

1. **xUnit Collection Fixtures Require Consistency**: All test classes sharing a fixture MUST use the `[Collection("Name")]` attribute. A single class without the attribute can interfere with the shared session.

2. **Debug/Diagnostic Tests Are Dangerous**: Test files with names like "DebugTest" that were created for one-off debugging should be immediately deleted or properly integrated. They often create their own resources and dispose them, breaking shared fixtures.

3. **Test Execution Order Matters**: xUnit runs test classes alphabetically by default. DebugTest ran between ButtonControlTests and EntryControlTests, causing subsequent tests to fail.

4. **Session Management is Critical**: In UI automation frameworks, the driver session is a shared resource. Any test that creates and disposes its own session will terminate the app for all other tests.

## Resolution

**Fixed in version**: srcnew/Brinell.Maui
**Resolution date**: 2026-01-15
**Fix**: Deleted `testsnew/Brinell.Maui.UITests/Tests/DebugTest.cs` which was creating its own MauiTestContext without using the shared `[Collection("Appium")]` fixture pattern.
