# SPX-ISSUE-001: UI Tests Failing - WinAppDriver Crash Due to Multiple App Instances

**Status:** Open  
**Priority:** High  
**Created:** 2025-01-14  
**Author:** Copilot  
**Component:** Brinell.Maui.UITests

---

## 1. Summary

UI tests in `Brinell.Maui.UITests` are failing with 28 out of 44 tests failing. The root cause appears to be WinAppDriver crashing due to multiple app instances being launched simultaneously.

---

## 2. Test Results

| Metric | Count |
|--------|-------|
| **Total** | 44 |
| **Succeeded** | 15 |
| **Failed** | 28 |
| **Skipped** | 1 |
| **Duration** | 195.8s |

---

## 3. Error Details

### Primary Exception

```
OpenQA.Selenium.UnknownErrorException : 'POST /session/{session-id}/timeouts' 
cannot be proxied to WinAppDriver server because its process is not running 
(probably crashed). Check the Appium log for more details
```

### Stack Trace

```
at OpenQA.Selenium.WebDriver.UnpackAndThrowOnError(Response errorResponse, String commandToExecute)
at OpenQA.Selenium.WebDriver.ExecuteAsync(String driverCommandToExecute, Dictionary`2 parameters)
at OpenQA.Selenium.WebDriver.Execute(String driverCommandToExecute, Dictionary`2 parameters)
```

### Affected Test Classes

- `ButtonControlTests`
- `EntryControlTests`
- (Likely all test classes using `AppiumFixture`)

---

## 4. Root Cause Analysis

### Symptoms Observed

1. Multiple app instances appear to start simultaneously
2. WinAppDriver process crashes mid-test execution
3. Subsequent tests fail because WinAppDriver is no longer running
4. First ~15 tests pass before the crash occurs

### Likely Causes

1. **Test Parallelization Issue**: xUnit may be running test classes in parallel, causing multiple Appium sessions to conflict
2. **Fixture Lifecycle Problem**: `AppiumFixture` may not be properly managing app instance lifecycle
3. **Missing Test Collection Isolation**: Tests may need explicit serialization to prevent concurrent WinAppDriver access
4. **Resource Cleanup**: Previous test runs may leave orphaned app processes

---

## 5. Affected Files

| File | Purpose |
|------|---------|
| `testsnew/Brinell.Maui.UITests/Tests/ButtonControlTests.cs` | Button control tests |
| `testsnew/Brinell.Maui.UITests/Tests/EntryControlTests.cs` | Entry control tests |
| `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` | Test fixture managing Appium driver |
| `testsnew/Brinell.Maui.UITests/xunit.runner.json` | xUnit runner configuration |

---

## 6. Proposed Solutions

### Option A: Disable Test Parallelization (Quick Fix)

Add or update `xunit.runner.json` to disable parallel execution:

```json
{
  "parallelizeAssembly": false,
  "parallelizeTestCollections": false
}
```

### Option B: Ensure Single Collection for All Appium Tests

All test classes using `[Collection("Appium")]` should share a single driver instance:

```csharp
[CollectionDefinition("Appium")]
public class AppiumCollection : ICollectionFixture<AppiumFixture>
{
}
```

### Option C: Implement Robust Driver Management

1. Add process cleanup before starting new sessions
2. Implement retry logic for driver initialization
3. Add proper disposal of WinAppDriver processes on fixture teardown

### Option D: Sequential Test Execution Attribute

Use `[assembly: CollectionBehavior(DisableTestParallelization = true)]` in `AssemblyInfo.cs`.

---

## 7. Reproduction Steps

1. Open solution in Visual Studio
2. Run all tests with `dotnet test`
3. Observe that first ~15 tests pass
4. Subsequent tests fail with WinAppDriver crash error

---

## 8. Investigation Tasks

- [ ] Check `AppiumFixture` implementation for proper session management
- [ ] Verify `xunit.runner.json` parallelization settings
- [ ] Check if `[Collection("Appium")]` collection definition exists
- [ ] Review Appium server logs for crash details
- [ ] Test with parallelization explicitly disabled
- [ ] Verify single app instance is launched per test run

---

## 9. Environment

- **.NET Version:** .NET 10
- **C# Version:** 14.0
- **Test Framework:** xUnit 3.x
- **Automation:** Appium with WinAppDriver
- **Platform:** Windows (MAUI)

---

## 10. Related Documentation

- xUnit Test Collections: https://xunit.net/docs/running-tests-in-parallel
- Appium WinAppDriver: https://github.com/microsoft/WinAppDriver

---

**Next Steps:** Investigate `AppiumFixture` implementation and xUnit configuration to determine exact cause of multiple app instance launches.
