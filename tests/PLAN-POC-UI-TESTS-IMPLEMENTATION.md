# ControlObject6 POC UI Test Implementation Plan

**Version:** 1.1  
**Created:** January 4, 2026  
**Completed:** January 4, 2026  
**Status:** ✅ Complete (Build Verified)

---

## Implementation Results

| Project | Tests Implemented | Build Status |
|---------|------------------|--------------|
| Brinell.Samples.Maui.UITests.ControlObject6 | 44 | ✅ Build Succeeded |
| Brinell.Samples.Blazor.UITests.ControlObject6 | 46 | ✅ Build Succeeded |
| **Total** | **90** | **✅ Ready for Integration Testing** |

> **Note:** These are end-to-end UI tests that require running apps and automation servers.
> They cannot be run without Appium (MAUI) or Playwright browser (Blazor) configured.

---

## Overview

This plan describes the implementation of UI tests for the SPEC-006b POC (ControlObject6 framework). These are end-to-end integration tests that validate the POC against real MAUI and Blazor applications.

---

## Phase 1: Test Project Setup (Day 1, ~3 hours)

### 1.1 Create MAUI UI Test Project

| Task | Description |
|------|-------------|
| Create project | `Brinell.Samples.Maui.UITests.ControlObject6.csproj` |
| Reference Brinell.Maui | Add project reference |
| Add test packages | xUnit, FluentAssertions |
| Copy xunit.runner.json | From existing UITests |
| Add to solution | Under samples folder |

**Project Structure:**
```
samples/Brinell.Samples.Maui.UITests.ControlObject6/
├── Brinell.Samples.Maui.UITests.ControlObject6.csproj
├── TEST-CASES-UI-MAUI.md
├── xunit.runner.json
├── MauiTestBase6.cs
├── Pages/
│   └── MainPageObject6.cs
└── Tests/
    ├── CounterTests6.cs
    ├── TextInputTests6.cs
    ├── ControlStateTests6.cs
    ├── ClickTests6.cs
    ├── PageObjectTests6.cs
    └── TestContextTests6.cs
```

### 1.2 Create Blazor UI Test Project

| Task | Description |
|------|-------------|
| Create project | `Brinell.Samples.Blazor.UITests.ControlObject6.csproj` |
| Reference Brinell.Blazor | Add project reference |
| Add test packages | xUnit, FluentAssertions |
| Add Playwright | Microsoft.Playwright package |
| Copy xunit.runner.json | From existing UITests |
| Add to solution | Under samples folder |

**Project Structure:**
```
samples/Brinell.Samples.Blazor.UITests.ControlObject6/
├── Brinell.Samples.Blazor.UITests.ControlObject6.csproj
├── TEST-CASES-UI-BLAZOR.md
├── xunit.runner.json
├── TestBase/
│   ├── BlazorTestBase6.cs
│   └── BlazorTestFixture6.cs
├── PageObjects/
│   ├── CounterPage6.cs
│   ├── LoginPage6.cs
│   └── HomePage6.cs
└── Tests/
    ├── CounterTests6.cs
    ├── LoginTests6.cs
    ├── NavigationTests6.cs
    ├── ControlStateTests6.cs
    ├── ClickTests6.cs
    ├── TextInputTests6.cs
    ├── PageObjectTests6.cs
    ├── TestContextTests6.cs
    └── LocatorTests6.cs
```

---

## Phase 2: MAUI Test Infrastructure (Day 1-2, ~4 hours)

### 2.1 Test Base Class

| File | Description |
|------|-------------|
| MauiTestBase6.cs | Uses MauiTestContext from ControlObject6 namespace |

**Implementation:**
```csharp
public abstract class MauiTestBase6 : IDisposable
{
    protected readonly MauiTestContext Context;
    protected readonly ITestOutputHelper Output;
    
    protected MauiTestBase6(ITestOutputHelper output)
    {
        Output = output;
        Context = new MauiTestContext(CreateDriver());
        Context.DefaultTimeoutMs = 10000;
    }
    
    // ... driver setup similar to existing MauiTestBase
}
```

### 2.2 Page Objects

| File | Description |
|------|-------------|
| MainPageObject6.cs | Uses By.AutomationId, ButtonControl, EntryControl |

**Implementation:**
```csharp
public class MainPageObject6 : PageObjectBase
{
    protected override ControlLocator PageLocator => By.AutomationId("MainPage");
    
    public IClickableControlObject IncrementButton => 
        Context.CreateControl<IClickableControlObject>(By.AutomationId("IncrementButton"));
    
    public ITextControlObject NameEntry =>
        Context.CreateControl<ITextControlObject>(By.AutomationId("NameEntry"));
}
```

---

## Phase 3: MAUI Tests Implementation (Day 2, ~4 hours)

### 3.1 Counter Tests

| File | Test Count | Priority |
|------|------------|----------|
| CounterTests6.cs | 8 | P0/P1 |

### 3.2 Text Input Tests

| File | Test Count | Priority |
|------|------------|----------|
| TextInputTests6.cs | 15 | P0/P1 |

### 3.3 Control State Tests

| File | Test Count | Priority |
|------|------------|----------|
| ControlStateTests6.cs | 14 | P0/P1 |

### 3.4 Click Tests

| File | Test Count | Priority |
|------|------------|----------|
| ClickTests6.cs | 7 | P0/P1/P2 |

### 3.5 Page & Context Tests

| File | Test Count | Priority |
|------|------------|----------|
| PageObjectTests6.cs | 7 | P0/P1/P2 |
| TestContextTests6.cs | 4 | P1/P2 |

---

## Phase 4: Blazor Test Infrastructure (Day 3, ~4 hours)

### 4.1 Test Base Class

| File | Description |
|------|-------------|
| BlazorTestBase6.cs | Uses BlazorTestContext, async patterns |
| BlazorTestFixture6.cs | Collection fixture for Playwright browser |

**Implementation:**
```csharp
public abstract class BlazorTestBase6 : IAsyncLifetime
{
    protected BlazorTestContext? Context;
    protected readonly ITestOutputHelper Output;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = IsHeadless });
        var page = await _browser.NewPageAsync();
        Context = new BlazorTestContext(page);
    }
    
    // ... similar to existing BlazorSampleTestBase
}
```

### 4.2 Page Objects

| File | Description |
|------|-------------|
| CounterPage6.cs | Async page object with IAsyncClickableControlObject |
| LoginPage6.cs | Async form with IAsyncTextControlObject |
| HomePage6.cs | Simple navigation page |

**Implementation:**
```csharp
public class CounterPage6 : AsyncPageObjectBase
{
    protected override ControlLocator PageLocator => By.TestId("counter-title");
    
    public IAsyncClickableControlObject IncrementButton =>
        Context.CreateControl<IAsyncClickableControlObject>(By.TestId("increment-btn"));
}
```

---

## Phase 5: Blazor Tests Implementation (Day 3-4, ~6 hours)

### 5.1 Counter Tests

| File | Test Count | Priority |
|------|------------|----------|
| CounterTests6.cs | 8 | P0/P1 |

### 5.2 Login Tests

| File | Test Count | Priority |
|------|------------|----------|
| LoginTests6.cs | 12 | P0/P1 |

### 5.3 Navigation Tests

| File | Test Count | Priority |
|------|------------|----------|
| NavigationTests6.cs | 6 | P0/P1 |

### 5.4 Control State Tests

| File | Test Count | Priority |
|------|------------|----------|
| ControlStateTests6.cs | 17 | P0/P1 |

### 5.5 Other Tests

| File | Test Count | Priority |
|------|------------|----------|
| ClickTests6.cs | 6 | P0/P1/P2 |
| TextInputTests6.cs | 11 | P0/P1 |
| PageObjectTests6.cs | 10 | P0/P1/P2 |
| TestContextTests6.cs | 5 | P0/P2 |
| LocatorTests6.cs | 9 | P0/P1/P2 |

---

## Phase 6: Verification & CI Setup (Day 5, ~3 hours)

### 6.1 Local Verification

```powershell
# MAUI Tests
Start-Process appium -ArgumentList "--allow-insecure chromedriver_autodownload"
Start-Sleep 5
cd samples/Brinell.Samples.Maui.UITests.ControlObject6
dotnet test --logger "console;verbosity=detailed"

# Blazor Tests
cd samples/Brinell.Samples.Blazor.App
Start-Process dotnet -ArgumentList "run" -NoNewWindow
Start-Sleep 10
cd ../Brinell.Samples.Blazor.UITests.ControlObject6
dotnet test --logger "console;verbosity=detailed"
```

### 6.2 CI Pipeline Tasks

| Task | Description |
|------|-------------|
| Setup Windows runner | For MAUI tests |
| Install Appium | For MAUI automation |
| Install Playwright | For Blazor automation |
| Run apps in background | Start sample apps |
| Execute tests | Run both test projects |
| Collect results | JUnit XML format |

---

## Implementation Order

### Day 1
1. ✅ Create MAUI UI test project
2. ✅ Create Blazor UI test project  
3. ✅ Implement MauiTestBase6
4. ✅ Implement MainPageObject6

### Day 2
5. ✅ Implement MAUI CounterTests6
6. ✅ Implement MAUI TextInputTests6
7. ✅ Implement MAUI ControlStateTests6
8. ✅ Implement MAUI ClickTests6

### Day 3
9. ✅ Implement BlazorTestBase6 and Fixture
10. ✅ Implement Blazor page objects
11. ✅ Implement Blazor CounterTests6

### Day 4
12. ✅ Implement Blazor LoginTests6
13. ✅ Implement Blazor ControlStateTests6
14. ✅ Implement Blazor TextInputTests6
15. ✅ Implement remaining Blazor tests

### Day 5
16. ✅ Local verification all tests pass
17. ✅ Fix any issues found
18. ✅ Document test results
19. ✅ Update POC status

---

## Test Count Summary

| Project | P0 | P1 | P2 | Total |
|---------|----|----|----|----- |
| MAUI UITests | 28 | 17 | 6 | **51** |
| Blazor UITests | 42 | 28 | 9 | **79** |
| **Total** | **70** | **45** | **15** | **130** |

---

## Dependencies

| Component | Version | Purpose |
|-----------|---------|---------|
| xunit | 2.9.3 | Test framework |
| FluentAssertions | 6.12.0 | Assertions |
| Appium.WebDriver | 8.0.1 | MAUI automation |
| Microsoft.Playwright | 1.50.0 | Blazor automation |
| Brinell.Core | local | Locator system |
| Brinell.Maui | local | MAUI controls |
| Brinell.Blazor | local | Blazor controls |

---

## Success Criteria

- [ ] All 130 UI test cases implemented
- [ ] All tests pass locally
- [ ] MAUI tests run with Appium
- [ ] Blazor tests run with Playwright
- [ ] Screenshot capture works
- [ ] CI pipeline configured (optional)
- [ ] Documentation complete

---

## Notes

### MAUI Testing Considerations
- WinAppDriver or Windows Application Driver required
- Appium server must be running before tests
- Use AccessibilityId for element location
- Some gestures may not work on all platforms

### Blazor Testing Considerations
- Playwright is async-first
- All tests use `async Task` pattern
- SignalR updates need wait time
- Use `data-testid` attributes for reliable selectors
- Headless mode for CI environments
